# ObsBank Alerts - Bridge (Grafana → FCM)

Este documento describe la configuración y uso del nuevo servicio `fcm-bridge`, el cual conecta las alertas disparadas por **Grafana Alerting** con tu aplicación Android **ObsBank Alerts** a través de **Firebase Cloud Messaging (FCM)**.

## Arquitectura

```text
Grafana Alerting → Webhook (POST JSON) → fcm-bridge (.NET 9) → Firebase Admin SDK → FCM (Topic) → App Android
```

El bridge expone un único endpoint seguro (`POST /alert`) que:
1. Recibe el *webhook* en formato JSON estándar de Grafana.
2. Identifica la etiqueta (`label`) `severity` dentro de `commonLabels` (critical, warning, info).
3. Parsea el título y cuerpo del mensaje.
4. Redirige (Push) el mensaje al *Topic* exacto de la App Android [**ObsBankAlerts**](app-movil.md) (`obsbank-critical`, `obsbank-warning` u `obsbank-info`).

---

## 🚀 1. Configuración de Firebase y Variables de Entorno

### Obtención del `service-account.json`
El código utiliza internamente `FirebaseAdmin SDK`. Para que esto funcione y tengas permisos de emitir notificaciones debes:
1. Ir a la consola web de Firebase (`Project Settings` > `Service Accounts`).
2. Generar y descargar una nueva **Clave privada** (JSON).
3. Guardar ese archivo en el repositorio como `observability/certs/firebase-service-account.json`. *(Asegúrate de que este archivo esté agregado al `.gitignore` para no subirlo al código fuente por error).*

### Variables Ocultas en `.env` principal
Añade la siguiente línea secreta a tu archivo de despliegue (`.env` local o de prod):
```dotenv
BRIDGE_API_KEY=secreto_123_cambiar_en_produccion
```
Esta será la "contraseña" que Grafana tendrá que enviarnos.

---

## 🐳 2. Configurando en Grafana Alerting

1. Ingresa a `Grafana` (http://localhost:3000)
2. Ve al menú lateral: **Alerting** -> **Contact points** -> **Add contact point**
3. Escoge el tipo: `Webhook`
4. Nombre: `FCM Bridge`
5. **URL:** `http://fcm-bridge:8080/alert` (Esta URL es accesible internamente dentro de la red Docker).
6. Despliega las opciones avanzadas (o "Optional Webhook Settings").
7. Busca la sección **HTTP Header** o **Authorization**.
8. Añade un header:
   - **Key:** `Authorization`
   - **Value:** `Bearer secreto_123_cambiar_en_produccion` (Debe coincidir EXACTAMENTE con tu env var `BRIDGE_API_KEY`).

Guardar y probar (usando el botón "Test" de Grafana). Si el JSON de Firebase está bien puesto, tu celular Android debería pitar inmediatamente.

---

## 🧪 3. Prueba Unitaria con cURL

Si quieres mandarle una notificación a la App de Android **sin necesidad de entrar a Grafana**, puedes simular un evento mandando un cURL a tu localhost (puerto `5001` expuesto del host al bridge interno `8080`).

```bash
curl -X POST http://localhost:5001/alert \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer secreto_123_cambiar_en_produccion" \
     -d '{
       "title": "[ALERTA] Uso Elevado de CPU",
       "message": "El nodo de pagos superó el 90%.",
       "commonLabels": {
           "severity": "critical",
           "service": "pagos"
       }
     }'
```
### Reacciones del FCM Topic
Como en el cURL anterior mapeamos `"severity": "critical"`, el bridge lo transmutará mágicamente como una solicitud POST hacia el Firebase Platform Cloud dirigido al topic: **`obsbank-critical`**. 

Si disparas otro con `"severity" : "warning"`, lo dirijirá al topic `obsbank-warning`.

---

## 🔔 4. Configuración de Etiquetas (Labels)

El **FCM Bridge** y la **Aplicación Android** dependen críticamente de la etiqueta `severity` incrustada por Grafana. Cuando crees una regla de alerta en Grafana, debes agregar un "Custom Label" llamado `severity`.

- **`severity = critical`**: Dispara notificaciones al topic de Firebase `obsbank-critical`. En la app Android el canal emite sonido de alarma alta prioritaria, y la tarjeta expansible brilla con un indicador lateral **Rojo 🔴**. Úsalo para errores catastróficos o bloqueantes (ej. fallos en transferencias interbancarias 5 o 9).
- **`severity = warning`**: Dispara notificaciones al topic de Firebase `obsbank-warning`. En la app Android se resalta la alerta en color **Naranja 🟠**. Úsalo para advertencias o fallos operativos de menor impacto (ej. transferencias internas rechazadas).
- **`severity = info`**: O cualquier etiqueta no reconocida enviará el Payload silencioso a `obsbank-info` dibujando una tarjeta **Azul 🔵** sin interrumpir al usuario drásticamente.
