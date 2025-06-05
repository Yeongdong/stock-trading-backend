using System.ComponentModel.DataAnnotations;

namespace StockTrading.Domain.Settings;

public class StockDataSyncSettings : IValidatableObject
{
    public const string SectionName = "StockDataSync";

    public bool Enabled { get; init; } = true;
    [Range(0, 23)] public int SyncHour { get; init; } = 6;
    [Range(0, 59)] public int SyncMinute { get; init; } = 0;
    public bool RetryOnFailure { get; init; } = true;
    [Range(1, 60)] public int RetryDelayMinutes { get; init; } = 30;
    [Range(0, 5)] public int MaxRetryCount { get; init; } = 2;
    public bool EnableCacheWarmup { get; init; } = true;
    public bool ResetMetricsOnSync { get; init; } = false;
    [Range(5, 60)] public int TimeoutMinutes { get; init; } = 30;
    public bool RunOnWeekends { get; init; } = false;
    public bool RunOnHolidays { get; init; } = false;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        if (SyncHour is < 0 or > 23)
            results.Add(new ValidationResult("SyncHour must be between 0 and 23", [nameof(SyncHour)]));

        if (SyncMinute is < 0 or > 59)
            results.Add(new ValidationResult("SyncMinute must be between 0 and 59", [nameof(SyncMinute)]));

        return results;
    }
}