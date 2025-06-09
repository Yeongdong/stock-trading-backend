using StockTrading.Application.DTOs.Users;

namespace StockTrading.Application.Services;

public interface IKisTokenRefreshService
{
    Task<bool> EnsureValidTokenAsync(UserInfo user);
}