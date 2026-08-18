using Microsoft.EntityFrameworkCore;
using OrderFlow.Orders.Api.Repository.DbContext;
using Testcontainers.PostgreSql;
using Xunit;

namespace OrderFlow.Orders.Tests;

// Поднимает реальный Postgres в контейнере на время тестов и накатывает миграции Orders.
// Каждый тест берёт свежий DbContext (как отдельный scope в проде), чтобы не ловить
// устаревшее состояние трекера между операциями.
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

    public OrdersDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new OrdersDbContext(options);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
