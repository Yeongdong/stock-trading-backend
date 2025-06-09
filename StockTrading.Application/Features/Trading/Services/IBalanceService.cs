using StockTrading.Application.Features.Trading.DTOs.Portfolio;
using StockTrading.Application.Features.Users.DTOs;

namespace StockTrading.Application.Features.Trading.Services;

public interface IBalanceService
{
    Task<AccountBalance> GetStockBalanceAsync(UserInfo userInfo);
}