using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StockTrading.Tests.Integration.Implementations;
using StockTrading.Tests.Integration.Interfaces;
using StockTrading.Tests.Integration.Utilities;

namespace StockTrading.Tests.Integration;

/// <summary>
/// 모든 통합 테스트의 기본 베이스 클래스
/// 공통 인프라를 제공하고 테스트 환경을 설정
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<IntegrationTestWebApplicationFactory>, IDisposable
{
    protected readonly IntegrationTestWebApplicationFactory Factory;
    protected readonly IDatabaseManager DatabaseManager;
    protected readonly IHttpClientHelper HttpClientHelper;
    protected readonly IAuthenticationHelper AuthenticationHelper;
    protected readonly ILogger Logger;
    protected readonly TestDataFactory TestDataFactory;

    private readonly IServiceScope _serviceScope;
    private bool _disposed;

    protected IntegrationTestBase(IntegrationTestWebApplicationFactory factory)
    {
        Factory = factory ?? throw new ArgumentNullException(nameof(factory));

        _serviceScope = Factory.Services.CreateScope();
        var serviceProvider = _serviceScope.ServiceProvider;

        DatabaseManager = CreateDatabaseManager(serviceProvider);
        HttpClientHelper = CreateHttpClientHelper(serviceProvider);
        AuthenticationHelper = CreateAuthenticationHelper(serviceProvider);
        TestDataFactory = Factory.TestDataFactory;

        Logger = CreateLogger();

        InitializeTestEnvironmentAsync().GetAwaiter().GetResult();
    }

    #region 팩토리 메서드들

    /// <summary>
    /// DatabaseManager 인스턴스 생성
    /// </summary>
    protected virtual IDatabaseManager CreateDatabaseManager(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<DatabaseManager>>();
        return new DatabaseManager(serviceProvider, TestDataFactory, logger);
    }

    /// <summary>
    /// HttpClientHelper 인스턴스 생성
    /// </summary>
    protected virtual IHttpClientHelper CreateHttpClientHelper(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<HttpClientHelper>>();
        return new HttpClientHelper(Factory, logger);
    }

    /// <summary>
    /// AuthenticationHelper 인스턴스 생성
    /// </summary>
    protected virtual IAuthenticationHelper CreateAuthenticationHelper(IServiceProvider serviceProvider)
    {
        var tokenGenerator = CreateTokenGenerator(serviceProvider);
        var logger = serviceProvider.GetRequiredService<ILogger<AuthenticationHelper>>();
        return new AuthenticationHelper(tokenGenerator, HttpClientHelper, Factory.TestConfiguration, logger);
    }

    /// <summary>
    /// TestJwtTokenGenerator 인스턴스 생성
    /// </summary>
    protected virtual TestJwtTokenGenerator CreateTokenGenerator(IServiceProvider serviceProvider)
    {
        return new TestJwtTokenGenerator(Factory.TestConfiguration);
    }

    /// <summary>
    /// 로거 인스턴스 생성
    /// </summary>
    protected virtual ILogger CreateLogger()
    {
        var loggerFactory = _serviceScope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        return loggerFactory.CreateLogger(GetType());
    }

    #endregion

    #region 테스트 환경 관리

    /// <summary>
    /// 테스트 환경 초기화
    /// </summary>
    protected virtual async Task InitializeTestEnvironmentAsync()
    {
        try
        {
            Logger.LogInformation("테스트 환경 초기화 시작: {TestClass}", GetType().Name);

            await DatabaseManager.InitializeAsync();
            await DatabaseManager.SeedTestDataAsync();
            ConfigureHttpClient();
            await OnTestEnvironmentInitializedAsync();

            Logger.LogInformation("테스트 환경 초기화 완료: {TestClass}", GetType().Name);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "테스트 환경 초기화 중 오류 발생: {TestClass}", GetType().Name);
            throw;
        }
    }

    /// <summary>
    /// HTTP 클라이언트 기본 설정
    /// </summary>
    protected virtual void ConfigureHttpClient()
    {
        // 요청 로깅 활성화 (디버그 모드에서만)
        HttpClientHelper.EnableRequestLogging(ShouldEnableRequestLogging());

        HttpClientHelper.SetTimeout(GetDefaultTimeout());
    }

    /// <summary>
    /// 요청 로깅 활성화 여부 결정
    /// </summary>
    protected virtual bool ShouldEnableRequestLogging()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
    }

    /// <summary>
    /// 기본 타임아웃 설정
    /// </summary>
    protected virtual TimeSpan GetDefaultTimeout()
    {
        return TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// 테스트 환경 초기화 완료 후 호출되는 가상 메서드
    /// 서브클래스에서 커스텀 초기화 로직 구현
    /// </summary>
    protected virtual Task OnTestEnvironmentInitializedAsync()
    {
        return Task.CompletedTask;
    }

    #endregion

    #region 테스트 메서드 생명주기

    /// <summary>
    /// 각 테스트 메서드 실행 전 호출
    /// </summary>
    protected virtual async Task SetupAsync()
    {
        Logger.LogDebug("테스트 셋업 시작");

        await EnsureDatabaseHealthAsync();

        Logger.LogDebug("테스트 셋업 완료");
    }

    /// <summary>
    /// 각 테스트 메서드 실행 후 호출
    /// </summary>
    protected virtual async Task TeardownAsync()
    {
        Logger.LogDebug("테스트 정리 시작");

        await OnTestTeardownAsync();

        Logger.LogDebug("테스트 정리 완료");
    }

    /// <summary>
    /// 테스트 정리 시 호출되는 가상 메서드
    /// 서브클래스에서 커스텀 정리 로직 구현
    /// </summary>
    protected virtual Task OnTestTeardownAsync()
    {
        return Task.CompletedTask;
    }

    #endregion

    #region 헬퍼 메서드들

    /// <summary>
    /// 데이터베이스 상태 확인
    /// </summary>
    protected virtual async Task EnsureDatabaseHealthAsync()
    {
        var isHealthy = await DatabaseManager.IsHealthyAsync();
        if (!isHealthy)
        {
            Logger.LogWarning("데이터베이스 상태 불량, 재초기화 시도");
            await DatabaseManager.InitializeAsync();
        }
    }

    /// <summary>
    /// 새로운 HTTP 클라이언트 생성
    /// </summary>
    protected HttpClient CreateClient()
    {
        return HttpClientHelper.CreateClient();
    }

    /// <summary>
    /// JSON 클라이언트 생성
    /// </summary>
    protected HttpClient CreateJsonClient()
    {
        return HttpClientHelper.CreateJsonClient();
    }

    /// <summary>
    /// 커스텀 헤더가 있는 클라이언트 생성
    /// </summary>
    protected HttpClient CreateClientWithHeaders(Dictionary<string, string> headers)
    {
        return HttpClientHelper.CreateClientWithHeaders(headers);
    }

    /// <summary>
    /// 서비스 인스턴스 가져오기
    /// </summary>
    protected T GetService<T>() where T : class
    {
        return _serviceScope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// 선택적 서비스 인스턴스 가져오기
    /// </summary>
    protected T GetService<T>(bool required) where T : class
    {
        return required
            ? _serviceScope.ServiceProvider.GetRequiredService<T>()
            : _serviceScope.ServiceProvider.GetService<T>();
    }

    /// <summary>
    /// 테스트 실행 전후 로깅을 위한 헬퍼
    /// </summary>
    protected async Task RunTestWithLoggingAsync(string testName, Func<Task> testAction)
    {
        Logger.LogInformation("테스트 시작: {TestName}", testName);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await SetupAsync();
            await testAction();

            stopwatch.Stop();
            Logger.LogInformation("테스트 성공: {TestName} (소요시간: {ElapsedMs}ms)",
                testName, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "테스트 실패: {TestName} (소요시간: {ElapsedMs}ms)",
                testName, stopwatch.ElapsedMilliseconds);
            throw;
        }
        finally
        {
            await TeardownAsync();
        }
    }

    /// <summary>
    /// 비동기 작업의 타임아웃 처리
    /// </summary>
    protected async Task<T> WithTimeoutAsync<T>(Task<T> task, TimeSpan timeout)
    {
        using var cancellationTokenSource = new CancellationTokenSource(timeout);
        try
        {
            return await task.WaitAsync(cancellationTokenSource.Token);
        }
        catch (OperationCanceledException) when (cancellationTokenSource.Token.IsCancellationRequested)
        {
            throw new TimeoutException($"작업이 {timeout.TotalSeconds}초 내에 완료되지 않았습니다.");
        }
    }

    /// <summary>
    /// 비동기 작업의 타임아웃 처리 (반환값 없음)
    /// </summary>
    protected async Task WithTimeoutAsync(Task task, TimeSpan timeout)
    {
        using var cancellationTokenSource = new CancellationTokenSource(timeout);
        try
        {
            await task.WaitAsync(cancellationTokenSource.Token);
        }
        catch (OperationCanceledException) when (cancellationTokenSource.Token.IsCancellationRequested)
        {
            throw new TimeoutException($"작업이 {timeout.TotalSeconds}초 내에 완료되지 않았습니다.");
        }
    }

    #endregion

    #region 디버깅 헬퍼

    /// <summary>
    /// 테스트 컨텍스트 정보 출력 (디버깅용)
    /// </summary>
    protected virtual void LogTestContext()
    {
        Logger.LogDebug("=== 테스트 컨텍스트 정보 ===");
        Logger.LogDebug("테스트 클래스: {TestClass}", GetType().Name);
        Logger.LogDebug("팩토리 타입: {FactoryType}", Factory.GetType().Name);
        Logger.LogDebug("환경: {Environment}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
        Logger.LogDebug("=============================");
    }

    /// <summary>
    /// 현재 데이터베이스 상태 출력 (디버깅용)
    /// </summary>
    protected virtual async Task LogDatabaseStateAsync()
    {
        Logger.LogDebug("=== 데이터베이스 상태 ===");

        try
        {
            var userCount = await DatabaseManager.CountAsync<StockTradingBackend.DataAccess.Entities.User>();
            var tokenCount = await DatabaseManager.CountAsync<StockTradingBackend.DataAccess.Entities.KisToken>();

            Logger.LogDebug("사용자 수: {UserCount}", userCount);
            Logger.LogDebug("토큰 수: {TokenCount}", tokenCount);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "데이터베이스 상태 조회 중 오류 발생");
        }

        Logger.LogDebug("========================");
    }

    #endregion

    #region IDisposable 구현

    /// <summary>
    /// 리소스 정리
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 리소스 정리 구현
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                Logger.LogDebug("IntegrationTestBase 리소스 정리 시작");

                CleanupResourcesAsync().GetAwaiter().GetResult();

                _serviceScope?.Dispose();

                Logger.LogDebug("IntegrationTestBase 리소스 정리 완료");
            }
            catch (Exception ex)
            {
                Logger?.LogWarning(ex, "리소스 정리 중 오류 발생");
            }
            finally
            {
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 비동기 리소스 정리
    /// </summary>
    protected virtual async Task CleanupResourcesAsync()
    {
        await Task.CompletedTask;
    }

    #endregion
}