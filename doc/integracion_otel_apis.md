# Guía de Integración OpenTelemetry (OTLP) en APIs Producción

Este documento detalla el procedimiento estándar y considerado "Clean Architecture" para integrar APIs .NET de Producción (como la API de EconetTransacciones) al stack de Observabilidad (`obs-bank-v2`).

Siguiendo este enfoque, la configuración de telemetría (Log, Trazas, y Métricas) permanece abstracta y aislada, evitando saturar el `Program.cs`.

## 1. Requisitos Previos (Paquetes NuGet)

Se deben instalar los paquetes oficiales de OpenTelemetry en el proyecto `.csproj` destino. Es crucial que las versiones empaten la versión de .NET SDK utilizada.

```bash
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Instrumentation.SqlClient
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```
> **Nota:** Si la aplicación utiliza **Serilog**, también debe instalarse el puente OTLP para logs: `dotnet add package Serilog.Sinks.OpenTelemetry`.

---

## 2. Creación del Archivo de Extensión (`OpenTelemetryExtensions.cs`)

Cree una clase de extensión estática en el proyecto destino para encapsular la canalización de telemetría. Puede guardarla típicamente en un directorio `Observability` o `Extensions`.

```csharp
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Econet.Api.Observability // 1. Reemplace por el namespace de la API
{
    // Clase auxiliar para transportar variables
    public class ServiceMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public Uri OtlpEndpoint { get; set; } = default!;
    }

    public static class OpenTelemetryExtensions
    {
        public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder, ServiceMetadata meta)
        {
            var resourceBuilder = ResourceBuilder.CreateDefault()
                .AddService(serviceName: meta.Name, serviceVersion: meta.Version);

            // Reemplace/Comente si no usan el bloque base de Logging o usan Serilog a nivel global.
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
                        .AddAspNetCoreInstrumentation() // Traza los endpoints REST entrantes
                        .AddHttpClientInstrumentation()   // Traza las llamadas HTTP salientes a otros micros
                        .AddSqlClientInstrumentation(options => 
                        {
                            // IMPORTANTE: Imprime la Query SQL textual en Tempo para debug de latencia transaccional.
                            // Evalué si requiere masking de datos PII antes de mandar a producción.
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
                        .AddRuntimeInstrumentation() // Métricas de CPU/RAM de .NET internas
                        .AddOtlpExporter(otlp =>
                        {
                            otlp.Endpoint = meta.OtlpEndpoint;
                            otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                        });
                });

            return builder;
        }
    }
}
```

---

## 3. Inyección en `Program.cs` / `Startup.cs`

Con el archivo de extensión creado, inyectar el flujo gRPC en el pipeline de la API se reanuda a **tres sencillas líneas de código**.

Abra el `Program.cs` de la API de producción e inserte lo siguiente antes del `builder.Build()`:

```csharp
// 1. Defina la metadata básica. Lo ideal es leer el endpoint desde appsettings.json.
//    OtlpEndpoint apunta directamente al OTel Collector gRPC de obs-bank-v2.
var meta = new ServiceMetadata { 
    Name = "Econet-Transacciones-API", 
    Version = "1.0.0", 
    OtlpEndpoint = new Uri("http://192.168.1.100:4317") // Reemplazar con IP real del Docker Collector
};

// 2. Registre OpenTelemetry usando la limpia extensión que creamos antes.
builder.AddObservability(meta);

var app = builder.Build();
```

---

## Qué esperar al desplegar a Producción 
Una vez que inicie su API de EconetTransacciones re-compilada, automáticamente hará el _handshake_ con el Collector OTLP:
- Cada hit HTTP en la API quedará registrado como **Log OTLP** visualizable en _Grafana Loki_.
- El flujo entero (tiempo tardado procesando la request en ASP.NET vs tiempo de espera en el `SqlCommand`) dibujará gráficas temporales en _Grafana Tempo_.
- El tráfico web será tabulado por `requests_per_second` nativamentee en _Prometheus_.
