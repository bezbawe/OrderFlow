using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OrderFlow.Orders.Api.Contracts;

public class CreateOrderRequest
{
    [Required]
    [StringLength(300)]
    [JsonPropertyName("customer_name")]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [JsonPropertyName("items")]
    public List<CreateOrderItemRequest> Items { get; set; } = [];
}

public class CreateOrderItemRequest
{
    [Required]
    [StringLength(300)]
    [JsonPropertyName("product_name")]
    public string ProductName { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    [JsonPropertyName("unit_price")]
    public decimal UnitPrice { get; set; }
}
