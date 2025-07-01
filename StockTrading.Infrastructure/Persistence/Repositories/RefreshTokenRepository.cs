using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockTrading.Application.Features.Auth.Repositories;
using StockTrading.Domain.Entities.Auth;
using StockTrading.Infrastructure.Persistence.Contexts;

namespace StockTrading.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository : BaseRepository<RefreshToken, int>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ApplicationDbContext context, ILogger<RefreshTokenRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await DbSet
            .Include(rt => rt.User)
            .ThenInclude(u => u.KisToken)
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task RevokeAllByUserIdAsync(int userId)
    {
        var tokens = await DbSet
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
        }

        await Context.SaveChangesAsync();
    }
}