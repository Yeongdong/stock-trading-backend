using StockTrading.Application.DTOs.Auth;

namespace StockTrading.Application.Repositories;

public interface ITokenRepository
{
    public Task SaveKisTokenAsync(int userId, TokenInfo tokenInfo);
    Task<bool> IsKisTokenValidAsync(int userId);
}