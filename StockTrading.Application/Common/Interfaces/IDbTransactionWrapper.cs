namespace StockTrading.Application.Common.Interfaces;

public interface IDbTransactionWrapper
{
    Task CommitAsync();
    Task RollbackAsync();
    ValueTask DisposeAsync();
}