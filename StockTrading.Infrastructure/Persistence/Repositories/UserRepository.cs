using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockTrading.Application.Features.Users.Repositories;
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

    public async Task<User?> GetByEmailWithTokenAsync(string? email)
    {
        return await DbSet
            .Include(u => u.KisToken)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task UpdatePreviousDayTotalAmountAsync(int userId, decimal amount)
    {
        var user = await DbSet.FindAsync(userId);

        if (user == null)
            throw new KeyNotFoundException($"UserId {userId}에 해당하는 사용자를 찾을 수 없습니다.");

        user.PreviousDayTotalAmount = amount;

        DbSet.Update(user);
        await Context.SaveChangesAsync();
    }
}