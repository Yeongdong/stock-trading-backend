using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockTrading.Application.Repositories;
using StockTrading.Domain.Entities;
using StockTrading.Domain.Enums;
using StockTrading.Infrastructure.Persistence.Contexts;

namespace StockTrading.Infrastructure.Persistence.Repositories;

public class StockRepository : BaseRepository<Stock, string>, IStockRepository
{
    public StockRepository(ApplicationDbContext context, ILogger<StockRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<Stock?> GetByCodeAsync(string code)
    {
        return await DbSet.FirstOrDefaultAsync(s => s.Code == code);
    }

    public async Task<List<Stock>> SearchByNameAsync(string searchTerm, int page = 1, int pageSize = 20)
    {
        Logger.LogDebug("종목명 검색: {SearchTerm}, 페이지: {Page}, 크기: {PageSize}", searchTerm, page, pageSize);

        var query = DbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(s => s.Name.Contains(searchTerm) || s.Code.Contains(searchTerm));

        return await query
            .OrderBy(s => s.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<Stock>> GetByMarketAsync(Market market)
    {
        return await DbSet
            .Where(s => s.Market == market)
            .OrderBy(s => s.Code)
            .ToListAsync();
    }

    public async Task<bool> BulkUpsertAsync(List<Stock> stocks)
    {
        foreach (var stock in stocks)
        {
            var existing = await DbSet.FindAsync(stock.Code);
            
            if (existing != null)
                existing.UpdateInfo(
                    name: stock.Name,
                    fullName: stock.FullName,
                    sector: stock.Sector,
                    market: stock.Market,
                    englishName: stock.EnglishName,
                    stockType: stock.StockType,
                    parValue: stock.ParValue,
                    listedShares: stock.ListedShares,
                    listedDate: stock.ListedDate
                );
            else
                await DbSet.AddAsync(stock);
        }
    
        var affectedRows = await Context.SaveChangesAsync();
        Logger.LogInformation("대량 업데이트 완료: {AffectedRows}개 행 처리", affectedRows);
    
        return true;
    }

    public async Task<DateTime?> GetLastUpdatedAsync()
    {
        return await DbSet.MaxAsync(s => (DateTime?)s.LastUpdated);
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await DbSet.CountAsync();
    }
}