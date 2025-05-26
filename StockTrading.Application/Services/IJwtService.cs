using System.Security.Claims;
using StockTrading.Application.DTOs.Common;

namespace StockTrading.Application.Services;

public interface IJwtService
{
    string GenerateToken(UserDto user);
    ClaimsPrincipal ValidateToken(string token);
}