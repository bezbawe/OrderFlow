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
            x.AddConfigureEndpointsCallback((context, _, cfg) => cfg.UseInMemoryOutbox(context));

            x.AddConsumer<ReserveStockConsumer>();
            x.AddConsumer<ReleaseStockConsumer>();

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

        return services;
    }
}
