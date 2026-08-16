using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Notifications.Api.Entities;

namespace OrderFlow.Notifications.Api.Repository.DbContext;

public class NotificationsDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public NotificationsDbContext()
    {
    }

    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : base(options)
    {
    }

    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddTransactionalOutboxEntities();

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasIndex(e => e.OrderId);
        });
    }
}
