using StockTrading.Application.DTOs.Common;
using StockTrading.Application.DTOs.Orders;
using StockTrading.Application.DTOs.Stocks;

namespace StockTrading.Application.Services;

public interface IKisApiClient
{
    Task<StockOrderResponse> PlaceOrderAsync(StockOrderRequest request, UserDto user);
    Task<StockBalance> GetStockBalanceAsync(UserDto user);
    
}