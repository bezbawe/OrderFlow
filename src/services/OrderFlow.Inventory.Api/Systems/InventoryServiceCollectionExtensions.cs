using MassTransit;
using OrderFlow.Inventory.Api.Repository.DbContext;
using OrderFlow.Inventory.Api.Repository.Implementations;
using OrderFlow.Inventory.Api.Repository.Interfaces;
using OrderFlow.Inventory.Api.Systems.Consumers;

namespace OrderFlow.Inventory.Api.Systems;

public static class InventoryServiceCollectionExtensions
{
    public static IServiceCollection AddInventoryServices(this IServiceCollection services, string connectionString, string rabbitMqHost, string rabbitMqUsername, string rabbitMqPassword)
    {
        services.AddInventoryDbContext(connectionString);

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IStockReservationRepository, StockReservationRepository>();

        services.AddScoped<IStockSubsystem, StockSubsystem>();

        services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<InventoryDbContext>(o => o.UsePostgres());

            // Откладывает publish консьюмера до успешного сохранения в БД и дедуплицирует
            // повторную доставку по MessageId (inbox) — резерв/списание идемпотентны.
            x.AddConfigureEndpointsCallback((context, _, cfg) => cfg.UseEntityFrameworkOutbox<InventoryDbContext>(context));

            x.AddConsumer<ReserveStockConsumer>();
            x.AddConsumer<ReleaseStockConsumer>();

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
