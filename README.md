# EcoMonitor

Plataforma de observabilidad para un entorno bancario, orientada a centralizar métricas, logs y trazas de servicios .NET y de procesos transaccionales consultados desde SQL Server.
---

## 1. Qué incluye hoy el proyecto

El repositorio contiene cuatro bloques principales:

- **`observability/`**: configuración base de Grafana, Prometheus, Loki, Tempo, Alloy y OpenTelemetry Collector.
- **`services/`**: servicios .NET del proyecto:
  - `Bank.Obs.SqlPoller`
  - `Bank.Obs.FcmBridge`
  - `Bank.Obs.DemoApi`
- **`deploy/prod/`**: guía, variables de ejemplo y archivos de configuración productiva montados en contenedores.
- **`sample-logs/`**: ejemplos de logs de aplicaciones externas para pruebas de Alloy/Loki.

---

## 2. Arquitectura real del branch actual

El `docker-compose.yml` de la raíz es hoy el archivo central del stack. No existe en esta rama un `deploy/prod/docker-compose.prod.yml` funcional dentro del árbol entregado, por lo que la separación desarrollo/producción **no está resuelta mediante dos Compose principales**, sino mediante:

- `docker-compose.yml` como stack base real
- `deploy/prod/env.example` como plantilla de variables
- `deploy/prod/config/` como conjunto de archivos productivos montados

### Servicios definidos actualmente

| Servicio | Rol | Observación actual |
|---|---|---|
| `otel-collector` | Recibe OTLP y exporta métricas, logs y trazas | Publica `4317` y `4318` |
| `prometheus` | Almacena métricas | Interno, sin puerto publicado |
| `loki` | Almacena logs | Interno, sin puerto publicado |
| `tempo` | Almacena trazas | Interno, sin puerto publicado |
| `grafana` | Visualización, alertas y exploración | Publica `GRAFANA_PORT` |
| `alloy` | Lee logs desde archivos del host y los envía a Loki | Monta `BANK_LOGS_PATH` |
| `sql-poller` | Consulta SQL Server y emite métricas OTel | Worker .NET |
| `fcm-bridge` | Recibe webhooks de Grafana y envía push por Firebase | Publica `FCM_BRIDGE_PORT` |
| `redis` | Persistencia auxiliar | Interno |
| `openldap` | LDAP local de prueba | Publica `LDAP_PORT` |
| `demo-api` | API de demostración | Solo levanta con perfil `demo` |
| `otel-agent-provider` | Provee binarios de auto-instrumentación .NET | Servicio auxiliar |

---

## 3. Flujo técnico del stack

### Métricas

1. `sql-poller` consulta SQL Server.
2. Los servicios .NET y/o APIs instrumentadas envían métricas por OTLP.
3. `otel-collector` recibe y exporta métricas a Prometheus.
4. Grafana consulta Prometheus.

### Logs

1. Los servicios .NET emiten logs estructurados JSON por consola y OTLP.
2. `otel-collector` reenvía logs OTLP a Loki.
3. `alloy` lee logs desde archivos del host y también los envía a Loki.
4. Grafana consulta Loki.

### Trazas

1. Los servicios instrumentados envían trazas a `otel-collector`.
2. `otel-collector` exporta trazas a Tempo.
3. Tempo genera métricas de trazas (`local-blocks`, `service-graphs`, `span-metrics`) y las escribe en Prometheus Remote Write.
4. Grafana consulta Tempo para trazas y Prometheus para métricas derivadas.

### Alertas push

1. Grafana evalúa reglas de alertas.
2. Grafana usa el contact point `FCM-BRIDGE` contra `http://fcm-bridge:5050/alert`.
3. `fcm-bridge` valida `BRIDGE_API_KEY`.
4. `fcm-bridge` consulta usuarios habilitados en `EcoMonitorDb` y tokens en `EconetDb`.
5. Envía notificaciones vía Firebase Admin SDK.

---

## 4. Estado actual del diseño:

### Archivos importantes

- `demo-api` ya no levanta por defecto; está detrás de `profiles: ["demo"]`.
- `OPENLDAP_VERSION` está parametrizado.
- Alloy usa `ALLOY_ENV` para entorno productivo.
- El `sql-poller` ya fue dividido en servicios temáticos (`HistoricalMetricsRepository`, `PendingRepository`, `ResolutionRepository`, etc.).
- Los workers del poller se separaron en `IntraMetricsWorker`, `InterbankMetricsWorker` y `SystemMetricsWorker`.
- Tempo ya incluye `metrics_generator` y `local-blocks`, lo cual ayuda a Grafana Traces Drilldown.

### Archivos de ejemplo

- `deploy/prod/env.example` mezcla variables activas con variables heredadas o no consumidas por el código actual.
- El compose raíz sigue montando `./deploy/prod/config/ldap.toml`, por lo que el entorno “base” todavía depende de configuración productiva para LDAP.
- `deploy/prod/config/ldap.toml` contiene valores de laboratorio (`openldap`, `planetexpress`, `GoodNewsEveryone`) y una contraseña escrita directamente en el archivo;
- `fcm-bridge` y `openldap` siguen publicando puertos al host en el compose base.

---

## 5. Requisitos mínimos

### Software

- Docker Engine / Docker Desktop con `docker compose`
- Git o acceso al repositorio ya descargado
- .NET SDK solo si vas a compilar o depurar fuera de Docker

### Archivos sensibles esperados

- `observability/certs/firebase-service-account.json`
- Un archivo `.env` válido
- Configuración LDAP real si no vas a usar el LDAP local de prueba

### Accesos externos necesarios

- SQL Server para `SQLSERVER_CONN`
- Base EcoMonitor y base Econet para `fcm-bridge`
- SMTP si usarás alertas por correo
- Firebase si usarás alertas push
- Un backend S3-compatible para Loki y Tempo si ejecutarás una instalación tipo productiva, actualmente uno disponible en `https://github.com/Jhuly1215/MinIOServer`

---

## 6. Inicio rápido real del repositorio

### 6.1 Preparar variables

La raíz del proyecto no trae un `.env` listo para usar. La plantilla disponible está en:

```bash
cp deploy/prod/env.example .env
```

Después debes editar `.env` y corregir los valores reales.

> Nota: también puedes crear `deploy/prod/.env` y ejecutar Compose con `--env-file deploy/prod/.env`, pero el compose real vigente es el de la raíz.

### 6.2 Arrancar el stack base

```bash
docker compose up -d --build
```

### 6.3 Arrancar también la API demo

```bash
docker compose --profile demo up -d --build
```
---

## 7. Accesos por defecto del stack actual

| Recurso | Dirección típica |
|---|---|
| Grafana | `http://localhost:3000` o el valor de `GRAFANA_PORT` |
| OTel Collector gRPC | `localhost:4317` |
| OTel Collector HTTP | `localhost:4318` |
| FCM Bridge | `http://localhost:5050` o el valor de `FCM_BRIDGE_PORT` |
| OpenLDAP local | `ldap://localhost:389` o el valor de `LDAP_PORT` |
| Demo API | `http://localhost:5000` si levantas `--profile demo` |

---

## 8. Variables realmente importantes hoy

### Consumidas de forma efectiva por el compose/configuración actual

- `GF_SECURITY_ADMIN_USER`
- `GF_SECURITY_ADMIN_PASSWORD`
- `GRAFANA_PORT`
- `DOMAIN_NAME`
- `GRAFANA_URL`
- `GF_SMTP_HOST`
- `GF_SMTP_USER`
- `GF_SMTP_PASSWORD`
- `GF_SMTP_FROM_ADDRESS`
- `GF_SMTP_FROM_NAME`
- `SQLSERVER_CONN`
- `ECONET_DB_CONN`
- `SQL_POLLER_INTERVAL`
- `OTEL_AGENT_ENDPOINT`
- `OTEL_AGENT_LOGS_EXPORT`
- `OTEL_AGENT_METRICS_EXPORT`
- `OTEL_AGENT_TRACES_EXPORT`
- `OTEL_COLLECTOR_URL`
- `LOKI_URL`
- `TEMPO_URL`
- `PROMETHEUS_URL`
- `BANK_LOGS_PATH`
- `ALLOY_ENV`
- `BRIDGE_API_KEY`
- `FCM_BRIDGE_PORT`
- `LDAP_DOMAIN`
- `LDAP_BIND_PASSWORD`
- `LDAP_PORT`
- `OPENLDAP_VERSION`
- `LOKI_S3_*`
- `TEMPO_S3_*`
- `TEMPO_HTTP_LISTEN_PORT`
- `TEMPO_GRPC_LISTEN_PORT`
- `TEMPO_HTTP_RECEIVER_PORT`
- `TEMPO_FRONTEND_PORT`
- `TEMPO_BLOCK_RETENTION`

---

## 9. Búsqueda y filtros útiles en Grafana

### Logs

Usa `service_name` o `service.name` según la fuente:

- `bank-sql-poller`
- `bank-fcm-bridge`
- `bank-obs-demo-api`
- nombres de carpeta bajo `sample-logs/` o bajo la ruta montada en Alloy

### Trazas

En Traces Drilldown puedes filtrar por ejemplo con:

- `resource.service.name = "bank-sql-poller"`
- `resource.service.name = "bank-fcm-bridge"`
- `span.name = "POST"`
- `span.name = "POST alert"`

---

## 10. Observaciones específicas por componente

### Grafana

- Usa provisioning de datasources y alertas desde `observability/grafana/provisioning/`.
- El contact point `FCM-BRIDGE` ya apunta a `http://fcm-bridge:5050/alert`.
- LDAP está habilitado en `observability/grafana/grafana.ini`, pero el archivo real montado es `deploy/prod/config/ldap.toml`.

### SQL Poller

- No es un único worker monolítico.
- El polling quedó separado en intra, interbancario y sistema.
- Sigue dependiendo de SQL Server y de la consistencia de las tablas observadas.

### FCM Bridge

- Usa Serilog estructurado y OTLP.
- Recibe `BRIDGE_API_KEY` por variable de entorno.
- Requiere `firebase-service-account.json` montado en `/etc/secrets/firebase-service-account.json`.

### Alloy

- Lee archivos desde `BANK_LOGS_PATH`.
- Agrega `source=alloy-file` y `env=<ALLOY_ENV>`.
- La configuración base de `observability/alloy/config.alloy` es más avanzada que la de `deploy/prod/config/alloy.alloy`.

---

## 11. Archivos clave para revisión técnica

| Archivo | Qué revisar |
|---|---|
| `docker-compose.yml` | topología real del stack |
| `deploy/prod/env.example` | variables ejemplo y inconsistencias |
| `deploy/prod/config/grafana.ini` | auth LDAP y usuarios |
| `deploy/prod/config/ldap.toml` | LDAP actual, hoy de laboratorio |
| `deploy/prod/config/loki.yml` | almacenamiento S3 de Loki |
| `deploy/prod/config/tempo.yml` | trazas, TraceQL metrics y S3 |
| `observability/otel-collector-config.yml` | pipelines OTel |
| `observability/alloy/config.alloy` | estrategia base de recolección de logs |
| `services/Bank.Obs.SqlPoller/...` | lógica del poller y workers |
| `services/Bank.Obs.FcmBridge/...` | webhook, Firebase y repositorios SQL |
