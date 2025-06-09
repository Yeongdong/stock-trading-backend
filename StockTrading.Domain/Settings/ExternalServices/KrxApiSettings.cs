using System.ComponentModel.DataAnnotations;
using StockTrading.Domain.Settings.Common;

namespace StockTrading.Domain.Settings.ExternalServices;

public class KrxApiSettings : BaseSettings
{
    public const string SectionName = "KrxApi";

    [Required] public string BaseUrl { get; init; } = "https://data.krx.co.kr";

    public string StockListEndpoint { get; init; } = "/comm/bldAttendant/getJsonData.cmd";
    public string StockListBuildId { get; init; } = "dbms/MDC/STAT/standard/MDCSTAT01901";
    public string UserAgent { get; init; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";

    [Range(5, 300)] public int TimeoutSeconds { get; init; } = 30;

    [Range(0, 10)] public int RetryCount { get; init; } = 3;

    [Range(100, 10000)] public int RetryDelayMs { get; init; } = 1000;

    protected override IEnumerable<ValidationResult> ValidateEnvironmentSpecific(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        var urlValidation = ValidateUrl(BaseUrl, nameof(BaseUrl));
        if (urlValidation != null)
            results.Add(urlValidation);

        return results;
    }
}