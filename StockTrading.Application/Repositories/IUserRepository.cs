using StockTrading.Domain.Entities;

namespace StockTrading.Application.Repositories;

public interface IUserRepository
{
    Task<User> GetByGoogleIdAsync(string googleId);
    Task<User> AddAsync(User user);
    Task<User> GetByEmailAsync(string email);
}