using OrderFlow.Notifications.Api.Repository.Migrator;
using OrderFlow.Notifications.Api.Systems;

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

var smtpHost = builder.Configuration["Smtp:Host"]
    ?? throw new InvalidOperationException("'Smtp:Host' is not configured.");
var smtpPort = builder.Configuration.GetValue<int>("Smtp:Port");

builder.Services.AddNotificationsServices(connectionString, rabbitMqHost, rabbitMqUsername, rabbitMqPassword, smtpHost, smtpPort);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

await app.Services.ApplyMigrationsAsync();

app.Run();
