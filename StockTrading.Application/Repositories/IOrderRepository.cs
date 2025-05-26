using StockTrading.Application.DTOs.Common;
using StockTrading.Domain.Entities;

namespace StockTrading.Application.Repositories;

public interface IOrderRepository
{
    Task<StockOrder> SaveAsync(StockOrder order, UserDto user);
}