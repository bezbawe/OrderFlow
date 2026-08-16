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
            // In-memory outbox откладывает publish/send до успешного завершения consume
            // (после сохранения саги) — снимает гонку publish-до-commit.
            x.AddConfigureEndpointsCallback((context, _, cfg) => cfg.UseInMemoryOutbox(context));

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
                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddHostedService<ReservationTimeoutSweeper>();

        return services;
    }
}
