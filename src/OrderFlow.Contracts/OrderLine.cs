namespace OrderFlow.Contracts;

public record OrderLine
{
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
}
