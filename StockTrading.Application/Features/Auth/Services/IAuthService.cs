using StockTrading.Application.Features.Auth.DTOs;

namespace StockTrading.Application.Features.Auth.Services;

public interface IAuthService
{
    Task<LoginResponse> GoogleLoginAsync(string credential);
    Task<RefreshTokenResponse> RefreshTokenAsync();
    Task LogoutAsync(int userId);
}