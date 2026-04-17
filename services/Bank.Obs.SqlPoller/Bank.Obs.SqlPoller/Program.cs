using Bank.Obs.SqlPoller.Metrics;
using Bank.Obs.SqlPoller.Polling;
using Bank.Obs.SqlPoller.State;
using Bank.Obs.SqlPoller.Workers;
using Bank.Obs.SqlPoller.Observability;
using Serilog;
using Serilog.Formatting.Compact;

var builder = Host.CreateApplicationBuilder(args);

// --- 1. Metadatos de Observabilidad ---
var meta = ServiceMetadata.FromConfiguration(builder.Configuration);

// --- 2. Serilog (Logging Estructurado + OTLP) ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = meta.OtlpEndpoint.ToString();
        options.ResourceAttributes = new Dictionary<string, object>
        {
            ["service.name"] = meta.Name,
            ["service_name"] = meta.Name, // Unificación de etiquetas
            ["service.version"] = meta.Version
        };
    })
    .CreateLogger();

builder.Services.AddSerilog();

// --- 3. Observabilidad (Métricas & Trazas Nativa) ---
builder.AddObservability(meta);

// --- 3. Core Services ---
builder.Services.AddSingleton<MetricState>();
builder.Services.AddSingleton<SqlPollingClient>();
builder.Services.AddSingleton<SqlMetrics>();
builder.Services.AddHostedService<SqlMetricsWorker>();

// --- 4. Start ---
var host = builder.Build();
await host.RunAsync();