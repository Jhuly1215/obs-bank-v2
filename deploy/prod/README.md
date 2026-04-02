# ObsBank - Despliegue a Producción

Este directorio contiene los overlays de Docker Compose que transforman el entorno de desarrollo en un stack listo para producción. Todo se basa en el mecanismo de **múltiples archivos `-f`** de Docker Compose.

## Prerequisitos

- Docker Engine ≥ 26 instalado en el servidor.
- Acceso de red al servidor SQL (`SQLSERVER_CONN`).
- Cuenta de Firebase con el archivo `firebase-service-account.json` disponible.
- Certificado CA del banco en `observability/certs/ca-banco.pem`.

---

## 1. Configurar el archivo `.env`

Edita `deploy/prod/.env` con los valores reales del entorno antes de arrancar. Los campos obligatorios marcados con `CAMBIAR_*` deben ser substituidos:

```dotenv
GF_SECURITY_ADMIN_PASSWORD=...   # Password de Grafana admin
SQLSERVER_CONN=Server=...        # Cadena de conexión real a SQL Server
BRIDGE_API_KEY=...               # Clave secreta del webhook FCM
```

---

## 2. Comandos de Arranque

Todos los comandos deben ejecutarse desde la **raíz del proyecto** (`obs-bank-v2/`).

### Arranque estándar (sin MinIO, almacenamiento local en volúmenes Docker)
```bash
docker compose \
  -f docker-compose.yml \
  -f deploy/prod/docker-compose.prod.yml \
  up -d
```

### Arranque con MinIO (S3 local para Loki y Tempo - recomendado para producción real)
```bash
docker compose \
  -f docker-compose.yml \
  -f deploy/prod/docker-compose.prod.yml \
  -f deploy/prod/docker-compose.minio.yml \
  -f deploy/prod/docker-compose.loki-s3.yml \
  -f deploy/prod/docker-compose.tempo-s3.yml \
  up -d
```

### Opción: Exponer la Demo API temporalmente (para pruebas internas)
Agregar `-f deploy/prod/docker-compose.expose-demo.yml` al comando anterior.

---

## 3. Puertos Expuestos al Host (Producción)

| Servicio | Puerto | Descripción |
|---|---|---|
| Grafana | `3000` (configurable con `GRAFANA_PORT`) | UI de monitoreo |
| OTLP gRPC | `4317` (configurable con `OTLP_GRPC_PORT`) | Ingesta de trazas/métricas desde APIs externas |
| OTLP HTTP | `4318` (configurable con `OTLP_HTTP_PORT`) | Ingesta OTLP alternativa |

> **Todos los demás servicios** (Prometheus, Loki, Tempo, Redis, Alloy) **NO están expuestos al host**. Se comunican internamente a través de la red Docker `obs`.

---

## 4. Integrar una API Externa (ej: EconetTransacciones)

La API de Econet debe enviar sus trazas y métricas al colector OTLP de este stack:

```
http://<IP_DEL_SERVIDOR_DOCKER>:4317   (gRPC)
http://<IP_DEL_SERVIDOR_DOCKER>:4318   (HTTP)
```

Para instrucciones de integración en código .NET, ver: [`doc/integracion_otel_apis.md`](../../doc/integracion_otel_apis.md)

---

## 5. LDAP / Active Directory

El archivo `ldap.prod.toml` contiene la configuración real del Active Directory corporativo de Ecofuturo. Revisar y ajustar:

- `host` → IP del Controlador de Dominio real.
- `bind_dn` → DN del Service Account de lectura.
- `search_base_dns` → OU donde viven los usuarios.
- `group_mappings` → DNs de los grupos de seguridad de Grafana.

La contraseña del bind se lee de la variable `LDAP_BIND_PASSWORD` en el `.env`.
