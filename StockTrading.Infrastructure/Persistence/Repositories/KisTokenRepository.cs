using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.Common;
using StockTrading.Application.Repositories;
using StockTrading.Domain.Entities;
using StockTrading.Infrastructure.Persistence.Contexts;

namespace StockTrading.Infrastructure.Repositories;

public class KisTokenRepository: IKisTokenRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<KisTokenRepository> _logger;

    public KisTokenRepository(ApplicationDbContext context, ILogger<KisTokenRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SaveKisToken(int userId, TokenResponse tokenResponse)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogError($"사용자를 찾을 수 없습니다 - UserId: {userId}");
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
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"토큰 저장 중 에러 발생: {ex.Message}");
            throw;
        }
    }
}