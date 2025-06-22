using System.Security.Claims;
using StockTrading.Application.Features.Users.DTOs;

namespace StockTrading.Application.Features.Auth.Services;

public interface IJwtService
{
    string GenerateAccessToken(UserInfo user);
    (string refreshToken, DateTime expiryDate) GenerateRefreshToken();
    ClaimsPrincipal ValidateToken(string token);
    int? GetUserIdFromToken(string token);
}