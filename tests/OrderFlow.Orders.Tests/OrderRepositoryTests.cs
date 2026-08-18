using Microsoft.EntityFrameworkCore;
using OrderFlow.Orders.Api.Entities;
using OrderFlow.Orders.Api.Repository.Implementations;
using Xunit;

namespace OrderFlow.Orders.Tests;

public class OrderRepositoryTests : IClassFixture<PostgresContainerFixture>
{
    private readonly PostgresContainerFixture _fixture;

    public OrderRepositoryTests(PostgresContainerFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task AddAsync_PersistsOrder_AndAssignsId()
    {
        await using var db = _fixture.CreateDbContext();
        var repo = new OrderRepository(db);
        var order = new Order
        {
            CustomerName = "Alice",
            Items = { new OrderItem { ProductName = "Widget", Quantity = 2, UnitPrice = 9.99m } },
        };

        var created = await repo.AddAsync(order);

        Assert.NotEqual(Guid.Empty, created.Id);

        await using var verify = _fixture.CreateDbContext();
        var fetched = await verify.Orders.FindAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Alice", fetched!.CustomerName);
    }

    [Fact]
    public async Task GetByIdWithItemsAsync_ReturnsOrderWithItems()
    {
        Guid orderId;
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new OrderRepository(db);
            var created = await repo.AddAsync(new Order
            {
                CustomerName = "Bob",
                Items =
                {
                    new OrderItem { ProductName = "Widget", Quantity = 1, UnitPrice = 5m },
                    new OrderItem { ProductName = "Gadget", Quantity = 3, UnitPrice = 2m },
                },
            });
            orderId = created.Id;
        }

        await using var verify = _fixture.CreateDbContext();
        var repo2 = new OrderRepository(verify);
        var order = await repo2.GetByIdWithItemsAsync(orderId);

        Assert.NotNull(order);
        Assert.Equal(2, order!.Items.Count);
    }

    [Fact]
    public async Task GetAllOrdered_ReturnsNewestFirst()
    {
        var tag = Guid.NewGuid().ToString("N");
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new OrderRepository(db);
            await repo.AddAsync(new Order { CustomerName = $"older-{tag}", DateCreated = DateTimeOffset.UtcNow.AddMinutes(-10) });
            await repo.AddAsync(new Order { CustomerName = $"newer-{tag}", DateCreated = DateTimeOffset.UtcNow });
        }

        await using var verify = _fixture.CreateDbContext();
        var repo2 = new OrderRepository(verify);
        var mine = (await repo2.GetAllOrdered())
            .Where(o => o.CustomerName.EndsWith(tag))
            .ToList();

        Assert.Equal(2, mine.Count);
        Assert.Equal($"newer-{tag}", mine[0].CustomerName);
        Assert.Equal($"older-{tag}", mine[1].CustomerName);
    }

    [Fact]
    public async Task RemoveAsync_DeletesOrder_AndCascadesItems()
    {
        Guid orderId;
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new OrderRepository(db);
            var created = await repo.AddAsync(new Order
            {
                CustomerName = "Carol",
                Items = { new OrderItem { ProductName = "Widget", Quantity = 1, UnitPrice = 1m } },
            });
            orderId = created.Id;
        }

        await using (var db = _fixture.CreateDbContext())
        {
            await new OrderRepository(db).RemoveAsync(orderId);
        }

        await using var verify = _fixture.CreateDbContext();
        Assert.Null(await verify.Orders.FindAsync(orderId));
        Assert.Equal(0, await verify.OrderItems.CountAsync(i => i.OrderId == orderId));
    }
}
