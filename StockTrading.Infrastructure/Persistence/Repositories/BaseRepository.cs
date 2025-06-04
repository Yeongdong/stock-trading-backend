using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockTrading.Application.Repositories;
using StockTrading.Infrastructure.Persistence.Contexts;

namespace StockTrading.Infrastructure.Persistence.Repositories;

public abstract class BaseRepository<TEntity, TKey> : IBaseRepository<TEntity, TKey> 
    where TEntity : class
{
    protected readonly ApplicationDbContext Context;
    protected readonly ILogger Logger;
    protected readonly DbSet<TEntity> DbSet;

    protected BaseRepository(ApplicationDbContext context, ILogger logger)
    {
        Context = context;
        Logger = logger;
        DbSet = context.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(TKey id)
    {
        return await DbSet.FindAsync(id);
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity)
    {
        var entry = await DbSet.AddAsync(entity);
        await Context.SaveChangesAsync();
        return entry.Entity;
    }

    public virtual async Task<TEntity> UpdateAsync(TEntity entity)
    {
        DbSet.Update(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task DeleteAsync(TKey id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null)
        {
            throw new KeyNotFoundException($"ID {id}에 해당하는 엔티티를 찾을 수 없습니다.");
        }
        
        DbSet.Remove(entity);
        await Context.SaveChangesAsync();
    }

    public virtual async Task<bool> ExistsAsync(TKey id)
    {
        return await DbSet.FindAsync(id) != null;
    }
}