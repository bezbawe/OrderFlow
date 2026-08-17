using Microsoft.EntityFrameworkCore;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OrderFlow.Inventory.Api.Repository.DbContext;
using OrderFlow.Inventory.Api.Repository.Migrator;
using OrderFlow.Inventory.Api.Systems;

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

builder.Services.AddInventoryServices(connectionString, rabbitMqHost, rabbitMqUsername, rabbitMqPassword);

// DB-подключение проверяется явно; MassTransit сам добавляет проверку шины (RabbitMQ),
// когда в контейнере есть health checks — оба статуса отдаёт эндпоинт /health.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<InventoryDbContext>();

// Распределённая трассировка → Jaeger (OTLP). MassTransit сам прокидывает W3C trace-context
// через заголовки сообщений, поэтому спаны Orders→Inventory→Notifications склеиваются в один trace.
var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://127.0.0.1:4317";
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("OrderFlow.Inventory"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource("MassTransit") // = DiagnosticHeaders.DefaultListenerName
        .AddOtlpExporter(otlp => otlp.Endpoint = new Uri(otlpEndpoint)));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

await app.Services.ApplyMigrationsAsync();

app.MapHealthChecks("/health");

app.MapGet("/products", async (InventoryDbContext db) => Results.Ok(await db.Products.ToListAsync()));

app.Run();
