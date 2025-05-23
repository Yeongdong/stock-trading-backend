using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StockTrading.Infrastructure.Repositories;

namespace StockTrading.Tests.Integration.Configuration;

/// <summary>
/// 통합테스트용 서비스 설정을 담당하는 클래스
/// </summary
public static class TestServicesConfigurator
{
    private static readonly string SharedDatabaseName = $"IntegrationTest_Shared_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
    private const string KIS_BASE_URL = "https://openapivts.koreainvestment.com:29443";

    /// <summary>
    /// 모든 테스트 서비스 설정
    /// </summary>
    public static void ConfigureTestServices(IServiceCollection services, IConfiguration testConfiguration)
    {
        ConfigureFoundationServices(services, testConfiguration);
        ConfigureDatabaseServices(services);
        ConfigureSecurityServices(services);
        ConfigureHttpClientServices(services, testConfiguration);
        ConfigureBusinessServices(services);
    }

    /// <summary>
    /// 기본 인프라 서비스 설정
    /// </summary>
    private static void ConfigureFoundationServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<TestDataFactory>(provider => new TestDataFactory(configuration));

        if (services.All(s => s.ServiceType != typeof(ILoggerFactory)))
        {
            services.AddLogging();
        }
    }

    /// <summary>
    /// 데이터베이스 서비스 설정
    /// </summary>
    private static void ConfigureDatabaseServices(IServiceCollection services)
    {
        // 기존 DbContext 제거
        DatabaseServiceCleaner.RemoveExistingDbContextServices(services);

        // 테스트용 InMemory 데이터베이스 등록
        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            options.UseInMemoryDatabase(SharedDatabaseName);
            options.EnableSensitiveDataLogging();
            options.ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
        });
    }

    /// <summary>
    /// 보안 관련 서비스 설정
    /// </summary>
    private static void ConfigureSecurityServices(IServiceCollection services)
    {
        // Mock 암호화 서비스
        EncryptionServiceMocker.ConfigureMockEncryption(services);

        // CSRF 설정
        AntiforgeryConfigurator.ConfigureForTesting(services);
    }

    /// <summary>
    /// HTTP 클라이언트 서비스 설정
    /// </summary>
    private static void ConfigureHttpClientServices(IServiceCollection services, IConfiguration testConfiguration)
    {
        services.AddHttpClient();
        HttpClientConfigurator.RegisterKisHttpClients(services, testConfiguration, KIS_BASE_URL);
    }

    /// <summary>
    /// 비즈니스 로직 서비스 설정
    /// </summary>
    private static void ConfigureBusinessServices(IServiceCollection services)
    {
        BusinessServiceRegistrar.RegisterAllServices(services);
    }
}