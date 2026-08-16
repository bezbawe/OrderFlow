using MassTransit;
using OrderFlow.Orders.Api.Repository.DbContext;
using OrderFlow.Orders.Api.Repository.Implementations;
using OrderFlow.Orders.Api.Repository.Interfaces;
using OrderFlow.Orders.Api.Systems.Consumers;
using OrderFlow.Orders.Api.Systems.Saga;

namespace OrderFlow.Orders.Api.Systems;

public static class OrdersServiceCollectionExtensions
{
    public static IServiceCollection AddOrdersServices(this IServiceCollection services, string connectionString, string rabbitMqHost, string rabbitMqUsername, string rabbitMqPassword)
    {
        services.AddOrdersDbContext(connectionString);

        services.AddScoped<IOrderRepository, OrderRepository>();

        services.AddScoped<IOrderSubsystem, OrderSubsystem>();
        services.AddScoped<IOrderSystem, OrderSystem>();

        services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<OrdersDbContext>(o =>
            {
                o.UsePostgres();
                // Bus outbox — для publish вне consume-контекста (POST /orders публикует OrderSubmitted
                // в том же DbContext-scope, что и создание заказа — классический dual-write).
                o.UseBusOutbox();
            });

            // Receive-endpoint outbox — откладывает publish/send консьюмера (в т.ч. саги) до успешного
            // сохранения в БД, и дедуплицирует повторную доставку по MessageId (inbox).
            x.AddConfigureEndpointsCallback((context, _, cfg) => cfg.UseEntityFrameworkOutbox<OrdersDbContext>(context));

            x.AddConsumer<OrderConfirmedConsumer>();
            x.AddConsumer<OrderCancelledConsumer>();

            x.AddSagaStateMachine<OrderStateMachine, OrderStateInstance>()
                .EntityFrameworkRepository(r =>
                {
                    r.ConcurrencyMode = ConcurrencyMode.Pessimistic;
                    r.ExistingDbContext<OrdersDbContext>();
                    r.UsePostgres();
                });

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

        services.AddHostedService<ReservationTimeoutSweeper>();

        return services;
    }
}
