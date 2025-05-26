using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.Common;
using StockTrading.Application.Repositories;
using StockTrading.Domain.Entities;
using StockTrading.Infrastructure.Persistence.Contexts;

namespace StockTrading.Infrastructure.Persistence.Repositories;

public class UserTokenRepository : IUserTokenRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserTokenRepository> _logger;

    public UserTokenRepository(ApplicationDbContext context, ILogger<UserTokenRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SaveKisTokenAsync(int userId, TokenResponse tokenResponse)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"사용자를 찾을 수 없습니다. ID: {userId}");

        var existingToken = await _context.KisTokens
            .FirstOrDefaultAsync(t => t.UserId == userId);

        var expiresIn = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

        if (existingToken != null)
        {
            existingToken.AccessToken = tokenResponse.AccessToken;
            existingToken.ExpiresIn = expiresIn;
            existingToken.TokenType = tokenResponse.TokenType;
            _context.KisTokens.Update(existingToken);
        }
        else
        {
            var newToken = new KisToken
            {
                UserId = userId,
                AccessToken = tokenResponse.AccessToken,
                ExpiresIn = expiresIn,
                TokenType = tokenResponse.TokenType
            };
            await _context.KisTokens.AddAsync(newToken);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("KIS 토큰 저장 완료: {UserId}", userId);
    }

    public async Task UpdateUserKisInfoAsync(int userId, string appKey, string appSecret, string accountNumber)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"사용자를 찾을 수 없습니다. ID: {userId}");

        user.KisAppKey = appKey;
        user.KisAppSecret = appSecret;
        user.AccountNumber = accountNumber;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("사용자 KIS 정보 업데이트 완료: {UserId}", userId);
    }

    public async Task SaveWebSocketTokenAsync(int userId, string approvalKey)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"사용자를 찾을 수 없습니다. ID: {userId}");

        user.WebSocketToken = approvalKey;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("WebSocket 토큰 저장 완료: {UserId}", userId);
    }

    public async Task<bool> IsKisTokenValidAsync(int userId)
    {
        var token = await _context.KisTokens
            .FirstOrDefaultAsync(t => t.UserId == userId);

        return token != null && token.ExpiresIn > DateTime.UtcNow;
    }
}