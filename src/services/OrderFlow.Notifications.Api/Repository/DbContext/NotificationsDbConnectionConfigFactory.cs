using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace OrderFlow.Notifications.Api.Repository.DbContext;

public static class NotificationsDbConnectionConfigFactory
{
    public static IServiceCollection AddNotificationsDbContext(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<NotificationsDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }
}
