using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Bank.Obs.DemoApi.Observability;

public static class OpenTelemetryExtensions
{
    public static WebApplicationBuilder AddObservability(
        this WebApplicationBuilder builder,
        ServiceMetadata meta)
    {
        ResourceBuilder resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService(
                serviceName: meta.Name,
                serviceVersion: meta.Version);

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(resourceBuilder);
            options.IncludeScopes = true;
            options.IncludeFormattedMessage = true;
            options.AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = meta.OtlpEndpoint;
                otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            });
        });

        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(meta.Name, meta.Version);
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation()
                    .AddOtlpExporter(otlp =>
                    {
                        otlp.Endpoint = meta.OtlpEndpoint;
                        otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                    });
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddOtlpExporter(otlp =>
                    {
                        otlp.Endpoint = meta.OtlpEndpoint;
                        otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                    });
            });

        return builder;
    }
}