using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockTrading.Infrastructure.Repositories;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.Tests.Integration;

public class StockTradingWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly List<Action<IServiceCollection>> _serviceConfigurations = new();

    public void ConfigureServices(Action<IServiceCollection> configureServices)
    {
        _serviceConfigurations.Add(configureServices);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((hostcontext, config) =>
        {
            var projectDir = Directory.GetCurrentDirectory();
            config.AddJsonFile(Path.Combine(projectDir, "appsettings.Testing.json"), optional: false,
                reloadOnChange: true);
            config.AddEnvironmentVariables();
        });
        builder.ConfigureServices(services =>
        {
            // 기존 DB 컨텍스트 제거
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            // EF Core 관련 서비스 제거
            var efCoreServices = services.Where(d =>
                d.ServiceType.FullName != null &&
                (d.ServiceType.FullName.Contains("EntityFrameworkCore") ||
                 d.ServiceType.FullName.Contains("Npgsql"))).ToList();

            foreach (var service in efCoreServices)
                services.Remove(service);

            // InMemory DB 사용
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("IntegrationTestDb"));

            // 테스트 데이터 초기화
            using (var scope = services.BuildServiceProvider().CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureCreated();

                if (!db.Users.Any())
                {
                    db.Users.Add(new User
                    {
                        Id = 1,
                        Email = "test@example.com",
                        Name = "Test User",
                        GoogleId = "test_google_id",
                        Role = "User",
                        CreatedAt = DateTime.UtcNow,
                        KisAppKey = "test_app_key",
                        KisAppSecret = "test_app_secret",
                        AccountNumber = "test_account_number"
                    });
                    db.SaveChanges();
                }
            }

            foreach (var configureServices in _serviceConfigurations)
            {
                configureServices(services);
            }
        });
    }
}