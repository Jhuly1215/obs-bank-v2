# ObsBank Alerts - Bridge (Grafana → FCM)

Este documento describe la configuración, modelo de datos y uso del servicio `fcm-bridge`, el cual conecta las alertas disparadas por **Grafana Alerting** con la aplicación móvil a través de **Firebase Cloud Messaging (FCM)**.

## Arquitectura (Integración por Tokens)

```text
Grafana Alerting → Webhook (POST JSON) → fcm-bridge (.NET 10) → SQL Server (Tokens) → Firebase Admin SDK → App Android
```

El servicio `fcm-bridge` dejó de utilizar la mensajería basada en *topics*. Ahora opera mediante una **Integración por Tokens** utilizando dos bases de datos, garantizando separación de responsabilidades y el principio de mínimo privilegio:

1. **EcoMonitorDb (Base Interna de Gestión):** Almacena de forma administrativa los usuarios operativos (mediante su `codigoAgenda`) dados de alta para recibir notificaciones y su estado de actividad (Activo/Inactivo).
2. **EconetDb (Base Externa Móvil - Solo Lectura):** Contiene la tabla con los tokens actualizados (`TokenNotificacion`) reportados por la aplicación móvil al ecosistema principal de Econet.

### Modelo de Datos (UsuariosNotificacion)
La base `EcoMonitorDb` contiene la tabla `dbo.UsuariosNotificacion`, la cual centraliza a quiénes despachar las notificaciones:

| Campo | Tipo | Descripción |
|---|---|---|
| `id` | `BIGINT` | Identificador interno único (PK). |
| `codigoAgenda` | `VARCHAR(20)` | Código único del usuario. Se usa para buscar su Token en EconetDb. |
| `correo` | `VARCHAR(150)` | Correo del usuario (uso administrativo/trazabilidad). |
| `estado` | `BIT` | `1` (Activo para recibir alertas) o `0` (Inactivo). |

---

## 🚀 1. Configuración de Firebase y Entorno

### Obtención del `service-account.json`
Para que el SDK de Firebase tenga permisos de emitir notificaciones:
1. Ve a la consola web de Firebase (`Project Settings` > `Service Accounts`).
2. Genera y descarga una nueva **Clave privada** (JSON).
3. Guarda el archivo en el repositorio como `observability/certs/firebase-service-account.json`.

### Variables en `.env` principal
Añade las conexiones a base de datos y la llave secreta para Grafana:
```dotenv
ECOMONITOR_DB_CONN=Server=host;Database=EcoMonitor;User Id=appuser;Password=admin;...
ECONET_DB_CONN=Server=host;Database=EconetTransacciones;User Id=readonly_user;Password=readonly_pass;...
BRIDGE_API_KEY=secreto_123_cambiar_en_produccion
```

---

## ⚙️ 2. Flujo de Funcionamiento del Envío

Cuando Grafana detecta un problema y dispara la alerta, ocurre lo siguiente de forma imperceptible:

1. **Recepción de Alerta:** Grafana envía un webhook (JSON) al endpoint `POST /alert` del puente. El puente valida la petición mediante el header `Authorization: Bearer <BRIDGE_API_KEY>`.
2. **Consulta de Activos:** El bridge ejecuta `dbo.spListarUsuariosNotificacionActivos` en **EcoMonitorDb** obteniendo los códigos de agenda de los operadores de turno.
3. **Lectura de Tokens:** Usando dichos códigos, hace un simple `SELECT` en **EconetDb** para extraer los `TokenNotificacion` de cada dispositivo.
4. **Depuración:** Ignora operadores sin token, depura tokens inválidos en la lista (vacíos/nulos) y elimina duplicados.
5. **Envío por Lotes (Multicast):** Firebase recibe los tokens en un único paquete masivo y asíncrono (bloques de 500) y dispara las notificaciones a los dispositivos correspondientes.
6. **Reporte:** Grafana recibe como respuesta un resumen confirmando cuántos usuarios estaban activos, cuántos tokens se procesaron, cuántos envíos fueron exitosos y el detalle de cualquier token inválido detectado por Firebase.

*Nota:* Si un token fue caducado o el usuario desinstaló la app, Firebase se quejará. El puente simplemente registrará el fallo en logs y respuestas, sin tocar, alterar o eliminar nada de la base externa `EconetDb`.

---

## 🐳 3. Configurando en Grafana Alerting

1. Ingresa a `Grafana` (http://localhost:3000)
2. Ve al menú lateral: **Alerting** -> **Contact points** -> **Add contact point**
3. Escoge el tipo: `Webhook`
4. Nombre: `FCM Bridge`
5. **URL:** `http://fcm-bridge:8080/alert` (o la URL pertinente en tu red Docker)
6. En las opciones avanzadas, busca la sección **HTTP Header** o **Authorization**.
7. Añade un header:
   - **Key:** `Authorization`
   - **Value:** `Bearer secreto_123_cambiar_en_produccion`

---

## 🔔 4. Endpoints de Administración del FcmBridge

Aunque el envío de alertas automatizado es el núcleo, el puente también sirve una API REST para gestionar a los operadores, asegurada por `BRIDGE_API_KEY`.

- **`POST /v1/usuarios-notificacion`**: Registra un nuevo operador proporcionando `"codigoAgenda"` y `"correo"`.
- **`PATCH /v1/usuarios-notificacion/estado`**: Habilita o deshabilita la recepción de alertas para un operador (`"estado": true/false`).
- **`GET /v1/usuarios-notificacion`**: Visualización administrativa completa de los registros.
- **`GET /v1/usuarios-notificacion/activos`**: Listado operativo de quiénes van a ser alertados.

*(La App Android no interactúa con estos endpoints; la App se limita a enviar sus tokens a Econet de forma habitual).*
