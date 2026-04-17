# Guía de Integración OpenTelemetry (OTLP) en APIs .NET 10

Este documento detalla el procedimiento estándar para integrar cualquier API de .NET al ecosistema de **ObsBank-v2**.

---

## 1. Requisitos (NuGet)

Añade estas dependencias a tu `.csproj`:

```xml
<ItemGroup>
  <!-- OpenTelemetry -->
  <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.11.1" />
  <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.11.1" />
  <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.11.0" />
  <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.11.0" />
  <PackageReference Include="OpenTelemetry.Instrumentation.SqlClient" Version="1.11.0-beta.1" />
  
  <!-- Logging con OTLP Directo -->
  <PackageReference Include="Serilog.AspNetCore" Version="10.0.0" />
  <PackageReference Include="Serilog.Sinks.OpenTelemetry" Version="2.0.0" />
</ItemGroup>
```

---

## 2. Configuración en `Program.cs`

Integra Serilog y OpenTelemetry de forma unificada. El punto clave es enviar los logs directamente al colector OTLP.

```csharp
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar Serilog con OpenTelemetry Sink
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://otel-collector:4317";
        options.ResourceAttributes = new Dictionary<string, object>
        {
            ["service.name"] = builder.Configuration["OpenTelemetry:ServiceName"] ?? "api-unknown"
        };
    })
    .CreateLogger();

builder.Host.UseSerilog();

// 2. Configurar Trazas y Métricas
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSqlClientInstrumentation()
        .AddOtlpExporter(o => o.Endpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://otel-collector:4317")))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter(o => o.Endpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://otel-collector:4317")));

var app = builder.Build();
app.UseSerilogRequestLogging();
app.MapControllers();
app.Run();
```

---

## 3. Variables de Entorno (Docker)

Para que la API sea parte del ecosistema ObsBank, inyecta estas variables:

| Variable | Ejemplo |
| :--- | :--- |
| `OpenTelemetry__OtlpEndpoint` | `http://otel-collector:4317` |
| `OpenTelemetry__ServiceName` | `mi-banco-api` |
| `OpenTelemetry__ServiceVersion` | `1.0.0` |

---

## 4. Beneficios
- **Correlación Automática**: Los logs creados durante una petición HTTP incluirán automáticamente el `TraceId`.
- **Visibilidad SQL**: Todas las consultas a la base de datos aparecerán en los diagramas de Gantt de Grafana Tempo.
- **Sin Archivos Intermedios**: Los logs viajan por red, no llenan el disco del servidor.
