using OrderFlow.Contracts;
using OrderFlow.Orders.Api.Systems.Saga;
using Xunit;

namespace OrderFlow.IntegrationTests;

public class OrderSagaTests : IClassFixture<SagaTestFixture>
{
    private readonly SagaTestFixture _fixture;

    public OrderSagaTests(SagaTestFixture fixture) => _fixture = fixture;

    private static OrderSubmitted Submit(Guid orderId, string productName) => new()
    {
        OrderId = orderId,
        CustomerName = "Test Customer",
        SubmittedAt = DateTimeOffset.UtcNow,
        Items = new[] { new OrderLine { ProductName = productName, Quantity = 1 } },
    };

    [Fact]
    public async Task HappyPath_InStockOrder_ReachesConfirmed()
    {
        var orderId = Guid.NewGuid();

        await _fixture.Bus.Publish(Submit(orderId, "Widget"));

        var confirmed = await SagaTestFixture.WaitUntilAsync(
            () => _fixture.Sink.Confirmed.Any(x => x.OrderId == orderId));
        Assert.True(confirmed, "OrderConfirmed не был опубликован сагой");

        Assert.Equal("Confirmed", await _fixture.WaitForSagaStateAsync(orderId, "Confirmed"));
    }

    [Fact]
    public async Task OutOfStockOrder_ReachesCancelled()
    {
        var orderId = Guid.NewGuid();

        await _fixture.Bus.Publish(Submit(orderId, "OUT_OF_STOCK"));

        var cancelled = await SagaTestFixture.WaitUntilAsync(
            () => _fixture.Sink.Cancelled.Any(x => x.OrderId == orderId));
        Assert.True(cancelled, "OrderCancelled не был опубликован сагой");

        Assert.Equal("Cancelled", await _fixture.WaitForSagaStateAsync(orderId, "Cancelled"));
    }

    [Fact]
    public async Task Timeout_TriggersCompensation_ReleaseStockAndCancel()
    {
        var orderId = Guid.NewGuid();

        // Inventory «молчит» → сага зависает в AwaitingStockReservation.
        await _fixture.Bus.Publish(Submit(orderId, "NO_RESPONSE"));
        Assert.Equal("AwaitingStockReservation",
            await _fixture.WaitForSagaStateAsync(orderId, "AwaitingStockReservation"));

        // Симулируем срабатывание таймаута (в проде это делает ReservationTimeoutSweeper).
        await _fixture.Bus.Publish(new ReservationTimedOut { OrderId = orderId });

        var released = await SagaTestFixture.WaitUntilAsync(
            () => _fixture.Sink.Released.Any(x => x.OrderId == orderId));
        Assert.True(released, "Компенсация ReleaseStock не была опубликована");

        var cancelled = await SagaTestFixture.WaitUntilAsync(
            () => _fixture.Sink.Cancelled.Any(x => x.OrderId == orderId));
        Assert.True(cancelled, "OrderCancelled не был опубликован после таймаута");

        Assert.Equal("Cancelled", await _fixture.WaitForSagaStateAsync(orderId, "Cancelled"));
    }
}
