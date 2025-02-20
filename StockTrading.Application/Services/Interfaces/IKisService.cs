using StockTrading.DataAccess.DTOs;
using StockTrading.DataAccess.DTOs.OrderDTOs;

namespace StockTrading.DataAccess.Services.Interfaces;

public interface IKisService
{
    Task<StockBalance> GetStockBalanceAsync(UserDto user);
    Task<TokenResponse> UpdateUserKisInfoAndTokensAsync(int userId, string appKey, string appSecret,
        string accountNumber);
    Task<StockOrderResponse> PlaceOrderAsync(StockOrderRequest request, UserDto user);
}