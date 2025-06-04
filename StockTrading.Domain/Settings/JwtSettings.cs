using System.ComponentModel.DataAnnotations;

namespace StockTrading.Domain.Settings;

public class JwtSettings : IValidatableObject
{
    public const string SectionName = "JwtSettings";

    [Required]
    [MinLength(32, ErrorMessage = "JWT Key must be at least 32 characters long")]
    public string Key { get; init; } = string.Empty;

    [Required] [Url] public string Issuer { get; init; } = string.Empty;

    [Required] [Url] public string Audience { get; init; } = string.Empty;

    [Range(5, 1440)] // 5분 ~ 24시간
    public int AccessTokenExpirationMinutes { get; init; } = 60;

    [Range(1, 30)] // 1일 ~ 30일
    public int RefreshTokenExpirationDays { get; init; } = 7;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        // 운영 환경에서는 Key 필수
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (environment == "Production" && string.IsNullOrWhiteSpace(Key))
        {
            results.Add(new ValidationResult(
                "JWT Key is required in production environment",
                [nameof(Key)]));
        }

        // Key 길이 검증
        if (!string.IsNullOrWhiteSpace(Key) && Key.Length < 32)
        {
            results.Add(new ValidationResult(
                "JWT Key must be at least 32 characters long for security",
                [nameof(Key)]));
        }

        return results;
    }
}