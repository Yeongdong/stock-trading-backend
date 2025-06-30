using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockTrading.Application.ExternalServices;
using StockTrading.Domain.Settings.ExternalServices;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Auth;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Trading;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Market;
using StockTrading.Infrastructure.ExternalServices.KRX;

namespace StockTrading.Infrastructure.Configuration.ServiceConfiguration;

public static class ExternalServiceConfiguration
{
    public static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
    {
        var kisSettings = configuration.GetSection(KoreaInvestmentSettings.SectionName).Get<KoreaInvestmentSettings>();
        var krxSettings = configuration.GetSection(KrxApiSettings.SectionName).Get<KrxApiSettings>();

        AddKoreaInvestmentClients(services, kisSettings);
        AddKrxClient(services, krxSettings);

        return services;
    }

    private static void AddKoreaInvestmentClients(IServiceCollection services, KoreaInvestmentSettings? settings)
    {
        if (settings?.BaseUrl == null) return;

        // Trading 관련 클라이언트들
        services.AddHttpClient<KisOrderApiClient>(client =>
            ConfigureKisHttpClient(client, settings.BaseUrl));

        services.AddHttpClient<KisBalanceApiClient>(client =>
            ConfigureKisHttpClient(client, settings.BaseUrl));

        // 해외 주식 Trading 관련 클라이언트
        services.AddHttpClient<KisOverseasTradingApiClient>(client =>
            ConfigureKisHttpClient(client, settings.BaseUrl));

        // Market 관련 클라이언트들
        services.AddHttpClient<KisPriceApiClient>(client =>
            ConfigureKisHttpClient(client, settings.BaseUrl));

        // 인터페이스 등록
        services.AddScoped<IKisOrderApiClient>(provider =>
            provider.GetRequiredService<KisOrderApiClient>());

        services.AddScoped<IKisBalanceApiClient>(provider =>
            provider.GetRequiredService<KisBalanceApiClient>());

        services.AddScoped<IKisPriceApiClient>(provider =>
            provider.GetRequiredService<KisPriceApiClient>());

        // === 해외 주식 인터페이스 ===
        services.AddScoped<IKisOverseasTradingApiClient>(provider =>
            provider.GetRequiredService<KisOverseasTradingApiClient>());

        // 공통 서비스들
        services.AddHttpClient(nameof(KisTokenService), client =>
            ConfigureKisHttpClient(client, settings.BaseUrl));
    }

    private static void AddKrxClient(IServiceCollection services, KrxApiSettings? settings)
    {
        if (settings?.BaseUrl == null) return;

        services.AddHttpClient<KrxApiClient>(client =>
        {
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        });
    }

    private static void ConfigureKisHttpClient(HttpClient client, string baseUrl)
    {
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("User-Agent", "StockTradingApp/1.0");
    }

    public static IServiceCollection AddExternalServiceHealthChecks(this IServiceCollection services,
        IConfiguration configuration)
    {
        var kisSettings = configuration.GetSection(KoreaInvestmentSettings.SectionName).Get<KoreaInvestmentSettings>();
        var krxSettings = configuration.GetSection(KrxApiSettings.SectionName).Get<KrxApiSettings>();

        var healthChecks = services.AddHealthChecks();

        if (!string.IsNullOrEmpty(kisSettings?.BaseUrl))
            healthChecks.AddUrlGroup(new Uri(kisSettings.BaseUrl), "korea-investment-api");

        if (!string.IsNullOrEmpty(krxSettings?.BaseUrl))
            healthChecks.AddUrlGroup(new Uri(krxSettings.BaseUrl), "krx-api");

        return services;
    }
}