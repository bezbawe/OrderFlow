using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace OrderFlow.Inventory.Api.Repository.DbContext;

public static class InventoryDbConnectionConfigFactory
{
    public static IServiceCollection AddInventoryDbContext(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<InventoryDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }
}
