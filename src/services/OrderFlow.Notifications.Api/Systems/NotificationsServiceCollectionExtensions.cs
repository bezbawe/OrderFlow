using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Notifications.Api.Repository.DbContext;
using OrderFlow.Notifications.Api.Repository.Implementations;
using OrderFlow.Notifications.Api.Repository.Interfaces;
using OrderFlow.Notifications.Api.Systems.Consumers;

namespace OrderFlow.Notifications.Api.Systems;

public static class NotificationsServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationsServices(this IServiceCollection services, string connectionString, string rabbitMqHost, string rabbitMqUsername, string rabbitMqPassword, string smtpHost, int smtpPort)
    {
        services.AddNotificationsDbContext(connectionString);

        services.AddScoped<INotificationRepository, NotificationRepository>();

        services.AddScoped<INotificationSubsystem>(sp => new NotificationSubsystem(
            sp.GetRequiredService<INotificationRepository>(), smtpHost, smtpPort));

        services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<NotificationsDbContext>(o => o.UsePostgres());

            // Notifications ничего не публикует — outbox тут только ради inbox-дедупликации
            // по MessageId: повторная доставка OrderConfirmed/OrderCancelled не шлёт письмо дважды.
            x.AddConfigureEndpointsCallback((context, _, cfg) => cfg.UseEntityFrameworkOutbox<NotificationsDbContext>(context));

            x.AddConsumer<OrderConfirmedConsumer>();
            x.AddConsumer<OrderCancelledConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqHost, "/", h =>
                {
                    h.Username(rabbitMqUsername);
                    h.Password(rabbitMqPassword);
                });
                // Имя очереди по умолчанию берётся из имени класса-консьюмера — без namespace
                // консьюмеры с одинаковым именем в разных сервисах (напр. OrderConfirmedConsumer
                // в Orders и Notifications) окажутся на одной очереди и будут конкурировать.
                cfg.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter(includeNamespace: true));
            });
        });

        return services;
    }
}
