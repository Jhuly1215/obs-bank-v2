# ObsBank-v2: Documentación de Arquitectura de Observabilidad

Esta plataforma centraliza la telemetría (métrica, traza, log) y el alertamiento de una infraestructura bancaria, utilizando un modelo de **Configuración Única (.env)**.

---

## 🏗️ Bloques Arquitectónicos

### 1. Ingesta Integrada (OpenTelemetry)
- **APIs .NET**: Utilizan el SDK de OpenTelemetry para emitir telemetría vía gRPC (Puerto 4317).
- **Logs Externos**: Grafana Alloy monitorea carpetas locales y extrae logs TXT/JSON/LOG, etiquetándolos dinámicamente según su origen.

### 2. Backend de Datos (Grafana Stack)
- **Prometheus**: Almacena métricas de performance y de negocio. Incluye reglas de alerta nativas en `rules.yml`.
- **Loki**: Indexador de logs de alta eficiencia.
- **Tempo**: Motor de trazas distribuidas para correlacionar eventos.
- **MinIO (Producción)**: Proporciona almacenamiento masivo persistente compatible con S3.

### 3. Canal de Alerta Móvil (Push)
- **FCM Bridge**: Traduce las alertas de Grafana en notificaciones Push para el app móvil de EcoFuturo vía Firebase.

---

## 🛠️ Despliegue Consolidado

El proyecto ya no requiere múltiples comandos complejos. Se basa en capas de Docker Compose:

### Modo Desarrollo
```bash
docker compose up -d
```

### Modo Producción
```powershell
./deploy.prod.ps1
```
*Este comando activa automáticamente el almacenamiento MinIO y la terminación SSL vía Nginx.*

---

## 🔒 Seguridad y Acceso

- **Identidad**: Integración nativa con **Active Directory (LDAP)**. La configuración se automatiza mediante plantillas `.template` que inyectan las credenciales del `.env` al arrancar.
- **Tráfico**: En producción, el tráfico externo solo se permite por el puerto **443 (HTTPS)**.
- **Aislamiento**: Los backends de datos están en una red aislada no accesible desde el exterior.

---

## 📊 Visualización Estándar

Para explorar datos, el eje central es la etiqueta `service_name`.
- **Métricas**: `sql_poller_*` para negocio, `process_cpu_seconds_total` para infraestructura.
- **Logs**: `{service_name="nombre-del-servicio"}`.
- **Trazas**: Acceso directo desde los logs vía el `trace_id` compartido.

---
_"Un sistema sin monitoreo es un sistema a ciegas; ObsBank-v2 es la vista del Banco"._