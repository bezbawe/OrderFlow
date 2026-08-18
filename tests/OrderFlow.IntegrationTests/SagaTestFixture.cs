using System.Diagnostics;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Orders.Api.Repository.DbContext;
using OrderFlow.Orders.Api.Systems.Saga;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace OrderFlow.IntegrationTests;

// Поднимает реальные RabbitMQ + Postgres в контейнерах и запускает in-process шину MassTransit
// с настоящей сагой (EF saga repository на Postgres), заглушкой Inventory и захватывающими
// консьюмерами. Это и есть «сквозной» тест оркестрации через настоящий брокер.
public class SagaTestFixture : IAsyncLifetime
{
    // Не guest: RabbitMQ блокирует guest при подключении не с loopback (через проброшенный порт).
    private const string RabbitUser = "orderflow";
    private const string RabbitPass = "orderflow";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:3-management")
        .WithUsername(RabbitUser)
        .WithPassword(RabbitPass)
        .Build();

    private ServiceProvider _provider = null!;
    private IBusControl _busControl = null!;

    public IBus Bus => _busControl;
    public SagaTestSink Sink { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbitMq.StartAsync());

        var services = new ServiceCollection();
        services.AddSingleton<SagaTestSink>();
        services.AddDbContext<OrdersDbContext>(options => options.UseNpgsql(_postgres.GetConnectionString()));

        services.AddMassTransit(x =>
        {
            x.AddSagaStateMachine<OrderStateMachine, OrderStateInstance>()
                .EntityFrameworkRepository(r =>
                {
                    r.ConcurrencyMode = ConcurrencyMode.Pessimistic;
                    r.ExistingDbContext<OrdersDbContext>();
                    r.UsePostgres();
                });

            x.AddConsumer<FakeReserveStockConsumer>();
            x.AddConsumer<OrderConfirmedCaptureConsumer>();
            x.AddConsumer<OrderCancelledCaptureConsumer>();
            x.AddConsumer<ReleaseStockCaptureConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(_rabbitMq.Hostname, _rabbitMq.GetMappedPublicPort(5672), "/", h =>
                {
                    h.Username(RabbitUser);
                    h.Password(RabbitPass);
                });
                cfg.ConfigureEndpoints(context);
            });
        });

        _provider = services.BuildServiceProvider(true);

        using (var scope = _provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            await db.Database.MigrateAsync();
        }

        Sink = _provider.GetRequiredService<SagaTestSink>();
        _busControl = _provider.GetRequiredService<IBusControl>();
        await _busControl.StartAsync();
    }

    // Текущее состояние саги для заказа (null, если инстанс ещё не создан).
    public async Task<string?> GetSagaStateAsync(Guid orderId)
    {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var instance = await db.Set<OrderStateInstance>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.CorrelationId == orderId);
        return instance?.CurrentState;
    }

    // Ждём, пока сага для заказа окажется в нужном состоянии (или таймаут).
    public async Task<string?> WaitForSagaStateAsync(Guid orderId, string state, TimeSpan? timeout = null)
    {
        var deadline = Stopwatch.StartNew();
        var limit = timeout ?? TimeSpan.FromSeconds(30);
        string? current = null;
        while (deadline.Elapsed < limit)
        {
            current = await GetSagaStateAsync(orderId);
            if (current == state)
            {
                return current;
            }
            await Task.Delay(150);
        }
        return current;
    }

    public static async Task<bool> WaitUntilAsync(Func<bool> condition, TimeSpan? timeout = null)
    {
        var deadline = Stopwatch.StartNew();
        var limit = timeout ?? TimeSpan.FromSeconds(30);
        while (deadline.Elapsed < limit)
        {
            if (condition())
            {
                return true;
            }
            await Task.Delay(150);
        }
        return condition();
    }

    public async Task DisposeAsync()
    {
        if (_busControl is not null)
        {
            await _busControl.StopAsync();
        }
        if (_provider is not null)
        {
            await _provider.DisposeAsync();
        }
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _rabbitMq.DisposeAsync().AsTask());
    }
}
