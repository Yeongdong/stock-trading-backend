using System.ComponentModel.DataAnnotations;

namespace StockTrading.Domain.Settings.Common;

public abstract class BaseSettings : IValidatableObject
{
    protected virtual IEnumerable<ValidationResult> ValidateEnvironmentSpecific(ValidationContext validationContext)
    {
        return Enumerable.Empty<ValidationResult>();
    }

    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        // 환경별 검증
        results.AddRange(ValidateEnvironmentSpecific(validationContext));

        return results;
    }

    protected static bool IsProduction =>
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production";

    protected static ValidationResult? ValidateUrl(string? url, string propertyName, string? displayName = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            return new ValidationResult($"{displayName ?? propertyName} must be a valid absolute URL", [propertyName]);

        return null;
    }

    protected static ValidationResult? ValidateRequiredInProduction(string? value, string propertyName,
        string? displayName = null)
    {
        if (!IsProduction)
            return null;

        if (string.IsNullOrWhiteSpace(value))
            return new ValidationResult($"{displayName ?? propertyName} is required in production environment",
                [propertyName]);

        return null;
    }
}