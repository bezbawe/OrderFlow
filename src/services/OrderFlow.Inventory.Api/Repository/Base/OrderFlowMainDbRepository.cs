using OrderFlow.Inventory.Api.Entities;
using OrderFlow.Inventory.Api.Repository.DbContext;

namespace OrderFlow.Inventory.Api.Repository.Base;

public class OrderFlowMainDbRepository<T> : OrderFlowBaseRepository<T, InventoryDbContext>
    where T : BaseOrderFlowEntity
{
    public OrderFlowMainDbRepository(InventoryDbContext dbContext) : base(dbContext)
    {
    }
}
