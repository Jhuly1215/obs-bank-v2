# ObsBank - Despliegue a Producción (S3 & IIS Integration)

Este directorio contiene la orquestación maestra para el entorno de producción del Banco. El sistema está diseñado para integrarse con la infraestructura de Windows/IIS existente mientras utiliza Docker para el stack de observabilidad.

---

## 🛠️ Configuración "Zero-Hardcode"

En producción, el sistema utiliza **MinIO (S3)** para garantizar que los logs y trazas se conserven por años sin saturar el disco del servidor, permitiendo escalabilidad horizontal.

### Pasos Críticos pre-Arranque:
1. **Archivo `.env`**: Edita `deploy/prod/.env` con los valores reales:
   - `DOMAIN_NAME`: El dominio DNS asignado (ej: `obs.ecofuturo.com.bo`).
   - `LOKI_S3_ENDPOINT`: IP y puerto del servidor remoto de MinIO (ej: `10.0.0.50:9000`).
   - `SQLSERVER_CONN`: Cadena de conexión al SQL Server corporativo.
   - `LDAP_SERVER_HOST`: IP del Controlador de Dominio (Active Directory).
   - `PROD_LOGS_PATH`: Ruta absoluta en el host donde las APIs escriben sus logs.
2. **Servidor de Almacenamiento**:
   - Asegúrese de que el servidor remoto de MinIO sea accesible desde este servidor en el puerto 9000.
   - Las credenciales (`MINIO_ROOT_USER`/`PASSWORD`) deben coincidir en ambos servidores.
3. **Proxy Inverso (IIS)**: 
   - Se debe configurar **Application Request Routing (ARR)** y **URL Rewrite** en el IIS del host.
   - Crear un sitio que escuche en el puerto 443 (HTTPS) y redirija el tráfico al puerto 3000 (Grafana).
4. **FCM (Firebase)**: Colocar `firebase-service-account.json` en `observability/certs/` para las notificaciones.

---

## 🚀 Comandos de Operación

Desde la raíz del proyecto (`obs-bank-v2/`):

### Iniciar / Actualizar
El script Powershell automatiza la combinación de configuraciones base y de producción:
```powershell
./deploy.prod.ps1
```

### Detener el Stack
```powershell
docker compose -f docker-compose.yml -f deploy/prod/docker-compose.prod.yml down
```

---

## 📂 Arquitectura de Producción Actualizada

| Componente | Función en Prod | Configuración |
| :--- | :--- | :--- |
| **IIS (Host)** | Proxy Inverso y SSL. | Puerto 443 -> localhost:3000 |
| **Loki / Tempo** | Almacenamiento en S3 (MinIO). | Persistencia de largo plazo |
| **Config-Init** | Automatizador de AD/LDAP. | Inyecta credenciales en Grafana |
| **OpenLDAP (Test)** | Solo para ambientes de prueba. | Habilitado con bootstrap.ldif |
| **SQL Poller** | Monitoreo de Transacciones. | Ingesta de métricas de negocio |

---

## 🚨 Seguridad y Cumplimiento

1. **Aislamiento**: Ningún servicio (Prometheus, Loki, MinIO API) debe exponerse a la red LAN. Solo el Colector OTLP (4317) y el IIS (443) deben ser accesibles.
2. **Retención**: La retención de datos se gestiona en los archivos `loki.yml` y `tempo.yml` en la carpeta `config/`. Ajustar según política de auditoría del banco.
3. **Backups**: Realizar backups periódicos del volumen `minio_data_prod` para asegurar la persistencia de los logs históricos.

---
_"La estabilidad del Banco depende de la visibilidad de sus datos"._
