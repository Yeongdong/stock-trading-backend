using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using StockTrading.Infrastructure.Implementations;
using StockTrading.Infrastructure.Interfaces;
using StockTrading.Infrastructure.Repositories;
using StockTrading.Infrastructure.Security.Encryption;

namespace StockTrading.Tests.Integration;

/// <summary>
/// 통합테스트용 WebApplicationFactory
/// 실제 애플리케이션과 동일한 환경을 구성하되, 테스트에 적합한 설정으로 오버라이드
/// </summary>
public class IntegrationTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName;
    private IConfiguration _testConfiguration;
    private TestDataFactory _testDataFactory;

    public IntegrationTestWebApplicationFactory()
    {
        _databaseName = $"{IntegrationTestConstants.TestDatabasePrefix}{Guid.NewGuid()}";
    }

    /// <summary>
    /// 테스트 설정값에 접근할 수 있는 Configuration 객체
    /// </summary>
    public IConfiguration TestConfiguration => _testConfiguration;

    /// <summary>
    /// 테스트 데이터 팩토리
    /// </summary>
    public TestDataFactory TestDataFactory
    {
        get
        {
            if (_testDataFactory == null)
            {
                using var scope = Services.CreateScope();
                _testDataFactory = scope.ServiceProvider.GetRequiredService<TestDataFactory>();
            }

            return _testDataFactory;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ConfigureTestConfiguration(builder);
        ConfigureTestServices(builder);
        builder.UseEnvironment(IntegrationTestConstants.TestEnvironmentName);
    }

    /// <summary>
    /// 테스트 프로젝트의 실제 경로를 찾는 메서드
    /// </summary>
    private static string GetTestProjectPath()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var projectDirectory = currentDirectory;

        while (projectDirectory != null &&
               !File.Exists(Path.Combine(projectDirectory, "StockTrading.Tests.csproj")))
        {
            projectDirectory = Directory.GetParent(projectDirectory)?.FullName;
        }

        if (projectDirectory == null)
        {
            throw new DirectoryNotFoundException(
                $"StockTrading.Tests 프로젝트 디렉토리를 찾을 수 없습니다. 현재 경로: {currentDirectory}");
        }

        return projectDirectory;
    }

    /// <summary>
    /// 테스트용 설정 구성
    /// </summary>
    private void ConfigureTestConfiguration(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.Sources.Clear();

            var testProjectPath = GetTestProjectPath();
            var testConfigPath = Path.Combine(testProjectPath, "appsettings.Testing.json");

            config
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile(testConfigPath, optional: false, reloadOnChange: false)
                .AddEnvironmentVariables();

            _testConfiguration = config.Build();
        });
    }

    /// <summary>
    /// 테스트용 서비스 구성
    /// </summary>
    private void ConfigureTestServices(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            ReplaceDbContext(services);
            RegisterMockEncryptionService(services);
            RegisterMockHttpClient(services);

            services.AddScoped<TestDataFactory>();
            services.AddScoped<IDbContextWrapper, DbContextWrapper>();
        });

        builder.ConfigureServices(services =>
        {
            services.AddScoped(provider =>
            {
                if (_testDataFactory == null)
                {
                    _testDataFactory = ActivatorUtilities.CreateInstance<TestDataFactory>(provider);
                }

                return _testDataFactory;
            });
        });
    }

    /// <summary>
    /// 기존 DbContext를 테스트용 InMemory DB로 교체
    /// </summary>
    private void ReplaceDbContext(IServiceCollection services)
    {
        // EF Core 관련 모든 서비스 제거
        var descriptorsToRemove = services
            .Where(d => d.ServiceType.Name.Contains("DbContext") ||
                        d.ServiceType == typeof(ApplicationDbContext) ||
                        (d.ImplementationType != null && d.ImplementationType == typeof(ApplicationDbContext)))
            .ToList();

        foreach (var descriptor in descriptorsToRemove)
        {
            services.Remove(descriptor);
        }

        // 테스트용 InMemory 데이터베이스로 교체
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseInMemoryDatabase(_databaseName);
            options.EnableSensitiveDataLogging();
            options.ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
        });
    }

    /// <summary>
    /// 암호화 서비스 모킹 등록
    /// </summary>
    private static void RegisterMockEncryptionService(IServiceCollection services)
    {
        // 기존 암호화 서비스 제거
        var encryptionDescriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(IEncryptionService));
        if (encryptionDescriptor != null)
        {
            services.Remove(encryptionDescriptor);
        }

        // 테스트에서는 암호화하지 않는 Mock 서비스 등록
        services.AddSingleton<IEncryptionService>(provider =>
        {
            var mockEncryption = new Mock<IEncryptionService>();
            mockEncryption.Setup(x => x.Encrypt(It.IsAny<string>()))
                .Returns<string>(input => input); // 암호화하지 않고 그대로 반환
            mockEncryption.Setup(x => x.Decrypt(It.IsAny<string>()))
                .Returns<string>(input => input); // 복호화하지 않고 그대로 반환
            return mockEncryption.Object;
        });
    }

    /// <summary>
    /// Mock HttpClient 등록
    /// </summary>
    private void RegisterMockHttpClient(IServiceCollection services)
    {
        // 기존 HttpClient 제거
        var httpClientDescriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(HttpClient));
        if (httpClientDescriptor != null)
        {
            services.Remove(httpClientDescriptor);
        }

        // 테스트용 HttpClient 등록 (실제 외부 API 호출 방지)
        services.AddHttpClient("TestClient")
            .ConfigurePrimaryHttpMessageHandler(() => new MockHttpMessageHandler(_testConfiguration));
    }

    /// <summary>
    /// 테스트용 데이터베이스 초기화
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await context.Database.EnsureCreatedAsync();
        await SeedTestDataAsync(context);
    }

    /// <summary>
    /// 테스트용 시드 데이터 생성
    /// </summary>
    private async Task SeedTestDataAsync(ApplicationDbContext context)
    {
        if (context.Users.Any())
        {
            return;
        }

        try
        {
            var testUser = _testDataFactory.CreateTestUser();
            context.Users.Add(testUser);
            await context.SaveChangesAsync();

            var kisToken = _testDataFactory.CreateTestKisToken(testUser.Id);
            context.KisTokens.Add(kisToken);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            var logger = Services.GetService<ILogger<IntegrationTestWebApplicationFactory>>();
            logger?.LogError(ex, "테스트 데이터 시딩 중 오류 발생");
            throw;
        }
    }

    /// <summary>
    /// 테스트 간 데이터베이스 정리
    /// </summary>
    public async Task CleanupDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            context.KisTokens.RemoveRange(context.KisTokens);
            context.Users.RemoveRange(context.Users);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            var logger = Services.GetService<ILogger<IntegrationTestWebApplicationFactory>>();
            logger?.LogError(ex, "테스트 데이터 정리 중 오류 발생");
            throw;
        }
    }

    /// <summary>
    /// 리소스 정리
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
        }

        base.Dispose(disposing);
    }
}