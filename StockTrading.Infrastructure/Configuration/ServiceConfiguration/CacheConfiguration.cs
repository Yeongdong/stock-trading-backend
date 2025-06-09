using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using StockTrading.Application.Features.Market.Services;
using StockTrading.Domain.Settings.Infrastructure;
using StockTrading.Infrastructure.Cache;
using StockTrading.Infrastructure.Services;
using StockTrading.Infrastructure.Services.Market;

namespace StockTrading.Infrastructure.Configuration.ServiceConfiguration;

public static class CacheConfiguration
{
    public static IServiceCollection AddCacheServices(this IServiceCollection services, IConfiguration configuration)
    {
        var redisSettings = configuration.GetSection(RedisSettings.SectionName).Get<RedisSettings>() 
                           ?? new RedisSettings();

        if (!redisSettings.Enabled)
        {
            services.AddMemoryCache();
            return services;
        }

        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var connectionOptions = ConfigurationOptions.Parse(redisSettings.ConnectionString);
            connectionOptions.ConnectTimeout = redisSettings.ConnectTimeoutSeconds * 1000;
            connectionOptions.SyncTimeout = redisSettings.SyncTimeoutMs;
            connectionOptions.ConnectRetry = redisSettings.RetryCount;
            connectionOptions.AbortOnConnectFail = false;
            
            return ConnectionMultiplexer.Connect(connectionOptions);
        });

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisSettings.ConnectionString;
            options.InstanceName = redisSettings.InstanceName;
        });

        services.AddSingleton<CacheTtl>();
        services.AddSingleton<CacheMetrics>();
        services.AddScoped<IStockCacheService, StockCacheService>();

        return services;
    }

    public static IServiceCollection AddCacheHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var redisSettings = configuration.GetSection(RedisSettings.SectionName).Get<RedisSettings>();
        
        if (redisSettings?.Enabled == true)
            services.AddHealthChecks()
                .AddRedis(redisSettings.ConnectionString, "redis");

        return services;
    }
}