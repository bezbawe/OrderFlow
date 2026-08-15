using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Orders.Api.Entities;

public class BaseOrderFlowEntity
{
    [Key]
    public Guid Id { get; set; }
}
