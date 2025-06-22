using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockTrading.Infrastructure.Persistence.Contexts;
using StockTrading.Infrastructure.Security.Encryption;

namespace StockTrading.Infrastructure.Configuration.ServiceConfiguration;

public static class DatabaseConfiguration
{
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        // PostgreSQL 연결 설정
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

            // 개발 환경에서만 민감한 데이터 로깅 활성화
            if (!IsDevelopment()) return;
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        });

        // ApplicationDbContext 수동 등록 (IEncryptionService 의존성)
        services.AddScoped<ApplicationDbContext>(provider =>
        {
            var options = provider.GetRequiredService<DbContextOptions<ApplicationDbContext>>();
            var encryptionService = provider.GetRequiredService<IEncryptionService>();
            return new ApplicationDbContext(options, encryptionService);
        });

        return services;
    }

    public static IServiceCollection AddDatabaseHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("database");

        return services;
    }

    private static bool IsDevelopment() => 
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
}