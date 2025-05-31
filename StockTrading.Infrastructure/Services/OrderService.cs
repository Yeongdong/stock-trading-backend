using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.Trading.Orders;
using StockTrading.Application.DTOs.Users;
using StockTrading.Application.Repositories;
using StockTrading.Application.Services;
using StockTrading.Domain.Entities;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Helpers;

namespace StockTrading.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly IKisApiClient _kisApiClient;
    private readonly IDbContextWrapper _dbContextWrapper;
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IKisApiClient kisApiClient, IDbContextWrapper dbContextWrapper,
        IOrderRepository orderRepository, ILogger<OrderService> logger)
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

        var stockOrder = new StockOrder(
            stockCode: order.PDNO,
            tradeType: order.tr_id,
            orderType: order.ORD_DVSN,
            quantity: order.QuantityAsInt,
            price: order.PriceAsDecimal,
            userId: user.Id
        );

        await using var transaction = await _dbContextWrapper.BeginTransactionAsync();

        var apiResponse = await _kisApiClient.PlaceOrderAsync(order, user);
        await _orderRepository.AddAsync(stockOrder);
        await transaction.CommitAsync();

        return apiResponse;
    }
}