using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace OrderFlow.Orders.Api.Repository.DbContext;

public static class OrdersDbConnectionConfigFactory
{
    public static IServiceCollection AddOrdersDbContext(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<OrdersDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }
}
