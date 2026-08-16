using MassTransit;
using OrderFlow.Contracts;
using OrderFlow.Orders.Api.Entities;
using OrderFlow.Orders.Api.Repository.Interfaces;

namespace OrderFlow.Orders.Api.Systems.Consumers;

// Обновляет статус заказа, когда сага отменила его (отказ резерва или таймаут).
public class OrderCancelledConsumer : IConsumer<OrderCancelled>
{
    private readonly IOrderRepository _orderRepository;

    public OrderCancelledConsumer(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task Consume(ConsumeContext<OrderCancelled> context)
    {
        var order = await _orderRepository.GetByIdAsync(context.Message.OrderId);
        if (order is null)
        {
            return;
        }

        order.Status = OrderStatus.Cancelled;
        await _orderRepository.UpdateAsync(order);
    }
}
