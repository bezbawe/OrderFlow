using OrderFlow.Orders.Api.Entities;
using OrderFlow.Orders.Api.Repository.Base;

namespace OrderFlow.Orders.Api.Repository.Interfaces;

public interface IOrderRepository : IOrderFlowBaseRepository<Order>
{
    Task<List<Order>> GetAllOrdered();
    Task<Order?> GetByIdWithItemsAsync(Guid id);
}
