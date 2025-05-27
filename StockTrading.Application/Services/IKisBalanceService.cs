using StockTrading.Application.DTOs.Trading.Portfolio;
using StockTrading.Application.DTOs.Users;

namespace StockTrading.Application.Services;

public interface IKisBalanceService
{
    Task<AccountBalance> GetStockBalanceAsync(UserInfo userInfo);
}