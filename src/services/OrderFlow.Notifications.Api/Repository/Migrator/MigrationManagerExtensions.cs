using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Notifications.Api.Repository.DbContext;

namespace OrderFlow.Notifications.Api.Repository.Migrator;

public static class MigrationManagerExtensions
{
    public static async Task ApplyMigrationsAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);
        if (pendingMigrations.Any())
        {
            await context.Database.MigrateAsync(cancellationToken);
        }
    }
}
