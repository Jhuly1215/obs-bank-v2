# obs-bank-v2 - Producción (override)

## Requisitos
- docker + docker compose

## Configuración
1) Crear archivo de secretos:
   cp deploy/prod/env.example deploy/prod/.env
   (editar deploy/prod/.env con valores reales)

## Levantar PROD
```bash
docker compose \
  -f docker-compose.yml \
  -f deploy/prod/docker-compose.prod.yml \
  -f deploy/prod/docker-compose.minio.yml \
  -f deploy/prod/docker-compose.tempo-s3.yml \
  --env-file deploy/prod/.env \
  up -d
``` 

## Validación esperada
- Solo debe estar accesible: http://<host>:3000
- NO deben estar expuestos al host:
  - Prometheus :9090
  - Loki :3100
  - Tempo :3200
  - OTEL collector :4317/:4318/:8889
  - Alloy :12345
  - Redis (ninguno)
  - demo-api :5000 (salvo que lo habilites explícitamente)
	- 

