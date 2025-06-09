using System.ComponentModel.DataAnnotations;
using StockTrading.Domain.Settings.Common;

namespace StockTrading.Domain.Settings.Application;

public class ApplicationSettings : BaseSettings
{
    public const string SectionName = "Application";

    [Required] public string Name { get; init; } = "Stock Trading API";

    [Required] public string Version { get; init; } = "1.0.0";

    [Required] public string BaseUrl { get; init; } = string.Empty;

    public FrontendSettings Frontend { get; init; } = new();
    public FeatureSettings Features { get; init; } = new();
    public LimitSettings Limits { get; init; } = new();

    protected override IEnumerable<ValidationResult> ValidateEnvironmentSpecific(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        // BaseUrl 검증
        var urlValidation = ValidateUrl(BaseUrl, nameof(BaseUrl));
        if (urlValidation != null)
            results.Add(urlValidation);

        // Version 형식 검증
        if (!System.Version.TryParse(Version, out _))
            results.Add(new ValidationResult("Version must be in valid semantic version format (e.g., 1.0.0)",
                [nameof(Version)]));

        return results;
    }
}

public class FrontendSettings
{
    [Required] public string Url { get; init; } = "http://localhost:3000";

    public List<string> AllowedOrigins { get; init; } =
    [
        "http://localhost:3000",
        "https://localhost:3000"
    ];
}

public class FeatureSettings
{
    public bool EnableRealTimeData { get; init; } = true;
    public bool EnableSwagger { get; init; } = true;
    public bool EnableDetailedLogging { get; init; } = true;
    public bool EnableHealthChecks { get; init; } = true;
}

public class LimitSettings
{
    [Range(1, 100)] public int MaxSubscriptionsPerUser { get; init; } = 50;

    [Range(5, 300)] public int RequestTimeoutSeconds { get; init; } = 30;

    [Range(1, 10)] public int MaxRetryAttempts { get; init; } = 3;
}