using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockTrading.Domain.Settings.Application;
using StockTrading.Domain.Settings.ExternalServices;
using StockTrading.Domain.Settings.Infrastructure;

namespace StockTrading.Infrastructure.Configuration;

public static class SettingsExtensions
{
    public static IServiceCollection AddApplicationSettings(this IServiceCollection services,
        IConfiguration configuration)
    {
        // Application Settings
        services.AddOptionsWithValidation<ApplicationSettings>(configuration);
        services.AddOptionsWithValidation<SecuritySettings>(configuration);
        services.AddOptionsWithValidation<SignalRSettings>(configuration);

        // External Service Settings
        services.AddOptionsWithValidation<KoreaInvestmentSettings>(configuration);
        services.AddOptionsWithValidation<KrxApiSettings>(configuration);
        services.AddOptionsWithValidation<RealTimeDataSettings>(configuration);
        services.AddOptionsWithValidation<FinnhubSettings>(configuration);

        // Infrastructure Settings
        services.AddOptionsWithValidation<JwtSettings>(configuration);
        services.AddOptionsWithValidation<CacheSettings>(configuration);
        services.AddOptionsWithValidation<RedisSettings>(configuration);
        services.AddOptionsWithValidation<StockDataSyncSettings>(configuration);

        return services;
    }

    private static IServiceCollection AddOptionsWithValidation<TOptions>(this IServiceCollection services,
        IConfiguration configuration) where TOptions : class
    {
        var sectionName = GetSectionName<TOptions>();

        services.Configure<TOptions>(configuration.GetSection(sectionName));

        services.AddOptions<TOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    public static IServiceCollection AddSettingsSummary(this IServiceCollection services)
    {
        services.AddSingleton<ISettingsSummaryService, SettingsSummaryService>();
        return services;
    }

    private static string GetSectionName<TOptions>()
    {
        var type = typeof(TOptions);
        var field = type.GetField("SectionName");
        return field?.GetValue(null)?.ToString() ?? type.Name.Replace("Settings", "");
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
            ["Application"] = GetApplicationSummary(),
            ["Features"] = GetFeaturesSummary(),
            ["ExternalServices"] = GetExternalServicesSummary(),
            ["Security"] = GetSecuritySummary()
        };
    }

    private object GetApplicationSummary()
    {
        return new
        {
            Name = _configuration["Application:Name"],
            Version = _configuration["Application:Version"],
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            BaseUrl = _configuration["Application:BaseUrl"]
        };
    }

    private object GetFeaturesSummary()
    {
        return new
        {
            RealTimeData = _configuration.GetValue<bool>("Application:Features:EnableRealTimeData"),
            Swagger = _configuration.GetValue<bool>("Application:Features:EnableSwagger"),
            DetailedLogging = _configuration.GetValue<bool>("Application:Features:EnableDetailedLogging"),
            HealthChecks = _configuration.GetValue<bool>("Application:Features:EnableHealthChecks")
        };
    }

    private object GetExternalServicesSummary()
    {
        return new
        {
            KoreaInvestmentConfigured = !string.IsNullOrEmpty(_configuration["KoreaInvestment:BaseUrl"]),
            KrxApiConfigured = !string.IsNullOrEmpty(_configuration["KrxApi:BaseUrl"]),
            DatabaseConfigured = !string.IsNullOrEmpty(_configuration.GetConnectionString("DefaultConnection"))
        };
    }

    private object GetSecuritySummary()
    {
        return new
        {
            JwtConfigured = !string.IsNullOrEmpty(_configuration["JwtSettings:Key"]),
            GoogleAuthConfigured = !string.IsNullOrEmpty(_configuration["Authentication:Google:ClientId"]),
            EncryptionConfigured = !string.IsNullOrEmpty(_configuration["Encryption:Key"])
        };
    }
}