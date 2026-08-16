using Microsoft.EntityFrameworkCore;
using OrderFlow.Inventory.Api.Entities;

namespace OrderFlow.Inventory.Api.Repository.Base;

public interface IOrderFlowBaseRepository<T> where T : BaseOrderFlowEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<List<T>?> GetAll();
    Task<T> AddAsync(T entity);
    Task<T> AddOrUpdateAsync(T entity);
    Task AddRangeAsync(List<T> entities);
    Task RemoveAsync(Guid id);
    Task RemoveRangeAsync(IEnumerable<T> entity);
    Task UpdateAsync(T entity);
    Task UpdateRangeAsync(List<T> entities);
    void Attach(T entity);
    Task<int> GetCountAsync();
}

public abstract class OrderFlowBaseRepository<T, TDbContext> : IOrderFlowBaseRepository<T>
    where T : BaseOrderFlowEntity
    where TDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    protected TDbContext db;

    protected OrderFlowBaseRepository(TDbContext db)
    {
        this.db = db;
    }

    public void Attach(T entity) => db.Attach(entity);

    public virtual async Task<T> AddAsync(T entity)
    {
        entity.Id = Guid.NewGuid();
        db.Set<T>().Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task<T> AddOrUpdateAsync(T entity)
    {
        entity.Id = Guid.NewGuid();
        db.Set<T>().Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task AddRangeAsync(List<T> entities)
    {
        entities.ForEach(p => p.Id = Guid.NewGuid());
        await db.Set<T>().AddRangeAsync(entities);
        await db.SaveChangesAsync();
    }

    public async Task<List<T>?> GetAll()
    {
        return await db.Set<T>().ToListAsync();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await db.Set<T>().FindAsync(id);
    }

    public virtual async Task RemoveAsync(Guid id)
    {
        var entityToRemove = db.Set<T>().Find(id);
        if (entityToRemove != null)
        {
            db.Set<T>().Remove(entityToRemove);
            await db.SaveChangesAsync();
        }
    }

    public virtual async Task RemoveRangeAsync(IEnumerable<T> entity)
    {
        db.Set<T>().RemoveRange(entity);
        await db.SaveChangesAsync();
    }

    public virtual async Task UpdateAsync(T entity)
    {
        db.Set<T>().Update(entity);
        await db.SaveChangesAsync();
    }

    public async Task UpdateRangeAsync(List<T> entities)
    {
        db.Set<T>().UpdateRange(entities);
        await db.SaveChangesAsync();
    }

    public async Task<int> GetCountAsync()
    {
        return await db.Set<T>().CountAsync();
    }
}
