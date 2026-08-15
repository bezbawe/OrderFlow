namespace OrderFlow.Orders.Api.Systems;

public class OrderSystem : IOrderSystem
{
    public IOrderSubsystem OrderSubsystem { get; }

    public OrderSystem(IOrderSubsystem orderSubsystem)
    {
        OrderSubsystem = orderSubsystem;
    }
}
