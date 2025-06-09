using StockTrading.Application.Features.Trading.DTOs.Orders;
using StockTrading.Application.Features.Users.DTOs;

namespace StockTrading.Application.Features.Trading.Services;

public interface IOrderService
{
    Task<OrderResponse> PlaceOrderAsync(OrderRequest orderRequest, UserInfo userInfo);
}