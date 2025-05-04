using StockTrading.Infrastructure.Interfaces;
using StockTrading.Infrastructure.Repositories;

namespace StockTrading.Infrastructure.Implementations;

public class DbContextWrapper : IDbContextWrapper
{
    private readonly ApplicationDbContext _dbContext;

    public DbContextWrapper(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IDbTransactionWrapper> BeginTransactionAsync()
    {
        var transaction = await _dbContext.Database.BeginTransactionAsync();
        return new DbTransactionWrapper(transaction);
    }
}