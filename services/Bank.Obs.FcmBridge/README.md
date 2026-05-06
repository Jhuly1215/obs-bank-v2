# Bank.Obs.FcmBridge

## Resumen de la Arquitectura

`Bank.Obs.FcmBridge` es el servicio intermediario encargado de recibir las alertas originadas por **Grafana** y despacharlas como notificaciones Push (vía Firebase Cloud Messaging - FCM) a los dispositivos móviles del personal operativo (EcoMonitor).

Este servicio abandonó la estrategia de *Topics* de FCM y la administración directa de dispositivos. Actualmente funciona mediante una **Integración por Tokens** utilizando dos bases de datos para garantizar la separación de responsabilidades y el principio de mínimo privilegio:

1. **EcoMonitorDb (Base Interna de Gestión):** Almacena qué usuarios (mediante su `codigoAgenda`) están dados de alta para recibir notificaciones y su estado de actividad (Activo/Inactivo).
2. **EconetDb (Base Externa Móvil - Solo Lectura):** Contiene los tokens actualizados (`TokenNotificacion`) que la aplicación móvil registra regularmente. 

---

## Flujo de Funcionamiento

El ciclo de vida de una alerta enviada al servicio sigue los siguientes pasos:

1. **Recepción de Alerta:** Grafana envía un webhook con formato JSON al endpoint `POST /alert`. La petición se valida verificando que el header `Authorization` contenga el Bearer Token configurado (`BRIDGE_API_KEY`).
2. **Consulta de Usuarios Activos:** El servicio consulta la tabla `dbo.UsuariosNotificacion` en `EcoMonitorDb` (mediante el stored procedure `spListarUsuariosNotificacionActivos`) para recuperar la lista de usuarios habilitados y sus respectivos `codigoAgenda`.
3. **Obtención de Tokens Móviles:** Con la lista de códigos de agenda, el servicio realiza una consulta SQL (en modo *Solo Lectura*) a `EconetDb` para obtener el `TokenNotificacion` de cada usuario válido.
4. **Depuración y Deduplicación:** Se filtran los usuarios que no poseen un token activo y se eliminan duplicados para evitar enviar el mismo Push varias veces a un mismo token.
5. **Envío por Lotes (Multicast):** Se divide la lista resultante de tokens en lotes (por defecto de 500 en 500, según el límite de Firebase) y se despacha a través de Firebase Admin SDK (`SendEachForMulticastAsync`).
6. **Resumen de Respuesta:** El servicio responde de vuelta a Grafana con un resumen consolidado de todo el proceso. Esto incluye la cantidad de usuarios activos procesados, tokens encontrados, la cantidad de envíos exitosos y fallidos (`successCount`, `failureCount`).

**Importante:** En caso de que Firebase reporte un token como inválido (`messaging/invalid-registration-token`, etc.), el FcmBridge dejará constancia en sus registros (logs) y los devolverá en el arreglo `tokensFallidos` de la respuesta, **pero no eliminará ni marcará el token en EconetDb**.

---

## Endpoints de Administración

Aunque el envío de alertas es automático, el servicio expone una API REST protegida por `BRIDGE_API_KEY` para administrar a los operadores que deben recibir los Push (modificando la base `EcoMonitorDb`):

*   `POST /v1/usuarios-notificacion`: Registra un nuevo usuario o reactiva/actualiza uno existente buscando por `codigoAgenda`.
*   `PATCH /v1/usuarios-notificacion/estado`: Permite cambiar manualmente el estado (Activo = 1 / Inactivo = 0) de un usuario en el sistema de alertas.
*   `GET /v1/usuarios-notificacion`: Lista administrativamente todos los usuarios y su historial de creación y modificación.
*   `GET /v1/usuarios-notificacion/activos`: Lista específicamente qué usuarios (código y correo) están actualmente activos para recibir la próxima alerta.

*Nota: La App Móvil **no interactúa** directamente con este microservicio para registrar su Token. La App Móvil continúa enviando su Token a los servicios regulares de Econet. Este Bridge simplemente "lee" esa tabla externa.*
