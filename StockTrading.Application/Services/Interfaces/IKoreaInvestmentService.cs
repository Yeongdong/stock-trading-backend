using stock_trading_backend;

namespace StockTrading.DataAccess.Services.Interfaces;

public interface IKoreaInvestmentService
{
    Task<TokenResponse> GetTokenAsync(string appKey, string appSecret);
}