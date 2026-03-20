# Catálogo de Mensajes de Log y Observabilidad

Este documento describe la estructura y el propósito de los mensajes de log (bitácoras) implementados a través de los contenedores de la solución. Estos logs se emiten hacia **Grafana Loki** y **Alloy** para fines de trazabilidad, monitoreo y alertas.

## 1. Contenedor: `Bank.Obs.DemoApi`

El API transaccional enruta y registra operativas bancarias e interbancarias hacia la base de datos `EconetTransacciones`.

### `[Information]`
*   **Mensaje:** `"Iniciando registro de transferencia interna/interbancaria. CuentaOrigen: {CuentaOrigen}, Monto: {Monto}"`
    *   **Propósito:** Marca el inicio del ciclo de vida de una solicitud exitosa que superó las validaciones básicas de negocio.
*   **Mensaje:** `"Guardando transferencia... en BD..."`
    *   **Propósito:** Trazabilidad de tiempo. Se dispara justo antes de mandar el comando `INSERT` hacia Entity Framework.
*   **Mensaje:** `"Transferencia ... guardada correctamente. ID: {Id}"`
    *   **Propósito:** Confirmación absoluta de que la transacción se persistió sin romper integridad referencial (por ejemplo, con el ID autogenerado o asignado manualmente).

### `[Warning]`
*   **Mensaje:** `"Transfer cancelled tx={Tx}"` *(Módulo Transacciones Simuladas)*
    *   **Propósito:** Advierte que el usuario o el cliente canceló la petición en medio del procesamiento (por ejemplo, cerró el navegador o la aplicación antes de recibir respuesta).

### `[Error]`
*   **Mensaje:** `"Error al guardar transferencia interna/interbancaria en la BD."`
    *   **Descripción del error:** Indica que SQL Server rechazó la inserción o que Entity Framework colisionó (ej. Timeout de conexión, llaves duplicadas, campos faltantes obligatorios como la conexión `SQLSERVER_CONN` nula).
    *   **Propósito:** Es un evento crítico que causa pérdida de transaccionalidad.

---

## 2. Contenedor: `Bank.Obs.SqlPoller`

Este servicio demonio se encarga de leer la base de datos en intervalos recabando métricas de negocio para Prometheus y OpenTelemetry.

### `[Information]`
*   **Mensaje:** `"SQL poll ok. 15m(...) ... duration_ms={durationMs}"`
    *   **Propósito:** Demuestra que el hilo de lectura recolectó todas las estadísticas de las diferentes tablas sin interrupción. Contiene agregaciones ricas en atributos.

### `[Warning]`
*   **Mensaje:** `"Nivel de transacciones pendientes elevado en las últimas 24h. Intra: {IntraPending}, Inter: {InterPending}"`
    *   **Propósito:** Actúa como alerta pre-configurada in-app. Se evalúa cuando la métrica de acumulados atascados supera el umbral de `100`. Indica degradación operativa del core bancario.
*   **Mensaje:** `"Revisión periódica de rendimiento (cada 5 min). La conexión a la BD está operando normalmente."`
    *   **Propósito:** Trazabilidad de "Heartbeat" (latido de corazón). Si este warning deja de aparecer en los logs a lo largo del tiempo, se confirmaría que el poller se quedó colgado (deadlock).

### `[Error]`
*   **Mensaje:** `"Error en SQL poller"`
    *   **Descripción del error:** Excepción crítica no controlada arrojada por la conexión SQL de ADO.NET (credenciales, base apagada, red inalcanzable, o la consulta fue cancelada).
    *   **Propósito:** Invalida el batch actual de métricas recolectadas; el servicio intentará reenganchar en la próxima ventana configurada.

---

## 3. Contenedor: `Bank.Obs.FcmBridge`

Es el microservicio puente (`Webhook`) responsable de tomar la alerta cruda de Grafana y empujarla hacia Android (Firebase Cloud Messaging).

### `[Information]`
*   **Mensaje:** `"Firebase Admin SDK inicializado exitosamente."`
    *   **Propósito:** Auditoría de arranque. Verifica que el archivo `.json` del Service Account se montó en el volumen y es válido.
*   **Mensaje:** `"Recibida alerta de Grafana: {title}"`
    *   **Propósito:** Verifica que el evento disparado por Alertmanager/Grafana sí llegó a la API puente de .NET.
*   **Mensaje:** `"Alerta enviada correctamente. MessageId: {MessageId}"`
    *   **Propósito:** Confirma que Google Firebase nos retornó un HTTP 200 por el mensaje Push despachado a la cola de los móviles.

### `[Warning]`
*   **Mensaje:** `"GOOGLE_APPLICATION_CREDENTIALS no está configurado o el archivo no existe..."`
    *   **Propósito:** Error de configuración de arquitectura Docker. Firebase no arrancará.
*   **Mensaje:** `"Intento de acceso denegado en /alert. Auth header inválido o inexistente."`
    *   **Propósito:** Alarma de seguridad. Alguien o un bot (o una mala configuración en Grafana Alerting) está haciendo peticiones no autorizadas sin el apiKey Bearer correcto.

### `[Error]`
*   **Mensaje:** `"Error al enviar alerta a FCM: {Details}"`
    *   **Descripción del error:** El payload enviado por código es rechazado por Firebase. Ocurre si el token del dispositivo venció, si hay intermitencias del lado de Google Cloud, o si la cuenta está suspendida.
    *   **Propósito:** Detalla el mensaje de descarte original emitido por la API de Google/FCM.
