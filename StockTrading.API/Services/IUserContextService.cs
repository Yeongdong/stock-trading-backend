using StockTrading.Application.DTOs.Common;

namespace StockTrading.API.Services;

public interface IUserContextService
{
    Task<UserDto> GetCurrentUserAsync();
}