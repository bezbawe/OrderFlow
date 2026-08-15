using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Orders.Api.Entities;

public class Order : BaseOrderFlowEntity
{
    [Required]
    [StringLength(300)]
    public string CustomerName { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.Submitted;

    public decimal TotalAmount { get; set; }

    public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;

    public List<OrderItem> Items { get; set; } = [];
}
