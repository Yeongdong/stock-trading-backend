using StockTrading.DataAccess.DTOs;

namespace stock_trading_backend.Services;

public interface IUserContextService
{
    Task<UserDto> GetCurrentUserAsync();
}