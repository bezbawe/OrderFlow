using MassTransit;
using OrderFlow.Contracts;
using OrderFlow.Orders.Api.Contracts;
using OrderFlow.Orders.Api.Entities;
using OrderFlow.Orders.Api.Repository.DbContext;
using OrderFlow.Orders.Api.Repository.Migrator;
using OrderFlow.Orders.Api.Systems;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

var rabbitMqHost = builder.Configuration["RabbitMq:Host"]
    ?? throw new InvalidOperationException("'RabbitMq:Host' is not configured.");
var rabbitMqUsername = builder.Configuration["RabbitMq:Username"]
    ?? throw new InvalidOperationException("'RabbitMq:Username' is not configured.");
var rabbitMqPassword = builder.Configuration["RabbitMq:Password"]
    ?? throw new InvalidOperationException("'RabbitMq:Password' is not configured.");

builder.Services.AddOrdersServices(connectionString, rabbitMqHost, rabbitMqUsername, rabbitMqPassword);

// DB-подключение проверяется явно; MassTransit сам добавляет проверку шины (RabbitMQ),
// когда в контейнере есть health checks — оба статуса отдаёт эндпоинт /health.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrdersDbContext>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

await app.Services.ApplyMigrationsAsync();

app.MapHealthChecks("/health");

app.MapPost("/orders", async (CreateOrderRequest request, IOrderSystem orderSystem, IPublishEndpoint publishEndpoint, OrdersDbContext db) =>
{
    var order = new Order
    {
        CustomerName = request.CustomerName.Trim(),
        Items = request.Items.Select(item => new OrderItem
        {
            ProductName = item.ProductName.Trim(),
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
        }).ToList(),
    };

    // Bus outbox лишь трекает сообщение на этом DbContext — физически он сохраняется
    // только следующим SaveChangesAsync. Оборачиваем создание заказа и publish в одну
    // транзакцию, чтобы запись заказа и постановка события в outbox были атомарны.
    await using var transaction = await db.Database.BeginTransactionAsync();

    var created = await orderSystem.OrderSubsystem.Create(order);

    await publishEndpoint.Publish(new OrderSubmitted
    {
        OrderId = created.Id,
        CustomerName = created.CustomerName,
        SubmittedAt = created.DateCreated,
        Items = created.Items
            .Select(item => new OrderLine { ProductName = item.ProductName, Quantity = item.Quantity })
            .ToList(),
    });
    await db.SaveChangesAsync();

    await transaction.CommitAsync();

    return Results.Created($"/orders/{created.Id}", created);
});

app.MapGet("/orders", async (IOrderSystem orderSystem) =>
{
    var orders = await orderSystem.OrderSubsystem.GetAll();
    return Results.Ok(orders);
});

app.MapGet("/orders/{id:guid}", async (Guid id, IOrderSystem orderSystem) =>
{
    var order = await orderSystem.OrderSubsystem.GetById(id);
    return order is not null ? Results.Ok(order) : Results.NotFound();
});

app.MapDelete("/orders/{id:guid}", async (Guid id, IOrderSystem orderSystem) =>
{
    await orderSystem.OrderSubsystem.DeleteById(id);
    return Results.NoContent();
});

app.Run();
