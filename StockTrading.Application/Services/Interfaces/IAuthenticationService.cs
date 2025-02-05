using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using stock_trading_backend;

namespace StockTradingBackend.DataAccess.Interfaces;

public interface IAuthenticationService
{
    AuthenticationProperties ConfigureGoogleAuth();
    // Task<AuthResponse> LoginAsync(LoginRequest request);
    // Task<AuthResponse> RefreshTokenAsync(string refreshToken);
    // Task<AuthResponse> HandleGoogleCallbackAsync(ClaimsPrincipal principal);
}