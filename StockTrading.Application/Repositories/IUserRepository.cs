using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.DataAccess.Repositories;

public interface IUserRepository
{
    Task<User> GetByGoogleIdAsync(string googleId);
    Task<User> AddAsync(User user);
    Task<User> GetByEmailAsync(string email);
}