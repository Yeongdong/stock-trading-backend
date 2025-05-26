using Microsoft.EntityFrameworkCore.Storage;
using StockTrading.Application.Repositories;

namespace StockTrading.Infrastructure.Services;

public class DbTransactionWrapper: IDbTransactionWrapper
{
    private readonly IDbContextTransaction _transaction;

    public DbTransactionWrapper(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }
    
    public Task CommitAsync() => _transaction.CommitAsync();
    
    public Task RollbackAsync() => _transaction.RollbackAsync();
    
    public ValueTask DisposeAsync() => _transaction.DisposeAsync();
}