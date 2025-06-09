using Microsoft.EntityFrameworkCore.Storage;
using StockTrading.Application.Common.Interfaces;

namespace StockTrading.Infrastructure.Services.Common;

public class AutoRollbackTransactionWrapper : IDbTransactionWrapper
{
    private readonly IDbContextTransaction _transaction;
    private bool _committed;

    public AutoRollbackTransactionWrapper(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public async Task CommitAsync()
    {
        await _transaction.CommitAsync();
        _committed = true;
    }

    public Task RollbackAsync() => _transaction.RollbackAsync();

    public async ValueTask DisposeAsync()
    {
        if (!_committed)
        {
            await _transaction.RollbackAsync();
        }
        await _transaction.DisposeAsync();
    }
}