
using stock_trading_backend.DTOs;
using StockTrading.DataAccess.DTOs;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.DataAccess.Services.Interfaces;

public interface IKoreaInvestmentService
{
    Task<TokenResponse> GetTokenAsync(string appKey, string appSecret);
    Task<StockBalance> GetStockBalanceAsync(User user);
}