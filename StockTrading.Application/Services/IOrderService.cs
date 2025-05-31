using StockTrading.Application.DTOs.Trading.Orders;
using StockTrading.Application.DTOs.Users;

namespace StockTrading.Application.Services;

public interface IOrderService
{
    Task<OrderResponse> PlaceOrderAsync(OrderRequest orderRequest, UserInfo userInfo);
}