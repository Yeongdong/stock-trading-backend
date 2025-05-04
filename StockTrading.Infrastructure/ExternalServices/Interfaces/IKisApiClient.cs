using StockTrading.DataAccess.DTOs;
using StockTrading.DataAccess.DTOs.OrderDTOs;

namespace StockTrading.Infrastructure.ExternalServices.Interfaces;

public interface IKisApiClient
{
    Task<StockOrderResponse> PlaceOrderAsync(StockOrderRequest request, UserDto user);
    Task<StockBalance> GetStockBalanceAsync(UserDto user);
    
}