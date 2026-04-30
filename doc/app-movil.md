# Aplicación Móvil

Esta aplicación Android fue desarrollada para funcionar como el único punto final visible de las notificaciones que provienen del ecosistema de monitoreo de **ObsBank**. 

## Propósito General
La aplicación **no se conecta directamente a la base de datos** ni hace peticiones constantes (Pull) rest hacia Grafana para revisar si hay alertas. En cambio, su arquitectura está diseñada para reaccionar de forma puramente pasiva (Push) empleando **Firebase Cloud Messaging (FCM)**.

## Arquitectura por Tokens Directos (Sin Topics)
Anteriormente, la aplicación se suscribía a canales masivos (topics) como `obsbank-critical`. **Esta arquitectura ha sido depreciada por seguridad y ha sido eliminada por completo de la interfaz de la aplicación**.

Actualmente, el envío está **dirigido por dispositivo (Token-Based)**:
1. La aplicación móvil de cada empleado solicita a Firebase un `TokenNotificacion` único e irremplazable asociado al dispositivo físico. **Este token ahora se muestra de forma transparente en la interfaz de usuario de la sección "Alertas" para facilitar su copiado y verificación.**
2. La aplicación móvil hace un *request* constante a los servicios backend (Econet) para guardar este Token, atándolo fuertemente al `codigoAgenda` del operador que inició sesión.
3. El servicio de monitoreo solo interactúa y reenvía alertas a aquellos `Tokens` específicos obtenidos desde la base de datos de auditoría `Econet.dbo.UsuariosNotificacion`.

## Flujo de Recepción (End-to-End)
1. **Detección**: La Alerta nace en el motor de **Grafana Alerting**.
2. **Webhook**: Grafana envía un webhook HTTP al microservicio **`fcm-bridge`**.
3. **Mapeo de Usuarios**: El puente determina **quiénes** están activos de turno mediante `EcoMonitorDb`.
4. **Obtención de Tokens**: El puente lee sigilosamente los `TokenNotificacion` reportados por la app en `EconetDb`.
5. **Distribución Multicast**: **Firebase FCM** recibe el paquete del puente y lo direcciona exacta y unívocamente a la capa de notificación (Action Layer) de los teléfonos validados.
6. **Recepción local**: Si la App está en primer plano o segundo plano, el sistema emite la vibración y notificación visual correspondiente (tomando el color según la metadata `severity` inyectada en el Push), sin que la app haya tenido que hacer nunca un *request* pidiendo los errores por sí misma.

## Configuración y Dependencias
Para compilar y conectar la aplicación a la nube de Google en tu propio entorno, debes configurar internamente los descriptores de Firebase:

1. **El archivo `google-services.json`:**
   Debes descargar el archivo descriptor de tu proyecto de la consola de administración de Firebase y colocarlo físicamente en la carpeta `app` del directorio del proyecto Android:
   > Ruta obligatoria: `/app/google-services.json`

2. **Sincronización Crítica de Archivos de Llave**:
   Para que todo funcione, la aplicación Android debe estar vinculada obligatoriamente **al mismo proyecto exacto de Firebase** del que se descargó el `firebase-service-account.json`. 
   * En palabras simples: El `.json` del backend (Docker) debe apuntar al mismo proyecto del que bajas el `google-services.json` para Android. De lo contrario, los permisos de empuje fallarán por invalid tokens.

*Para referencia del backend Puente y sus bases de datos, consulta [`fcm-bridge.md`](fcm-bridge.md).*
