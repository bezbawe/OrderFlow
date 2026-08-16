namespace OrderFlow.Inventory.Api.Entities;

public class StockReservation : BaseOrderFlowEntity
{
    public Guid OrderId { get; set; }

    public StockReservationStatus Status { get; set; } = StockReservationStatus.Reserved;

    public DateTimeOffset ReservedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<StockReservationLine> Lines { get; set; } = [];
}
