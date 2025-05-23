using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StockTrading.Infrastructure.Repositories;
using StockTrading.Tests.Integration.Configuration;

namespace StockTrading.Tests.Integration;

/// <summary>
/// 통합테스트용 WebApplicationFactory
/// </summary>
public class IntegrationTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private IConfiguration _testConfiguration;

    /// <summary>
    /// 테스트 설정값에 접근할 수 있는 Configuration 객체
    /// </summary>
    public IConfiguration TestConfiguration => _testConfiguration;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(IntegrationTestConstants.TestEnvironmentName);

        ConfigureTestConfiguration(builder);
        ConfigureTestServices(builder);
    }

    #region 설정 메서드

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
            TestServicesConfigurator.ConfigureTestServices(services, _testConfiguration);
        });
    }

    #endregion

    #region 유틸리티 메서드

    /// <summary>
    /// 테스트 프로젝트 경로 찾기
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

    #endregion

    #region 데이터베이스 관리

    /// <summary>
    /// 테스트용 데이터베이스 초기화
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var testDataFactory = scope.ServiceProvider.GetRequiredService<TestDataFactory>();

        await context.Database.EnsureCreatedAsync();
        await SeedTestDataAsync(context, testDataFactory);
    }

    /// <summary>
    /// 테스트용 시드 데이터 생성
    /// </summary>
    private async Task SeedTestDataAsync(ApplicationDbContext context, TestDataFactory testDataFactory)
    {
        if (context.Users.Any()) return;

        try
        {
            var testUser = testDataFactory.CreateTestUser();
            context.Users.Add(testUser);
            await context.SaveChangesAsync();

            var kisToken = testDataFactory.CreateTestKisToken(testUser.Id);
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

    #endregion

    #region 디버깅 메서드들 (필요시 사용)

    /// <summary>
    /// 서비스 등록 상태 확인 (디버깅용)
    /// </summary>
    public bool IsServiceRegistered<T>()
    {
        try
        {
            using var scope = Services.CreateScope();
            var service = scope.ServiceProvider.GetService<T>();
            return service != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 테스트용 서비스 의존성 검증
    /// </summary>
    public async Task ValidateServicesAsync()
    {
        using var scope = Services.CreateScope();
        var logger = scope.ServiceProvider.GetService<ILogger<IntegrationTestWebApplicationFactory>>();

        try
        {
            // 필수 서비스들이 정상적으로 생성되는지 확인
            var testDataFactory = scope.ServiceProvider.GetRequiredService<TestDataFactory>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userService = scope.ServiceProvider
                .GetRequiredService<StockTrading.DataAccess.Services.Interfaces.IUserService>();
            var kisService = scope.ServiceProvider
                .GetRequiredService<StockTrading.DataAccess.Services.Interfaces.IKisService>();

            logger?.LogInformation("모든 필수 서비스가 정상적으로 등록되고 생성됨");

            // 데이터베이스 연결 확인
            await context.Database.EnsureCreatedAsync();
            logger?.LogInformation("데이터베이스 연결 확인 완료");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "서비스 검증 중 오류 발생");
            throw;
        }
    }

    #endregion
}