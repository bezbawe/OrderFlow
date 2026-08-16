using MassTransit;
using OrderFlow.Contracts;

namespace OrderFlow.Notifications.Api.Systems.Consumers;

public class OrderConfirmedConsumer : IConsumer<OrderConfirmed>
{
    private readonly INotificationSubsystem _notificationSubsystem;

    public OrderConfirmedConsumer(INotificationSubsystem notificationSubsystem)
    {
        _notificationSubsystem = notificationSubsystem;
    }

    public async Task Consume(ConsumeContext<OrderConfirmed> context)
    {
        await _notificationSubsystem.NotifyAsync(context.Message.OrderId, context.Message.CustomerName, OrderNotificationKind.Confirmed);
    }
}
