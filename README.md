# ObsBank-v2: Plataforma de Observabilidad Profesional

**ObsBank-v2** es una solución de observabilidad de nivel bancario diseñada para centralizar métricas, trazas (tracing) y logs estructurados. Utiliza estándares globales de la industria para garantizar la visibilidad total de la infraestructura y el negocio.

---

## ⚡ Estructura del Proyecto

El sistema está diseñado bajo el principio **"Zero-Hardcode"**. Toda la configuración se controla desde archivos `.env`, permitiendo despliegues idénticos en Desarrollo y Producción.

- **`observability/`**: Configuraciones base de Grafana, Prometheus, Loki y Tempo.
- **`services/`**: Microservicios de monitoreo (SqlPoller, DemoApi, FcmBridge).
- **`deploy/prod/`**: Orquestación y secretos para el entorno de producción con S3/MinIO.
- **`sample-logs/`**: Repositorio para logs externos de aplicaciones externas (TXT, JSON, Log).

---

## 🚀 Guía de Inicio Rápido

### 💻 1. Desarrollo (Local)
1. Configura tu [`.env`](.env) en la raíz.
2. Levanta el stack:
   ```bash
   docker compose up -d
   ```
3. Acceso: `http://localhost:3000` (Grafana).

### 🏦 2. Producción (S3 & Seguridad)
1. Configura [`deploy/prod/.env`](deploy/prod/.env) con credenciales reales.
2. Ejecuta el script de despliegue oficial:
   ```powershell
   ./deploy.prod.ps1
   ```

---

## 🛠️ Configuración "Zero-Hardcode"

A diferencia de versiones anteriores, **ObsBank-v2** extrae toda su inteligencia del archivo `.env`.

### Variables Clave:
- **`LOKI_URL` / `TEMPO_URL`**: Enlaces internos entre contenedores.
- **`DOMAIN_NAME`**: Define la identidad del servidor para el Proxy SSL.
- **`LDAP_BIND_DN` / `LDAP_SEARCH_BASE`**: Conecta Grafana con el Active Directory corporativo.
- **`SQLSERVER_CONN`**: Cadena de conexión centralizada.
- **`MINIO_ROOT_USER` / `MINIO_ROOT_PASSWORD`**: Credenciales maestras del almacenamiento S3.

---

## 🔍 Visualización y Filtros

Para ver datos en Grafana (Sección **Explore**), utiliza siempre la etiqueta **`service_name`**:

| Origen | `service_name` |
| :--- | :--- |
| **Sql Poller** | `bank-sql-poller` |
| **Demo API** | `bank-obs-demo-api` |
| **Logs Externos** | Nombre de la subcarpeta en `/sample-logs/` |

---

## 🚨 Almacenamiento y Seguridad

- **Desenvolvimiento**: Los logs se guardan en el disco local y son efímeros.
- **Producción**: Los logs se persisten en **MinIO (S3)** de forma comprimida y segura.
- **Acceso**: El acceso está protegido por **LDAP (Active Directory)** y el tráfico externo es estrictamente **HTTPS (Puerto 443)**.

---
_"Observabilidad es saber qué te está doliendo dentro de tu servidor, incluso antes de que el usuario haga el primer reclamo"._
