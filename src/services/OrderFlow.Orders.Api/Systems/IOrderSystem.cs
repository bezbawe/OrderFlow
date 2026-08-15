namespace OrderFlow.Orders.Api.Systems;

public interface IOrderSystem
{
    IOrderSubsystem OrderSubsystem { get; }
}
