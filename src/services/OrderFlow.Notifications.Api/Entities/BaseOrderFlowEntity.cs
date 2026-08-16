using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Notifications.Api.Entities;

public class BaseOrderFlowEntity
{
    [Key]
    public Guid Id { get; set; }
}
