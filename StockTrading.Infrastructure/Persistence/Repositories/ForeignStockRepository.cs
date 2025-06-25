// StockTrading.Infrastructure/Persistence/Repositories/ForeignStockRepository.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockTrading.Application.Features.Market.Repositories;
using StockTrading.Domain.Entities;
using StockTrading.Infrastructure.Persistence.Contexts;

namespace StockTrading.Infrastructure.Persistence.Repositories;

public class ForeignStockRepository : BaseRepository<ForeignStock, int>, IForeignStockRepository
{
    public ForeignStockRepository(ApplicationDbContext context, ILogger<ForeignStockRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<List<ForeignStock>> SearchByTermAsync(string searchTerm, int limit)
    {
        var searchTermLower = searchTerm.ToLower();
        
        return await DbSet
            .Where(s => s.Description.ToLower().Contains(searchTermLower) || 
                        s.Symbol.ToLower().Contains(searchTermLower))
            .OrderBy(s => s.Symbol.ToLower().StartsWith(searchTermLower) ? 0 : 1)
            .ThenBy(s => s.Symbol)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<ForeignStock?> GetBySymbolAsync(string symbol)
    {
        return await DbSet.FirstOrDefaultAsync(s => s.Symbol == symbol);
    }

    public async Task<List<ForeignStock>> GetBySymbolsAsync(IEnumerable<string> symbols)
    {
        return await DbSet
            .Where(s => symbols.Contains(s.Symbol))
            .ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<ForeignStock> stocks)
    {
        await DbSet.AddRangeAsync(stocks);
        await Context.SaveChangesAsync();
    }
}