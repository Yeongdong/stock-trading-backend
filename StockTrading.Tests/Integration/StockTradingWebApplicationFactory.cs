using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StockTrading.Infrastructure.Repositories;
using StockTrading.Infrastructure.Security.Encryption;
using StockTradingBackend.DataAccess.Entities;
using StockTradingBackend.DataAccess.Settings;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using StockTrading.Infrastructure.Security.Options;

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
        builder.ConfigureAppConfiguration((hostContext, config) =>
        {
            var projectDir = Directory.GetCurrentDirectory();
            config.AddJsonFile(Path.Combine(projectDir, "appsettings.Testing.json"), optional: false);
            config.AddEnvironmentVariables();
        });

        builder.ConfigureServices(services =>
        {
            // 기존 DB 제거 후 InMemory DB로 교체
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("IntegrationTestDb"));

            // DB 초기화
            using (var scope = services.BuildServiceProvider().CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureCreated();

                db.Users.Add(new User
                {
                    Id = 1,
                    Email = "test@example.com",
                    Name = "Test User",
                    GoogleId = "test_google_id",
                    Role = "User",
                    CreatedAt = DateTime.UtcNow
                });

                db.SaveChanges();
            }

            // Antiforgery 설정 완화
            services.Configure<Microsoft.AspNetCore.Antiforgery.AntiforgeryOptions>(options =>
            {
                options.Cookie.SecurePolicy = CookieSecurePolicy.None;
            });

            // JwtSettings 설정 주입
            var jwtSettingsDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IOptions<JwtSettings>));
            if (jwtSettingsDescriptor != null) services.Remove(jwtSettingsDescriptor);

            services.Configure<JwtSettings>(options =>
            {
                options.Key = "abcdefghijklmnopqrstuvwxyz123456"; // 32자 이상
                options.Issuer = "test_issuer";
                options.Audience = "test_audience";
                options.AccessTokenExpirationMinutes = 30;
                options.RefreshTokenExpirationDays = 7;
            });

            // 사용자 정의 서비스 구성
            foreach (var configure in _serviceConfigurations)
                configure(services);
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", "antiforgery-token-for-testing");
    }
}
