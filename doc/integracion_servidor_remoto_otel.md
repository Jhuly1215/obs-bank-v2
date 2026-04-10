# Guía: Instrumentación de Servidores Remotos (Zero-Code)

Esta guía explica cómo conectar una API .NET que reside en un servidor externo (ajeno al clúster Docker de ObsBank) para que envíe sus logs, trazas y métricas de forma automática sin modificar el código fuente.

## 1. Preparación de Red (Requisito Crítico)

El servidor remoto debe ser capaz de alcanzar al servidor donde reside **ObsBank-v2**.

1. Identifica la IP del servidor de Observabilidad (ej. `172.16.0.100`).
2. Asegúrate de que el puerto **4317 (gRPC)** esté abierto para tráfico entrante en el firewall de ese servidor.
3. Verifica la conectividad desde el servidor remoto (PowerShell):
   ```powershell
   Test-NetConnection -ComputerName 172.16.0.100 -Port 4317
   ```

---

## 2. Configuración en Servidor Remoto (Windows Server / IIS)

### Paso A: Obtener los binarios del Agente
1. Descarga la versión de Windows de [OpenTelemetry .NET Auto-Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases).
2. Descomprime en una ruta estática, ejemplo: `C:\otel-agent\`.

### Paso B: Configurar Variables de Sistema
Debes establecer las siguientes Variables de Entorno a nivel de SISTEMA para que afecten a todos los procesos .NET (incluyendo el W3WP de IIS):

| Variable | Valor | Descripción |
| :--- | :--- | :--- |
| `CORECLR_ENABLE_PROFILING` | `1` | Activa el perfilador del runtime. |
| `CORECLR_PROFILER` | `{ff15165d-5152-4531-90f6-bc61ef90a334}` | ID oficial del perfilador OTel. |
| `CORECLR_PROFILER_PATH` | `C:\otel-agent\OpenTelemetry.DotNet.Auto.Native.dll` | Ruta al binario nativo. |
| `DOTNET_STARTUP_HOOKS` | `C:\otel-agent\net\OpenTelemetry.DotNet.Auto.dll` | Hook de inicio de .NET. |
| `OTEL_DOTNET_AUTO_HOME` | `C:\otel-agent` | Directorio base del agente. |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `http://tu-ip-obs-bank:4317` | IP de tu servidor de monitoreo. |
| `OTEL_SERVICE_NAME` | `api-externa-produccion` | Nombre que verás en Grafana. |
| `OTEL_LOGS_EXPORTER` | `otlp` | Activa el envío de Logs vía OTLP. |

### Paso C: Aplicar cambios
1. **IIS**: Es necesario ejecutar `iisreset` desde una terminal de administrador.
2. **Servicio Windows**: Reinicia el servicio específico de tu aplicación.

---

## 3. Configuración en Servidor Remoto (Linux / Systemd)

Si tu aplicación corre en Linux sin Docker:

1. **Descomprimir el agente** en `/opt/otel-agent/`.
2. **Configurar el servicio** de tu API (ej: `/etc/systemd/system/tu-api.service`) agregando estas líneas bajo `[Service]`:

```ini
Environment=CORECLR_ENABLE_PROFILING=1
Environment=CORECLR_PROFILER={ff15165d-5152-4531-90f6-bc61ef90a334}
Environment=CORECLR_PROFILER_PATH=/opt/otel-agent/OpenTelemetry.DotNet.Auto.Native.so
Environment=DOTNET_STARTUP_HOOKS=/opt/otel-agent/net/OpenTelemetry.DotNet.Auto.dll
Environment=OTEL_DOTNET_AUTO_HOME=/opt/otel-agent
Environment=OTEL_EXPORTER_OTLP_ENDPOINT=http://tu-ip-obs-bank:4317
Environment=OTEL_SERVICE_NAME=api-linux-externa
Environment=OTEL_LOGS_EXPORTER=otlp
```

3. **Reiniciar el servicio**:
```bash
sudo systemctl daemon-reload
sudo systemctl restart tu-api
```

---

## 4. Visualización de Resultados

Una vez configurado y reiniciado el servicio remoto, no necesitas hacer nada más. Los datos aparecerán solos:

- **Logs**: En Grafana Loki filtra por `{service_name="tu-nombre-de-api"}`.
- **Trazas**: En Grafana Tempo busca por el mismo nombre para ver el flujo de peticiones.
- **Métricas**: Automáticamente se poblarán los dashboards de uso de recursos .NET (CPU, RAM, GC).
