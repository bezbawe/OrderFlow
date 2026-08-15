using Microsoft.EntityFrameworkCore;
using OrderFlow.Orders.Api.Entities;
using OrderFlow.Orders.Api.Systems.Saga;

namespace OrderFlow.Orders.Api.Repository.DbContext;

public class OrdersDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public OrdersDbContext()
    {
    }

    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OrderStateInstance>(entity =>
        {
            entity.HasKey(x => x.CorrelationId);
            entity.Property(x => x.CurrentState).HasMaxLength(64);
            entity.Property(x => x.CustomerName).HasMaxLength(300);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(e => e.DateCreated);
            entity.HasIndex(e => e.Status);
            entity.HasMany(e => e.Items)
                .WithOne(e => e.Order)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasIndex(e => e.OrderId);
        });
    }
}
