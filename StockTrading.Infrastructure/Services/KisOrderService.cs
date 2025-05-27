using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.Trading.Orders;
using StockTrading.Application.DTOs.Users;
using StockTrading.Application.Repositories;
using StockTrading.Application.Services;
using StockTrading.Domain.Entities;
using StockTrading.Infrastructure.Services.Helpers;

namespace StockTrading.Infrastructure.Services;

public class KisOrderService : IKisOrderService
{
    private readonly IKisApiClient _kisApiClient;
    private readonly IDbContextWrapper _dbContextWrapper;
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<KisOrderService> _logger;

    public KisOrderService(IKisApiClient kisApiClient, IDbContextWrapper dbContextWrapper, IOrderRepository orderRepository, ILogger<KisOrderService> logger)
    {
        _kisApiClient = kisApiClient;
        _dbContextWrapper = dbContextWrapper;
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<OrderResponse> PlaceOrderAsync(OrderRequest order, UserInfo user)
    {
        ArgumentNullException.ThrowIfNull(order);
        KisValidationHelper.ValidateUserForKisApi(user);

        _logger.LogInformation("주문 시작: 사용자 {UserId}, 종목 {StockCode}, 수량 {Quantity}", 
            user.Id, order.PDNO, order.ORD_QTY);

        var stockOrder = new StockOrder(
            stockCode: order.PDNO,
            tradeType: order.tr_id,
            orderType: order.ORD_DVSN,
            quantity: order.ORD_QTY,
            price: order.ORD_UNPR,
            user: user.ToEntity()
        );

        await using var transaction = await _dbContextWrapper.BeginTransactionAsync();
    
        var apiResponse = await _kisApiClient.PlaceOrderAsync(order, user);
        await _orderRepository.AddAsync(stockOrder);
        await transaction.CommitAsync();
    
        _logger.LogInformation("주문 완료: 사용자 {UserId}, 주문번호 {OrderNumber}", 
            user.Id, apiResponse?.Info?.ODNO ?? "알 수 없음");

        return apiResponse;
    }
}