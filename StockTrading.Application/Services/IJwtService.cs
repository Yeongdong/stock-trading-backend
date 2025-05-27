using System.Security.Claims;
using StockTrading.Application.DTOs.Users;

namespace StockTrading.Application.Services;

public interface IJwtService
{
    string GenerateToken(UserInfo user);
    ClaimsPrincipal ValidateToken(string token);
}