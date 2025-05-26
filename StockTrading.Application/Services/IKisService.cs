using StockTrading.Application.DTOs.Common;
using StockTrading.Application.DTOs.Orders;
using StockTrading.Application.DTOs.Stocks;

namespace StockTrading.Application.Services;

public interface IKisService
{
    Task<StockBalance> GetStockBalanceAsync(UserDto user);
    Task<TokenResponse> UpdateUserKisInfoAndTokensAsync(int userId, string appKey, string appSecret,
        string accountNumber);
    Task<StockOrderResponse> PlaceOrderAsync(StockOrderRequest request, UserDto user);
}