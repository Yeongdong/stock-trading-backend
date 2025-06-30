using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrading.Application.Features.Market.Services;
using StockTrading.Domain.Settings.Infrastructure;

namespace StockTrading.Infrastructure.Services.Background;

/// <summary>
/// 종목 데이터 자동 동기화 백그라운드 서비스 (매일 오전 6시 실행)
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

        await stockService.SyncDomesticStockDataAsync();

        _logger.LogInformation("주식 데이터 동기화 실행 완료");
    }

    private DateTime GetNextRuntime()
    {
        var now = DateTime.Now;
        var today = now.Date;
        var targetTime = new TimeSpan(_settings.SyncHour, _settings.SyncMinute, 0);
        var todayTarget = today.Add(targetTime);

        // 오늘 실행 시간이 지났다면 내일로
        return todayTarget <= now
            ? today.AddDays(1).Add(targetTime)
            : todayTarget; // 아직 오늘 실행 시간이 남았다면 오늘
    }
}