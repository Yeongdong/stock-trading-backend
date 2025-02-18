using StockTrading.DataAccess.DTOs;
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

    public async Task<StockOrder> SaveAsync(StockOrder order, UserDto user)
    {
        throw new NotImplementedException();
    }
}