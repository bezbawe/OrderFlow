using MassTransit;
using OrderFlow.Contracts;

namespace OrderFlow.Notifications.Api.Systems.Consumers;

public class OrderCancelledConsumer : IConsumer<OrderCancelled>
{
    private readonly INotificationSubsystem _notificationSubsystem;

    public OrderCancelledConsumer(INotificationSubsystem notificationSubsystem)
    {
        _notificationSubsystem = notificationSubsystem;
    }

    public async Task Consume(ConsumeContext<OrderCancelled> context)
    {
        await _notificationSubsystem.NotifyAsync(context.Message.OrderId, context.Message.CustomerName, OrderNotificationKind.Cancelled);
    }
}
