using StockTrading.Application.Features.Users.DTOs;

namespace StockTrading.Application.Features.Users.Services;

public interface IKisTokenRefreshService
{
    Task<bool> EnsureValidTokenAsync(UserInfo user);
}