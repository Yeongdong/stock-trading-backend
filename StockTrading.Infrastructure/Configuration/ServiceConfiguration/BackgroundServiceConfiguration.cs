using Microsoft.Extensions.DependencyInjection;
using StockTrading.Infrastructure.Services.Background;

namespace StockTrading.Infrastructure.Configuration.ServiceConfiguration;

public static class BackgroundServiceConfiguration
{
    public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<StockDataSyncService>();
        
        return services;
    }

}