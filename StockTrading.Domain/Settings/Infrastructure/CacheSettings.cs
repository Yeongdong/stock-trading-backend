using System.ComponentModel.DataAnnotations;
using StockTrading.Domain.Settings.Common;

namespace StockTrading.Domain.Settings.Infrastructure;

public class CacheSettings : BaseSettings
{
    public const string SectionName = "Cache";

    public bool Enabled { get; init; } = true;
    public string Provider { get; init; } = "Redis";

    [Range(1024, 1048576)] // 1KB ~ 1MB
    public int CompressionThreshold { get; init; } = 1024;

    [Range(100, 1000)] public int MaxKeyLength { get; init; } = 250;

    public CacheTtlSettings Ttl { get; init; } = new();
    public CacheMonitoringSettings Monitoring { get; init; } = new();

    protected override IEnumerable<ValidationResult> ValidateEnvironmentSpecific(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();
        var validProviders = new[] { "Redis", "Memory", "Hybrid" };

        if (!validProviders.Contains(Provider))
            results.Add(new ValidationResult($"Cache Provider must be one of: {string.Join(", ", validProviders)}",
                [nameof(Provider)]));

        return results;
    }
}

public class CacheTtlSettings
{
    [Range(1, 168)] // 1시간 ~ 7일
    public int AllStocksHours { get; init; } = 24;

    [Range(1, 24)] public int SearchResultsHours { get; init; } = 4;

    [Range(1, 12)] public int StockDetailHours { get; init; } = 1;

    [Range(1, 48)] public int AutoCompleteHours { get; init; } = 12;

    [Range(1, 24)] public int MetadataHours { get; init; } = 1;
}

public class CacheMonitoringSettings
{
    public bool EnableMetrics { get; init; } = true;

    [Range(10, 10000)] public int SlowQueryThresholdMs { get; init; } = 100;

    [Range(1, 168)] public int MetricsResetHours { get; init; } = 24;
}