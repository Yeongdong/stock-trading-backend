using StockTrading.Application.DTOs.Common;
using StockTrading.Application.DTOs.Orders;

namespace StockTrading.Application.Services;

public interface IKisOrderService
{
    Task<StockOrderResponse> PlaceOrderAsync(StockOrderRequest stockOrderRequest, UserDto userDto);
}