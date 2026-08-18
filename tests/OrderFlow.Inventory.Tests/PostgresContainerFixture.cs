using Microsoft.EntityFrameworkCore;
using OrderFlow.Inventory.Api.Repository.DbContext;
using OrderFlow.Inventory.Api.Repository.Implementations;
using OrderFlow.Inventory.Api.Systems;
using Testcontainers.PostgreSql;
using Xunit;

namespace OrderFlow.Inventory.Tests;

// Реальный Postgres в контейнере + миграции Inventory. Подсистема и репозитории собираются
// на свежем DbContext на каждую операцию — как отдельный scope на каждое сообщение в проде.
public class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public InventoryDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new InventoryDbContext(options);
    }

    // Свежий DbContext + реальные репозитории + реальная подсистема — как в проде на один scope.
    public (IStockSubsystem Subsystem, InventoryDbContext Db) CreateSubsystem()
    {
        var db = CreateDbContext();
        var subsystem = new StockSubsystem(new ProductRepository(db), new StockReservationRepository(db));
        return (subsystem, db);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
