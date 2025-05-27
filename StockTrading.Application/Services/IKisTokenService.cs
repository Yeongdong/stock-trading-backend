using StockTrading.Application.DTOs.Auth;

namespace StockTrading.Application.Services;

public interface IKisTokenService
{
    Task<TokenInfo> GetKisTokenAsync(int userId, string appKey, string appSecret, string accountNumber);
    Task<string> GetWebSocketTokenAsync(int userId, string appKey, string appSecret);
    Task<TokenInfo> UpdateUserKisInfoAndTokensAsync(int userId, string appKey, string appSecret,
        string accountNumber);
}