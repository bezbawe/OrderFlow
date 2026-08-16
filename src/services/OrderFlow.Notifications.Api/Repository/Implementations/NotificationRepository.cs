using OrderFlow.Notifications.Api.Entities;
using OrderFlow.Notifications.Api.Repository.Base;
using OrderFlow.Notifications.Api.Repository.DbContext;
using OrderFlow.Notifications.Api.Repository.Interfaces;

namespace OrderFlow.Notifications.Api.Repository.Implementations;

public class NotificationRepository(NotificationsDbContext dbContext)
    : OrderFlowMainDbRepository<Notification>(dbContext), INotificationRepository
{
}
