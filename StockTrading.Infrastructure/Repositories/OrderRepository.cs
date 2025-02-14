using StockTrading.DataAccess.Repositories;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.Infrastructure.Repositories;

public class OrderRepository: IOrderRepository
{
    private readonly ApplicationDbContext _context;

    public OrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<StockOrder> SaveAsync(StockOrder order)
    {
        throw new NotImplementedException();
    }
}