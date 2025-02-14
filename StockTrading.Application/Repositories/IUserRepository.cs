using StockTrading.DataAccess.DTOs;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.DataAccess.Repositories;

public interface IUserRepository
{
    Task<User> GetByGoogleIdAsync(string googleId);
    Task<User> AddAsync(User user);
    Task<User> GetByIdAsync(int id);
    Task<UserDto> GetByEmailAsync(string email);
}