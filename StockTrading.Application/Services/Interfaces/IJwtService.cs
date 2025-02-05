using System.Security.Claims;
using StockTradingBackend.DataAccess.Entities;

namespace StockTradingBackend.DataAccess.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
    (string token, DateTime expiryDate) GenerateRefreshToken();
    ClaimsPrincipal ValidateToken(string token);
}