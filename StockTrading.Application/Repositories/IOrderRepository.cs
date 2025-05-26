using StockTrading.Domain.Entities;

namespace StockTrading.Application.Repositories;

public interface IOrderRepository : IBaseRepository<StockOrder, int>
{
    Task<List<StockOrder>> GetByUserIdAsync(int userId);
    Task<List<StockOrder>> GetByStockCodeAsync(string stockCode);
}