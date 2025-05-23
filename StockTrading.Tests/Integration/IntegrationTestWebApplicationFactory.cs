using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment;
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
    private const string BASE_URL = "https://openapivts.koreainvestment.com:29443";

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
            throw new DirectoryNotFoundException(
                $"StockTrading.Tests 프로젝트 디렉토리를 찾을 수 없습니다. 현재 경로: {currentDirectory}");

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
            DisableAntiforgeryForTesting(services);
            ReplaceHttpClientServices(services);
            RegisterBusinessServices(services);

            services.AddScoped<TestDataFactory>();
            services.AddScoped<IDbContextWrapper, DbContextWrapper>();
        });

        builder.ConfigureServices(services =>
        {
            OverrideHttpClientServices(services);

            services.AddScoped(provider => _testDataFactory);
        });
    }

    /// <summary>
    /// 비즈니스 로직 서비스들 등록
    /// </summary>
    private static void RegisterBusinessServices(IServiceCollection services)
    {

        // Repository 서비스들
        services.AddScoped<StockTrading.DataAccess.Repositories.IUserRepository, UserRepository>();
        services.AddScoped<StockTrading.DataAccess.Repositories.IOrderRepository, OrderRepository>();
        services.AddScoped<StockTrading.DataAccess.Repositories.IKisTokenRepository, KisTokenRepository>();
        services.AddScoped<StockTrading.DataAccess.Repositories.IUserKisInfoRepository, UserKisInfoRepository>();

        // Application Service들
        services.AddScoped<StockTrading.DataAccess.Services.Interfaces.IJwtService, JwtService>();
        services.AddScoped<StockTrading.DataAccess.Services.Interfaces.IUserService, UserService>();
        services.AddScoped<StockTrading.DataAccess.Services.Interfaces.IGoogleAuthProvider, GoogleAuthProvider>();
        services.AddScoped<StockTrading.DataAccess.Services.Interfaces.IKisService, KisService>();
        services.AddScoped<StockTrading.DataAccess.Services.Interfaces.IKisTokenService, KisTokenService>();

        // External API Client들
        services.AddScoped<StockTrading.Infrastructure.ExternalServices.Interfaces.IKisApiClient, KisApiClient>();

        // Infrastructure 서비스들
        services.AddScoped<IDbContextWrapper, DbContextWrapper>();

    }

    /// <summary>
    /// HttpClient 관련 서비스들을 모두 Mock으로 교체
    /// </summary>
    private void ReplaceHttpClientServices(IServiceCollection services)
    {

        services.AddHttpClient();

        // KisTokenService용 Named HttpClient 등록
        services.AddHttpClient(nameof(KisTokenService), client =>
            {
                client.BaseAddress = new Uri(BASE_URL);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                return new MockHttpMessageHandler(_testConfiguration);
            });

        // KisApiClient용 Named HttpClient 등록
        services.AddHttpClient(nameof(KisApiClient), client =>
            {
                client.BaseAddress = new Uri(BASE_URL);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new MockHttpMessageHandler(_testConfiguration));

    }

    /// <summary>
    /// HttpClient 관련 서비스들만 제거 (비즈니스 로직 서비스는 보존)
    /// </summary>
    private static void RemoveOnlyHttpClientServices(IServiceCollection services)
    {
        // HttpClient 관련 서비스만 제거
        var httpServiceTypesToRemove = new[]
        {
            typeof(HttpClient),
            typeof(IHttpClientFactory)
        };

        foreach (var serviceType in httpServiceTypesToRemove)
        {
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == serviceType ||
                            (d.ServiceType.Name.Contains("HttpClient") &&
                             !d.ServiceType.Name.Contains("Service") &&
                             !d.ServiceType.Name.Contains("Repository")))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
                Console.WriteLine($"[Factory] HttpClient 관련 서비스 제거됨: {descriptor.ServiceType.Name}");
            }
        }
    }

    private void OverrideHttpClientServices(IServiceCollection services)
    {
        // 1. 기존 HttpClient 관련 서비스들 모두 제거
        var httpClientDescriptors = services
            .Where(d => d.ServiceType.Name.Contains("HttpClient") ||
                        d.ServiceType == typeof(HttpClient) ||
                        d.ServiceType == typeof(IHttpClientFactory) ||
                        d.ImplementationType?.Name.Contains("HttpClient") == true)
            .ToList();

        foreach (var descriptor in httpClientDescriptors)
        {
            services.Remove(descriptor);
        }

        // 2. HttpClientFactory 재등록
        services.AddHttpClient();

        // 3. 특정 서비스들을 Mock HttpClient로 교체
        services.AddHttpClient<KisTokenService>(client =>
            {
                client.BaseAddress = new Uri(BASE_URL);
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                return new MockHttpMessageHandler(_testConfiguration);
            });

        // 4. KisApiClient도 Mock으로 교체
        services.AddHttpClient<KisApiClient>(client =>
            {
                client.BaseAddress = new Uri(BASE_URL);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new MockHttpMessageHandler(_testConfiguration));

    }

    /// <summary>
    /// 테스트 환경에서 CSRF 검증 완전 비활성화
    /// </summary>
    private static void DisableAntiforgeryForTesting(IServiceCollection services)
    {
        var mvcBuilder = services.AddMvc(options =>
        {
            // 모든 CSRF 관련 필터 제거
            for (int i = options.Filters.Count - 1; i >= 0; i--)
            {
                var filter = options.Filters[i];
                if (filter is AutoValidateAntiforgeryTokenAttribute ||
                    (filter is ServiceFilterAttribute serviceFilter &&
                     serviceFilter.ServiceType ==
                     typeof(AutoValidateAntiforgeryTokenAttribute)))
                {
                    options.Filters.RemoveAt(i);
                }
            }
        });

        // 테스트용 Antiforgery 설정 (완화된 설정)
        services.Configure<AntiforgeryOptions>(options =>
        {
            options.Cookie.SecurePolicy = CookieSecurePolicy.None;
            options.Cookie.SameSite = SameSiteMode.None;
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
            services.Remove(httpClientDescriptor);

        // 테스트용 HttpClient 등록 (실제 외부 API 호출 방지)
        services.AddHttpClient("TestClient")
            .ConfigurePrimaryHttpMessageHandler(() => new MockHttpMessageHandler(_testConfiguration));

        // KisTokenService용 HttpClient도 Mock으로 설정
        services.AddHttpClient<KisTokenService>()
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
        if (context.Users.Any()) return;

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