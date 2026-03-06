# obs-bank-v2 - Documentación Técnica

Stack de **observabilidad end-to-end** orientado a un escenario bancario (simulado), construido con **.NET 9 + OpenTelemetry + Grafana (Prometheus/Loki/Tempo)** y orquestado con **Docker Compose**.

> **Importante:** Este repositorio **no es un core bancario** ni una banca completa. Es un **sandbox/laboratorio de observabilidad** diseñado para demostrar la integración de telemetría y su maduración hacia entornos productivos.

---

## Tabla de contenido
- [Qué hace este proyecto](#qué-hace-este-proyecto)
- [Arquitectura](#arquitectura)
- [Componentes](#componentes)
- [Estructura del Repositorio](#estructura-del-repositorio)
- [Despliegue y Ambientes](#despliegue-y-ambientes)
- [Flujo de Observabilidad y Uso Rápido](#flujo-de-observabilidad-y-uso-rápido)
- [Configuración Clave](#configuración-clave)
- [Troubleshooting](#troubleshooting)
- [Seguridad y Recomendaciones de Producción](#seguridad-y-recomendaciones-de-producción)

---

## Qué hace este proyecto
Este proyecto centraliza telemetría (logs, métricas y trazas) desde distintas fuentes:
1. **Aplicaciones .NET instrumentadas con OpenTelemetry** (`demo-api` y `sql-poller`).
2. **Logs de archivos locales** ingeridos por **Grafana Alloy**.
3. **Backend de observabilidad** basado en el ecosistema de Grafana (Prometheus, Loki, Tempo).

### Casos de uso
- Probar OpenTelemetry en .NET.
- Validar pipelines OTLP → Collector → Grafana stack.
- Configurar backends escalables usando MinIO (S3) para almacenamiento de logs y trazas en un contexto casi real.
- Exponer métricas operativas obtenidas desde fuentes externas de base de datos SQL.

---

## Arquitectura

La arquitectura soporta dos métodos de almacenamiento principales dependiendo del entorno configurado: **Local (Filesystem)** para desarrollo rápido y **Producción (Object Storage S3 con MinIO)**.

```text
                     ┌────────────────────────────┐
                     │        demo-api (.NET 9)   │
                     │  - Logs, Traces, Metrics   │
                     └─────────────┬──────────────┘
                                   │ OTLP (4317)
                                   ▼
                     ┌────────────────────────────┐
                     │  OpenTelemetry Collector   │
                     └───────┬─────────┬──────────┘
                             │         │
                  metrics    │         │ logs, traces
                             ▼         ▼
                     ┌────────────┐  ┌───────────┐  ┌───────────┐
                     │ Prometheus │  │   Loki    │  │   Tempo   │
                     └─────┬──────┘  └─────┬─────┘  └─────┬─────┘
                           │               │              │
                           │               ▼              ▼
                           │         [Almacenamiento S3 / MinIO] * (En Producción)
                           └───────────────┬──────────────┘
                                           │
                                           ▼
                                     ┌──────────┐
                                     │ Grafana  │
                                     └──────────┘
```

Componentes adicionales inyectan datos al stack:
- **`sql-poller`**: Consulta un servidor SQL externo y expone métricas operativas de forma constante enviándolas al Collector.
- **`Grafana Alloy`**: Observa el directorio físico `sample-logs/` ingestando estos logs sueltos a Loki de forma estructurada.

---

## Componentes

### 1) `demo-api` (API demo en .NET 9)
Expone `/health` y `POST /api/transactions/transfer`. Emite logs enriquecidos, trazas distribuidas y métricas por OTLP usando la telemetría .NET predeterminada y personalizada al Collector. Integra y reenvía headers tipo `X-Correlation-Id` en las requests.

### 2) `sql-poller`
Worker standalone de .NET que lee constantemente métricas de negocio desde una base de datos utilizando `SQLSERVER_CONN`. Genera métricas operativas en formato OpenTelemetry (indicadores de transferencias, tiempos).

### 3) OpenTelemetry Collector
Recibe métricas, logs y trazas a través del de los receivers OTLP gRPC/HTTP (`4317`/`4318`) y las enruta a través de diversos *pipelines* hacia Prometheus (`:8889`), Loki y Tempo de forma unificada.

### 4) Stack de Almacenamiento y Visualización
- **Prometheus**: Realiza *scraping* de métricas expuestas en el Collector (`:8889`).
- **Loki**: Sistema de manejo de logs altamente escalable (optimizado usando meta-labels).
- **Tempo**: Backend ligero de trazas distribuidas. 
- **MinIO (Sólo Prod)**: Nodo S3 compatible que otorga *object storage* a las estructuras de Loki y Tempo para archivar grandes volúmenes de datos en paralelo.
- **Grafana**: Panel visual universal centralizado (interfaz expuesta en `localhost:3000`).

---

## Estructura del Repositorio

La arquitectura cuenta con su rama original base, sumándole un subdirectorio con "overrides" destinados al despliegue productivo.

```text
obs-bank-v2/
├─ docker-compose.yml           # Definición de servicios base (Modo Local/Desarrollo)
├─ deploy/
│  └─ prod/                     # Directivas compose y configuraciones para Producción
│     ├─ .env                   # Variables de entorno y contraseñas
│     ├─ docker-compose.prod.yml
│     ├─ docker-compose.minio.yml
│     ├─ docker-compose.loki-s3.yml
│     ├─ docker-compose.tempo-s3.yml
│     └─ docker-compose.loki-config-prod.yml
├─ observability/               # Mapeo de Volúmenes y configuraciones nativas
│  ├─ loki-config.yml           # Configuración Local (filesystem storage)
│  └─ loki-config.prod.yml      # Configuración Prod (S3 block storage)
├─ sample-logs/                 # Ejemplos de logs recogidos por Alloy
└─ services/                    # Código fuente base .NET 9 API y Workers
```

---

## Despliegue y Ambientes

Antes de iniciar cualquier comando, asegúrate de detener arquitecturas previamente subidas del mismo puerto para evitar colisión de hosts.

### Opción A: Entorno Local (Desarrollo Rápido)
Modo rápido y liviano. Evita MinIO usando el almacenamiento base del disco.
```bash
docker-compose up --build -d
```
> **Nota Opcional:** Si no definiste un servidor de base de datos válido en la variable global `SQLSERVER_CONN`, el contenedor `sql-poller` arrojará excepciones de inicialización.

### Opción B: Entorno Producción (Arquitectura S3 Storage)
Se superponen y combinan archivos compose del directorio `deploy/prod/`. Esto inicializa a MinIO con un contenedor auxiliar aprovisionando automáticamente los *buckets* `loki` y `tempo`.
```bash
docker-compose -f docker-compose.yml \
  -f deploy/prod/docker-compose.prod.yml \
  -f deploy/prod/docker-compose.minio.yml \
  -f deploy/prod/docker-compose.loki-s3.yml \
  -f deploy/prod/docker-compose.tempo-s3.yml \
  -f deploy/prod/docker-compose.loki-config-prod.yml \
  up -d
```

---

## Flujo de Observabilidad y Uso Rápido

1. **Uso Mínimo de la API**:
   - Comprobación: GET `http://localhost:5000/health`
   - Transacción: POST `http://localhost:5000/api/transactions/transfer` (Puedes adjuntar en la cabecera del Request HTTP o Postman: `X-Correlation-Id: test-trace-123`).

2. **Visualizar y Correlacionar**:
   - Navega al **Grafana** (`http://localhost:3000` — Login: `admin`/`admin`).
   - Ve a `Explore` y busca en el *datasource* de Loki tu log asociado al *correlation ID* enviado durante el HTTP POST.
   - Observa si el log lleva asociado un Trace ID, y presiona el enlace generado para que te redirija al datasource de Tempo, mostrando el flujo interno estructurado.

---

## Configuración Clave

- **`SQLSERVER_CONN`**: Requerido por el `sql-poller`. Ejemplo `Server=171.3.0.58,1433;Database=MiDB;User Id=Usr;Password=Psw;TrustServerCertificate=True;`.
- **MinIO Bindings**: Variables en `deploy/prod/.env` especifican S3 endpoints (Ej: `LOKI_S3_ENDPOINT=http://minio:9000` y `TEMPO_S3_ENDPOINT=minio:9000`).

---

## Troubleshooting

- **Crash del `sql-poller` en inicio:** Revisa dependencias de red del SQL Docker. Confirma también si las tablas `Transferencia` y `TransferenciaInterbancaria` existen físicamente. Ve a Docker Desktop y revisa sus logs.
- **Loki o Tempo arrojan errores S3 "No bucket found" en MinIO:** Significa que el contenedor aprovisionador (`minio-init`) falló la rápida inyección inicial del script `mc` al levantar los composes. Reiniciar el bundle general de Docker por lo general resuelve este comportamiento asíncrono.
- **Data vacía inicial:** Si corres Alloy bajo el comando de `tail_from_end: true`, este solo escuchará logs NUEVOS generados. Agrega nuevas líneas o toca los archivos de texto bajo `sample-logs/`.

---

## Seguridad y Recomendaciones de Producción

Para elevar este stack al estricto estándar productivo real y evitar incidencias graves, se deben aplicar obligatoriamente los siguientes parches de infraestructura detectados en auditoría técnica:

1. **Parcheo de Variables de Config Loki (CRÍTICO):**  
   Por defecto, al llamar en red `${VAR}` desde un yaml de Loki asume un string literal. Debes inyectar el arg `-config.expand-env=true` mediante el bloque `command` del propio base yaml o de las dependencias prod para asegurar que Loki conecte al hostname correcto del S3.

2. **Protección *OOM* por Límites de Recursos (RAM/CPU):**  
   Fuerte uso de Prometheus o el Alloy puede saturar la RAM completa del sistema host matando al nodo entero. Define limites usando *Docker Deploy Limits*:
   ```yaml
   deploy:
     resources:
       limits:
         memory: 2G
   ```

3. **Retención de Logs Internos de Docker:**  
   Todo contenedor escupe output hacia el daemon de Docker (generalmente en pesado JSON). Añadir el driver a cada nodo mayor limitando histórico a `50m` impedirá reventar el disco de log base de la propia maquina anfitriona.

4. **Autenticación Fuerte de Secretos .ENV:**  
   Migrar el `deploy/prod/.env` provisto a **Docker Secrets** auténticos o manejadores como AWS Secrets Manager / Vault, ocultando las credenciales planas y débiles incrustadas para SMTP y MinIO Admin.