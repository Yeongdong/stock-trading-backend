using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace StockTrading.Tests.Integration.Configuration;

/// <summary>
/// CSRF 설정 담당
/// </summary>
public static class AntiforgeryConfigurator
{
    public static void ConfigureForTesting(IServiceCollection services)
    {
        services.AddMvc(options =>
        {
            for (int i = options.Filters.Count - 1; i >= 0; i--)
            {
                var filter = options.Filters[i];
                if (filter is AutoValidateAntiforgeryTokenAttribute ||
                    (filter is ServiceFilterAttribute serviceFilter &&
                     serviceFilter.ServiceType == typeof(AutoValidateAntiforgeryTokenAttribute)))
                    options.Filters.RemoveAt(i);
            }
        });

        services.Configure<AntiforgeryOptions>(options =>
        {
            options.Cookie.SecurePolicy = CookieSecurePolicy.None;
            options.Cookie.SameSite = SameSiteMode.None;
        });
    }
}