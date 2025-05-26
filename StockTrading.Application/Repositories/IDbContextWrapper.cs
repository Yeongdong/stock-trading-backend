namespace StockTrading.Application.Repositories;

public interface IDbContextWrapper
{
    Task<IDbTransactionWrapper> BeginTransactionAsync();
}