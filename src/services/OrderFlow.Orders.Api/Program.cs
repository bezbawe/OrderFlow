using OrderFlow.Orders.Api.Contracts;
using OrderFlow.Orders.Api.Entities;
using OrderFlow.Orders.Api.Repository.Migrator;
using OrderFlow.Orders.Api.Systems;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddOrdersServices(connectionString);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

await app.Services.ApplyMigrationsAsync();

app.MapPost("/orders", async (CreateOrderRequest request, IOrderSystem orderSystem) =>
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

    var created = await orderSystem.OrderSubsystem.Create(order);
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
