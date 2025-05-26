using StockTrading.Domain.Entities;

namespace StockTrading.Application.Repositories;

public interface IUserRepository : IBaseRepository<User, int>
{
    Task<User?> GetByGoogleIdAsync(string googleId);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByEmailWithTokenAsync(string email);
}