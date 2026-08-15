using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OrderFlow.Orders.Api.Entities;

public class OrderItem : BaseOrderFlowEntity
{
    public Guid OrderId { get; set; }

    [JsonIgnore]
    public Order? Order { get; set; }

    [Required]
    [StringLength(300)]
    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }
}
