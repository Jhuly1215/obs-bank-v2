# Guía de Despliegue y Configuración: Econet.Transactions.Api

Esta guía resume la configuración necesaria para desplegar la API en un servidor Windows con IIS, habilitando HTTPS y Observabilidad "Zero Code".

---

## 1. Requisitos Previos: Open Telemetry

### Opción A: Comandos PowerShell (Recomendado)
```powershell
# 1. Definir ruta de instalación (EVITA el directorio raíz C:\)
$install_path = "C:\Users\$env:USERNAME\otel-agent"

# 2. Descargar el módulo de instalación
$module_url = "https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/latest/download/OpenTelemetry.DotNet.Auto.psm1"
$download_path = Join-Path $env:temp "OpenTelemetry.DotNet.Auto.psm1"
Invoke-WebRequest -Uri $module_url -OutFile $download_path -UseBasicParsing

# 3. Importar e instalar en la ruta elegida
Import-Module $download_path
Install-OpenTelemetryCore -InstallDirectory $install_path

# 4. Registrar el agente para IIS
Register-OpenTelemetryForIIS
```

### Opción B: Comandos CMD (Alternativa)
Si no puedes usar PowerShell directamente, ejecuta estos comandos en una terminal con permisos de Administrador:
```cmd
:: 1. Descargar el script de instalación (usando curl, incluido en Windows 10/11)
curl -L -o %TEMP%\OpenTelemetry.DotNet.Auto.psm1 https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/latest/download/OpenTelemetry.DotNet.Auto.psm1

:: 2. Instalar el agente en una carpeta de usuario (ejemplo: %USERPROFILE%\otel-agent)
powershell -Command "Import-Module %TEMP%\OpenTelemetry.DotNet.Auto.psm1; Install-OpenTelemetryCore -InstallDirectory %USERPROFILE%\otel-agent"

:: 3. Registrar para IIS
powershell -Command "Import-Module %TEMP%\OpenTelemetry.DotNet.Auto.psm1; Register-OpenTelemetryForIIS"
```
---

## 2. Configuración Final (`web.config`)

El archivo debe estar ubicado en la raíz de la aplicación desplegada (`web.config`).

### Contenido Recomendado:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\Econet.Transactions.Api.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess">
        <environmentVariables>
          <!-- 1. Observabilidad (Zero Code Overrides) -->
          <environmentVariable name="OpenTelemetry__OtlpEndpoint" value="http://localhost:4317" />
          <environmentVariable name="OpenTelemetry__ServiceName" value="econet-transactions-api-https" />
          
          <!-- 2. Variables Estándar OTel -->
          <environmentVariable name="OTEL_EXPORTER_OTLP_ENDPOINT" value="http://localhost:4317" />
          <environmentVariable name="OTEL_EXPORTER_OTLP_PROTOCOL" value="grpc" />
          <environmentVariable name="OTEL_SERVICE_NAME" value="econet-transactions-api-https" />
          <environmentVariable name="OTEL_RESOURCE_ATTRIBUTES" value="deployment.environment=development,service.namespace=econet" />
          <environmentVariable name="OTEL_DOTNET_AUTO_LOG_DIRECTORY" value=".\logs\otel-agent" />
          
          <!-- 3. Entorno y HTTPS (Puerto 8443) -->
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Development" />
          <environmentVariable name="ASPNETCORE_FORWARDEDHEADERS_ENABLED" value="true" />
          <environmentVariable name="ASPNETCORE_HTTPS_PORT" value="8443" />
          <environmentVariable name="ASPNETCORE_URLS" value="https://+:8443;http://+:8080" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
```

---

## 3. Comandos de Operación

### Aplicar Cambios de Configuración
Cada vez que se modifique el `web.config` o las variables de entorno, es recomendable reiniciar el pool de aplicaciones:

```powershell
# Reiniciar todo el servidor IIS
iisreset

# O reiniciar solo el sitio específico (Recomendado)
Restart-WebAppPool -Name "Econet.Transactions.Api"
```
---

## 4. Notas de Seguridad
- **Certificados**: Asegúrese de que el certificado SSL esté correctamente vinculado al sitio en el **IIS Manager** (Bindings -> HTTPS -> Puerto 8443).
- **Firewall**: El puerto **8443** debe estar abierto en el Firewall de Windows para tráfico entrante.
