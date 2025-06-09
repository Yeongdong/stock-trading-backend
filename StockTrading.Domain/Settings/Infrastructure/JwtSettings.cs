using System.ComponentModel.DataAnnotations;
using StockTrading.Domain.Settings.Common;

namespace StockTrading.Domain.Settings.Infrastructure;

public class JwtSettings : BaseSettings
{
    public const string SectionName = "JwtSettings";

    [Required]
    [MinLength(32, ErrorMessage = "JWT Key must be at least 32 characters long")]
    public string Key { get; init; } = string.Empty;

    [Required] public string Issuer { get; init; } = string.Empty;

    [Required] public string Audience { get; init; } = string.Empty;

    [Range(5, 1440)] // 5분 ~ 24시간
    public int AccessTokenExpirationMinutes { get; init; } = 60;

    [Range(1, 30)] // 1일 ~ 30일
    public int RefreshTokenExpirationDays { get; init; } = 7;

    protected override IEnumerable<ValidationResult> ValidateEnvironmentSpecific(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        // 운영 환경에서 Key 필수
        var keyValidation = ValidateRequiredInProduction(Key, nameof(Key), "JWT Key");
        if (keyValidation != null) results.Add(keyValidation);

        // Key 길이 검증
        if (!string.IsNullOrWhiteSpace(Key) && Key.Length < 32)
            results.Add(new ValidationResult("JWT Key must be at least 32 characters long for security",
                [nameof(Key)]));

        return results;
    }
}