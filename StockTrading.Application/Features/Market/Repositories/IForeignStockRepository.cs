using StockTrading.Application.Common.Interfaces;
using StockTrading.Domain.Entities;

namespace StockTrading.Application.Features.Market.Repositories;

public interface IForeignStockRepository : IBaseRepository<ForeignStock, int>
{
    Task<List<ForeignStock>> SearchByTermAsync(string searchTerm, int limit);
    Task<ForeignStock?> GetBySymbolAsync(string symbol);
    Task<List<ForeignStock>> GetBySymbolsAsync(IEnumerable<string> symbols);
    Task AddRangeAsync(IEnumerable<ForeignStock> stocks);
}