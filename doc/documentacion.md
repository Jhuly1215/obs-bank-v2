# obs-bank-v2 - Documentación Técnica

Stack de **observabilidad end-to-end** orientado a un escenario bancario (simulado), construido con **.NET 9 + OpenTelemetry + Grafana (Prometheus/Loki/Tempo)** y orquestado con **Docker Compose**. 

> **Importante:** Este repositorio **no es un core bancario** ni una banca completa. Es un **sandbox/laboratorio de observabilidad** diseñado para demostrar la integración de telemetría y su maduración hacia entornos productivos, y ahora incluye la emisión de alertas a dispositivos móviles Android.

---

## Tabla de contenido
- [Qué hace este proyecto](#qué-hace-este-proyecto)
- [Arquitectura](#arquitectura)
- [Componentes Centrales](#componentes-centrales)
- [Estructura del Repositorio](#estructura-del-repositorio)
- [Pre-requisitos y Configuración Clave](#pre-requisitos-y-configuración-clave)
- [Despliegue y Ambientes](#despliegue-y-ambientes)
- [Flujo de Observabilidad y Uso Rápido](#flujo-de-observabilidad-y-uso-rápido)
- [Alertas Móviles: Integración con Android y Firebase (FCM)](#alertas-móviles-integración-con-android-y-firebase-fcm)
- [Referencia a la Aplicación Android Móvil](#referencia-a-la-aplicación-android-móvil)
- [Troubleshooting](#troubleshooting)
- [Seguridad y Recomendaciones de Producción](#seguridad-y-recomendaciones-de-producción)

---

## Qué hace este proyecto
Este proyecto centraliza telemetría (logs, métricas y trazas) desde distintas fuentes y reacciona ante anomalías:
1. **Aplicaciones .NET instrumentadas con OpenTelemetry** (`demo-api` conectado a base de datos externa y `sql-poller`).
2. **Logs de archivos locales** ingeridos por **Grafana Alloy**.
3. **Backend de observabilidad** basado en el ecosistema de Grafana (Prometheus, Loki, Tempo).
4. **Alertamiento Activo** enviando notificaciones push a dispositivos móviles a través de un Bridge a Firebase Cloud Messaging.

### Casos de uso
- Probar OpenTelemetry en .NET y validar pipelines OTLP → Grafana stack.
- Simular flujos de transacciones monetarias persistentes usando Entity Framework Core sobre SQL Server.
- Exponer y monitorear operativamente bases de datos SQL transaccionales.
- Validar arquitecturas *Push* para que Grafana alerte directamente a dispositivos Android en tiempo real separando la app móvil por completo del stack de observabilidad.

---

## Arquitectura

La arquitectura soporta telemetría y alerta en tiempo real.

```text
                               ┌───────────────────────────────────┐
                               │  Base de Datos SQL Server         │
                               │  (EconetTransacciones)            │
                               └─────────┬───────────────┬─────────┘
                                         │               │
  ┌────────────────────────────┐         │        ┌──────┴──────────────┐
  │        demo-api (.NET 9)   │◄────────┘        │ sql-poller (.NET 9) │
  │ Transacciones & Telemetría │EF Core           │ Métricas Negocio    │
  └─────────────┬──────────────┘                  └──────┬──────────────┘
                │ OTLP (4317)                            │ OTLP (4317)
                ▼                                        ▼
  ┌───────────────────────────────────────────────────────────────┐
  │                    OpenTelemetry Collector                    │
  └───────┬────────────────────────┬─────────────────────┬────────┘
          │                        │                     │
       metrics                     │                logs, traces
          ▼                        ▼                     ▼
  ┌────────────┐             ┌───────────┐         ┌───────────┐
  │ Prometheus │             │   Loki    │         │   Tempo   │
  └─────┬──────┘             └─────┬─────┘         └─────┬─────┘
        │                          │                     │
        ▼                          ▼                     ▼
  ┌───────────────────────────────────────────────────────────────┐
  │                            Grafana                            │
  │                     Dashboards & Alerting                     │
  └────────────────────────┬──────────────────────────────────────┘
                           │ Webhook (Contact Point)
                           ▼
  ┌───────────────────────────────────────────────────────────────┐
  │                     FCM Bridge (.NET 9)                       │
  │                  (Traducción a Firebase)                      │
  └────────────────────────┬──────────────────────────────────────┘
                           │ Firebase Cloud Messaging (FCM)
                           ▼
  ┌───────────────────────────────────────────────────────────────┐
  │                 App Móvil Android (Topics)                    │
  │         [obsbank-critical] [warning] [info]                   │
  └───────────────────────────────────────────────────────────────┘
```

---

## Componentes Centrales

### 1) `demo-api` (Simulador Transaccional en .NET 9)
Actúa como un core bancario simplificado. 
- Expone `GET /health`.
- Expone los endpoints `POST /api/v1/transferencias/internas` e `interbancarias` para la creación inicial.
- **Novedad:** Expone los endpoints `PUT /api/v1/transferencias/internas/{id}/estado` e `interbancarias/{id}/estado` para simular fallos operativos manualmente (estados 4, 5 o 9).
- **Novedad:** Inserta registros funcionales en la BD SQL `EconetTransacciones` mediante Entity Framework Core.
- Emite logs estructurados, trazas y métricas OTLP.

### 2) `sql-poller`
Worker de .NET que consulta métricas de negocio directamente desde `EconetTransacciones` y envía a OTel métricas como contadores y tiempos.

### 3) `fcm-bridge` (Servicio Puente de Notificaciones)
Microservicio backend que recibe payloads estándar de Grafana Webhooks. Valida la autenticación, mapea la severidad de la alerta y empuja mediante Firebase Admin SDK una notificación Push al tópico FCM correspondiente.

### 4) Stack de Almacenamiento y Visualización
- **OTel Collector**: Enruta métricas, traces y logs.
- **Prometheus, Loki y Tempo**: Motores de series temporales, logs indexados y trazas. MinIO (S3) se activa sólo en modo Producción.
- **Grafana Alloy**: Componente alternativo de ingesta física de logs en `sample-logs/`.

### 5) Aplicación Móvil Android (ObsBankAlerts)
- Es el destino asíncrono y final de todo este flujo Push de notificaciones. Trabaja mediante su registro de canales FCM.
- **Historial Interactivo**: Muestra un listado ordenado (`RecyclerView`) de las últimas 50 alertas guardadas localmente mediante tarjetas expansibles.
- **Indicadores de Severidad**: Decodifica el payload para pintar estéticamente la tarjeta según la urgencia (Rojo = Crítico, Naranja = Warning, Azul = Info) y emitir sonidos distintos en Android.
- *Consulta [`doc/app-movil.md`](app-movil.md) para más detalles.*

---

## Estructura del Repositorio

```text
obs-bank-v2/
├─ docker-compose.yml           # Definición de servicios base locales
├─ deploy/prod/                 # Configuración para Producción (MinIO / S3 Archiving)
│  └─ .env                      # Variables de entorno prod (SMTP, S3)
├─ observability/               # Mapeo de Volúmenes y configs nativas
│  ├─ certs/                    # Ubicación para llaves, como firebase-service-account.json
│  └─ grafana/                  # Dashboards auto-aprovisionados, LDAP, etc.
└─ services/                    # Código fuente base .NET 9 API, Poller y FCM Bridge
```

---

## Pre-requisitos y Configuración Clave

Para ejecutar el proyecto, renombra a `.env` si es necesario y edita los valores globales. Como componente integrado externo necesitarás atender a los siguientes puntos críticos:

1. **SQLSERVER_CONN (Base de Datos):** 
   Ambos (`demo-api` y `sql-poller`) exigen conectividad a una base de datos `EconetTransacciones` con las tablas `Transferencia` y `TransferenciaInterbancaria` creadas.
   - *Ejemplo:* `SQLSERVER_CONN=Server=MI_IP;Database=EconetTransacciones;Trusted_Connection=True;TrustServerCertificate=True;`

2. **Keys de Firebase (`FCM Bridge`):**
   Para que el puente logre despachar alertas al celular, Docker montará la identidad de Google. 
   - Debes colocar tu llave de cuenta de servicio de Firebase allí: `./observability/certs/firebase-service-account.json`.

3. **Autenticación del Webhook (`BRIDGE_API_KEY`):**
   Contraseña (Bearer) mediante la cual Grafana podrá interactuar con el FCM Bridge.

---

## Despliegue y Ambientes

Antes de iniciar, detén contenedores que usen puertos como `3000` (Grafana), `5000` (DemoAPI), `5001` (FCMBridge), etc.

### Opción A: Entorno Local (Desarrollo Rápido)
Evita MinIO/S3 usando sistema de archivos regular de Docker.
```bash
docker-compose up --build -d
```

### Opción B: Entorno Producción (Arquitectura S3 Storage)
Exige dependencias extra (combina archivos Compose para lanzar MinIO y habilitar Multi-Tenant logs).
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

1. **Operación Transaccional**:
   Dianas a los nuevos endpoints interactuando con Swagger en `http://localhost:5000/swagger`.
   O bien, envía un POST a `http://localhost:5000/api/v1/transferencias/internas` (puedes inyectar tu `X-Correlation-Id: mibanco-123`).
   Verás que el retorno será un **Identity ID** real persistido directamente en SQL Server.

2. **Correlación de Traza-Logs**:
   En Grafana (`http://localhost:3000`), usando el "Explorer" (Data sources: Loki & Tempo), filtra por tu Correlation ID. Si encuentras un Log de la Demo Api, verás el enlace al Trace para ver todo el recorrido exacto de tiempo que le tomó hacer el Insert en base de datos.

---

## Alertas Móviles: Integración con Android y Firebase (FCM)

**Rol de la aplicación móvil (ObsBank Alerts)**
La App Android se ha diseñado intencionalmente manteniéndose *absolutamente agnóstica* de Grafana o nuestro stack de Observabilidad. Sus únicas responsabilidades son registrarse en Firebase, obtener su token, y suscribirse explícitamente a los diferentes *topics* de severidad: `obsbank-critical`, `obsbank-warning` y `obsbank-info`. No hace consultas (Pull) al backend; reacciona a los Webhooks (Push).

**Cómo conectar la plataforma para activar las alertas en la APK:**

1. **Sincronización Firebase:** La app y el Bridge deben estar en el mismo Proyecto Firebase (`google-services.json` compilado en APK, y `firebase-service-account.json` montado en el docker de la BD).
2. **Setup en Grafana (Contact Point):** 
   - Dentro de *Alerting*, crea un *Contact Point* Webhook.
   - Apunta al endpoint interno de la red docker: `http://fcm-bridge:8080/alert`. (Ojo: puerto nativo vs puerto expuesto).
   - Añade un Custom Header -> `Authorization: Bearer <TU_BRIDGE_API_KEY>`.
3. **Notification Policies:**
   - Asigna tu nueva regla para que Grafana dirija alertas por severidad a este nuevo Webhook en vez de solo enviar un Email.
4. **Validación:**
   Cuando Grafana detecte anomalías (ej: `sql-poller` advierte de transferencias falladas masivas), derivará a Webhook -> FCM Bridge procesa -> Firebase empuja a la severidad adecuada -> Tu dispositivo emite la notificación push y se guarda en *SharedPreferences*.

---

## Referencia a la Aplicación Android Móvil
Puedes revisar de manera independiente toda la arquitectura asociada a la App móvil en Android consultando el documento de arquitectura dedicado: [**Documentación ObsBankAlerts (`app-movil.md`)**](app-movil.md). 
Para comprender a fondo los errores y sentencias que originan todo el ecosistema de alertas de Grafana, recuerda leer la [**Guía de Errores Operativos (`mensajes_de_errores.md`)**](mensajes_de_errores.md) y documentación de [**Métricas SQL (`queries.md`)**](queries.md) para un entendimiento total y absoluto.

---

## Troubleshooting

- **Crash del `sql-poller` o `demo-api` al Iniciar:** 
  Verifica que tu host y puerto desde `SQLSERVER_CONN` admiten acceso desde un entorno de contenedores, a menudo requiere usar `host.docker.internal` en lugar de `localhost`.
- **Bridge Error (Token Invalid):**
  Asegúrate que la `BRIDGE_API_KEY` coincida milimétricamente entre tu `.env` de docker y lo que digitaste en el *Contact Point* de Grafana y que el archivo `.json` de Google esté debidamente inyectado en `/observability/certs/`.
- **Pérdida de S3 / Buckets no Encontrados:**
  Ocasionalmente la inicialización asíncrona tipo *job* (minio-init) puede retardarse frente a Loki. Un reinicio usual del stack solucionará la disponibilidad asíncrona de AWS CLI creando los buckets.

---

## Seguridad y Recomendaciones de Producción

Para elevar este stack al estricto estándar productivo real y evitar incidencias graves, aplica los siguientes parches de infraestructura detectados en auditoría técnica:
1. **Protección *OOM*:** Grafana, LTS Prometheus y Alloy pueden devorar la memoria del Host Node afectando servicios de base de datos colindantes. Aplica rigurosos Deploy Limits en tu Compose.
2. **Retención nativa de Docker:** Configura el log driver predeterminado del Daemon docker con `max-size: 50m` y `max-file: 3` para prevenir un disk flush out.
3. **Migración a Bóvedas K/V Activas:** Evita los archivos `.env` planos y transita tu modelo de contraseñas de Grafana y Firebase JSON hacia el inyector de *Docker Secrets*, AWS Secrets Manager, o Hashicorp Vault para protección frente a fugas de código fuente.