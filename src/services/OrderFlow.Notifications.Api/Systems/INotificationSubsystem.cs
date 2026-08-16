using OrderFlow.Contracts;

namespace OrderFlow.Notifications.Api.Systems;

public interface INotificationSubsystem
{
    Task NotifyAsync(Guid orderId, string customerName, OrderNotificationKind kind);
}
