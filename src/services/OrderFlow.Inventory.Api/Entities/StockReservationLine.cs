using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OrderFlow.Inventory.Api.Entities;

public class StockReservationLine : BaseOrderFlowEntity
{
    public Guid StockReservationId { get; set; }

    [JsonIgnore]
    public StockReservation? StockReservation { get; set; }

    [Required]
    [StringLength(300)]
    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }
}
