# Guía de Integración OpenTelemetry (OTLP) y Logs en APIs .NET 10

Este documento detalla el procedimiento estándar y de "Arquitectura Limpia" para integrar cualquier API de .NET (ej. EconetTransacciones) al ecosistema de observabilidad de **ObsBank-v2**. Esto garantiza que logs, trazas y métricas se correlacionen automáticamente bajo una misma identidad de transacción.

---

## 1. Requisitos Previos (Paquetes NuGet)

Añade las siguientes dependencias oficiales a tu archivo `.csproj`. Estas versiones están validadas para .NET 10:

```xml
<ItemGroup>
  <!-- Núcleo de OpenTelemetry y Exportación OTLP -->
  <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.11.1" />
  <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.11.1" />
  
  <!-- Instrumentación Automática -->
  <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.11.0" />
  <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.11.0" />
  <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" Version="1.11.0" />
  <PackageReference Include="OpenTelemetry.Instrumentation.SqlClient" Version="1.11.0-beta.1" />
  
  <!-- Logging Estructurado con Serilog -->
  <PackageReference Include="Serilog.AspNetCore" Version="10.0.0" />
  <PackageReference Include="Serilog.Formatting.Compact" Version="3.0.0" />
</ItemGroup>
```

---

## 2. Componentes de Infraestructura Base

Para mantener el `Program.cs` limpio, portaremos dos clases de infraestructura al directorio `Observability/` de la API destino.

### 2.1 ServiceMetadata.cs
Responsable de identificar el servicio en el cluster.

```csharp
namespace SuNamespace.Observability;

public sealed record ServiceMetadata(string Name, string Version, Uri OtlpEndpoint)
{
    public static ServiceMetadata FromConfiguration(IConfiguration config)
    {
        var endpoint = config["OpenTelemetry:OtlpEndpoint"] ?? "http://otel-collector:4317";
        var name = config["OpenTelemetry:ServiceName"] ?? "api-unknown";
        var version = config["OpenTelemetry:ServiceVersion"] ?? "1.0.0";

        return new ServiceMetadata(name, version, new Uri(endpoint));
    }
}
```

### 2.2 OpenTelemetryExtensions.cs
Configura los pipelines de trazas, métricas y logs hacia el Collector.

```csharp
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SuNamespace.Observability;

public static class OpenTelemetryExtensions
{
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder, ServiceMetadata meta)
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(meta.Name, meta.Version);

        // Logs: Redirección hacia OTLP
        builder.Logging.ClearProviders();
        builder.Logging.AddOpenTelemetry(o =>
        {
            o.SetResourceBuilder(resourceBuilder);
            o.IncludeScopes = true;
            o.IncludeFormattedMessage = true;
            o.AddOtlpExporter(otlp => {
                otlp.Endpoint = meta.OtlpEndpoint;
                otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            });
        });

        // Traces & Metrics
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(meta.Name, meta.Version))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation(o => o.SetDbStatementForText = true)
                .AddOtlpExporter(otlp => otlp.Endpoint = meta.OtlpEndpoint))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter(otlp => otlp.Endpoint = meta.OtlpEndpoint));

        return builder;
    }
}
```

---

## 3. Middlewares de Trazabilidad Crítica

Para que la observabilidad sea útil, debemos asegurar la correlación de logs y el manejo de errores. Copie estos archivos al directorio `Middleware/`.

1. **CorrelationIdMiddleware**: Inyecta un `X-Correlation-Id` en cada cabecera y log.
2. **ExceptionHandlingMiddleware**: Transforma errores fatales en respuestas JSON estandarizadas (ProblemDetails) incluyendo el `trace_id`.

---

## 4. Integración Final en `Program.cs`

La integración en el archivo principal se resume en este flujo:

```csharp
using SuNamespace.Observability;
using SuNamespace.Middleware;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// 1. Iniciar Serilog (JSON para Loki)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();

// 2. Registrar Observabilidad
var meta = ServiceMetadata.FromConfiguration(builder.Configuration);
builder.AddObservability(meta);

builder.Services.AddControllers();

var app = builder.Build();

// 3. Registrar Middlewares en orden
app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthorization();
app.MapControllers();

app.Run();
```

---

## 5. Variables de Entorno Recomendadas (Docker)

| Variable | Ejemplo | Propósito |
| :--- | :--- | :--- |
| `OpenTelemetry__OtlpEndpoint` | `http://otel-collector:4317` | Punto de entrada del colector. |
| `OpenTelemetry__ServiceName` | `econet-transacciones-api` | Identificador único en el dashboard. |
| `OpenTelemetry__ServiceVersion` | `2.1.0` | Versión para auditoría de despliegues. |

---

> _"El monitoreo te dice que el servidor está vivo; la observabilidad te dice por qué se siente mal"._
