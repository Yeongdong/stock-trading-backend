using StockTrading.Application.DTOs.Common;
using StockTrading.Application.DTOs.Stocks;

namespace StockTrading.Application.Services;

public interface IKisBalanceService
{
    Task<StockBalance> GetStockBalanceAsync(UserDto userDto);
}