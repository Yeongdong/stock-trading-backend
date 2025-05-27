using StockTrading.Application.DTOs.Common;

namespace StockTrading.Application.Repositories;

public interface IKisTokenRepository
{
    public Task SaveKisTokenAsync(int userId, TokenResponse tokenResponse);
    Task<bool> IsKisTokenValidAsync(int userId);
}