using Microsoft.EntityFrameworkCore;
using StockTrading.DataAccess.DTOs;
using StockTrading.DataAccess.Repositories;
using StockTrading.DataAccess.Services.Interfaces;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User> GetByGoogleIdAsync(string googleId)
    {
        var user = await _context.Users
            .Include(u => u.KisToken)
            .FirstOrDefaultAsync(u => u.GoogleId == googleId);
        
        if (user == null)
            throw new ArgumentNullException();

        return user;
    }

    public async Task<User> AddAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User> GetByEmailAsync(string email)
    {
        var user = await _context.Users
            .Include(u => u.KisToken)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
            throw new ArgumentNullException();

        return user;
    }
}