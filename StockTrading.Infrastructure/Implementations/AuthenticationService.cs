using Microsoft.AspNetCore.Authentication;
using IAuthenticationService = StockTradingBackend.DataAccess.Interfaces.IAuthenticationService;

namespace StockTrading.Infrastructure.Implementations;

public class AuthenticationService : IAuthenticationService
{
    public AuthenticationService()
    {
    }

    public AuthenticationProperties ConfigureGoogleAuth()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = "/api/auth/oauth2/callback/google",
            IsPersistent = true,
            AllowRefresh = true,
        };
        
        var state = Guid.NewGuid().ToString();
        properties.Items["state"] = state;
        
        Console.WriteLine($"Generated state: {state}");
        
        return properties;
    }
}