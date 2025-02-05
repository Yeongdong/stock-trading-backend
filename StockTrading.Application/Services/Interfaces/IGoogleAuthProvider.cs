using System.Security.Claims;
using stock_trading_backend;

namespace StockTrading.DataAccess.Services.Interfaces;

public interface IGoogleAuthProvider
{
    Task<GoogleUserInfo> GetUserInfoAsync(ClaimsPrincipal principal);
}