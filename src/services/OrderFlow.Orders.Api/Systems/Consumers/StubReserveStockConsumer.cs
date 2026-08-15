using MassTransit;
using OrderFlow.Contracts;

namespace OrderFlow.Orders.Api.Systems.Consumers;

// ВРЕМЕННЫЙ stub. В задаче 4 резерв переезжает в реальный Inventory-сервис,
// и этот consumer удаляется из Orders.
public class StubReserveStockConsumer : IConsumer<ReserveStock>
{
    public async Task Consume(ConsumeContext<ReserveStock> context)
    {
        await context.Publish(new StockReserved
        {
            OrderId = context.Message.OrderId,
        });
    }
}
