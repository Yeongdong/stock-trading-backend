using System.ComponentModel.DataAnnotations;
using StockTrading.Domain.Settings.Common;

namespace StockTrading.Domain.Settings.Infrastructure;

public class RedisSettings : BaseSettings
{
    public const string SectionName = "Redis";

    [Required] public string ConnectionString { get; init; } = "localhost:6379";

    public string InstanceName { get; init; } = "StockTrading";

    [Range(1, 168)] // 1시간 ~ 7일
    public int DefaultExpirationHours { get; init; } = 24;

    [Range(5, 60)] public int ConnectTimeoutSeconds { get; init; } = 30;

    [Range(1000, 30000)] public int SyncTimeoutMs { get; init; } = 5000;

    [Range(0, 10)] public int RetryCount { get; init; } = 3;

    public bool EnableCompression { get; init; } = true;
    public bool Enabled { get; init; } = true;

    protected override IEnumerable<ValidationResult> ValidateEnvironmentSpecific(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        if (string.IsNullOrWhiteSpace(ConnectionString))
            results.Add(new ValidationResult("Redis ConnectionString is required", [nameof(ConnectionString)]));

        if (string.IsNullOrWhiteSpace(InstanceName))
            results.Add(new ValidationResult("Redis InstanceName is required", [nameof(InstanceName)]));

        return results;
    }
}