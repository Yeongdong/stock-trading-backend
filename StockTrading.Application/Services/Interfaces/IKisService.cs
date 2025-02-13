using stock_trading_backend.DTOs;
using StockTrading.DataAccess.DTOs;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.DataAccess.Services.Interfaces;

public interface IKisService
{
    Task<StockBalance> GetStockBalanceAsync(UserDto user);
    Task<TokenResponse> UpdateUserKisInfoAndTokenAsync(int userId, string appKey, string appSecret,
        string accountNumber);
}