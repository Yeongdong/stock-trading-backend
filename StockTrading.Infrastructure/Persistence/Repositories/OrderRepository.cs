using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockTrading.Application.Repositories;
using StockTrading.Domain.Entities;
using StockTrading.Infrastructure.Persistence.Contexts;

namespace StockTrading.Infrastructure.Persistence.Repositories;

public class OrderRepository : BaseRepository<StockOrder, int>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext context, ILogger<OrderRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<List<StockOrder>> GetByUserIdAsync(int userId)
    {
        return await DbSet
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.Id)
            .ToListAsync();
    }

    public async Task<List<StockOrder>> GetByStockCodeAsync(string stockCode)
    {
        Logger.LogDebug("종목별 주문 내역 조회: {StockCode}", stockCode);

        return await DbSet
            .Where(o => o.StockCode == stockCode)
            .Include(o => o.User)
            .OrderByDescending(o => o.Id)
            .ToListAsync();
    }
}