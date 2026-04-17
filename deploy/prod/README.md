# ObsBank - Despliegue a Producción (S3 & Seguridad)

Este directorio contiene la orquestación maestra para el entorno de misión crítica del Banco. Gracias a la arquitectura unificada, la configuración es **100% externa** y reside exclusivamente en el archivo `.env`.

---

## 🛠️ Configuración "Zero-Hardcode" en PROD

En producción, el sistema utiliza **MinIO (S3)** para garantizar que los logs y trazas se conserven por años sin saturar el sistema de archivos del servidor.

### Pasos Críticos pre-Arranque:
1. **Archivo `.env`**: Edita `deploy/prod/.env` con las credenciales reales de Ecofuturo.
   - `SQLSERVER_CONN`: Cadena oficial del Banco.
   - `LDAP_SERVER_HOST`: IP del Controlador de Dominio.
   - `MINIO_ROOT_PASSWORD`: Contraseña robusta para el almacenamiento.
2. **Certificados SSL**: Coloca `fullchain.pem` y `privkey.pem` en `deploy/prod/certs/`.
3. **FCM (Firebase)**: Asegúrate de que `firebase-service-account.json` esté en `observability/certs/`.

---

## 🚀 Comandos de Operación

Desde la raíz del proyecto (`obs-bank-v2/`):

### Iniciar / Actualizar (Recomendado)
El script Powershell maneja la limpieza y la inyección de los archivos de producción automáticamente:
```powershell
./deploy.prod.ps1
```

### Detener el Stack
```bash
docker compose -f docker-compose.yml -f deploy/prod/docker-compose.prod.yml down
```

---

## 📂 Arquitectura de Producción

| Componente | Función en Prod | Configuración |
| :--- | :--- | :--- |
| **Nginx Proxy** | Seguridad SSL y balanceo. | Puerto 443 (HTTPS) |
| **Loki / Tempo** | Almacenamiento persistente en S3. | Buckets en MinIO |
| **Config-Init** | Automatizador de AD/LDAP. | Convierte plantillas en configs |
| **SQL Poller** | Monitoreo de Transacciones DB. | Métrica de negocio P99 |

---

## 🚨 Seguridad y Backup

1. **Invisibilidad**: Ningún servicio (Prometheus, Loki, DB) está expuesto a la red LAN, excepto el Proxy HTTPS (443) y el Colector OTLP (4317).
2. **Alertas Nativas**: El sistema incluye reglas de alerta en `observability/prometheus/rules.yml` que monitorean caídas de servicio y carga de CPU automáticamente.
3. **Backup de Logs**: Al estar en MinIO, puedes apuntar cualquier herramienta de backup de S3 a la carpeta de `minio_data` para respaldos fuera del sitio.

---
_"La estabilidad del Banco depende de la visibilidad de sus datos"._
