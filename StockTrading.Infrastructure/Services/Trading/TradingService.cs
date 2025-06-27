using Microsoft.Extensions.Logging;
using StockTrading.Application.Common.Interfaces;
using StockTrading.Application.ExternalServices;
using StockTrading.Application.Features.Market.Services;
using StockTrading.Application.Features.Trading.DTOs.Inquiry;
using StockTrading.Application.Features.Trading.DTOs.Orders;
using StockTrading.Application.Features.Trading.DTOs.Portfolio;
using StockTrading.Application.Features.Trading.Repositories;
using StockTrading.Application.Features.Trading.Services;
using StockTrading.Application.Features.Users.DTOs;
using StockTrading.Domain.Entities;
using StockTrading.Domain.Enums;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Common.Helpers;

namespace StockTrading.Infrastructure.Services.Trading;

public class TradingService : ITradingService
{
    private readonly IKisOrderApiClient _kisOrderApiClient;
    private readonly IKisBalanceApiClient _kisBalanceApiClient;
    private readonly IKisOverseasTradingApiClient _kisOverseasTradingApiClient;
    private readonly IDbContextWrapper _dbContextWrapper;
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<TradingService> _logger;

    public TradingService(IKisOrderApiClient kisOrderApiClient, IKisBalanceApiClient kisBalanceApiClient,
        IKisOverseasTradingApiClient kisOverseasTradingApiClient, IDbContextWrapper dbContextWrapper,
        IOrderRepository orderRepository, ILogger<TradingService> logger)
    {
        _kisOrderApiClient = kisOrderApiClient;
        _kisBalanceApiClient = kisBalanceApiClient;
        _kisOverseasTradingApiClient = kisOverseasTradingApiClient;
        _dbContextWrapper = dbContextWrapper;
        _orderRepository = orderRepository;
        _logger = logger;
    }

    #region 국내 주식 주문

    public async Task<OrderResponse> PlaceOrderAsync(OrderRequest order, UserInfo user)
    {
        ArgumentNullException.ThrowIfNull(order);
        KisValidationHelper.ValidateUserForKisApi(user);

        var stockOrder = new StockOrder(
            stockCode: order.PDNO,
            tradeType: order.tr_id,
            orderType: order.ORD_DVSN,
            quantity: order.QuantityAsInt,
            price: order.PriceAsDecimal,
            market: StockTrading.Domain.Enums.Market.Kospi,
            currency: Currency.Krw,
            userId: user.Id
        );

        await using var transaction = await _dbContextWrapper.BeginTransactionAsync();

        var apiResponse = await _kisOrderApiClient.PlaceOrderAsync(order, user);
        await _orderRepository.AddAsync(stockOrder);
        await transaction.CommitAsync();

        _logger.LogInformation("국내 주식 주문 완료: 사용자 {UserId}, 주문번호 {OrderNumber}",
            user.Id, apiResponse?.Output?.OrderNumber ?? "알 수 없음");

        return apiResponse;
    }

    public async Task<BuyableInquiryResponse> GetBuyableInquiryAsync(BuyableInquiryRequest request, UserInfo userInfo)
    {
        KisValidationHelper.ValidateUserForKisApi(userInfo);
        return await _kisBalanceApiClient.GetBuyableInquiryAsync(request, userInfo);
    }

    #endregion

    #region 국내 주식 조회

    public async Task<AccountBalance> GetStockBalanceAsync(UserInfo user)
    {
        KisValidationHelper.ValidateUserForKisApi(user);
        return await _kisBalanceApiClient.GetStockBalanceAsync(user);
    }

    public async Task<OrderExecutionInquiryResponse> GetOrderExecutionsAsync(OrderExecutionInquiryRequest request,
        UserInfo userInfo)
    {
        KisValidationHelper.ValidateUserForKisApi(userInfo);
        ValidateOrderExecutionRequest(request);

        return await _kisOrderApiClient.GetOrderExecutionsAsync(request, userInfo);
    }

    #endregion

    #region 해외 주식 주문

    public async Task<OverseasOrderResponse> PlaceOverseasOrderAsync(OverseasOrderRequest order, UserInfo user)
    {
        ArgumentNullException.ThrowIfNull(order);
        KisValidationHelper.ValidateUserForKisApi(user);

        order.CANO = user.AccountNumber;

        // 예약주문인지 확인
        if (order is ScheduledOverseasOrderRequest scheduledOrder)
            return await PlaceScheduledOrderAsync(scheduledOrder, user);
        return await PlaceImmediateOrderAsync(order, user);
    }

    #endregion

    #region 해외 주식 조회

    public async Task<List<OverseasOrderExecution>> GetOverseasOrderExecutionsAsync(string startDate, string endDate,
        UserInfo user)
    {
        ArgumentNullException.ThrowIfNull(startDate);
        ArgumentNullException.ThrowIfNull(endDate);
        KisValidationHelper.ValidateUserForKisApi(user);

        var executions =
            await _kisOverseasTradingApiClient.GetOverseasOrderExecutionsAsync(startDate, endDate, user);

        return executions;
    }

    public async Task<OverseasAccountBalance> GetOverseasStockBalanceAsync(UserInfo user)
    {
        KisValidationHelper.ValidateUserForKisApi(user);
        return await _kisBalanceApiClient.GetOverseasStockBalanceAsync(user);
    }

    #endregion

    #region Private Helper Methods

    private void ValidateOrderExecutionRequest(OrderExecutionInquiryRequest request)
    {
        if (!DateTime.TryParseExact(request.StartDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None,
                out var start))
            throw new ArgumentException("시작일자 형식이 올바르지 않습니다.");

        if (!DateTime.TryParseExact(request.EndDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None,
                out var end))
            throw new ArgumentException("종료일자 형식이 올바르지 않습니다.");

        if (start > end)
            throw new ArgumentException("시작일자는 종료일자보다 이전이어야 합니다.");

        if (end > DateTime.Now.Date)
            throw new ArgumentException("종료일자는 현재 날짜보다 이후일 수 없습니다.");
    }

    private Currency GetCurrencyEnum(StockTrading.Domain.Enums.Market market)
    {
        return market switch
        {
            StockTrading.Domain.Enums.Market.Nasdaq => Currency.Usd,
            StockTrading.Domain.Enums.Market.Nyse => Currency.Usd,
            StockTrading.Domain.Enums.Market.Tokyo => Currency.Jpy,
            StockTrading.Domain.Enums.Market.London => Currency.Gbp,
            StockTrading.Domain.Enums.Market.Hongkong => Currency.Hkd,
            _ => Currency.Usd
        };
    }

    private async Task<OverseasOrderResponse> PlaceImmediateOrderAsync(OverseasOrderRequest order, UserInfo user)
    {
        var stockOrder = new StockOrder(
            stockCode: order.PDNO,
            tradeType: order.tr_id,
            orderType: order.ORD_DVSN,
            quantity: order.QuantityAsInt,
            price: order.PriceAsDecimal,
            market: order.Market,
            currency: GetCurrencyEnum(order.Market),
            userId: user.Id
        );

        await using var transaction = await _dbContextWrapper.BeginTransactionAsync();

        var apiResponse = await _kisOverseasTradingApiClient.PlaceOverseasOrderAsync(order, user);
        await _orderRepository.AddAsync(stockOrder);
        await transaction.CommitAsync();

        _logger.LogInformation("해외 주식 즉시주문 완료: 사용자 {UserId}, 주문번호 {OrderNumber}",
            user.Id, apiResponse.OrderNumber);

        return apiResponse;
    }

    private async Task<OverseasOrderResponse> PlaceScheduledOrderAsync(ScheduledOverseasOrderRequest order,
        UserInfo user)
    {
        var stockOrder = new StockOrder(
            stockCode: order.PDNO,
            tradeType: GetScheduledOrderTradeId(order),
            orderType: order.ORD_DVSN,
            quantity: order.QuantityAsInt,
            price: order.PriceAsDecimal,
            market: order.Market,
            currency: GetCurrencyEnum(order.Market),
            userId: user.Id,
            scheduledExecutionTime: order.ScheduledExecutionTime
        );

        await using var transaction = await _dbContextWrapper.BeginTransactionAsync();

        var apiResponse = await _kisOverseasTradingApiClient.PlaceScheduledOverseasOrderAsync(order, user);
        
        // 예약주문번호 설정
        stockOrder.SetReservedOrderNumber(apiResponse.OrderNumber);
        await _orderRepository.AddAsync(stockOrder);
        await transaction.CommitAsync();

        _logger.LogInformation("해외 주식 예약주문 접수 완료: 사용자 {UserId}, 예약주문번호 {ReservedOrderNumber}, 예약시간 {ScheduledTime}",
            user.Id, apiResponse.OrderNumber, order.ScheduledExecutionTime);

        return apiResponse;
    }
    
    private string GetScheduledOrderTradeId(ScheduledOverseasOrderRequest request)
    {
        var isUsOrder = IsUsOrder(request.OVRS_EXCG_CD);
        var isBuyOrder = request.tr_id.Contains("1002") || request.tr_id.Contains("3014");

        if (isUsOrder)
            return isBuyOrder ? "VTTT3014U" : "VTTT3016U"; // 미국 예약 매수/매도
        return "VTTS3013U"; // 아시아 예약주문 (통합)
    }

    private bool IsUsOrder(string exchangeCode)
    {
        return exchangeCode == "NASD" || exchangeCode == "NYSE" || exchangeCode == "AMEX";
    }

    #endregion
}