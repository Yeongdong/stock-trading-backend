using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment;
using StockTrading.Infrastructure.Implementations;

namespace StockTrading.Tests.Integration.Configuration;

/// <summary>
/// HTTP 클라이언트 설정 담당
/// </summary>
public static class HttpClientConfigurator
{
    public static void RegisterKisHttpClients(IServiceCollection services, IConfiguration testConfiguration,
        string baseUrl)
    {
        services.AddHttpClient(nameof(KisTokenService), client =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new MockHttpMessageHandler(testConfiguration));

        services.AddHttpClient(nameof(KisApiClient), client =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new MockHttpMessageHandler(testConfiguration));
    }
}