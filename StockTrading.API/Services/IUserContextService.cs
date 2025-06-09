using StockTrading.Application.Features.Users.DTOs;

namespace StockTrading.API.Services;

public interface IUserContextService
{
    Task<UserInfo> GetCurrentUserAsync();
}