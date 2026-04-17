# Guía completa de configuración OTLP para conectar servicios a ObsBank-v2

## Objetivo

Esta guía explica **todas las formas prácticas de configurar un servicio .NET** para que envíe **trazas, métricas y logs** hacia el stack de observabilidad de **ObsBank-v2**, según **cómo esté arrancando el servicio**.

Está pensada para cubrir estos escenarios:

- el servicio corre en la **misma máquina** que ObsBank, pero **fuera de Docker**;
- el servicio corre en un **servidor remoto**;
- hay **varias APIs diferentes** enviando al mismo collector;
- el servicio puede arrancar desde:
  - terminal manual,
  - Visual Studio,
  - IIS,
  - Windows Service,
  - Linux con `systemd`,
  - Docker / Docker Compose.

---

## 1. Idea clave: qué significa `OTEL_EXPORTER_OTLP_ENDPOINT`

`OTEL_EXPORTER_OTLP_ENDPOINT` **no es el endpoint funcional de tu API**.

No significa:

- “voy a capturar solo la traza de `/login`”
- “voy a observar solo un endpoint HTTP de negocio”

Sí significa:

- “este proceso va a enviar su telemetría a este receptor OTLP”

En otras palabras:

**tu aplicación produce telemetría** → **OTLP exporter la envía al collector** → **ObsBank la procesa y la muestra en Grafana/Tempo/Loki/Prometheus**.

Entonces, si configuras:

```text
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
```

estás diciendo:

> “envía la telemetría de este proceso al collector OTLP que escucha en localhost:4317”.

---

## 2. ¿Se pueden conectar varias APIs diferentes al mismo ObsBank?

Sí. **Varias APIs distintas pueden enviar al mismo collector**.

Lo correcto es:

- usar el **mismo endpoint OTLP** para todas,
- pero poner un **`OTEL_SERVICE_NAME` distinto** para cada una.

### Ejemplo

- API 1: `OTEL_SERVICE_NAME=api-auth`
- API 2: `OTEL_SERVICE_NAME=api-pagos`
- API 3: `OTEL_SERVICE_NAME=api-reportes`

Así, todas pueden mandar al mismo:

```text
http://localhost:4317
```

pero en Grafana/Tempo/Loki aparecerán separadas por nombre de servicio.

### Recomendación adicional

Agrega también atributos de recurso para ordenar mejor:

```text
OTEL_RESOURCE_ATTRIBUTES=service.namespace=obsbank,deployment.environment.name=dev,service.version=1.0.0
```

---

## 3. Caso 1: el otro servicio corre en la misma máquina que ObsBank

Si:

- **ObsBank está en Docker**,
- y **tu otra API corre normal en el host**, fuera de Docker,

entonces normalmente debes usar:

- `http://localhost:4317` para **OTLP gRPC**
- `http://localhost:4318` para **OTLP HTTP**

### Recomendación general

Usa primero **gRPC**:

```text
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
```

### Si prefieres HTTP

```text
OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4318
```

---

## 4. Caso 2: el servicio corre en un servidor remoto

Si la API no corre en tu máquina, sino en **otro servidor**, entonces debes usar la **IP o DNS del servidor donde corre ObsBank**.

Ejemplo:

```text
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
OTEL_EXPORTER_OTLP_ENDPOINT=http://172.16.0.100:4317
```

### Requisitos de red

1. El servidor remoto debe alcanzar al servidor de ObsBank.
2. Debe estar abierto el puerto:
   - `4317` si usarás OTLP/gRPC
   - `4318` si usarás OTLP/HTTP
3. El collector de ObsBank debe estar escuchando hacia afuera.

### Prueba rápida en Windows

```powershell
Test-NetConnection -ComputerName 172.16.0.100 -Port 4317
```

### Prueba rápida en Linux

```bash
nc -vz 172.16.0.100 4317
```

---

## 5. Bloque base recomendado de variables OTEL

Este es el bloque base que reutilizarás en casi todos los casos.

### Variante gRPC

```text
OTEL_SERVICE_NAME=api-auth
OTEL_TRACES_EXPORTER=otlp
OTEL_METRICS_EXPORTER=otlp
OTEL_LOGS_EXPORTER=otlp
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
OTEL_RESOURCE_ATTRIBUTES=service.namespace=obsbank,deployment.environment.name=dev,service.version=1.0.0
```

### Variante HTTP

```text
OTEL_SERVICE_NAME=api-auth
OTEL_TRACES_EXPORTER=otlp
OTEL_METRICS_EXPORTER=otlp
OTEL_LOGS_EXPORTER=otlp
OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4318
OTEL_RESOURCE_ATTRIBUTES=service.namespace=obsbank,deployment.environment.name=dev,service.version=1.0.0
```

---

## 6. Importante: dos modelos distintos

Antes de configurar, debes distinguir entre estos dos casos:

### A. La aplicación ya está instrumentada en código

Ejemplo: el proyecto ya usa OpenTelemetry SDK en `Program.cs` o en alguna librería de observabilidad.

En ese caso, normalmente **basta con las variables `OTEL_*`**.

### B. La aplicación NO está instrumentada en código y quieres zero-code

En ese caso, **no basta solo con `OTEL_*`**.

Necesitas además **.NET Automatic Instrumentation**, es decir:

- instalar el agente,
- cargar hooks/runtime variables,
- y luego definir el exporter OTLP y el nombre del servicio.

---

# 7. Todas las formas de configuración según cómo arranca el servicio

---

## 7.1. El servicio arranca manualmente desde terminal

Este es el caso de:

- `dotnet run`
- `dotnet MiApi.dll`
- `MiApi.exe`

Aquí las variables se ponen en la **misma terminal desde la que arrancas la app**.

### 7.1.1. Bash / Linux / Git Bash

#### Si la app ya está instrumentada en código

```bash
export OTEL_SERVICE_NAME="api-auth"
export OTEL_TRACES_EXPORTER="otlp"
export OTEL_METRICS_EXPORTER="otlp"
export OTEL_LOGS_EXPORTER="otlp"
export OTEL_EXPORTER_OTLP_PROTOCOL="grpc"
export OTEL_EXPORTER_OTLP_ENDPOINT="http://localhost:4317"
export OTEL_RESOURCE_ATTRIBUTES="service.namespace=obsbank,deployment.environment.name=dev,service.version=1.0.0"

dotnet ApiAuth.dll
```

#### En una sola línea

```bash
OTEL_SERVICE_NAME="api-auth" \
OTEL_TRACES_EXPORTER="otlp" \
OTEL_METRICS_EXPORTER="otlp" \
OTEL_LOGS_EXPORTER="otlp" \
OTEL_EXPORTER_OTLP_PROTOCOL="grpc" \
OTEL_EXPORTER_OTLP_ENDPOINT="http://localhost:4317" \
OTEL_RESOURCE_ATTRIBUTES="service.namespace=obsbank,deployment.environment.name=dev,service.version=1.0.0" \
dotnet ApiAuth.dll
```

### 7.1.2. PowerShell / Windows

#### Si la app ya está instrumentada en código

```powershell
$env:OTEL_SERVICE_NAME="api-auth"
$env:OTEL_TRACES_EXPORTER="otlp"
$env:OTEL_METRICS_EXPORTER="otlp"
$env:OTEL_LOGS_EXPORTER="otlp"
$env:OTEL_EXPORTER_OTLP_PROTOCOL="grpc"
$env:OTEL_EXPORTER_OTLP_ENDPOINT="http://localhost:4317"
$env:OTEL_RESOURCE_ATTRIBUTES="service.namespace=obsbank,deployment.environment.name=dev,service.version=1.0.0"

dotnet .\ApiAuth.dll
```

### 7.1.3. Zero-code en terminal con .NET Automatic Instrumentation

Si **no hay instrumentación en código**, necesitas el agente.

#### Opción recomendada en Windows

Usar el módulo oficial PowerShell:

```powershell
Import-Module "OpenTelemetry.DotNet.Auto.psm1"
Install-OpenTelemetryCore
Register-OpenTelemetryForCurrentSession -OTelServiceName "api-auth"

$env:OTEL_TRACES_EXPORTER="otlp"
$env:OTEL_METRICS_EXPORTER="otlp"
$env:OTEL_LOGS_EXPORTER="otlp"
$env:OTEL_EXPORTER_OTLP_PROTOCOL="grpc"
$env:OTEL_EXPORTER_OTLP_ENDPOINT="http://localhost:4317"
$env:OTEL_RESOURCE_ATTRIBUTES="service.namespace=obsbank,deployment.environment.name=dev,service.version=1.0.0"

dotnet .\ApiAuth.dll
```

#### Opción manual de bajo nivel

Solo si realmente necesitas controlar todo a mano.

Variables típicas de auto-instrumentación manual:

```text
CORECLR_ENABLE_PROFILING=1
CORECLR_PROFILER={918728DD-259F-4A6A-AC2B-B85E1B658318}
DOTNET_STARTUP_HOOKS=<ruta>/net/OpenTelemetry.AutoInstrumentation.StartupHook.dll
DOTNET_ADDITIONAL_DEPS=<ruta>/AdditionalDeps
DOTNET_SHARED_STORE=<ruta>/store
OTEL_DOTNET_AUTO_HOME=<ruta-base-del-agente>
```

Y además:

```text
OTEL_SERVICE_NAME=api-auth
OTEL_TRACES_EXPORTER=otlp
OTEL_METRICS_EXPORTER=otlp
OTEL_LOGS_EXPORTER=otlp
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
```

---

## 7.2. El servicio arranca desde Visual Studio o launch profile

Esto aplica si se ejecuta con:

- F5,
- Debug,
- perfil local,
- `launchSettings.json`.

Aquí lo normal es poner las variables dentro de `Properties/launchSettings.json`.

### Ejemplo

```json
{
  "profiles": {
    "ApiAuth": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "https://localhost:7205;http://localhost:5205",
      "environmentVariables": {
        "OTEL_SERVICE_NAME": "api-auth",
        "OTEL_TRACES_EXPORTER": "otlp",
        "OTEL_METRICS_EXPORTER": "otlp",
        "OTEL_LOGS_EXPORTER": "otlp",
        "OTEL_EXPORTER_OTLP_PROTOCOL": "grpc",
        "OTEL_EXPORTER_OTLP_ENDPOINT": "http://localhost:4317",
        "OTEL_RESOURCE_ATTRIBUTES": "service.namespace=obsbank,deployment.environment.name=dev,service.version=1.0.0"
      }
    }
  }
}
```

### Importante

- esto sirve muy bien para **desarrollo local**;
- no sirve como mecanismo de configuración de producción publicado;
- si quieres zero-code y la app no tiene SDK en código, `launchSettings.json` **no sustituye** la instalación del agente.

---

## 7.3. El servicio corre como Windows Service

En este caso:

- **no** sirve poner variables en una consola cualquiera;
- deben existir en el **entorno del servicio**.

### Método recomendado para zero-code

Usar el módulo PowerShell oficial:

```powershell
Import-Module "OpenTelemetry.DotNet.Auto.psm1"
Install-OpenTelemetryCore
Register-OpenTelemetryForWindowsService -WindowsServiceName "NombreRealDelServicio" -OTelServiceName "api-auth"
```

Después reinicias el servicio:

```powershell
Restart-Service -Name "NombreRealDelServicio" -Force
```

### Si la app ya está instrumentada en código

Tienes varias opciones prácticas:

#### Opción A: variables del servicio en el Registro

Debajo de:

```text
HKLM\SYSTEM\CurrentControlSet\Services\NombreDelServicio
```

se puede usar un valor `Environment` tipo `REG_MULTI_SZ`.

#### Opción B: `App.config` para .NET Framework

```xml
<configuration>
  <appSettings>
    <add key="OTEL_SERVICE_NAME" value="api-auth" />
    <add key="OTEL_TRACES_EXPORTER" value="otlp" />
    <add key="OTEL_METRICS_EXPORTER" value="otlp" />
    <add key="OTEL_LOGS_EXPORTER" value="otlp" />
    <add key="OTEL_EXPORTER_OTLP_PROTOCOL" value="grpc" />
    <add key="OTEL_EXPORTER_OTLP_ENDPOINT" value="http://localhost:4317" />
    <add key="OTEL_RESOURCE_ATTRIBUTES" value="service.namespace=obsbank,deployment.environment.name=prod,service.version=1.0.0" />
  </appSettings>
</configuration>
```

### Recomendación

Si es Windows Service y no estás seguro, esta es la mejor ruta:

1. identificar el nombre real del servicio;
2. decidir si es .NET Framework o .NET moderno;
3. si quieres zero-code, usar el módulo oficial;
4. reiniciar el servicio.

---

## 7.4. El servicio corre en IIS

Aquí hay que separar dos cosas:

### A. IIS con .NET Framework

Para este caso sí existe helper oficial:

```powershell
Import-Module "OpenTelemetry.DotNet.Auto.psm1"
Install-OpenTelemetryCore
Register-OpenTelemetryForIIS
```

Esto aplica principalmente a apps .NET Framework hospedadas por IIS.

### B. Configuración por `Web.config`

En apps compatibles, puedes poner las variables `OTEL_*` comunes en `Web.config`.

```xml
<configuration>
  <appSettings>
    <add key="OTEL_SERVICE_NAME" value="api-auth" />
    <add key="OTEL_TRACES_EXPORTER" value="otlp" />
    <add key="OTEL_METRICS_EXPORTER" value="otlp" />
    <add key="OTEL_LOGS_EXPORTER" value="otlp" />
    <add key="OTEL_EXPORTER_OTLP_PROTOCOL" value="grpc" />
    <add key="OTEL_EXPORTER_OTLP_ENDPOINT" value="http://localhost:4317" />
    <add key="OTEL_RESOURCE_ATTRIBUTES" value="service.namespace=obsbank,deployment.environment.name=prod,service.version=1.0.0" />
  </appSettings>
</configuration>
```

### Importante

- `Web.config` sirve bien para `OTEL_*` comunes;
- no sustituye toda la instalación de auto-instrumentación si realmente vas por zero-code;
- para producción, si la app es ASP.NET Core detrás de IIS, debes tratar la configuración como configuración del entorno hospedado, no como simple perfil local.

---

## 7.5. El servicio corre en Linux con systemd

Aquí las variables se ponen en el archivo `.service` bajo `[Service]`.

### Si la app ya está instrumentada en código

Ejemplo:

```ini
[Unit]
Description=API Auth
After=network.target

[Service]
WorkingDirectory=/opt/api-auth
Environment=OTEL_SERVICE_NAME=api-auth
Environment=OTEL_TRACES_EXPORTER=otlp
Environment=OTEL_METRICS_EXPORTER=otlp
Environment=OTEL_LOGS_EXPORTER=otlp
Environment=OTEL_EXPORTER_OTLP_PROTOCOL=grpc
Environment=OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
Environment=OTEL_RESOURCE_ATTRIBUTES=service.namespace=obsbank,deployment.environment.name=prod,service.version=1.0.0
ExecStart=/usr/bin/dotnet /opt/api-auth/ApiAuth.dll
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
```

Luego:

```bash
sudo systemctl daemon-reload
sudo systemctl restart api-auth
sudo systemctl status api-auth
```

### Si quieres zero-code en Linux

La forma práctica es usar un **wrapper script** que cargue el entorno del agente y luego ejecute la app.

#### Wrapper ejemplo

Archivo `/opt/api-auth/run-with-otel.sh`:

```bash
#!/usr/bin/env bash
set -e
. /root/.otel-dotnet-auto/instrument.sh

export OTEL_SERVICE_NAME="api-auth"
export OTEL_TRACES_EXPORTER="otlp"
export OTEL_METRICS_EXPORTER="otlp"
export OTEL_LOGS_EXPORTER="otlp"
export OTEL_EXPORTER_OTLP_PROTOCOL="grpc"
export OTEL_EXPORTER_OTLP_ENDPOINT="http://localhost:4317"
export OTEL_RESOURCE_ATTRIBUTES="service.namespace=obsbank,deployment.environment.name=prod,service.version=1.0.0"

exec /usr/bin/dotnet /opt/api-auth/ApiAuth.dll
```

Darle permisos:

```bash
chmod +x /opt/api-auth/run-with-otel.sh
```

Y en el service:

```ini
[Service]
ExecStart=/opt/api-auth/run-with-otel.sh
```

---

## 7.6. El servicio corre dentro de Docker o Docker Compose

Aquí las variables se ponen dentro del contenedor.

### En Docker Compose

```yaml
services:
  api-auth:
    image: mi-api-auth:latest
    environment:
      OTEL_SERVICE_NAME: api-auth
      OTEL_TRACES_EXPORTER: otlp
      OTEL_METRICS_EXPORTER: otlp
      OTEL_LOGS_EXPORTER: otlp
      OTEL_EXPORTER_OTLP_PROTOCOL: grpc
      OTEL_EXPORTER_OTLP_ENDPOINT: http://otel-collector:4317
      OTEL_RESOURCE_ATTRIBUTES: service.namespace=obsbank,deployment.environment.name=dev,service.version=1.0.0
```

### Nota importante

Si el servicio está dentro de Docker y el collector también, lo normal es usar el **nombre del servicio Docker**, por ejemplo:

```text
http://otel-collector:4317
```

no `localhost`, porque dentro del contenedor `localhost` apunta al propio contenedor.

### Si quieres zero-code en contenedor

Debes incluir el agente en la imagen o usar un entrypoint/wrapper que cargue la auto-instrumentación antes de arrancar la app.

---

# 8. Cómo identificar rápidamente cómo está arrancando el servicio

Si todavía no sabes cómo corre, usa esta lista:

## Si ves esto...

### `dotnet run`, `dotnet app.dll`, `MiApi.exe`
Entonces corre **manual desde terminal**.

### F5 / Debug / perfil del proyecto / launch profile
Entonces corre desde **Visual Studio / launchSettings.json**.

### Está en `services.msc`
Entonces es **Windows Service**.

### Está en IIS, app pool, sitio web, bindings
Entonces corre en **IIS**.

### Existe un archivo `.service` y responde a `systemctl status`
Entonces corre con **systemd**.

### Aparece en `docker ps` o se levanta con compose
Entonces corre en **Docker / Docker Compose**.

---

# 9. Cómo decidir cuál método usar

Usa esta regla:

## Si la app YA tiene OpenTelemetry en código

Aplica solo:

- `OTEL_SERVICE_NAME`
- `OTEL_TRACES_EXPORTER`
- `OTEL_METRICS_EXPORTER`
- `OTEL_LOGS_EXPORTER`
- `OTEL_EXPORTER_OTLP_PROTOCOL`
- `OTEL_EXPORTER_OTLP_ENDPOINT`
- `OTEL_RESOURCE_ATTRIBUTES`

Y colócalas según el modo de arranque.

## Si la app NO tiene OpenTelemetry en código

Aplica:

- .NET Automatic Instrumentation,
- más las variables `OTEL_*`,
- más el reinicio del proceso/servicio.

---

# 10. Ejemplos completos listos para copiar

## 10.1. API local en la misma máquina, fuera de Docker, desde terminal PowerShell

```powershell
$env:OTEL_SERVICE_NAME="api-auth"
$env:OTEL_TRACES_EXPORTER="otlp"
$env:OTEL_METRICS_EXPORTER="otlp"
$env:OTEL_LOGS_EXPORTER="otlp"
$env:OTEL_EXPORTER_OTLP_PROTOCOL="grpc"
$env:OTEL_EXPORTER_OTLP_ENDPOINT="http://localhost:4317"
$env:OTEL_RESOURCE_ATTRIBUTES="service.namespace=obsbank,deployment.environment.name=dev,service.version=1.0.0"

dotnet .\ApiAuth.dll
```

## 10.2. Segunda API local distinta, en otra terminal

```powershell
$env:OTEL_SERVICE_NAME="api-pagos"
$env:OTEL_TRACES_EXPORTER="otlp"
$env:OTEL_METRICS_EXPORTER="otlp"
$env:OTEL_LOGS_EXPORTER="otlp"
$env:OTEL_EXPORTER_OTLP_PROTOCOL="grpc"
$env:OTEL_EXPORTER_OTLP_ENDPOINT="http://localhost:4317"
$env:OTEL_RESOURCE_ATTRIBUTES="service.namespace=obsbank,deployment.environment.name=dev,service.version=1.0.0"

dotnet .\ApiPagos.dll
```

## 10.3. API remota en otro servidor Windows

```powershell
$env:OTEL_SERVICE_NAME="api-auth-produccion"
$env:OTEL_TRACES_EXPORTER="otlp"
$env:OTEL_METRICS_EXPORTER="otlp"
$env:OTEL_LOGS_EXPORTER="otlp"
$env:OTEL_EXPORTER_OTLP_PROTOCOL="grpc"
$env:OTEL_EXPORTER_OTLP_ENDPOINT="http://172.16.0.100:4317"
$env:OTEL_RESOURCE_ATTRIBUTES="service.namespace=obsbank,deployment.environment.name=prod,service.version=1.0.0"

dotnet .\ApiAuth.dll
```

## 10.4. API remota en Linux con systemd

```ini
[Service]
Environment=OTEL_SERVICE_NAME=api-auth-produccion
Environment=OTEL_TRACES_EXPORTER=otlp
Environment=OTEL_METRICS_EXPORTER=otlp
Environment=OTEL_LOGS_EXPORTER=otlp
Environment=OTEL_EXPORTER_OTLP_PROTOCOL=grpc
Environment=OTEL_EXPORTER_OTLP_ENDPOINT=http://172.16.0.100:4317
Environment=OTEL_RESOURCE_ATTRIBUTES=service.namespace=obsbank,deployment.environment.name=prod,service.version=1.0.0
ExecStart=/usr/bin/dotnet /opt/api-auth/ApiAuth.dll
```

---

# 11. Checklist final antes de probar

## En ObsBank

- [ ] El collector está levantado.
- [ ] El puerto `4317` o `4318` está publicado.
- [ ] Si es remoto, el firewall permite entrada.
- [ ] Si es remoto, el collector escucha hacia afuera.

## En la API externa

- [ ] Tiene `OTEL_SERVICE_NAME` único.
- [ ] Tiene definido exporter de trazas.
- [ ] Si quieres métricas y logs, también están definidos.
- [ ] El endpoint OTLP apunta correctamente a ObsBank.
- [ ] El proceso fue reiniciado después del cambio.

## En Grafana

- [ ] Buscar por `service.name = api-auth` o el nombre correspondiente.
- [ ] Verificar trazas en Tempo.
- [ ] Verificar logs filtrando por el servicio.
- [ ] Verificar métricas del runtime si están llegando.

---

# 12. Recomendación práctica final

Si todavía no sabes cómo corre el servicio, sigue este orden:

1. averigua si corre por terminal, Visual Studio, IIS, Windows Service, systemd o Docker;
2. confirma si ya tiene OpenTelemetry en código o no;
3. si ya lo tiene, usa solo las variables `OTEL_*` en el lugar correcto;
4. si no lo tiene, usa .NET Automatic Instrumentation;
5. usa un `OTEL_SERVICE_NAME` distinto por cada API.

Si quieres una primera prueba rápida y simple, esta suele ser la mejor:

```powershell
$env:OTEL_SERVICE_NAME="api-prueba"
$env:OTEL_TRACES_EXPORTER="otlp"
$env:OTEL_METRICS_EXPORTER="otlp"
$env:OTEL_LOGS_EXPORTER="otlp"
$env:OTEL_EXPORTER_OTLP_PROTOCOL="grpc"
$env:OTEL_EXPORTER_OTLP_ENDPOINT="http://localhost:4317"

dotnet .\TuApi.dll
```

Con eso ya puedes validar si ObsBank recibe telemetría desde otra API local.
