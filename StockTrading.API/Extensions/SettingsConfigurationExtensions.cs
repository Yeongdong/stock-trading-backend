using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using StockTrading.Domain.Settings;

namespace StockTrading.API.Extensions;

public static class SettingsConfigurationExtensions
{
    public static void AddSettingsWithValidation(this IServiceCollection services, IConfiguration configuration)
    {
        AddAllSettings(services, configuration);
    }

    public static void ValidateAllSettingsOnStartup(this IServiceCollection services)
    {
        AddSettingsValidation(services);
    }

    public static void AddSettingsSummary(this IServiceCollection services)
    {
        services.AddSingleton<ISettingsSummaryService, SettingsSummaryService>();
    }

    private static void AddAllSettings(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptionsWithValidation<JwtSettings>(configuration, JwtSettings.SectionName);
        services.AddOptionsWithValidation<KrxApiSettings>(configuration, KrxApiSettings.SectionName);

        services.AddOptionsWithValidation<KoreaInvestmentSettings>(configuration, KoreaInvestmentSettings.SectionName);
        services.AddOptionsWithValidation<RealTimeDataSettings>(configuration, RealTimeDataSettings.SectionName);
        services.AddOptionsWithValidation<ApplicationSettings>(configuration, ApplicationSettings.SectionName);
        services.AddOptionsWithValidation<SecuritySettings>(configuration, SecuritySettings.SectionName);
        services.AddOptionsWithValidation<SignalRSettings>(configuration, SignalRSettings.SectionName);
    }

    private static void AddOptionsWithValidation<TOptions>(this IServiceCollection services,
        IConfiguration configuration, string sectionName) where TOptions : class
    {
        services.Configure<TOptions>(configuration.GetSection(sectionName));
        services.AddSingleton<IValidateOptions<TOptions>>(_ => new ValidateOptionsResult<TOptions>());
    }

    private static void AddSettingsValidation(IServiceCollection services)
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
    }
}

public class ValidateOptionsResult<TOptions> : IValidateOptions<TOptions>
    where TOptions : class
{
    public ValidateOptionsResult Validate(string name, TOptions options)
    {
        var errors = new List<string>();

        if (options is IValidatableObject validatable)
        {
            var results = validatable.Validate(new ValidationContext(options));
            errors.AddRange(results.Select(r => r.ErrorMessage).Where(e => !string.IsNullOrEmpty(e)));
        }

        ValidateEnvironmentSpecificSettings(options, errors);

        return errors.Any()
            ? ValidateOptionsResult.Fail(
                $"Configuration validation failed for {typeof(TOptions).Name}: {string.Join(", ", errors)}")
            : ValidateOptionsResult.Success;
    }

    private static void ValidateEnvironmentSpecificSettings<T>(T options, List<string> errors)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        if (environment != "Production") return;

        switch (options)
        {
            case KoreaInvestmentSettings kisSettings:
                ValidateProductionKisSettings(kisSettings, errors);
                break;
            case JwtSettings jwtSettings:
                ValidateProductionJwtSettings(jwtSettings, errors);
                break;
        }
    }

    private static void ValidateProductionKisSettings(KoreaInvestmentSettings settings, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(settings.AppKey))
            errors.Add("AppKey is required in production environment");

        if (string.IsNullOrWhiteSpace(settings.AppSecret))
            errors.Add("AppSecret is required in production environment");
    }

    private static void ValidateProductionJwtSettings(JwtSettings settings, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(settings.Key))
            errors.Add("JWT Key is required in production environment");

        if (settings.Key?.Length < 32)
            errors.Add("JWT Key must be at least 32 characters long in production");
    }
}

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