using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.DataAccess.Services.Interfaces;

public interface IUserRepository
{
    Task<User> GetByGoogleIdAsync(string googleId);
    Task<User> AddAsync(User user);
    Task<User> GetByIdAsync(int id);
    Task<User> GetByEmailAsync(string email);
}