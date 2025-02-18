using StockTrading.DataAccess.DTOs;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.DataAccess.Repositories;

public interface IOrderRepository
{
    Task<StockOrder> SaveAsync(StockOrder order, UserDto user);
}