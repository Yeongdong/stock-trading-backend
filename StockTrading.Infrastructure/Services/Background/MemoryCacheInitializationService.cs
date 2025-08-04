using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockTrading.Application.Features.Market.Services;

namespace StockTrading.Infrastructure.Services.Background;

public class MemoryCacheInitializationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MemoryCacheInitializationService> _logger;

    public MemoryCacheInitializationService(
        IServiceProvider serviceProvider,
        ILogger<MemoryCacheInitializationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("메모리 캐시 초기화 서비스 시작");

        // 애플리케이션 시작 후 3초 대기
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

        using var scope = _serviceProvider.CreateScope();
        var stockCacheService = scope.ServiceProvider.GetRequiredService<IStockCacheService>();

        _logger.LogInformation("메모리에 주식 데이터 초기 로드 시작");
        await stockCacheService.LoadAllStocksAsync();
        _logger.LogInformation("메모리 캐시 초기화 완료");
    }
}
