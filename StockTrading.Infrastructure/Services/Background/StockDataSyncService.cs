using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrading.Application.Features.Market.Services;
using StockTrading.Application.Features.Trading.Services;
using StockTrading.Application.Features.Users.DTOs;
using StockTrading.Application.Features.Users.Repositories;
using StockTrading.Application.Features.Users.Services;
using StockTrading.Domain.Settings.Infrastructure;

namespace StockTrading.Infrastructure.Services.Background;

/// <summary>
/// 데이터 자동 동기화 백그라운드 서비스 (매일 오전 6시 실행)
/// </summary>
public class StockDataSyncService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StockDataSyncService> _logger;
    private readonly StockDataSyncSettings _settings;

    public StockDataSyncService(IServiceProvider serviceProvider, ILogger<StockDataSyncService> logger,
        IOptions<StockDataSyncSettings> settings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = settings.Value;
    }

    /// <summary>
    /// 종목 데이터 자동 동기화 백그라운드 서비스 (매일 오전 6시 실행)
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("주식 데이터 동기화 서비스 시작 - 매일 {SyncHour}시 실행", _settings.SyncHour);

        while (!stoppingToken.IsCancellationRequested)
        {
            var nextSyncTime = GetNextSyncTime();
            var delay = nextSyncTime - DateTime.Now;

            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;

            await PerformSyncAsync();
        }
    }

    private async Task PerformSyncAsync()
    {
        _logger.LogInformation("주식 데이터 동기화 및 메모리 캐시 갱신 시작");

        using var scope = _serviceProvider.CreateScope();
        var stockService = scope.ServiceProvider.GetRequiredService<IStockService>();

        await stockService.SyncDomesticStockDataAsync();
        _logger.LogInformation("주식 데이터 동기화 및 메모리 캐시 갱신 완료");
    }

    private DateTime GetNextSyncTime()
    {
        var today = DateTime.Today;
        var todaySync = today.Add(new TimeSpan(2, 0, 0));
        return DateTime.Now > todaySync ? todaySync.AddDays(1) : todaySync;
    }

    // private bool IsNonBusinessDay(DateTime date)
    // {
    //     return !_settings.RunOnWeekends &&
    //            date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    // }
    //
    // private async Task UpdateAllUsersPreviousDayBalanceAsync(IServiceProvider serviceProvider)
    // {
    //     _logger.LogInformation("전일 총평가금액 업데이트 시작");
    //
    //     var userRepository = serviceProvider.GetRequiredService<IUserRepository>();
    //     var tradingService = serviceProvider.GetRequiredService<ITradingService>();
    //
    //     // KIS API 연동 정보가 있는 사용자들만 조회
    //     var users = await userRepository.GetAllAsync();
    //     var activeUsers = users.Where(u =>
    //             !string.IsNullOrEmpty(u.KisAppKey) &&
    //             !string.IsNullOrEmpty(u.KisAppSecret) &&
    //             !string.IsNullOrEmpty(u.AccountNumber))
    //         .ToList();
    //
    //     var successCount = 0;
    //     var errorCount = 0;
    //
    //     foreach (var user in activeUsers)
    //     {
    //         var userInfo = new UserInfo
    //         {
    //             Id = user.Id,
    //             Email = user.Email,
    //             AccountNumber = user.AccountNumber,
    //             KisAppKey = user.KisAppKey,
    //             KisAppSecret = user.KisAppSecret
    //         };
    //
    //         // 현재 잔고 조회
    //         var balance = await tradingService.GetStockBalanceAsync(userInfo);
    //         var currentTotalAmount = decimal.Parse(balance.Summary.TotalEvaluation);
    //
    //         // 전일 총평가금액으로 업데이트
    //         await userRepository.UpdatePreviousDayTotalAmountAsync(user.Id, currentTotalAmount);
    //
    //         successCount++;
    //         _logger.LogDebug("사용자 {UserId} 전일 총평가금액 업데이트 완료: {Amount:C}", user.Id, currentTotalAmount);
    //     }
    //
    //     _logger.LogInformation("전일 총평가금액 업데이트 완료: 성공 {SuccessCount}명, 실패 {ErrorCount}명", successCount, errorCount);
    // }
}