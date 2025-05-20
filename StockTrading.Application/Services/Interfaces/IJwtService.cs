using StockTrading.DataAccess.DTOs;

namespace StockTrading.DataAccess.Services.Interfaces;

public interface IJwtService
{
    string GenerateToken(UserDto user);
}