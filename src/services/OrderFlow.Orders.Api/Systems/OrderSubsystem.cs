using OrderFlow.Orders.Api.Entities;
using OrderFlow.Orders.Api.Repository.Interfaces;

namespace OrderFlow.Orders.Api.Systems;

public class OrderSubsystem : IOrderSubsystem
{
    private readonly IOrderRepository _orderRepository;

    public OrderSubsystem(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Order> Create(Order entity)
    {
        entity.TotalAmount = entity.Items.Sum(item => item.Quantity * item.UnitPrice);
        return await _orderRepository.AddAsync(entity);
    }

    public async Task<Order?> GetById(Guid id)
    {
        return await _orderRepository.GetByIdWithItemsAsync(id);
    }

    public async Task<IList<Order>> GetAll()
    {
        return await _orderRepository.GetAllOrdered();
    }

    public async Task DeleteById(Guid id)
    {
        await _orderRepository.RemoveAsync(id);
    }
}
