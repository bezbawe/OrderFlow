using MassTransit;
using OrderFlow.Contracts;

namespace OrderFlow.Orders.Api.Systems.Saga;

public class OrderStateMachine : MassTransitStateMachine<OrderStateInstance>
{
    public State AwaitingStockReservation { get; private set; } = null!;
    public State Confirmed { get; private set; } = null!;
    public State Cancelled { get; private set; } = null!;

    public Event<OrderSubmitted> OrderSubmitted { get; private set; } = null!;
    public Event<StockReserved> StockReserved { get; private set; } = null!;
    public Event<StockReservationFailed> StockReservationFailed { get; private set; } = null!;
    public Event<ReservationTimedOut> ReservationTimedOut { get; private set; } = null!;

    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => OrderSubmitted, x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => StockReserved, x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => StockReservationFailed, x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => ReservationTimedOut, x => x.CorrelateById(m => m.Message.OrderId));

        Initially(
            When(OrderSubmitted)
                .Then(ctx =>
                {
                    ctx.Saga.CustomerName = ctx.Message.CustomerName;
                    ctx.Saga.SubmittedAt = ctx.Message.SubmittedAt;
                })
                .Publish(ctx => new ReserveStock
                {
                    OrderId = ctx.Saga.CorrelationId,
                    Items = ctx.Message.Items,
                })
                .TransitionTo(AwaitingStockReservation));

        During(AwaitingStockReservation,
            When(StockReserved)
                .Publish(ctx => new OrderConfirmed
                {
                    OrderId = ctx.Saga.CorrelationId,
                    CustomerName = ctx.Saga.CustomerName,
                })
                .TransitionTo(Confirmed),

            When(StockReservationFailed)
                .Publish(ctx => new OrderCancelled
                {
                    OrderId = ctx.Saga.CorrelationId,
                    CustomerName = ctx.Saga.CustomerName,
                    Reason = ctx.Message.Reason,
                })
                .TransitionTo(Cancelled),

            // Компенсация: резерв мог пройти, но ответ потерялся — освобождаем на всякий случай.
            When(ReservationTimedOut)
                .Publish(ctx => new ReleaseStock { OrderId = ctx.Saga.CorrelationId })
                .Publish(ctx => new OrderCancelled
                {
                    OrderId = ctx.Saga.CorrelationId,
                    CustomerName = ctx.Saga.CustomerName,
                    Reason = "Stock reservation timed out.",
                })
                .TransitionTo(Cancelled));

        // Заказ уже отменён (по таймауту), но исходный ReserveStock всё же был обработан
        // Inventory уже после отмены — опоздавший StockReserved компенсируется повторным
        // ReleaseStock. Без этого сообщение падает в _error как unhandled event.
        During(Cancelled,
            When(StockReserved)
                .Publish(ctx => new ReleaseStock { OrderId = ctx.Saga.CorrelationId }),
            Ignore(StockReservationFailed),
            Ignore(ReservationTimedOut));

        // Защита от дублирующей доставки (at-least-once) уже после подтверждения заказа.
        During(Confirmed,
            Ignore(StockReserved),
            Ignore(ReservationTimedOut));
    }
}
