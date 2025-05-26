namespace StockTrading.Application.Repositories;

public interface IDbTransactionWrapper
{
    Task CommitAsync();
    Task RollbackAsync();
    ValueTask DisposeAsync();
}