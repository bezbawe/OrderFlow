using MassTransit;
using OrderFlow.Contracts;

namespace OrderFlow.Inventory.Api.Systems.Consumers;

public class ReleaseStockConsumer : IConsumer<ReleaseStock>
{
    private readonly IStockSubsystem _stockSubsystem;

    public ReleaseStockConsumer(IStockSubsystem stockSubsystem)
    {
        _stockSubsystem = stockSubsystem;
    }

    public async Task Consume(ConsumeContext<ReleaseStock> context)
    {
        await _stockSubsystem.ReleaseAsync(context.Message.OrderId);
    }
}
