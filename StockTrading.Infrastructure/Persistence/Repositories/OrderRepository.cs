using StockTrading.Application.DTOs.Common;
using StockTrading.Application.Repositories;
using StockTrading.Domain.Entities;
using StockTrading.Infrastructure.Persistence.Contexts;

namespace StockTrading.Infrastructure.Persistence.Repositories;

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