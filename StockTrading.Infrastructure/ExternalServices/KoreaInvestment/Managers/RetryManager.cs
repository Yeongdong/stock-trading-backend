using Microsoft.Extensions.Logging;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Managers;

/// <summary>
/// 재시도 로직 관리자
/// </summary>
public class RetryManager
{
    private readonly ILogger<RetryManager> _logger;

    private const int DefaultMaxRetries = 3;
    private const int DefaultRetryDelayMs = 1000;

    public RetryManager(ILogger<RetryManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 재시도 로직을 포함한 작업 실행
    /// </summary>
    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, Predicate<Exception> shouldRetry,
        int maxRetries = DefaultMaxRetries, int retryDelayMs = DefaultRetryDelayMs, Func<Task>? onRetry = null,
        string? operationName = null)
    {
        var retryCount = 0;
        Exception lastException = null;

        while (retryCount <= maxRetries)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (retryCount < maxRetries && shouldRetry(ex))
            {
                lastException = ex;
                retryCount++;

                _logger.LogWarning(ex, "{Operation} 실패. 재시도 {Retry}/{MaxRetries}: {Error}",
                    operationName ?? "작업", retryCount, maxRetries, ex.Message);

                if (onRetry != null)
                    await onRetry();

                await Task.Delay(retryDelayMs);
            }
        }

        _logger.LogError(lastException, "{Operation} 최대 재시도 횟수 초과: {MaxRetries}", operationName ?? "작업", maxRetries);

        throw lastException ?? new InvalidOperationException("알 수 없는 오류로 작업 실패");
    }

    /// <summary>
    /// 재시도 로직을 포함한 작업 실행 (반환값 없음)
    /// </summary>
    public async Task ExecuteWithRetryAsync(Func<Task> operation, Predicate<Exception> shouldRetry,
        int maxRetries = DefaultMaxRetries, int retryDelayMs = DefaultRetryDelayMs, Func<Task>? onRetry = null,
        string? operationName = null)
    {
        await ExecuteWithRetryAsync(
            async () =>
            {
                await operation();
                return true; // void를 bool로 래핑
            },
            shouldRetry,
            maxRetries,
            retryDelayMs,
            onRetry,
            operationName);
    }

    /// <summary>
    /// 연결 관련 예외인지 확인
    /// </summary>
    public static bool IsConnectionException(Exception ex)
    {
        return ex is InvalidOperationException &&
               ex.Message.Contains("재연결이 필요합니다");
    }
}