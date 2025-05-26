using StockTrading.Application.DTOs.Common;

namespace StockTrading.Application.Services;

public interface IKisTokenService
{
    Task<TokenResponse> GetKisTokenAsync(int userId, string appKey, string appSecret, string accountNumber);
    Task<string> GetWebSocketTokenAsync(int userId, string appKey, string appSecret);
}