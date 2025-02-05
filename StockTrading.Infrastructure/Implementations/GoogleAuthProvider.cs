using System.Security.Claims;
using stock_trading_backend;
using StockTrading.DataAccess.Services.Interfaces;

namespace StockTrading.Infrastructure.Implementations;

public class GoogleAuthProvider : IGoogleAuthProvider
{
    private readonly IHttpClientFactory _httpClientFactory;

    public Task<GoogleUserInfo> GetUserInfoAsync(ClaimsPrincipal principal)
    {
        var email = principal.FindFirst(ClaimTypes.Email).Value;
        var name = principal.FindFirst(ClaimTypes.Name).Value;

        var googleUser = new GoogleUserInfo
        {
            Email = email,
            Name = name
        };

        return Task.FromResult(googleUser);
    }
}