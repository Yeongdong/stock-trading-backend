using StockTrading.Domain.Entities;
using StockTrading.Domain.Enums;

namespace StockTrading.Application.Repositories;

public interface IStockRepository : IBaseRepository<Stock, string>
{
    Task<List<Stock>> SearchByNameAsync(string searchTerm, int page = 1, int pageSize = 20);
    Task<Stock?> GetByCodeAsync(string code);
    Task<List<Stock>> GetByMarketAsync(Market market);
    Task<bool> BulkUpsertAsync(List<Stock> stocks);
    Task<DateTime?> GetLastUpdatedAsync();
    Task<int> GetTotalCountAsync();
}