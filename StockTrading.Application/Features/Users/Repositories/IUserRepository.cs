using StockTrading.Application.Common.Interfaces;
using StockTrading.Domain.Entities;

namespace StockTrading.Application.Features.Users.Repositories;

public interface IUserRepository : IBaseRepository<User, int>
{
    Task<User?> GetByGoogleIdAsync(string googleId);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByEmailWithTokenAsync(string email);
}