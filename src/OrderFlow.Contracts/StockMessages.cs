namespace OrderFlow.Contracts;

// Команда Orders → Inventory: зарезервировать товар под заказ.
public record ReserveStock
{
    public Guid OrderId { get; init; }
    public IReadOnlyList<OrderLine> Items { get; init; } = [];
}

// Событие Inventory → сага: резерв успешен.
public record StockReserved
{
    public Guid OrderId { get; init; }
}

// Событие Inventory → сага: резерв не удался.
public record StockReservationFailed
{
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = string.Empty;
}

// Компенсирующая команда сага → Inventory: освободить резерв.
public record ReleaseStock
{
    public Guid OrderId { get; init; }
}
