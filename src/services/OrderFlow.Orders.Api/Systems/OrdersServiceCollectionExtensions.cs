using OrderFlow.Orders.Api.Repository.DbContext;
using OrderFlow.Orders.Api.Repository.Implementations;
using OrderFlow.Orders.Api.Repository.Interfaces;

namespace OrderFlow.Orders.Api.Systems;

public static class OrdersServiceCollectionExtensions
{
    public static IServiceCollection AddOrdersServices(this IServiceCollection services, string connectionString)
    {
        services.AddOrdersDbContext(connectionString);

        services.AddScoped<IOrderRepository, OrderRepository>();

        services.AddScoped<IOrderSubsystem, OrderSubsystem>();
        services.AddScoped<IOrderSystem, OrderSystem>();

        return services;
    }
}
