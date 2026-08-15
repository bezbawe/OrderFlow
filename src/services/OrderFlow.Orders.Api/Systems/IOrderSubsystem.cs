using OrderFlow.Orders.Api.Entities;

namespace OrderFlow.Orders.Api.Systems;

public interface IOrderSubsystem
{
    Task<Order> Create(Order entity);
    Task<Order?> GetById(Guid id);
    Task<IList<Order>> GetAll();
    Task DeleteById(Guid id);
}
