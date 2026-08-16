using MassTransit;
using OrderFlow.Contracts;

namespace OrderFlow.Inventory.Api.Systems.Consumers;

public class ReserveStockConsumer : IConsumer<ReserveStock>
{
    private readonly IStockSubsystem _stockSubsystem;

    public ReserveStockConsumer(IStockSubsystem stockSubsystem)
    {
        _stockSubsystem = stockSubsystem;
    }

    public async Task Consume(ConsumeContext<ReserveStock> context)
    {
        var result = await _stockSubsystem.ReserveAsync(context.Message.OrderId, context.Message.Items);

        if (result.Success)
        {
            await context.Publish(new StockReserved { OrderId = context.Message.OrderId });
        }
        else
        {
            await context.Publish(new StockReservationFailed
            {
                OrderId = context.Message.OrderId,
                Reason = result.FailureReason ?? "Insufficient stock.",
            });
        }
    }
}
