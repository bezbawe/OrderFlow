namespace OrderFlow.Orders.Api.Entities;

public enum OrderStatus
{
    Submitted,
    AwaitingStockReservation,
    Confirmed,
    Cancelled,
}
