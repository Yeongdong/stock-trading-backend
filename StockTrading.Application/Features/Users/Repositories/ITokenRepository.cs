using StockTrading.Application.Features.Auth.DTOs;

namespace StockTrading.Application.Features.Users.Repositories;

public interface ITokenRepository
{
    public Task SaveKisTokenAsync(int userId, TokenInfo tokenInfo);
    Task<bool> IsKisTokenValidAsync(int userId);
}