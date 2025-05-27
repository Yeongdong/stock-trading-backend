using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.Common;
using StockTrading.Application.Repositories;
using StockTrading.Domain.Entities;
using StockTrading.Infrastructure.Persistence.Contexts;

namespace StockTrading.Infrastructure.Persistence.Repositories;

public class KisTokenRepository : IKisTokenRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<KisTokenRepository> _logger;

    public KisTokenRepository(ApplicationDbContext context, ILogger<KisTokenRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SaveKisTokenAsync(int userId, TokenResponse tokenResponse)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"UserId {userId}에 해당하는 사용자를 찾을 수 없습니다.");
        }

        var existingToken = await _context.KisTokens
            .FirstOrDefaultAsync(t => t.UserId == userId);

        var expiresIn = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

        if (existingToken != null)
        {
            _logger.LogInformation("기존 토큰 업데이트");
            existingToken.AccessToken = tokenResponse.AccessToken;
            existingToken.ExpiresIn = expiresIn;
            existingToken.TokenType = tokenResponse.TokenType;
            _context.KisTokens.Update(existingToken);
        }
        else
        {
            _logger.LogInformation("새로운 토큰 생성");
            var newToken = new KisToken
            {
                UserId = userId,
                AccessToken = tokenResponse.AccessToken,
                ExpiresIn = expiresIn,
                TokenType = tokenResponse.TokenType
            };

            await _context.KisTokens.AddAsync(newToken);
            _logger.LogInformation("KIS 토큰 저장 완료: {UserId}", userId);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> IsKisTokenValidAsync(int userId)
    {
        var token = await _context.KisTokens
            .FirstOrDefaultAsync(t => t.UserId == userId);

        return token != null && token.ExpiresIn > DateTime.UtcNow;
    }
}