using Microsoft.Extensions.Logging;
using StockTrading.Application.Repositories;
using StockTrading.Infrastructure.Persistence.Contexts;

namespace StockTrading.Infrastructure.Persistence.Repositories;

public class UserKisInfoRepository : IUserKisInfoRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TokenRepository> _logger;

    public UserKisInfoRepository(ApplicationDbContext context, ILogger<TokenRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task UpdateKisCredentialsAsync(int userId, string appKey, string appSecret, string accountNumber)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            throw new KeyNotFoundException($"UserId {userId}에 해당하는 사용자를 찾을 수 없습니다.");

        user.KisAppKey = appKey;
        user.KisAppSecret = appSecret;
        user.AccountNumber = accountNumber;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task SaveWebSocketTokenAsync(int userId, string approvalKey)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"UserId {userId}에 해당하는 사용자를 찾을 수 없습니다.");

        user.WebSocketToken = approvalKey;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
}