using MassTransit;
using OrderFlow.Contracts;

namespace OrderFlow.Orders.Api.Systems.Saga;

public class OrderStateMachine : MassTransitStateMachine<OrderStateInstance>
{
    public State AwaitingStockReservation { get; private set; } = null!;
    public State Confirmed { get; private set; } = null!;

    public Event<OrderSubmitted> OrderSubmitted { get; private set; } = null!;
    public Event<StockReserved> StockReserved { get; private set; } = null!;

    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => OrderSubmitted, x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => StockReserved, x => x.CorrelateById(m => m.Message.OrderId));

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
                .TransitionTo(Confirmed));
    }
}
