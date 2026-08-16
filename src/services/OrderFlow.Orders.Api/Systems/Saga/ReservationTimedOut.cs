namespace OrderFlow.Orders.Api.Systems.Saga;

// Внутреннее сообщение саги: публикуется ReservationTimeoutSweeper, когда заказ
// слишком долго ждёт резерва — служит компенсирующим триггером (ReleaseStock на
// случай, если резерв всё же прошёл, но ответ потерялся).
public record ReservationTimedOut
{
    public Guid OrderId { get; init; }
}
