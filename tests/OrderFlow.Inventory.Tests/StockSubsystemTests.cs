using Microsoft.EntityFrameworkCore;
using OrderFlow.Contracts;
using OrderFlow.Inventory.Api.Entities;
using OrderFlow.Inventory.Api.Systems;
using Xunit;

namespace OrderFlow.Inventory.Tests;

public class StockSubsystemTests : IClassFixture<PostgresContainerFixture>
{
    private readonly PostgresContainerFixture _fixture;

    public StockSubsystemTests(PostgresContainerFixture fixture) => _fixture = fixture;

    // Уникальное имя товара на каждый тест — товары изолированы (у Name уникальный индекс).
    private static string UniqueName() => $"P-{Guid.NewGuid():N}";

    private async Task SeedProductAsync(string name, int quantity)
    {
        await using var db = _fixture.CreateDbContext();
        db.Products.Add(new Product { Id = Guid.NewGuid(), Name = name, AvailableQuantity = quantity });
        await db.SaveChangesAsync();
    }

    private async Task<int> GetQuantityAsync(string name)
    {
        await using var db = _fixture.CreateDbContext();
        return (await db.Products.FirstAsync(p => p.Name == name)).AvailableQuantity;
    }

    private static IReadOnlyList<OrderLine> Line(string name, int qty) =>
        new[] { new OrderLine { ProductName = name, Quantity = qty } };

    [Fact]
    public async Task ReserveAsync_WithSufficientStock_DecrementsAndCreatesReservation()
    {
        var name = UniqueName();
        await SeedProductAsync(name, 10);
        var orderId = Guid.NewGuid();

        var (subsystem, db) = _fixture.CreateSubsystem();
        await using (db)
        {
            var result = await subsystem.ReserveAsync(orderId, Line(name, 3));
            Assert.True(result.Success);
        }

        Assert.Equal(7, await GetQuantityAsync(name));

        await using var verify = _fixture.CreateDbContext();
        var reservation = await verify.StockReservations.Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.OrderId == orderId);
        Assert.NotNull(reservation);
        Assert.Equal(StockReservationStatus.Reserved, reservation!.Status);
        Assert.Equal(3, reservation.Lines.Single().Quantity);
    }

    [Fact]
    public async Task ReserveAsync_WithInsufficientStock_Fails_AndDoesNotDecrement()
    {
        var name = UniqueName();
        await SeedProductAsync(name, 2);
        var orderId = Guid.NewGuid();

        var (subsystem, db) = _fixture.CreateSubsystem();
        StockReservationResult result;
        await using (db)
        {
            result = await subsystem.ReserveAsync(orderId, Line(name, 5));
        }

        Assert.False(result.Success);
        Assert.Contains("Insufficient", result.FailureReason);
        Assert.Equal(2, await GetQuantityAsync(name));

        await using var verify = _fixture.CreateDbContext();
        Assert.False(await verify.StockReservations.AnyAsync(r => r.OrderId == orderId));
    }

    [Fact]
    public async Task ReserveAsync_CalledTwiceForSameOrder_IsIdempotent()
    {
        var name = UniqueName();
        await SeedProductAsync(name, 10);
        var orderId = Guid.NewGuid();

        var (first, db1) = _fixture.CreateSubsystem();
        await using (db1) await first.ReserveAsync(orderId, Line(name, 3));

        var (second, db2) = _fixture.CreateSubsystem();
        await using (db2)
        {
            var result = await second.ReserveAsync(orderId, Line(name, 3));
            Assert.True(result.Success);
        }

        // Списание произошло только один раз.
        Assert.Equal(7, await GetQuantityAsync(name));
    }

    [Fact]
    public async Task ReleaseAsync_RestoresStock_AndMarksReleased()
    {
        var name = UniqueName();
        await SeedProductAsync(name, 10);
        var orderId = Guid.NewGuid();

        var (reserve, db1) = _fixture.CreateSubsystem();
        await using (db1) await reserve.ReserveAsync(orderId, Line(name, 4));

        var (release, db2) = _fixture.CreateSubsystem();
        await using (db2) await release.ReleaseAsync(orderId);

        Assert.Equal(10, await GetQuantityAsync(name));

        await using var verify = _fixture.CreateDbContext();
        var reservation = await verify.StockReservations.FirstAsync(r => r.OrderId == orderId);
        Assert.Equal(StockReservationStatus.Released, reservation.Status);
    }

    [Fact]
    public async Task ReleaseAsync_CalledTwice_RestoresStockOnlyOnce()
    {
        var name = UniqueName();
        await SeedProductAsync(name, 10);
        var orderId = Guid.NewGuid();

        var (reserve, db1) = _fixture.CreateSubsystem();
        await using (db1) await reserve.ReserveAsync(orderId, Line(name, 4));

        var (release1, db2) = _fixture.CreateSubsystem();
        await using (db2) await release1.ReleaseAsync(orderId);

        var (release2, db3) = _fixture.CreateSubsystem();
        await using (db3) await release2.ReleaseAsync(orderId);

        // Повторный release — no-op, остаток не «раздувается».
        Assert.Equal(10, await GetQuantityAsync(name));
    }
}
