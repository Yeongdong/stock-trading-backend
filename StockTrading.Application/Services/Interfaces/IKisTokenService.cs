using StockTrading.DataAccess.DTOs;

namespace StockTrading.DataAccess.Services.Interfaces;

public interface IKisTokenService
{
    Task<TokenResponse> GetKisTokenAsync(int userId, string appKey, string appSecret, string accountNumber);
    Task<string> GetWebSocketTokenAsync(int userId, string appKey, string appSecret);
}