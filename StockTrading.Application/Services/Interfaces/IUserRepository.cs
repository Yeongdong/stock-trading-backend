using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.DataAccess.Services.Interfaces;

public interface IUserRepository
{
    Task<User> GetByEmailAsync(string email);
    Task<User> GetByRefreshTokenAsync(string refreshToken);
    Task UpdateAsync(User user);
    Task CreateAsync(User user);
}