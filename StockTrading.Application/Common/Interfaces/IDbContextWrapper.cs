namespace StockTrading.Application.Common.Interfaces;

public interface IDbContextWrapper
{
    Task<IDbTransactionWrapper> BeginTransactionAsync();
}