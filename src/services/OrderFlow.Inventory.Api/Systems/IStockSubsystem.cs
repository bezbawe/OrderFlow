using OrderFlow.Contracts;

namespace OrderFlow.Inventory.Api.Systems;

public record StockReservationResult(bool Success, string? FailureReason);

public interface IStockSubsystem
{
    Task<StockReservationResult> ReserveAsync(Guid orderId, IReadOnlyList<OrderLine> items);
    Task ReleaseAsync(Guid orderId);
}
