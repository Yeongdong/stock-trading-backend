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
    private readonly Timer _timer;

    public StockDataSyncService(
        IServiceProvider serviceProvider,
        ILogger<StockDataSyncService> logger,
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
        if (!_settings.Enabled)
        {
            _logger.LogInformation("주식 데이터 동기화가 비활성화됨");
            return;
        }

        _logger.LogInformation("주식 데이터 동기화 서비스 시작 - 매일 {Hour:D2}:{Minute:D2}에 실행", _settings.SyncHour,
            _settings.SyncMinute);

        while (!stoppingToken.IsCancellationRequested)
        {
            var nextRunTime = GetNextRuntime();
            var delay = nextRunTime - DateTime.Now;

            _logger.LogInformation("다음 동기화 예정: {NextRunTime:yyyy-MM-dd HH:mm:ss}", nextRunTime);

            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;

            await ExecuteSyncAsync();

            // 동기화 후 1분 대기 (중복 실행 방지)
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ExecuteSyncAsync()
    {
        _logger.LogInformation("주식 데이터 동기화 실행 시작");

        using var scope = _serviceProvider.CreateScope();
        var stockService = scope.ServiceProvider.GetRequiredService<IStockService>();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        // 기존 주식 데이터 동기화
        await stockService.SyncDomesticStockDataAsync();

        // 전일 총평가금액 업데이트 추가
        await UpdateAllUsersPreviousDayBalanceAsync(scope.ServiceProvider);

        _logger.LogInformation("주식 데이터 동기화 실행 완료");
    }

    private DateTime GetNextRuntime()
    {
        var now = DateTime.Now;
        var today = now.Date;
        var targetTime = new TimeSpan(_settings.SyncHour, _settings.SyncMinute, 0);
        var todayTarget = today.Add(targetTime);

        // 오늘 실행 시간이 지났다면 다음 영업일로
        var nextRunDate = todayTarget <= now ? today.AddDays(1) : today;

        // 주말 및 공휴일 건너뛰기
        while (IsNonBusinessDay(nextRunDate))
        {
            nextRunDate = nextRunDate.AddDays(1);
        }

        return nextRunDate.Add(targetTime);
    }

    private bool IsNonBusinessDay(DateTime date)
    {
        return !_settings.RunOnWeekends &&
               date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    }

    private async Task UpdateAllUsersPreviousDayBalanceAsync(IServiceProvider serviceProvider)
    {
        _logger.LogInformation("전일 총평가금액 업데이트 시작");

        var userRepository = serviceProvider.GetRequiredService<IUserRepository>();
        var tradingService = serviceProvider.GetRequiredService<ITradingService>();

        // KIS API 연동 정보가 있는 사용자들만 조회
        var users = await userRepository.GetAllAsync();
        var activeUsers = users.Where(u =>
                !string.IsNullOrEmpty(u.KisAppKey) &&
                !string.IsNullOrEmpty(u.KisAppSecret) &&
                !string.IsNullOrEmpty(u.AccountNumber))
            .ToList();

        var successCount = 0;
        var errorCount = 0;

        foreach (var user in activeUsers)
        {
            var userInfo = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                AccountNumber = user.AccountNumber,
                KisAppKey = user.KisAppKey,
                KisAppSecret = user.KisAppSecret
            };

            // 현재 잔고 조회
            var balance = await tradingService.GetStockBalanceAsync(userInfo);
            var currentTotalAmount = decimal.Parse(balance.Summary.TotalEvaluation);

            // 전일 총평가금액으로 업데이트
            await userRepository.UpdatePreviousDayTotalAmountAsync(user.Id, currentTotalAmount);

            successCount++;
            _logger.LogDebug("사용자 {UserId} 전일 총평가금액 업데이트 완료: {Amount:C}", user.Id, currentTotalAmount);
        }

        _logger.LogInformation("전일 총평가금액 업데이트 완료: 성공 {SuccessCount}명, 실패 {ErrorCount}명", successCount, errorCount);
    }
}