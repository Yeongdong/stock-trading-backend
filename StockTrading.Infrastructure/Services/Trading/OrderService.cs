using StockTrading.Application.Common.Interfaces;
using StockTrading.Application.ExternalServices;
using StockTrading.Application.Features.Trading.DTOs.Orders;
using StockTrading.Application.Features.Trading.Repositories;
using StockTrading.Application.Features.Trading.Services;
using StockTrading.Application.Features.Users.DTOs;
using StockTrading.Domain.Entities;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Common.Helpers;

namespace StockTrading.Infrastructure.Services.Trading;

public class OrderService : IOrderService
{
    private readonly IKisOrderApiClient _kisOrderApiClient;
    private readonly IDbContextWrapper _dbContextWrapper;
    private readonly IOrderRepository _orderRepository;

    public OrderService(IKisOrderApiClient kisOrderApiClient, IDbContextWrapper dbContextWrapper, IOrderRepository orderRepository)
    {
        _kisOrderApiClient = kisOrderApiClient;
        _dbContextWrapper = dbContextWrapper;
        _orderRepository = orderRepository;
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

        var apiResponse = await _kisOrderApiClient.PlaceOrderAsync(order, user);
        await _orderRepository.AddAsync(stockOrder);
        await transaction.CommitAsync();

        return apiResponse;
    }
}