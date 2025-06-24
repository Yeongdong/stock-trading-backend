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
    private readonly IDbContextWrapper _dbContextWrapper;
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<TradingService> _logger;

    public TradingService(IKisOrderApiClient kisOrderApiClient, IKisBalanceApiClient kisBalanceApiClient,
        IDbContextWrapper dbContextWrapper, IOrderRepository orderRepository, ILogger<TradingService> logger)
    {
        _kisOrderApiClient = kisOrderApiClient;
        _kisBalanceApiClient = kisBalanceApiClient;
        _dbContextWrapper = dbContextWrapper;
        _orderRepository = orderRepository;
        _logger = logger;
    }

    #region 주문 관리

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

        _logger.LogInformation("주문 완료: 사용자 {UserId}, 주문번호 {OrderNumber}",
            user.Id, apiResponse?.Output?.OrderNumber ?? "알 수 없음");

        return apiResponse;
    }

    public async Task<BuyableInquiryResponse> GetBuyableInquiryAsync(BuyableInquiryRequest request, UserInfo userInfo)
    {
        KisValidationHelper.ValidateUserForKisApi(userInfo);
        return await _kisBalanceApiClient.GetBuyableInquiryAsync(request, userInfo);
    }

    #endregion

    #region 조회

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

    #endregion
}