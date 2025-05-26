using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockTrading.Application.Repositories;
using StockTrading.Domain.Entities;
using StockTrading.Infrastructure.Persistence.Contexts;

namespace StockTrading.Infrastructure.Persistence.Repositories;

public class UserRepository : BaseRepository<User, int>, IUserRepository
{
    public UserRepository(ApplicationDbContext context, ILogger<UserRepository> logger) 
        : base(context, logger)
    {
    }

    public async Task<User?> GetByGoogleIdAsync(string googleId)
    {
        return await DbSet
            .Include(u => u.KisToken)
            .FirstOrDefaultAsync(u => u.GoogleId == googleId);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await DbSet.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByEmailWithTokenAsync(string email)
    {
        return await DbSet
            .Include(u => u.KisToken)
            .FirstOrDefaultAsync(u => u.Email == email);
    }
}