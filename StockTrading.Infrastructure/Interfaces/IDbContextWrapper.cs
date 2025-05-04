namespace StockTrading.Infrastructure.Interfaces;

public interface IDbContextWrapper
{
    Task<IDbTransactionWrapper> BeginTransactionAsync();
}