using OrderFlow.Orders.Api.Entities;
using OrderFlow.Orders.Api.Repository.DbContext;

namespace OrderFlow.Orders.Api.Repository.Base;

public class OrderFlowMainDbRepository<T> : OrderFlowBaseRepository<T, OrdersDbContext>
    where T : BaseOrderFlowEntity
{
    public OrderFlowMainDbRepository(OrdersDbContext dbContext) : base(dbContext)
    {
    }
}
