using MassTransit;

namespace OrderFlow.Orders.Api.Systems.Saga;

public class OrderStateInstance : SagaStateMachineInstance
{
    // CorrelationId == OrderId — заказ и есть ключ корреляции саги.
    public Guid CorrelationId { get; set; }

    public string CurrentState { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public DateTimeOffset SubmittedAt { get; set; }
}
