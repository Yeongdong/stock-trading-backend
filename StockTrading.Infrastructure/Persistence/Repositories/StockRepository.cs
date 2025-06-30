using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockTrading.Application.Features.Market.Repositories;
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
        if (stocks.Count == 0) return false;

        Logger.LogInformation("대량 업데이트 시작: {Count}개 종목 처리", stocks.Count);

        // 모든 종목 코드를 한 번의 쿼리로 조회
        var stockCodes = stocks.Select(s => s.Code).ToList();
        var existingStocks = await DbSet
            .Where(s => stockCodes.Contains(s.Code))
            .ToDictionaryAsync(s => s.Code, s => s);

        var updateCount = 0;
        var insertCount = 0;

        foreach (var stock in stocks)
        {
            if (existingStocks.TryGetValue(stock.Code, out var existing))
            {
                existing.UpdateInfo(
                    name: stock.Name,
                    fullName: stock.FullName,
                    sector: stock.Sector,
                    market: stock.Market,
                    currency: stock.Currency,
                    englishName: stock.EnglishName,
                    stockType: stock.StockType,
                    parValue: stock.ParValue,
                    listedShares: stock.ListedShares,
                    listedDate: stock.ListedDate
                );
                updateCount++;
            }
            else
            {
                await DbSet.AddAsync(stock);
                insertCount++;
            }
        }

        var affectedRows = await Context.SaveChangesAsync();
        Logger.LogInformation("대량 업데이트 완료: 업데이트 {UpdateCount}개, 삽입 {InsertCount}개", updateCount, insertCount);

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