using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Bank.Obs.SqlPoller.Metrics;

namespace Bank.Obs.SqlPoller.Observability;

public static class OpenTelemetryExtensions
{
    public static TBuilder AddObservability<TBuilder>(this TBuilder builder, ServiceMetadata meta) 
        where TBuilder : IHostApplicationBuilder
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName: meta.Name, serviceVersion: meta.Version);

        // Logs via OTLP
        builder.Logging.ClearProviders();
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.SetResourceBuilder(resourceBuilder);
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.AddOtlpExporter(otlp => 
            {
                otlp.Endpoint = meta.OtlpEndpoint;
                otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            });
        });

        // Traces & Metrics
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(meta.Name, meta.Version))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(otlp => otlp.Endpoint = meta.OtlpEndpoint);
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddRuntimeInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddMeter(SqlMetrics.MeterName) // Métricas personalizadas del Poller
                    .AddOtlpExporter(otlp => otlp.Endpoint = meta.OtlpEndpoint);
            });

        return builder;
    }
}
