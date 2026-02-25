# obs-bank-v2

Stack de **observabilidad end-to-end** orientado a un escenario bancario (simulado), construido con **.NET 9 + OpenTelemetry + Grafana (Prometheus/Loki/Tempo)** y orquestado con **Docker Compose**.

> **Importante:** este repositorio **no es un core bancario** ni una banca completa. Es un **sandbox/laboratorio de observabilidad** con:
> - una API demo (`demo-api`) que simula transferencias,
> - un worker (`sql-poller`) que consulta SQL Server y expone métricas,
> - y un stack de observabilidad para centralizar **logs, métricas y trazas**.

---

## Tabla de contenido

- [Qué hace este proyecto](#qué-hace-este-proyecto)
- [Arquitectura](#arquitectura)
- [Componentes](#componentes)
- [Estructura del repositorio](#estructura-del-repositorio)
- [Tecnologías](#tecnologías)
- [Requisitos](#requisitos)
- [Cómo levantar el entorno](#cómo-levantar-el-entorno)
- [Accesos y puertos](#accesos-y-puertos)
- [Uso rápido](#uso-rápido)
- [Flujo de observabilidad](#flujo-de-observabilidad)
- [Configuración clave](#configuración-clave)
- [Troubleshooting](#troubleshooting)
- [Limitaciones actuales](#limitaciones-actuales)
- [Mejoras sugeridas](#mejoras-sugeridas)

---

## Qué hace este proyecto

Este proyecto centraliza telemetría desde distintas fuentes:

1. **Aplicaciones .NET instrumentadas con OpenTelemetry**
   - `demo-api` (API mínima)
   - `sql-poller` (worker de métricas desde SQL Server)

2. **Logs de archivos locales**
   - Ingeridos por **Grafana Alloy** desde `sample-logs/`

3. **Backend de observabilidad**
   - **Prometheus** (métricas)
   - **Loki** (logs)
   - **Tempo** (trazas)
   - **Grafana** (visualización y correlación)

### Casos de uso del repo
- Probar OpenTelemetry en .NET
- Validar pipelines OTLP → Collector → Grafana stack
- Correlacionar logs/trazas/métricas en un flujo tipo “transferencia bancaria”
- Exponer métricas operativas obtenidas desde SQL Server

---

## Arquitectura

```text
                     ┌────────────────────────────┐
                     │        demo-api (.NET 9)   │
                     │  - Logs OTEL               │
HTTP :5000 ────────▶ │  - Traces OTEL             │
                     │  - Metrics OTEL            │
                     └─────────────┬──────────────┘
                                   │ OTLP gRPC (4317)
                                   ▼
                     ┌────────────────────────────┐
                     │  OpenTelemetry Collector   │
                     │  Receivers: OTLP gRPC/HTTP │
                     │  Pipelines: traces/metrics/logs
                     └───────┬─────────┬──────────┘
                             │         │
                  metrics    │         │ logs
               (Prom exp)    │         │
                             ▼         ▼
                     ┌────────────┐  ┌───────────┐
                     │ Prometheus │  │   Loki    │
                     └─────┬──────┘  └─────┬─────┘
                           │               │
                           └──────┬────────┘
                                  ▼
                            ┌──────────┐
                            │ Grafana  │
                            └────┬─────┘
                                 │
                                 ▼
                              ┌──────┐
                              │Tempo │
                              └──────┘


      ┌────────────────────────────┐           ┌───────────┐
      │   sql-poller (.NET 9)      │ OTLP gRPC │ OTel Col. │
      │ - consulta SQL Server      ├──────────▶│           │
      │ - emite métricas OTEL      │           └───────────┘
      └──────────────┬─────────────┘
                     │ SQL Server (externo)
                     ▼
                [SQLSERVER_CONN]


      ┌────────────────────────────┐
      │ Grafana Alloy              │
      │ - lee sample-logs          │
      │ - envía a Loki             │
      └──────────────┬─────────────┘
                     ▼
                    Loki

```

---
## Componentes

### 1) `demo-api` (API demo en .NET 9)

API mínima instrumentada con OpenTelemetry que simula transferencias.

#### Funcionalidad
- Expone endpoint de health
- Expone endpoint de transferencia simulada
- Genera logs estructurados
- Emite trazas y métricas por OTLP al OTel Collector
- Usa `X-Correlation-Id` para correlación (si no llega, lo genera)

#### Endpoints
- `GET /health`
- `POST /api/transactions/transfer`

#### Qué **no** hace
- No persiste transferencias
- No implementa core bancario real
- No integra con terceros reales

---

### 2) `sql-poller` (worker de métricas desde SQL Server)

Worker en .NET que consulta periódicamente un SQL Server externo y transforma resultados en métricas (via OpenTelemetry).

#### Funcionalidad
- Se ejecuta en background
- Lee conexión desde `SQLSERVER_CONN`
- Consulta tablas del dominio de transferencias
- Calcula métricas operativas (conteos, pendientes, ratios, antigüedad)
- Exporta métricas OTLP al Collector

#### Dependencia clave
Necesita acceso a SQL Server con tablas como:
- `Transferencia`
- `TransferenciaInterbancaria`

> Si no defines `SQLSERVER_CONN`, este servicio fallará al iniciar.

---

### 3) OpenTelemetry Collector

Punto central de recepción de telemetría.

#### Pipelines
- **traces** → Tempo
- **metrics** → Prometheus exporter (`:8889`)
- **logs** → Loki

También tiene exporter `debug` (útil para demo; ruidoso en producción).

---

### 4) Prometheus

Hace scrape al endpoint de métricas expuesto por el Collector (`otel-collector:8889`) y a sí mismo.

> No scrapea directamente `demo-api` ni `sql-poller`.

---

### 5) Loki

Backend de logs para:
- logs enviados por OTel Collector (desde apps .NET)
- logs de archivos enviados por Alloy

---

### 6) Tempo

Backend de trazas OpenTelemetry.

---

### 7) Grafana

UI de observabilidad con datasources provisionados:
- Prometheus
- Loki
- Tempo

Incluye provisioning de datasources, pero **no dashboards listos** (ver [Limitaciones actuales](#limitaciones-actuales)).

---

### 8) Grafana Alloy

Lee logs desde `sample-logs/` y los envía a Loki.

#### Rutas esperadas (según config)
- `InterbancariaAsyncAPI/*.log`
- `TransaccionInternaApi/*.log`
- `logsEconetTransacciones/*.json`
- `logsEconetTransaccionesInterbancarias/*.json`

---

## Estructura del repositorio

```text
obs-bank-v2/
├─ docker-compose.yml
├─ README.md
├─ sample-logs/
│  ├─ InterbancariaAsyncAPI/
│  ├─ TransaccionInternaApi/
│  ├─ logsEconetTransacciones/
│  └─ logsEconetTransaccionesInterbancarias/
├─ observability/
│  ├─ otel-collector-config.yml
│  ├─ prometheus.yml
│  ├─ loki-config.yml
│  ├─ tempo.yml
│  ├─ alloy/
│  │  └─ config.alloy
│  └─ grafana/
│     └─ provisioning/
│        ├─ datasources/
│        │  └─ datasources.yml
│        └─ dashboards/
│           └─ dashboards.yml
└─ services/
   ├─ Bank.Obs.DemoApi/
   │  └─ Bank.Obs.DemoApi/
   └─ Bank.Obs.SqlPoller/
      └─ Bank.Obs.SqlPoller/
```
## Tecnologías

- **.NET 9**
- **OpenTelemetry (OTLP)**
- **OpenTelemetry Collector**
- **Prometheus**
- **Loki**
- **Tempo**
- **Grafana**
- **Grafana Alloy**
- **Docker Compose**
- **SQL Server** (externo, para `sql-poller`)

---

## Requisitos

### Requisitos mínimos
- Docker
- Docker Compose
- (Opcional pero recomendado) SQL Server accesible si quieres usar `sql-poller`

### Puertos libres (host)
- `3000` (Grafana)
- `3100` (Loki)
- `3200` (Tempo)
- `4317` (OTLP gRPC - Collector)
- `4318` (OTLP HTTP - Collector)
- `4319` (OTLP gRPC - Tempo, opcional)
- `5000` (demo-api)
- `9090` (Prometheus)
- `12345` (Alloy)
- `8889` (Prometheus exporter del Collector)

---

## Cómo levantar el entorno

### Opción A: Stack completo (incluyendo `sql-poller`)
> Requiere `SQLSERVER_CONN`

#### Linux / macOS
```bash
export SQLSERVER_CONN="Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True"
docker compose up --build
```
### Opción B: Levantar sin sql-poller (si no tienes SQL Server)
```bash
docker compose up --build otel-collector prometheus loki tempo grafana alloy demo-api
```
## Accesos y puertos

### UIs / APIs
- **Grafana**: `http://localhost:3000`
  - usuario: `admin`
  - contraseña: `admin`
- **Prometheus**: `http://localhost:9090`
- **Loki**: `http://localhost:3100`
- **Tempo**: `http://localhost:3200`
- **Alloy**: `http://localhost:12345`
- **Demo API**: `http://localhost:5000`

> Credenciales y configuración actuales son de **demo/local**, no de producción.

---

## Uso rápido

### 1) Verificar que la API demo responde
Abre en tu navegador:

- `http://localhost:5000/health`

Deberías recibir una respuesta JSON con estado del servicio.

---

### 2) Ejecutar una transferencia simulada

Haz un `POST` a:

- `http://localhost:5000/api/transactions/transfer`

#### Opción recomendada (sin consola): Postman / Insomnia
- Método: `POST`
- URL: `http://localhost:5000/api/transactions/transfer`
- Body: vacío (si el endpoint no exige payload)
- Header opcional:
  - `X-Correlation-Id: prueba-123`

> Si falla de forma aleatoria, no siempre es un bug: la API simula fallos/latencia como parte del escenario demo.

---

### 3) Probar correlación (`X-Correlation-Id`)
Puedes enviar manualmente este header en Postman/Insomnia:

- **Key:** `X-Correlation-Id`
- **Value:** `prueba-123`

Luego, en Grafana (Loki), puedes buscar logs relacionados con ese valor para ver la trazabilidad del request.

---

### 4) Revisar telemetría en Grafana

Entra a **Grafana** (`http://localhost:3000`) y revisa:

- **Logs** (Loki): eventos del `demo-api` y de `sample-logs`
- **Traces** (Tempo): trazas OTEL de la API
- **Métricas** (Prometheus): métricas de apps y collector

> Si no ves nada al principio, genera tráfico llamando varias veces al endpoint de transferencia.

---

## Flujo de observabilidad

### A) `demo-api`
1. Se llama `POST /api/transactions/transfer`
2. La API genera logs, trazas y métricas con OpenTelemetry
3. Envía la telemetría al **OpenTelemetry Collector** (`otel-collector:4317`)
4. El Collector distribuye:
   - trazas → **Tempo**
   - métricas → **Prometheus**
   - logs → **Loki**
5. **Grafana** consume esas fuentes y permite correlación

---

### B) `sql-poller`
1. El worker consulta SQL Server periódicamente
2. Calcula métricas operativas (conteos, pendientes, ratios, antigüedad)
3. Publica métricas vía OpenTelemetry
4. El Collector las entrega a Prometheus
5. Grafana las visualiza

---

### C) `sample-logs` + Alloy
1. Alloy lee logs desde archivos montados
2. Los envía a Loki con labels
3. Grafana consulta Loki y los muestra junto a logs de aplicaciones

---

## Configuración clave

### Variables de entorno importantes

#### `SQLSERVER_CONN`
Connection string para `sql-poller`.

Ejemplo de formato:
```text
Server=mi-servidor;Database=MiBD;User Id=usuario;Password=clave;TrustServerCertificate=True
```
### `demo-api`
- Corre en modo `Development` dentro del compose
- Envía telemetría OTLP al Collector (`otel-collector:4317`)

---

### `sql-poller`
- Toma conexión desde `SqlPoller__ConnectionString` (inyectada con `${SQLSERVER_CONN}`)
- Usa `SqlPoller__IntervalSeconds` para la frecuencia de polling (en compose está en `30`)

---

### OpenTelemetry Collector
- Recibe OTLP por gRPC y HTTP
- Exporta a:
  - Tempo (trazas)
  - Loki (logs)
  - Prometheus (métricas)
- También usa exporter `debug` (útil en demo, no en producción)

---

### Alloy (logs de archivos)
- Tiene `tail_from_end = true`
  - Empieza a leer desde el final del archivo
  - No reingesta automáticamente todo el histórico previo
- Añade labels para identificar origen/tipo de log

---

## Troubleshooting

### `sql-poller` no levanta
**Causas probables:**
- Falta `SQLSERVER_CONN`
- Connection string inválida
- SQL Server inaccesible
- Tablas esperadas no existen o no hay permisos

**Cómo revisarlo (sin consola):**
- Abre Docker Desktop
- Ve al contenedor `sql-poller`
- Revisa logs de inicio

**Qué deberías confirmar antes de culpar al código:**
- ¿La BD y tablas realmente existen?
- ¿El usuario tiene permisos de lectura?
- ¿La red permite conexión desde Docker?

---

### No aparecen métricas en Prometheus/Grafana
**Qué revisar:**
- `demo-api` y/o `sql-poller` están activos
- `otel-collector` está corriendo
- Prometheus está levantado
- Hay tráfico generado (si no llamas la API, habrá poca señal)

**Revisión visual recomendada:**
1. Grafana → Explore → Prometheus
2. Busca métricas relacionadas con OTEL / runtime / servicio
3. Si no hay datos, revisa logs del Collector en Docker Desktop

> Error común: asumir que Prometheus scrapea directo a `demo-api`. En este stack, scrapea al **Collector**.

---

### No aparecen logs de `sample-logs` en Loki
**Qué revisar:**
- Que la carpeta `sample-logs/` tenga archivos
- Que Alloy esté corriendo
- Que las rutas coincidan con las definidas en la config
- Que entiendas el efecto de `tail_from_end = true`

**Señal de diagnóstico útil:**
- En Grafana → Explore → Loki, intenta listar labels y filtrar por `source=alloy-file`

---

### Grafana abre pero no ves dashboards
Eso es esperable en el estado actual del repo.

**Razón:** el repo provisiona **datasources**, pero no dashboards listos para usar.

---

## Mejoras sugeridas

### Prioridad alta
- [ ] Agregar dashboards versionados (JSON) y provisioning real
- [ ] Agregar healthchecks en `docker-compose.yml`
- [ ] Documentar nombres exactos de métricas exportadas
- [ ] Parsing real de JSON en Alloy (stages/pipeline), no solo labels
- [ ] Alertas básicas (Prometheus/Grafana Alerting) para fallos del `sql-poller`

### Prioridad media
- [ ] Separar configuraciones por ambiente (`dev`, `demo`, `prod`)
- [ ] Reintentos/backoff más robustos en `sql-poller`
- [ ] Limpiar artefactos de plantilla (`weatherforecast`)
- [ ] Tests de integración para `demo-api` y consultas SQL

### Prioridad baja
- [ ] Ajustar `sql-poller` a SDK de Worker (más coherente semánticamente)
- [ ] Añadir ejemplos de consultas de Loki / PromQL / TraceQL
- [ ] Añadir scripts/generador de tráfico para la API demo

---

## Seguridad (importante)

Este stack está orientado a **entornos locales de prueba**. No lo despliegues en producción tal como está sin:

- credenciales seguras
- autenticación/autorización
- TLS
- endurecimiento de contenedores
- gestión de secretos
- límites de recursos
- retención/almacenamiento adecuados

---

## Notas finales

La arquitectura base está bien encaminada para observabilidad (Collector + Prometheus/Loki/Tempo + Grafana), pero todavía le faltan piezas operativas clave:

- dashboards
- alertas
- healthchecks
- documentación de métricas
- hardening

Conclusión directa: **es un laboratorio funcional**, no una solución terminada.
---