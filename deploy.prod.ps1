# deploy.prod.ps1
# Script para iniciar todo el stack de observabilidad de producción con S3/MinIO
Write-Host "Inicializando ObsBank Stack de Produccion..." -ForegroundColor Green

# 1. Bajar todo y limpiar volúmenes huérfanos/minio dañado si hubiera
Write-Host "Limpiando contenedores anteriores (incluyendo volúmenes si así se requiere)..."
docker compose -f docker-compose.yml -f deploy/prod/docker-compose.prod.yml down

# 2. Levantar todo el stack con la arquitectura unificada en docker-compose.prod.yml
Write-Host "Levantando el stack..."
docker compose --env-file deploy/prod/.env `
               -f docker-compose.yml `
               -f deploy/prod/docker-compose.prod.yml `
               up -d --build

Write-Host "
==================================================
  Observability Bank v2 (PROD - COMPACTED)
==================================================
  S3 MinIO (UI) -> http://localhost:9001
  Grafana (SSL) -> https://$((Get-Content deploy/prod/.env | Select-String 'DOMAIN_NAME=').ToString().Split('=')[1])
  Demo API      -> Oculto en Prod (Sin puerto expuesto)
  FCM / SMTP    -> Configurado mediante .env
==================================================
NOTA: El puerto de MinIO Console es configurable en el .env." -ForegroundColor Cyan
