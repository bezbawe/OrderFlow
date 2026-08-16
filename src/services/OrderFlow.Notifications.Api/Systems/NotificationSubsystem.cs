using System.Net.Mail;
using OrderFlow.Contracts;
using OrderFlow.Notifications.Api.Entities;
using OrderFlow.Notifications.Api.Repository.Interfaces;

namespace OrderFlow.Notifications.Api.Systems;

public class NotificationSubsystem : INotificationSubsystem
{
    private readonly INotificationRepository _notificationRepository;
    private readonly string _smtpHost;
    private readonly int _smtpPort;

    public NotificationSubsystem(INotificationRepository notificationRepository, string smtpHost, int smtpPort)
    {
        _notificationRepository = notificationRepository;
        _smtpHost = smtpHost;
        _smtpPort = smtpPort;
    }

    public async Task NotifyAsync(Guid orderId, string customerName, OrderNotificationKind kind)
    {
        var (subject, body) = BuildMessage(orderId, customerName, kind);

        using var client = new SmtpClient(_smtpHost, _smtpPort);
        using var message = new MailMessage("orders@orderflow.local", "customer@orderflow.local", subject, body);
        await client.SendMailAsync(message);

        await _notificationRepository.AddAsync(new Notification
        {
            OrderId = orderId,
            CustomerName = customerName,
            Kind = kind,
            Subject = subject,
            Body = body,
        });
    }

    private static (string Subject, string Body) BuildMessage(Guid orderId, string customerName, OrderNotificationKind kind) => kind switch
    {
        OrderNotificationKind.Confirmed => (
            $"Order {orderId} confirmed",
            $"Hi {customerName}, your order {orderId} has been confirmed."),
        OrderNotificationKind.Cancelled => (
            $"Order {orderId} cancelled",
            $"Hi {customerName}, your order {orderId} has been cancelled."),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };
}
