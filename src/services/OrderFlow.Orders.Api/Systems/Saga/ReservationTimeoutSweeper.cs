using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Orders.Api.Repository.DbContext;

namespace OrderFlow.Orders.Api.Systems.Saga;

// Периодически ищет заказы, застрявшие в AwaitingStockReservation дольше таймаута,
// и триггерит компенсацию через саму state machine (см. ReservationTimedOut).
// Простая альтернатива MassTransit message scheduler (не требует delayed-exchange
// плагина RabbitMQ или отдельного Quartz-сервиса).
public class ReservationTimeoutSweeper : BackgroundService
{
    private static readonly TimeSpan ReservationTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IBus _bus;

    public ReservationTimeoutSweeper(IServiceScopeFactory scopeFactory, IBus bus)
    {
        _scopeFactory = scopeFactory;
        _bus = bus;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

            var cutoff = DateTimeOffset.UtcNow - ReservationTimeout;
            var staleOrderIds = await db.Set<OrderStateInstance>()
                .Where(instance => instance.CurrentState == "AwaitingStockReservation" && instance.SubmittedAt < cutoff)
                .Select(instance => instance.CorrelationId)
                .ToListAsync(stoppingToken);

            foreach (var orderId in staleOrderIds)
            {
                await _bus.Publish(new ReservationTimedOut { OrderId = orderId }, stoppingToken);
            }
        }
    }
}
