using StockTrading.Application.Common.Interfaces;
using StockTrading.Domain.Entities.Auth;

namespace StockTrading.Application.Features.Auth.Repositories;

public interface IRefreshTokenRepository : IBaseRepository<RefreshToken, int>
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task RevokeAllByUserIdAsync(int userId);
}