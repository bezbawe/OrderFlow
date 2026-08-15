using MassTransit;
using OrderFlow.Contracts;
using OrderFlow.Orders.Api.Entities;
using OrderFlow.Orders.Api.Repository.Interfaces;

namespace OrderFlow.Orders.Api.Systems.Consumers;

// Обновляет статус заказа, когда сага подтвердила его.
public class OrderConfirmedConsumer : IConsumer<OrderConfirmed>
{
    private readonly IOrderRepository _orderRepository;

    public OrderConfirmedConsumer(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task Consume(ConsumeContext<OrderConfirmed> context)
    {
        var order = await _orderRepository.GetByIdAsync(context.Message.OrderId);
        if (order is null)
        {
            return;
        }

        order.Status = OrderStatus.Confirmed;
        await _orderRepository.UpdateAsync(order);
    }
}
