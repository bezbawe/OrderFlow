using System.Collections.Concurrent;
using MassTransit;
using OrderFlow.Contracts;

namespace OrderFlow.IntegrationTests;

// Собирает исходящие события саги, чтобы тесты могли на них дождаться и проверить.
public class SagaTestSink
{
    public ConcurrentBag<OrderConfirmed> Confirmed { get; } = new();
    public ConcurrentBag<OrderCancelled> Cancelled { get; } = new();
    public ConcurrentBag<ReleaseStock> Released { get; } = new();
}

// Заглушка Inventory: отвечает на ReserveStock в зависимости от имени товара.
// "OUT_OF_STOCK" → отказ, "NO_RESPONSE" → молчание (для сценария таймаута), иначе → успех.
public class FakeReserveStockConsumer : IConsumer<ReserveStock>
{
    public async Task Consume(ConsumeContext<ReserveStock> context)
    {
        var names = context.Message.Items.Select(item => item.ProductName).ToList();

        if (names.Contains("NO_RESPONSE"))
        {
            return;
        }

        if (names.Contains("OUT_OF_STOCK"))
        {
            await context.Publish(new StockReservationFailed
            {
                OrderId = context.Message.OrderId,
                Reason = "Insufficient stock.",
            });
            return;
        }

        await context.Publish(new StockReserved { OrderId = context.Message.OrderId });
    }
}

public class OrderConfirmedCaptureConsumer(SagaTestSink sink) : IConsumer<OrderConfirmed>
{
    public Task Consume(ConsumeContext<OrderConfirmed> context)
    {
        sink.Confirmed.Add(context.Message);
        return Task.CompletedTask;
    }
}

public class OrderCancelledCaptureConsumer(SagaTestSink sink) : IConsumer<OrderCancelled>
{
    public Task Consume(ConsumeContext<OrderCancelled> context)
    {
        sink.Cancelled.Add(context.Message);
        return Task.CompletedTask;
    }
}

public class ReleaseStockCaptureConsumer(SagaTestSink sink) : IConsumer<ReleaseStock>
{
    public Task Consume(ConsumeContext<ReleaseStock> context)
    {
        sink.Released.Add(context.Message);
        return Task.CompletedTask;
    }
}
