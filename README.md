# Documentación Técnica y Operativa

Bienvenido al ecosistema central de observabilidad diseñado para infraestructuras tecnológicas bancarias. **ObsBank-v2** consolida métricas, trazas (traces) y logs mediante una arquitectura híbrida lista para integrarse de inmediato tanto en entornos locales (Desarrollo) como en infraestructuras orientadas al despliegue remoto seguro con almacenamiento S3 (Producción).

El Stack de herramientas incluye:
* **Grafana (v11+)** - Presentación de Dashboards, Gestor de Alertas y Enlace principal con Active Directory (LDAP).
* **OpenTelemetry Collector & Grafana Alloy** - Conductos de telemetría y agentes extractores de recolección de apis y logs dispersos de tu plataforma bancaria.
* **Prometheus** - Corazón del motor de series de tiempo para Métricas de Infraestructura.
* **Loki & Tempo** - Monitoreo unificado de Logs textualmente ruteados y Traces de APIs distribuidos. (Retención masiva con MinIO interno).
* **FCM Bridge** - Microtrabajador customizado para empujar las alertas en tiempo real al proyecto Android/Mobile nativo de EcoFuturo (Alerts App).
* **SQL Poller** - Servicio auxiliar responsable de raspar e inyectar telemetrías directamente de tu base de datos central (EconetTransacciones).

---

## 📂 1. Explicación del Mapa de Archivos

Para facilitar el crecimiento, los repositorios de código están divididos y aislados según la fase de madurez del proyecto:

### 🛠️ 1.1 Entorno Base / Desarrollo Local
Diseñado para iniciar en 5 segundos sin pedir bases de datos previas o red distribuida. 
* **`docker-compose.yml`**: Contiene todo el clúster. Expone cada herramienta internamente, arranca Loki y Tempo usando el sistema de archivos del PC (efímero, fácil caída) y habilita un dominio LDAP local de prueba "fake" para debugear autenticaciones sin tocar el dominio del Banco Real.
* **`.env` (En la Raíz de la carpeta)**: Archivo con palabras clave simples (`admin:admin`, `minioadmin123`). Ideal para el programador.
* **`observability/*.yml` (sin sufijos)**: Contienen la parametrización más blanda y por defecto de cada elemento.

### 🏭 1.2 Entorno Producción (S3, Retenciones y Seguridad)
El directorio crucial **`deploy/prod/`** es la clave de bóveda de ObsBank-v2. Actúa encapsulando el arranque inyectando Overrides (Sobreescrituras) sobre tu base que le transforman totalmente el comportamiento:
* **`deploy/prod/.env`**: El archivo que contiene *los secretos reales*. Nunca se debe publicar en Git crudo, contiene el Active Directory Passwords (LDAP), URLs Productivas, Base de datos SQL reales y tu S3 `MINIO_ROOT_PASSWORD`.
* **`deploy/prod/docker-compose.prod.yml`**: Al inyectarse, *desautoriza* el guardado directo de Loki y Tempo, y los reconvierte obligándolos a buscar un host S3 (`http://minio:9000`). Además levanta tu contenedor de almacenamiento en red local **MinIO**. Apaga servicios Mock/Dummy para forzar despliegues auténticos y le esconde a la red pública (host ports) las IPs de los componentes.
* **`observability/loki-config.prod.yml` | `tempo.s3.yml`**: Las arquitecturas complejas de `aws:s3`, habilitadas para generar miles de chunks al disco tolerante y manejar `max_look_back_period`.

---

## 🚀 2. Guía Universal para Correr el Proyecto

Estar en Windows, Linux, Ubuntu da igual. Solo necesitas **Docker Desktop** (O Docker Daemon con motor `docker compose` plugin activo).

### ☀️ A. Quiero correrlo en modo "Desarrollo" (Rápido)
1. Abre tu terminal en la carpeta principal `obs-bank-v2/`
2. Simplemente escribe:
   ```bash
   docker compose up -d
   ```
3. Docker absorberá tu archivito base `.env` nativo y encenderá todo local. Para derribarlo o apagar el trabajo el viernes: `docker compose down`.

### 🌑 B. Quiero correrlo como "Producción Definitiva" (Recomendado)
Es altamente vital correrlo con el script Powershell que condensa la lógica de inyección y une el archivo maestro de secretos.
1. Confirma que instalaste el Volumen Inicial y rellenaste contraseñas robustas en **`deploy/prod/.env`**.
2. Corre en tu terminal de PowerShell en la raíz (`obs-bank-v2/`):
   ```powershell
   ./deploy.prod.ps1
   ```
*(Este script limpia volúmenes corruptos antiguos, unifica las capas del docker compose de producción maestro y despliega en silencio el ambiente S3. Ojo: La caída de este Stack de producción se efectúa con un mero `docker compose down`).*

---

## 🔧 3. Guía de Configuraciones Vitales

### 3.1 Conexión al Active Directory (Grafana AD/LDAP)
Si el Banco quiere permitir inicio de sesiones a Grafana logeándose con las credenciales de Windows, editar:
1. Tocar el archivo **`deploy/prod/ldap.prod.toml`**.
2. Llenar el host (`host = "ad.tudominio.com"`), port (389 o 636) y el `bind_dn` que debe coincidir con el usuario administrador en tu dominio.
3. La contraseña de lectura (`bind_password`) se inyecta desde **`deploy/prod/.env` (LDAP_BIND_PASSWORD)** por seguridad.

### 3.2 SQL Poller (Conexión Base de Datos Transaccional)
1. Abrir **`deploy/prod/.env`** (O tu raíz `.env` si es desarrollo).
2. Variar el bloque **SQLSERVER_CONN**:
   ```env
   SQLSERVER_CONN=Server=HOST,IP;Database=TU_DB;User Id=TU_USUARIO;Password=MI_CONSTRASEÑA;Encrypt=True;TrustServerCertificate=True;
   ```
   *Nota Windows Local*: Usar `host.docker.internal,1433` si apuntas a tu propio PC (SQL Express Local) dentro de un contenedor Docker.

### 3.3 Autenticidad de Notificaciones Móviles (App Firebase Push)
Para que las Alertas rojas de Grafana brinquen a la App Móvil:
1. Generar la clave de Firebase de tu consola Google Developers `firebase-service-account.json`.
2. Pegar este archivo literal en la carpeta **`observability/certs/`**.
3. Asegurar que en Grafana (Contact Points), el webhook está apuntando a `http://fcm-bridge:5000/api/alert` adjuntando a la cabecera `Authorization: Bearer <Tu-BRIDGE_API_KEY>`. La API key está en el campo de tu archivo `.env`.

### 3.4 Configuración del Servidor S3 MinIO en Producción
Para soportar terabytes de logs bancarios sin cuelgues usando Tempo/Loki:
1. Definir claves mayores a 8 dígitos en **`deploy/prod/.env`**
   - `MINIO_ROOT_PASSWORD`
   - `TEMPO_S3_SECRET_KEY`
   - `LOKI_S3_SECRET_KEY`
2. **Importante:** Estas tres deben coincidir, ya que Loki y Tempo le pediran un Token de acceso validado a la puerta raíz del MinIO.
3. Puedes ver todo el registro virtual del disco duro en Producción conectándote a **`http://localhost:9001`**. (Pestaña "Object Browser" mostrará tus depósitos llenándose como `bucket/ tempo/ loki/`).

### 3.5 Recolección Automática de Logs con Alloy
Grafana Alloy (La evolución de Promtail) es el componente inteligente atado a este modelo.
Si tienes APIs de .NET publicando sus logs estructurados .txt en una capeta tipo `C:\LogsBanco\`:
1. Mapea la ruta de lectura estricta en el `deploy/prod/.env`:
   - `PROD_LOGS_PATH=/TuCarpeta/De/Logs_Host`
2. Alloy la sincronizará vía volúmenes de solo lectura (`:ro`) y los mandará a Loki emparejados bajo el entorno declarado en la variable `ALLOY_ENV=prod`.

---

## 4. Reglas Críticas de Resolución de Problemas

**Error: "The Access Key Id you provided does not exist in our records" en Loki/Tempo**
> **Motivo:** Cambiaste la contraseña de MinIO en `.env`, pero el contenedor del MinIO ya la había guardado en un previo encendido interno usando credenciales del disco local y ahora las desincronizó.
> **Solución:** Mata el contenedor **Borrando su Volumen persistente** para forzar un refresco. 
> Ejecuta en tu terminal: `docker compose down -v` O directamente suprime el disco asociado corriendo `docker volume rm obs-bank-v2_minio_data_prod` antes de volver a intentar el `./deploy.prod.ps1`.

**Error: SQL Poller con 11 Fallas o "No data" en Grafana Dashboard**
> **Motivo:** Tu IP del MS SQL en tu cadena *SQLSERVER_CONN* es incorrecta o inalcanzable.
> **Solución:** Recuerda que los sub-contenedores viven en una Mini-Red LAN propia ajena a tu disco C:\. Si alojaste tu DB en tu mismo Windows, no sirve poner `localhost` (para el contenedor el localhost es él mismo). Usa siempre  `host.docker.internal` en lugar de la palabra localhost en el archivo `.env`.

**Archivo Compose Mágicamente mal leído "El archivo o sintaxis es incorrecto" en Powershell**
> **Motivo:** Powershell interpreta rutas en un salto de carro (`\n`) diferente a Bash.
> **Solución:** Nuestro script `./deploy.prod.ps1` ya solventa este problema encadenando las subredes de docker mediante retrocesos de carro en línea (`\``), úsalo siempre.

---

> _"Observabilidad es saber qué te está doliendo dentro de tu servidor, incluso antes de que el usuario haga el primer reclamo"._
> **Operación y Monitoreo.**
