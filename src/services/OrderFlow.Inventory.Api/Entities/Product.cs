using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Inventory.Api.Entities;

public class Product : BaseOrderFlowEntity
{
    [Required]
    [StringLength(300)]
    public string Name { get; set; } = string.Empty;

    public int AvailableQuantity { get; set; }
}
