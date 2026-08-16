using Microsoft.EntityFrameworkCore;
using OrderFlow.Inventory.Api.Entities;

namespace OrderFlow.Inventory.Api.Repository.DbContext;

public class InventoryDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public InventoryDbContext()
    {
    }

    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<StockReservation> StockReservations { get; set; }
    public DbSet<StockReservationLine> StockReservationLines { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();

            entity.HasData(
                new Product { Id = new Guid("11111111-1111-1111-1111-111111111111"), Name = "Widget", AvailableQuantity = 100 },
                new Product { Id = new Guid("22222222-2222-2222-2222-222222222222"), Name = "Gadget", AvailableQuantity = 2 },
                new Product { Id = new Guid("33333333-3333-3333-3333-333333333333"), Name = "Out Of Stock Item", AvailableQuantity = 0 }
            );
        });

        modelBuilder.Entity<StockReservation>(entity =>
        {
            entity.HasIndex(e => e.OrderId).IsUnique();
            entity.HasMany(e => e.Lines)
                .WithOne(e => e.StockReservation)
                .HasForeignKey(e => e.StockReservationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StockReservationLine>(entity =>
        {
            entity.HasIndex(e => e.StockReservationId);
        });
    }
}
