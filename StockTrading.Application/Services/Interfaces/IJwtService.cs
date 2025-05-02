using System.Security.Claims;
using StockTrading.DataAccess.DTOs;
using StockTradingBackend.DataAccess.Entities;

namespace StockTradingBackend.DataAccess.Interfaces;

public interface IJwtService
{
    string GenerateToken(UserDto user);
}