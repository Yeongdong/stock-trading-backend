using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.Trading.Portfolio;
using StockTrading.Application.DTOs.Users;
using StockTrading.Application.Services;
using StockTrading.Infrastructure.Services.Helpers;

namespace StockTrading.Infrastructure.Services;

public class KisBalanceService : IKisBalanceService
{
    private readonly IKisApiClient _kisApiClient;
    private readonly ILogger<KisBalanceService> _logger;

    public KisBalanceService(IKisApiClient kisApiClient, ILogger<KisBalanceService> logger)
    {
        _kisApiClient = kisApiClient;
        _logger = logger;
    }
    
    public async Task<AccountBalance> GetStockBalanceAsync(UserInfo user)
    {
        KisValidationHelper.ValidateUserForKisApi(user);

        _logger.LogInformation("잔고 조회 시작: 사용자 {UserId}", user.Id);

        var balance = await _kisApiClient.GetStockBalanceAsync(user);

        _logger.LogInformation("잔고 조회 완료: 사용자 {UserId}, 보유종목 {PositionCount}개",
            user.Id, balance.Positions?.Count ?? 0);

        return balance;
    }
}