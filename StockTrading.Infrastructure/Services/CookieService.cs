using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using StockTrading.Application.Services;
using StockTrading.Domain.Settings;

namespace StockTrading.Infrastructure.Services;

public class CookieService : ICookieService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly JwtSettings _jwtSettings;

    private const string AuthCookieName = "auth_token";

    public CookieService(IHttpContextAccessor httpContextAccessor, IOptions<JwtSettings> jwtSettings)
    {
        _httpContextAccessor = httpContextAccessor;
        _jwtSettings = jwtSettings.Value;
    }

    public void SetAuthCookie(string token)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return;

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            Path = "/"
        };

        context.Response.Cookies.Append(AuthCookieName, token, cookieOptions);
    }

    public void DeleteAuthCookie()
    {
        var context = _httpContextAccessor.HttpContext;

        context?.Response.Cookies.Delete(AuthCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/"
        });
    }

    public string? GetAuthToken()
    {
        var context = _httpContextAccessor.HttpContext;
        return context?.Request.Cookies.TryGetValue(AuthCookieName, out var token) == true ? token : null;
    }
}