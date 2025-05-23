using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using stock_trading_backend.Validator.Interfaces;
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
        ConfigureMockServices(services, testConfiguration);
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
                w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
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
    /// Mock 서비스 설정
    /// </summary>
    private static void ConfigureMockServices(IServiceCollection services, IConfiguration testConfiguration)
    {
        // 기존 GoogleAuthValidator 서비스 제거
        var existingValidator = services.SingleOrDefault(d => d.ServiceType == typeof(IGoogleAuthValidator));
        if (existingValidator != null)
            services.Remove(existingValidator);

        // Mock GoogleAuthValidator 등록
        services.AddSingleton<IGoogleAuthValidator>(provider =>
        {
            var mockValidator = new Mock<IGoogleAuthValidator>();
            ConfigureGoogleAuthValidatorMock(mockValidator, testConfiguration);
            return mockValidator.Object;
        });
    }

    /// <summary>
    /// GoogleAuthValidator Mock 설정
    /// </summary>
    private static void ConfigureGoogleAuthValidatorMock(Mock<IGoogleAuthValidator> mockValidator,
        IConfiguration testConfiguration)
    {
        // 유효한 페이로드 생성
        var validPayload = new GoogleJsonWebSignature.Payload
        {
            Subject = testConfiguration["TestData:User:GoogleId"] ?? "test_google_id_123",
            Email = testConfiguration["TestData:User:Email"] ?? "test@example.com",
            Name = testConfiguration["TestData:User:Name"] ?? "Test User",
            Issuer = "https://accounts.google.com",
            Audience = testConfiguration["Authentication:Google:ClientId"] ?? "test_google_client_id",
            ExpirationTimeSeconds = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            IssuedAtTimeSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        // 유효한 토큰 패턴 설정
        mockValidator
            .Setup(v => v.ValidateAsync(
                It.Is<string>(token =>
                    !string.IsNullOrEmpty(token) &&
                    token.Contains('.') &&
                    !token.StartsWith("invalid_") &&
                    !token.StartsWith("expired_") &&
                    token != "malformed_token"
                ),
                It.IsAny<string>()))
            .ReturnsAsync(validPayload);

        // 잘못된 토큰 패턴 설정
        mockValidator
            .Setup(v => v.ValidateAsync(
                It.Is<string>(token =>
                    string.IsNullOrEmpty(token) ||
                    token.StartsWith("invalid_") ||
                    token.StartsWith("expired_") ||
                    token == "malformed_token" ||
                    !token.Contains('.')
                ),
                It.IsAny<string>()))
            .ThrowsAsync(new InvalidJwtException("Invalid JWT token"));

        // 특정 에러 시나리오
        mockValidator
            .Setup(v => v.ValidateAsync("network_error_token", It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        mockValidator
            .Setup(v => v.ValidateAsync("google_service_error", It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Google service unavailable"));
    }

    /// <summary>
    /// 비즈니스 로직 서비스 설정
    /// </summary>
    private static void ConfigureBusinessServices(IServiceCollection services)
    {
        BusinessServiceRegistrar.RegisterAllServices(services);
    }
}