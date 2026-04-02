using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Bank.Obs.DemoApi.Observability;

public static class OpenTelemetryExtensions
{
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder, ServiceMetadata meta)
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName: meta.Name, serviceVersion: meta.Version);

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        builder.Logging.AddOpenTelemetry(o =>
        {
            o.SetResourceBuilder(resourceBuilder);
            o.IncludeScopes = true;
            o.IncludeFormattedMessage = true;

            o.AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = meta.OtlpEndpoint;
                otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            });
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(meta.Name, meta.Version))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation(options => 
                    {
                        // IMPORTANTE PARA PRODUCCIÓN: Permite ver el texto del query SQL en la traza (cuidado con PII en prod)
                        options.SetDbStatementForText = true; 
                    })
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