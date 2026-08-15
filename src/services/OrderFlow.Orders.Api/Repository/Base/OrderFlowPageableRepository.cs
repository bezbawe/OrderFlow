using OrderFlow.Orders.Api.Entities;
using OrderFlow.Orders.Api.Repository.DbContext;

namespace OrderFlow.Orders.Api.Repository.Base;

public interface IOrderFlowPageableRepository<T> : IOrderFlowBaseRepository<T> where T : BaseOrderFlowEntity
{
    (IList<T> Items, int TotalCount) GetPageByFilter(string searchString, int pageIndex, int pageSize,
        IQueryable<T>? entities = null);
}

public abstract class OrderFlowPageableRepository<T>(OrdersDbContext dbContext) : OrderFlowMainDbRepository<T>(dbContext) where T : BaseOrderFlowEntity
{
    public virtual (IList<T> Items, int TotalCount) GetPageByFilter(string searchString, int pageIndex, int pageSize, IQueryable<T>? entities = null)
    {
        var query = ApplySearchFilter(entities ?? db.Set<T>(), searchString);
        var items = query.OrderByDescending(x => x.Id).Skip(pageIndex * pageSize).Take(pageSize).ToList();
        return (items, query.Count());
    }

    protected virtual IQueryable<T> ApplySearchFilter(IQueryable<T> query, string searchString) => query;
}
