namespace StockTrading.Application.Common.Interfaces;

public interface IBaseRepository<TEntity, TKey> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(TKey id);
    Task<TEntity> AddAsync(TEntity entity);
    Task<TEntity> UpdateAsync(TEntity entity);
    Task DeleteAsync(TKey id);
    Task<bool> ExistsAsync(TKey id);
    Task<List<TEntity>> GetAllAsync();
}