using Microsoft.EntityFrameworkCore;
using StockTrading.DataAccess.Services.Interfaces;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public async Task<User> GetByGoogleIdAsync(string googleId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.GoogleId == googleId);
    }

    public async Task<User> AddAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User> GetByIdAsync(int id)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }
}