# Implementación de Alertas FCM basadas en Token

## Resumen de Cambios
Se ha modificado el servicio `Bank.Obs.FcmBridge` para dejar de utilizar la mensajería basada en *topics* y enviar notificaciones directamente a tokens específicos de dispositivos registrados en la base de datos de auditoría.

## Integración con la App Móvil

La aplicación móvil deberá interactuar con los nuevos endpoints del backend para asegurar la correcta entrega de notificaciones push.

### 1. Registrar Token (`POST /v1/dispositivos-notificaciones`)
**Cuándo llamar:**
- Cuando el usuario inicia sesión.
- Cuando la app se abre (para asegurar que el `FechaUltimoUso` se actualice y el estado siga activo).
- Cada vez que el SDK de Firebase genera o refresca un nuevo token de registro en el dispositivo.

**Ejemplo de Payload:**
```json
{
  "codigoUsuario": "JPEREZ",
  "tokenFcm": "fz_yK12...",
  "identificadorDispositivo": "Galaxy S23",
  "versionAplicacion": "2.1.0"
}
```

### 2. Dar de Baja Token (`PATCH /v1/dispositivos-notificaciones/baja`)
**Cuándo llamar:**
- Cuando el usuario cierra sesión de manera voluntaria.
- Si desde la app se ofrece la opción explícita de "Desactivar Notificaciones".

**Ejemplo de Payload:**
```json
{
  "tokenFcm": "fz_yK12...",
  "motivoBaja": "cierre_sesion"
}
```

### 3. Invalidación Automática
El backend está configurado para que cuando Firebase devuelva un error indicando que un token FCM es inválido o el usuario desinstaló la app (`messaging/invalid-registration-token` o `messaging/registration-token-not-registered`), automáticamente se marque ese registro con `EstadoRegistro = 0` y `MotivoBaja = 'token_invalido'`, sin intervención adicional de la app móvil.
