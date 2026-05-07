# ==========================================================
# EcoMonitor - Deploy productivo
# Usa SOLO deploy/prod/docker-compose.prod.yml
# No mezcla docker-compose.yml base para evitar heredar configuración DEV.
#
# IMPORTANTE:
# No usar --project-directory aquí, porque docker-compose.prod.yml
# está dentro de deploy/prod y sus rutas relativas usan ../../
# ==========================================================

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $Root

$EnvFile = Join-Path $Root "deploy/prod/.env"
$ComposeFile = Join-Path $Root "deploy/prod/docker-compose.prod.yml"

$LokiConfig = Join-Path $Root "deploy/prod/config/loki.yml"
$TempoConfig = Join-Path $Root "deploy/prod/config/tempo.yml"
$GrafanaConfig = Join-Path $Root "deploy/prod/config/grafana.ini"
$AlloyConfig = Join-Path $Root "deploy/prod/config/alloy.alloy"
$LdapConfig = Join-Path $Root "deploy/prod/config/ldap.toml"
$FirebaseJson = Join-Path $Root "observability/certs/firebase-service-account.json"

function Write-Section {
  param([string]$Text)

  Write-Host ""
  Write-Host "==================================================" -ForegroundColor Cyan
  Write-Host " $Text" -ForegroundColor Cyan
  Write-Host "==================================================" -ForegroundColor Cyan
}

function Fail {
  param([string]$Message)

  Write-Host "ERROR: $Message" -ForegroundColor Red
  exit 1
}

function Warn {
  param([string]$Message)

  Write-Host "ADVERTENCIA: $Message" -ForegroundColor Yellow
}

function Ok {
  param([string]$Message)

  Write-Host "OK: $Message" -ForegroundColor Green
}

function Require-File {
  param(
    [string]$Path,
    [string]$Description
  )

  if (!(Test-Path $Path)) {
    Fail "No existe $Description en: $Path"
  }

  Ok "$Description encontrado"
}

function Get-EnvValue {
  param(
    [string]$FilePath,
    [string]$Key
  )

  $line = Get-Content $FilePath |
  Where-Object {
    $_ -match "^\s*$([regex]::Escape($Key))\s*="
  } |
  Select-Object -Last 1

  if (-not $line) {
    return $null
  }

  return ($line -replace "^\s*$([regex]::Escape($Key))\s*=", "").Trim()
}

function Require-EnvValue {
  param([string]$Key)

  $value = Get-EnvValue -FilePath $EnvFile -Key $Key

  if ([string]::IsNullOrWhiteSpace($value)) {
    Fail "La variable $Key no está definida en deploy/prod/.env"
  }

  if ($value -match "CAMBIAR_|CHANGE_ME|REEMPLAZAR") {
    Fail "La variable $Key todavía tiene un valor placeholder: $value"
  }

  Ok "Variable $Key definida"
}

function Invoke-Compose {
  param(
    [Parameter(Mandatory = $true)]
    [string[]]$ComposeArgs
  )

  & docker compose --env-file "$EnvFile" -f "$ComposeFile" @ComposeArgs

  if ($LASTEXITCODE -ne 0) {
    throw "docker compose $($ComposeArgs -join ' ') falló con código $LASTEXITCODE"
  }
}

Write-Section "EcoMonitor - Deploy productivo"

Write-Host "Directorio raíz : $Root" -ForegroundColor White
Write-Host "Archivo .env    : $EnvFile" -ForegroundColor White
Write-Host "Compose PROD    : $ComposeFile" -ForegroundColor White

Write-Section "Validando archivos requeridos"

Require-File $EnvFile "archivo deploy/prod/.env"
Require-File $ComposeFile "archivo deploy/prod/docker-compose.prod.yml"
Require-File $LokiConfig "configuración productiva de Loki"
Require-File $TempoConfig "configuración productiva de Tempo"
Require-File $GrafanaConfig "configuración productiva de Grafana"
Require-File $AlloyConfig "configuración productiva de Alloy"
Require-File $LdapConfig "configuración LDAP de Grafana"
Require-File $FirebaseJson "firebase-service-account.json"

Write-Section "Validando variables críticas de producción"

Require-EnvValue "DOMAIN_NAME"
Require-EnvValue "GRAFANA_URL"
Require-EnvValue "GRAFANA_PORT"

Require-EnvValue "OTEL_COLLECTOR_VERSION"
Require-EnvValue "PROMETHEUS_VERSION"
Require-EnvValue "LOKI_VERSION"
Require-EnvValue "TEMPO_VERSION"
Require-EnvValue "GRAFANA_VERSION"
Require-EnvValue "ALLOY_VERSION"
Require-EnvValue "REDIS_VERSION"

Require-EnvValue "GF_SECURITY_ADMIN_USER"
Require-EnvValue "GF_SECURITY_ADMIN_PASSWORD"

Require-EnvValue "SQLSERVER_CONN"
Require-EnvValue "ECONET_DB_CONN"

Require-EnvValue "BRIDGE_API_KEY"

Require-EnvValue "MINIO_SCHEME"
Require-EnvValue "MINIO_ENDPOINT"
Require-EnvValue "MINIO_ROOT_USER"
Require-EnvValue "MINIO_ROOT_PASSWORD"
Require-EnvValue "MINIO_BUCKET_LOKI"
Require-EnvValue "MINIO_BUCKET_TEMPO"

Require-EnvValue "LOKI_S3_ENDPOINT"
Require-EnvValue "LOKI_S3_BUCKET"
Require-EnvValue "LOKI_S3_ACCESS_KEY"
Require-EnvValue "LOKI_S3_SECRET_KEY"
Require-EnvValue "LOKI_S3_INSECURE"
Require-EnvValue "LOKI_S3_SKIP_VERIFY"
Require-EnvValue "LOKI_RETENTION_PERIOD"

Require-EnvValue "TEMPO_S3_ENDPOINT"
Require-EnvValue "TEMPO_S3_BUCKET"
Require-EnvValue "TEMPO_S3_ACCESS_KEY"
Require-EnvValue "TEMPO_S3_SECRET_KEY"
Require-EnvValue "TEMPO_S3_INSECURE"
Require-EnvValue "TEMPO_S3_SKIP_VERIFY"
Require-EnvValue "TEMPO_BLOCK_RETENTION"

Require-EnvValue "OTLP_GRPC_PORT"
Require-EnvValue "OTLP_HTTP_PORT"

Write-Section "Revisando coherencia de configuración"

$minioScheme = Get-EnvValue -FilePath $EnvFile -Key "MINIO_SCHEME"
$lokiEndpoint = Get-EnvValue -FilePath $EnvFile -Key "LOKI_S3_ENDPOINT"
$tempoEndpoint = Get-EnvValue -FilePath $EnvFile -Key "TEMPO_S3_ENDPOINT"
$lokiInsecure = Get-EnvValue -FilePath $EnvFile -Key "LOKI_S3_INSECURE"
$tempoInsecure = Get-EnvValue -FilePath $EnvFile -Key "TEMPO_S3_INSECURE"
$lokiSkip = Get-EnvValue -FilePath $EnvFile -Key "LOKI_S3_SKIP_VERIFY"
$tempoSkip = Get-EnvValue -FilePath $EnvFile -Key "TEMPO_S3_SKIP_VERIFY"
$sqlConn = Get-EnvValue -FilePath $EnvFile -Key "SQLSERVER_CONN"

if ($lokiEndpoint -eq "minio:9000" -or $tempoEndpoint -eq "minio:9000") {
  Fail "En PROD no debes usar minio:9000. Debes usar minio.ecofuturo.com.bo porque MinIO estará en otro servidor físico."
}

if ($minioScheme -eq "https") {
  if ($lokiInsecure -eq "true") {
    Fail "LOKI_S3_INSECURE=true no corresponde si usas HTTPS. Debe ser false."
  }

  if ($tempoInsecure -eq "true") {
    Fail "TEMPO_S3_INSECURE=true no corresponde si usas HTTPS. Debe ser false."
  }
}

if ($lokiSkip -eq "true" -or $tempoSkip -eq "true") {
  Warn "Tienes SKIP_VERIFY=true. Sirve temporalmente por certificado, pero no debería quedar así en producción final."
}

if ($sqlConn -match "Database=EconetTransacciones") {
  Warn "SQLSERVER_CONN apunta a EconetTransacciones. La arquitectura final indica que debe apuntar a la BD EcoMonitor."
}

if ($sqlConn -match "Password=admin") {
  Warn "SQLSERVER_CONN parece usar una contraseña de prueba. Revisa credenciales productivas."
}

Ok "Validación de coherencia terminada"

Write-Section "Validando Docker"

try {
  docker version | Out-Null

  if ($LASTEXITCODE -ne 0) {
    throw "docker version falló"
  }

  Ok "Docker responde correctamente"
}
catch {
  Fail "Docker no responde. Verifica que Docker esté instalado y el daemon esté activo."
}

try {
  docker compose version | Out-Null

  if ($LASTEXITCODE -ne 0) {
    throw "docker compose version falló"
  }

  Ok "Docker Compose responde correctamente"
}
catch {
  Fail "Docker Compose no responde."
}

Write-Section "Validando docker-compose.prod.yml"

try {
  Invoke-Compose @("config") | Out-Null
  Ok "La configuración Compose es válida"
}
catch {
  Fail "docker compose config falló. Revisa deploy/prod/.env y deploy/prod/docker-compose.prod.yml. Detalle: $($_.Exception.Message)"
}

Write-Section "Resumen de despliegue"

Write-Host "Stack               : EcoMonitor PROD" -ForegroundColor White
Write-Host "Compose usado       : deploy/prod/docker-compose.prod.yml" -ForegroundColor White
Write-Host "Archivo .env        : deploy/prod/.env" -ForegroundColor White
Write-Host "Grafana interno     : http://127.0.0.1:3000" -ForegroundColor White
Write-Host "Grafana publicado   : https://obs.ecofuturo.com.bo" -ForegroundColor White
Write-Host "OTLP gRPC           : 4317" -ForegroundColor White
Write-Host "OTLP HTTP           : 4318" -ForegroundColor White
Write-Host "MinIO remoto        : https://minio.ecofuturo.com.bo" -ForegroundColor White
Write-Host "MinIO consola       : https://minio-console.ecofuturo.com.bo" -ForegroundColor White
Write-Host ""
Write-Host "Este script NO levanta MinIO. MinIO debe estar corriendo aparte en el servidor del proyecto MinIOServer." -ForegroundColor Yellow

Write-Section "Bajando stack anterior"

try {
  Invoke-Compose @("down", "--remove-orphans")
  Ok "Stack anterior detenido"
}
catch {
  Fail "No se pudo bajar el stack anterior. Detalle: $($_.Exception.Message)"
}

Write-Section "Levantando stack productivo"

try {
  Invoke-Compose @("up", "-d", "--build")
  Ok "Stack productivo levantado"
}
catch {
  Fail "No se pudo levantar el stack productivo. Detalle: $($_.Exception.Message)"
}

Write-Section "Estado de contenedores"

try {
  Invoke-Compose @("ps")
}
catch {
  Warn "No se pudo listar el estado de los contenedores. Detalle: $($_.Exception.Message)"
}

Write-Section "Validación rápida"

Write-Host "Esperando unos segundos para que los servicios inicialicen..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

$containers = @(
  "ecomonitor-minio-init",
  "ecomonitor-loki",
  "ecomonitor-tempo",
  "ecomonitor-grafana",
  "ecomonitor-sql-poller",
  "ecomonitor-fcm-bridge"
)

foreach ($container in $containers) {
  Write-Host ""
  Write-Host "Logs de ${container}:" -ForegroundColor Cyan

  docker ps -a --format "{{.Names}}" | Select-String -SimpleMatch $container | Out-Null

  if ($LASTEXITCODE -eq 0) {
    docker logs $container --tail 50
  }
  else {
    Warn "No existe el contenedor $container"
  }
}

Write-Section "Prueba de acceso local a Grafana"

try {
  $response = Invoke-WebRequest -Uri "http://127.0.0.1:3000/api/health" -UseBasicParsing -TimeoutSec 10

  if ($response.StatusCode -eq 200) {
    Ok "Grafana responde en http://127.0.0.1:3000/api/health"
  }
  else {
    Warn "Grafana respondió con código HTTP $($response.StatusCode)"
  }
}
catch {
  Warn "No se pudo consultar Grafana local todavía. Puede estar inicializando o IIS/Docker aún no terminó de levantar."
}

Write-Section "EcoMonitor PROD levantado"

Write-Host "Grafana interno     : http://127.0.0.1:3000" -ForegroundColor White
Write-Host "Grafana publicado   : https://obs.ecofuturo.com.bo" -ForegroundColor White
Write-Host "OTLP gRPC           : 4317" -ForegroundColor White
Write-Host "OTLP HTTP           : 4318" -ForegroundColor White
Write-Host "MinIO remoto        : https://minio.ecofuturo.com.bo" -ForegroundColor White
Write-Host "MinIO consola       : https://minio-console.ecofuturo.com.bo" -ForegroundColor White
Write-Host ""
Write-Host "Comandos útiles:" -ForegroundColor Cyan
Write-Host "docker logs ecomonitor-minio-init" -ForegroundColor White
Write-Host "docker logs ecomonitor-loki" -ForegroundColor White
Write-Host "docker logs ecomonitor-tempo" -ForegroundColor White
Write-Host "docker logs ecomonitor-grafana" -ForegroundColor White
Write-Host "docker logs ecomonitor-sql-poller" -ForegroundColor White
Write-Host "docker logs ecomonitor-fcm-bridge" -ForegroundColor White
Write-Host "==================================================" -ForegroundColor Cyan