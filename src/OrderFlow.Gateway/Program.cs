using Microsoft.Extensions.Http.Resilience;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// YARP: единая точка входа, роутинг публичного HTTP-трафика на сервисы (конфиг в appsettings).
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Сервисы, чьи /health агрегируются шлюзом. Ключ — имя, значение — базовый адрес.
var downstreamServices = builder.Configuration.GetSection("DownstreamServices")
    .Get<Dictionary<string, string>>() ?? new Dictionary<string, string>();

// На каждый сервис — свой HttpClient со своим resilience-конвейером, значит и свой
// независимый circuit breaker: падение одного сервиса не размыкает цепь к остальным.
foreach (var (name, baseUrl) in downstreamServices)
{
    builder.Services.AddHttpClient(name, client =>
    {
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(10);
    })
    .AddResilienceHandler($"health-{name}", pipeline =>
    {
        // Порядок стратегий: retry снаружи, circuit breaker внутри, timeout на попытку.
        // Каждая попытка (в т.ч. retry) проходит через CB, поэтому один вызов к упавшему
        // сервису = 1 + 2 повтора = 3 неудачи → цепь размыкается сразу (MinimumThroughput: 3).
        pipeline.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 2,
            BackoffType = DelayBackoffType.Constant,
            Delay = TimeSpan.FromMilliseconds(200),
        });
        pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            SamplingDuration = TimeSpan.FromSeconds(10),
            FailureRatio = 0.5,
            MinimumThroughput = 3,
            BreakDuration = TimeSpan.FromSeconds(15),
        });
        pipeline.AddTimeout(TimeSpan.FromSeconds(3));
    });
}

var app = builder.Build();

app.MapReverseProxy();

// Агрегированный health: шлюз опрашивает /health каждого сервиса через resilient-клиент.
// Открытый circuit breaker бросает BrokenCircuitException — сервис помечается недоступным
// без реального обращения, что и демонстрирует срабатывание предохранителя.
app.MapGet("/health", async (IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var services = new Dictionary<string, string>();
    var allHealthy = true;

    foreach (var name in downstreamServices.Keys)
    {
        var client = httpClientFactory.CreateClient(name);
        try
        {
            using var response = await client.GetAsync("/health", cancellationToken);
            var healthy = response.IsSuccessStatusCode;
            allHealthy &= healthy;
            services[name] = healthy ? "Healthy" : $"Unhealthy ({(int)response.StatusCode})";
        }
        catch (Exception ex)
        {
            allHealthy = false;
            services[name] = $"Unavailable ({ex.GetType().Name})";
        }
    }

    var payload = new { status = allHealthy ? "Healthy" : "Unhealthy", services };
    return allHealthy
        ? Results.Ok(payload)
        : Results.Json(payload, statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.Run();
/