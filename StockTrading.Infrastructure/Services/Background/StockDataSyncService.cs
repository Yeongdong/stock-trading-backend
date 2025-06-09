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
        if (!_settings.Enabled) return;

        while (!stoppingToken.IsCancellationRequested)
        {
            var nextRunTime = GetNextRuntime();
            var delay = nextRunTime - DateTime.Now;

            if (delay <= TimeSpan.Zero) continue;
            _logger.LogInformation("다음 동기화 예정: {NextRunTime:yyyy-MM-dd HH:mm:ss}", nextRunTime);
            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            await ExecuteSyncAsync();
        }
    }

    private async Task ExecuteSyncAsync()
    {
        using var scope = _serviceProvider.CreateScope();

        var stockService = scope.ServiceProvider.GetRequiredService<IStockService>();
        var stockCacheService = scope.ServiceProvider.GetRequiredService<IStockCacheService>();

        await stockService.UpdateStockDataFromKrxAsync();
        await WarmupCacheAsync(stockCacheService, stockService);

        if (_settings.ResetMetricsOnSync)
        {
            var metrics = await stockCacheService.GetCacheMetricsAsync();
        }

        _logger.LogInformation("종목 데이터 동기화 완료: {DateTime:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
    }

    private async Task WarmupCacheAsync(IStockCacheService stockCacheService, IStockService stockService)
    {
        if (!_settings.EnableCacheWarmup) return;

        var popularTerms = await stockCacheService.GetPopularSearchTermsAsync(10);

        foreach (var term in popularTerms)
        {
            await stockService.SearchStocksAsync(term.Term, 1, 20);
            
            // 과부하 방지를 위한 딜레이
            await Task.Delay(100);
        }
        
        var majorStocks = new[] { "005930", "000660", "035420", "005490", "051910" };

        foreach (var stockCode in majorStocks)
        {
            await stockService.GetStockByCodeAsync(stockCode);
            
            await Task.Delay(100);
        }
    }

    private DateTime GetNextRuntime()
    {
        var now = DateTime.Now;
        var targetTime = new TimeSpan(_settings.SyncHour, _settings.SyncMinute, 0);

        var nextRun = now.Date.Add(targetTime);

        if (nextRun <= now)
            nextRun = nextRun.AddDays(1);
        
        return nextRun;
    }
}