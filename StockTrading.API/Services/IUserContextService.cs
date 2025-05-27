using StockTrading.Application.DTOs.Users;

namespace StockTrading.API.Services;

public interface IUserContextService
{
    Task<UserInfo> GetCurrentUserAsync();
}