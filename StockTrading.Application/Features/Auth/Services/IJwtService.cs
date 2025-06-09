using System.Security.Claims;
using StockTrading.Application.Features.Users.DTOs;

namespace StockTrading.Application.Features.Auth.Services;

public interface IJwtService
{
    string GenerateToken(UserInfo user);
    ClaimsPrincipal ValidateToken(string token);
}