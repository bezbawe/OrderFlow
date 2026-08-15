namespace OrderFlow.Contracts;

// Публикуется Orders при создании заказа — запускает сагу.
public record OrderSubmitted
{
    public Guid OrderId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; init; }
    public IReadOnlyList<OrderLine> Items { get; init; } = [];
}

// Публикуется сагой, когда резерв прошёл успешно.
public record OrderConfirmed
{
    public Guid OrderId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
}

// Публикуется сагой при откате (компенсация).
public record OrderCancelled
{
    public Guid OrderId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}
