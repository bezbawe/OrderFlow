using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Inventory.Api.Entities;

public class BaseOrderFlowEntity
{
    [Key]
    public Guid Id { get; set; }
}
