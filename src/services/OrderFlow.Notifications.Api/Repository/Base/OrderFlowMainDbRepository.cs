using OrderFlow.Notifications.Api.Entities;
using OrderFlow.Notifications.Api.Repository.DbContext;

namespace OrderFlow.Notifications.Api.Repository.Base;

public class OrderFlowMainDbRepository<T> : OrderFlowBaseRepository<T, NotificationsDbContext>
    where T : BaseOrderFlowEntity
{
    public OrderFlowMainDbRepository(NotificationsDbContext dbContext) : base(dbContext)
    {
    }
}
