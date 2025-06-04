using Microsoft.Extensions.Logging;

namespace StockTrading.Infrastructure.Persistence.Repositories;

public static class RepositoryExceptionHandler
{
    public static async Task<T> ExecuteWithLoggingAsync<T>(Func<Task<T>> operation, ILogger logger, string operationName, object? parameters = null)
    {
        logger.LogDebug("{Operation} 시작. Parameters: {Parameters}", operationName, parameters);
        
        var result = await operation();
        
        logger.LogDebug("{Operation} 완료. Parameters: {Parameters}", operationName, parameters);
        return result;
    }

    public static async Task ExecuteWithLoggingAsync(Func<Task> operation, ILogger logger, string operationName, object? parameters = null)
    {
        await ExecuteWithLoggingAsync(
            async () =>
            {
                await operation();
                return true;
            },
            logger, operationName, parameters);
    }
}