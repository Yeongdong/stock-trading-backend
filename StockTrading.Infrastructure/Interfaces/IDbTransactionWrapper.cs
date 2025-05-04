namespace StockTrading.Infrastructure.Interfaces;

public interface IDbTransactionWrapper
{
    Task CommitAsync();
    Task RollbackAsync();
    ValueTask DisposeAsync();
}