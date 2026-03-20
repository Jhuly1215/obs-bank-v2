# Aplicación Móvil: ObsBankAlerts

Esta aplicación Android fue desarrollada para funcionar como el único punto final visible de las notificaciones que provienen del ecosistema de monitoreo de **ObsBank**. 

## Propósito General
`ObsBankAlerts` **no se conecta directamente a la base de datos** ni hace peticiones constantes (Pull) rest hacia Grafana para revisar si hay alertas. En cambio, su arquitectura está diseñada para reaccionar de forma puramente pasiva (Push) empleando **Firebase Cloud Messaging (FCM)**.

## Arquitectura de Tópicos (Topics)
La aplicación se suscribe automáticamente a canales de mensajería (topics) manejados directamente en la plataforma Firebase. Estos tópicos reflejan directamente el nivel de severidad ("severity") configurado en Grafana:

1. **`obsbank-critical`**: Tópico para problemas graves, como la caída de servicios, desconexiones continuas de la base de datos (advertidas por el *SQL Poller*), o latencias altísimas.
2. **`obsbank-warning`**: Tópico para métricas que alcanzan niveles inusuales pero que no rompen el uso principal de negocio.
3. **`obsbank-info`**: Tópico reservado para eventos puramente informativos.

## Flujo de Recepción (End-to-End)
1. **Detección**: La Alerta nace en el motor de **Grafana Alerting**.
2. **Webhook**: Grafana envía un webhook HTTP al microservicio **`fcm-bridge`**.
3. **Procesamiento**: El puente en .NET mapea la propiedad _severity_ (ej. _critical_) y dispara un Push hacia el servicio en la Nube de Firebase usando el Admin SDK.
4. **Distribución**: **Firebase FCM** distribuye masivamente la alerta Push hacia el Action Layer de todos los teléfonos que tienen la app instalada.
5. **Recepción local**: Si la App está en primer plano o segundo plano, el sistema emite la vibración y notificación visual correspondiente almacenando esto en su base interna (`SharedPreferences`), sin que la app haya tenido que hacer nunca un *request* pidiendo los errores por sí misma.

## Configuración y Dependencias
Para compilar y conectar `ObsBankAlerts` exitosamente a la nube de Google en tu propio entorno, debes configurar internamente los descriptores de Firebase:

1. **El archivo `google-services.json`:**
   Debes descargar el archivo descriptor de tu proyecto de la consola de administración de Firebase y colocarlo físicamente en la carpeta `app` del directorio del proyecto Android:
   > Ruta obligatoria: `/ObsBankAlerts/app/google-services.json`

2. **Sincronización Crítica de Archivos de Llave**:
   Para que todo funcione, la aplicación Android debe estar vinculada obligatoriamente **al mismo proyecto exacto de Firebase** del que se descargó el `firebase-service-account.json`. 
   * En palabras simples: El `.json` del backend (Docker) debe apuntar al mismo proyecto del que bajas el `google-services.json` para Android. De lo contrario, los permisos de empuje fallarán.

*Para referencia del backend Puente, consulta [`fcm-bridge.md`](fcm-bridge.md).*
