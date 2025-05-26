using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockTrading.Application.Repositories;
using StockTrading.Infrastructure.Persistence.Contexts;
using StockTrading.Infrastructure.Repositories;

namespace StockTrading.Infrastructure.Persistence.Repositories;

public class UserKisInfoRepository: IUserKisInfoRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<KisTokenRepository> _logger;

    public UserKisInfoRepository(ApplicationDbContext context, ILogger<KisTokenRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task UpdateUserKisInfo(int userId, string appKey, string appSecret, string accountNumber)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null)
            {
                _logger.LogError($"사용자를 찾을 수 없습니다 - UserId: {userId}");
                throw new KeyNotFoundException($"UserId {userId}에 해당하는 사용자를 찾을 수 없습니다.");
            }

            user.KisAppKey = appKey;
            user.KisAppSecret = appSecret;
            user.AccountNumber = accountNumber;

            _context.Users.Update(user);
            var result = await _context.SaveChangesAsync();

            _logger.LogInformation($"KIS 정보 업데이트 완료 - 변경사항: {result}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"KIS 정보 업데이트 중 에러 발생: {ex.Message}");
            throw;
        }
    }
    
    public async Task SaveWebSocketTokenAsync(int userId, string approvalKey)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null)
            {
                _logger.LogError($"사용자를 찾을 수 없습니다 - UserId: {userId}");
                throw new KeyNotFoundException($"UserId {userId}에 해당하는 사용자를 찾을 수 없습니다.");
            }

            user.WebSocketToken = approvalKey;

            _context.Users.Update(user);
            var result = await _context.SaveChangesAsync();

            _logger.LogInformation($"KIS 정보 업데이트 완료 - 변경사항: {result}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"KIS 정보 업데이트 중 에러 발생: {ex.Message}");
            throw;
        }
    }
}