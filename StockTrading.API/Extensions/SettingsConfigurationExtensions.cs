using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using StockTrading.Domain.Settings;

namespace StockTrading.API.Extensions;

public static class SettingsConfigurationExtensions
{
    public static IServiceCollection AddSettingsWithValidation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptionsWithValidation<JwtSettings>(configuration, JwtSettings.SectionName);
        services.AddOptionsWithValidation<KrxApiSettings>(configuration, KrxApiSettings.SectionName);

        services.AddOptionsWithValidation<KoreaInvestmentSettings>(configuration, KoreaInvestmentSettings.SectionName);
        services.AddOptionsWithValidation<RealTimeDataSettings>(configuration, RealTimeDataSettings.SectionName);
        services.AddOptionsWithValidation<ApplicationSettings>(configuration, ApplicationSettings.SectionName);
        services.AddOptionsWithValidation<SecuritySettings>(configuration, SecuritySettings.SectionName);
        services.AddOptionsWithValidation<SignalRSettings>(configuration, SignalRSettings.SectionName);

        return services;
    }

    private static IServiceCollection AddOptionsWithValidation<TOptions>(this IServiceCollection services, IConfiguration configuration, string sectionName) where TOptions : class
    {
        services.Configure<TOptions>(configuration.GetSection(sectionName));
        
        // 설정 검증 활성화
        services.AddSingleton<IValidateOptions<TOptions>>(_ => new ValidateOptionsResult<TOptions>());

        return services;
    }

    public static IServiceCollection ValidateAllSettingsOnStartup(this IServiceCollection services)
    {
        services.AddOptions<KoreaInvestmentSettings>()
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<RealTimeDataSettings>()
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<ApplicationSettings>()
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<JwtSettings>()
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<KrxApiSettings>()
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    public static IServiceCollection AddSettingsSummary(this IServiceCollection services)
    {
        services.AddSingleton<ISettingsSummaryService, SettingsSummaryService>();
        return services;
    }
}

/// <summary>
/// 설정 검증 결과를 제공하는 클래스
/// </summary>
public class ValidateOptionsResult<TOptions> : IValidateOptions<TOptions>
    where TOptions : class
{
    public ValidateOptionsResult Validate(string name, TOptions options)
    {
        if (options is not IValidatableObject validatable) return ValidateOptionsResult.Success;
        var results = validatable.Validate(new ValidationContext(options));
        var errors = results.Select(r => r.ErrorMessage).Where(e => !string.IsNullOrEmpty(e));
            
        return errors.Any() ? ValidateOptionsResult.Fail($"Configuration validation failed for {typeof(TOptions).Name}: {string.Join(", ", errors)}") : ValidateOptionsResult.Success;
    }
}

/// <summary>
/// 설정 요약 정보 서비스
/// </summary>
public interface ISettingsSummaryService
{
    Dictionary<string, object> GetSettingsSummary();
}

public class SettingsSummaryService : ISettingsSummaryService
{
    private readonly IConfiguration _configuration;

    public SettingsSummaryService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Dictionary<string, object> GetSettingsSummary()
    {
        return new Dictionary<string, object>
        {
            ["Application"] = new
            {
                Name = _configuration["Application:Name"],
                Version = _configuration["Application:Version"],
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                BaseUrl = _configuration["Application:BaseUrl"]
            },
            ["Features"] = new
            {
                RealTimeData = _configuration.GetValue<bool>("Application:Features:EnableRealTimeData"),
                Swagger = _configuration.GetValue<bool>("Application:Features:EnableSwagger"),
                DetailedLogging = _configuration.GetValue<bool>("Application:Features:EnableDetailedLogging"),
                HealthChecks = _configuration.GetValue<bool>("Application:Features:EnableHealthChecks")
            },
            ["ExternalServices"] = new
            {
                KoreaInvestmentConfigured = !string.IsNullOrEmpty(_configuration["KoreaInvestment:BaseUrl"]),
                KrxApiConfigured = !string.IsNullOrEmpty(_configuration["KrxApi:BaseUrl"]),
                DatabaseConfigured = !string.IsNullOrEmpty(_configuration.GetConnectionString("DefaultConnection"))
            },
            ["Security"] = new
            {
                JwtConfigured = !string.IsNullOrEmpty(_configuration["JwtSettings:Key"]),
                GoogleAuthConfigured = !string.IsNullOrEmpty(_configuration["Authentication:Google:ClientId"]),
                EncryptionConfigured = !string.IsNullOrEmpty(_configuration["Encryption:Key"])
            }
        };
    }
}