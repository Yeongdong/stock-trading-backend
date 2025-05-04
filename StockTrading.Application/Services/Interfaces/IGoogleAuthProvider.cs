using System.Security.Claims;
using StockTrading.DataAccess.DTOs;

namespace StockTrading.DataAccess.Services.Interfaces;

public interface IGoogleAuthProvider
{
    Task<GoogleUserInfo> GetUserInfoAsync(ClaimsPrincipal principal);
}