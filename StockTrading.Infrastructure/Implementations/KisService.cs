using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockTrading.DataAccess.Services.Interfaces;
using StockTrading.Infrastructure.Repositories;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.Infrastructure.Implementations;

public class KisService : IKisService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<KisService> _logger;

    public KisService(ApplicationDbContext context, ILogger<KisService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SaveTokenAsync(int userId, string accessToken, DateTime expiresIn, string tokenType)
    {
        try
        {
            _logger.LogInformation($"토큰 저장 시도 - UserId: {userId}");
            _logger.LogInformation($"토큰 저장 시도 - AccessToken: {accessToken}");
            
            var existingToken = await _context.KisTokens
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (existingToken != null)
            {
                _logger.LogInformation("기존 토큰 업데이트");
                existingToken.AccessToken = accessToken;
                existingToken.ExpiresIn = expiresIn;
                existingToken.TokenType = tokenType;
                _context.KisTokens.Update(existingToken);
            }
            else
            {
                _logger.LogInformation("새로운 토큰 생성");
                var newToken = new KisToken
                {
                    UserId = userId,
                    AccessToken = accessToken,
                    ExpiresIn = expiresIn,
                    TokenType = tokenType
                };
                await _context.KisTokens.AddAsync(newToken);
            }

            var result = await _context.SaveChangesAsync();
            _logger.LogInformation($"저장된 변경사항: {result}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"토큰 저장 중 에러 발생: {ex.Message}");
            _logger.LogError($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }
}