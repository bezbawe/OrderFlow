namespace OrderFlow.Contracts;

public enum OrderNotificationKind
{
    Confirmed,
    Cancelled,
}

// Команда сага → Notifications: отправить уведомление о статусе заказа.
public record SendOrderNotification
{
    public Guid OrderId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public OrderNotificationKind Kind { get; init; }
}
