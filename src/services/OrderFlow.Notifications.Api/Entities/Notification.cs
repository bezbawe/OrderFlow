using System.ComponentModel.DataAnnotations;
using OrderFlow.Contracts;

namespace OrderFlow.Notifications.Api.Entities;

public class Notification : BaseOrderFlowEntity
{
    public Guid OrderId { get; set; }

    [Required]
    [StringLength(300)]
    public string CustomerName { get; set; } = string.Empty;

    public OrderNotificationKind Kind { get; set; }

    [Required]
    [StringLength(300)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Body { get; set; } = string.Empty;

    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;
}
