using Bank.Obs.SqlPoller.Metrics;
using Bank.Obs.SqlPoller.Polling;
using Bank.Obs.SqlPoller.State;
using Bank.Obs.SqlPoller.Workers;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

var builder = Host.CreateApplicationBuilder(args);

var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://otel-collector:4317";
var serviceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "bank-sql-poller";
var serviceVersion = builder.Configuration["OpenTelemetry:ServiceVersion"] ?? "2.0.0";

builder.Services.AddSingleton<MetricState>();
builder.Services.AddSingleton<SqlPollingClient>();
builder.Services.AddSingleton<SqlMetrics>(); // crea el Meter y gauges
builder.Services.AddHostedService<SqlMetricsWorker>();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(serviceName, serviceVersion))
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter(SqlMetrics.MeterName)
            .AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri(otlpEndpoint);
                otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            });
    });

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

await builder.Build().RunAsync();