using StockTrading.Application.DTOs.Common;

namespace StockTrading.Application.Repositories;

public interface IUserTokenRepository
{
    Task SaveKisTokenAsync(int userId, TokenResponse tokenResponse);
    Task UpdateUserKisInfoAsync(int userId, string appKey, string appSecret, string accountNumber);
    Task SaveWebSocketTokenAsync(int userId, string approvalKey);
    Task<bool> IsKisTokenValidAsync(int userId);
}