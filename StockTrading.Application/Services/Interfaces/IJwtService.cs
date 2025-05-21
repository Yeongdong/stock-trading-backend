using System.Security.Claims;
using StockTrading.DataAccess.DTOs;

namespace StockTrading.DataAccess.Services.Interfaces;

public interface IJwtService
{
    string GenerateToken(UserDto user);
    ClaimsPrincipal ValidateToken(string token);
}