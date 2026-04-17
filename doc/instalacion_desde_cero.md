# Guía de Instalación desde Cero (Paso a Paso)

Este documento te guía a través del proceso exacto para levantar la plataforma de observabilidad **ObsBank-v2** inmediatamente después de haber clonado el repositorio por primera vez.

---

## 🛠️ Requisitos Previos (Tu Entorno)

Antes de empezar, asegúrate de tener instalados:
1. **Docker Engine / Docker Desktop** (Versión 24.0 o superior). Debe estar ejecutándose.
2. **Git** (Para clonar el repositorio).
3. **.NET 9/10 SDK** (Opcional, solo si necesitas abrir o compilar manualmente el código de las APIs).

---

## 🚀 PASO 1: Clonar y Preparar

Abre tu terminal (PowerShell o Bash) y ejecuta:

```bash
git clone <URL_DEL_REPOSITORIO>
cd obs-bank-v2
```

---

## 🔐 PASO 2: Configurar el `.env` (Zero-Hardcode)

Todo el proyecto depende de su archivo maestro de configuración.

1. En la raíz del proyecto, debes crear un archivo llamado `.env` (si aún no existe) o revisar el que viene por defecto. 
   > *Tip:* Si viene un archivo llamado `.env.example`, cópialo y renómbralo a `.env`.
2. **Variables a revisar:**
   - Asegúrate de que `DOMAIN_NAME` sea el tuyo (ej. `localhost` para pruebas).
   - Revisa la conexión de Base de Datos en `SQLSERVER_CONN`. El servidor local de tu máquina usualmente es `host.docker.internal,1433`.

---

## 🔑 PASO 3: Certificados y Notificaciones Móviles

Para que la App de Android pueda recibir alertas y HTTPS funcione, debes cumplir con dos requisitos de archivos físicos antes de levantar:

1. **Firebase / Alertas Push:** Solicita el archivo `firebase-service-account.json` al administrador de Google Cloud e insértalo en la carpeta:
   `observability/certs/firebase-service-account.json`. *(Sin esto, el contenedor `fcm-bridge` fallará)*.
   
2. **Certificación SSL (Solo Producción):** Si estás simulando producción, necesitas poner tus certificados en la carpeta `deploy/prod/certs/` llamándolos `fullchain.pem` y `privkey.pem`.

---

## 🏗️ PASO 4: Levantar la Infraestructura

Decide bajo qué escenario jugarás:

### Opción A: Desarrollo Rápido (Recomendado para tu PC)
Levanta todos los componentes sin almacenamiento masivo S3. Los logs vivirán temporalmente de forma local.

```bash
docker compose up -d --build
```

### Opción B: Producción (Almacenamiento S3 + Nginx SSL)
Levanta la arquitectura definitiva que comprime historial en Terabytes.

```powershell
./deploy.prod.ps1
```

---

## ✅ PASO 5: Verificar Componentes

Una vez que la terminal devuelva el control, comprueba que todos arranquen limpios:
```bash
docker compose ps
```

1. **Accede a Grafana:** Abre tu navegador en `http://localhost:3000` (o `https://TuDominio` en Producción).
2. **Ingresa con LDAP (O cuentas quemadas):** Usuario `admin` / Password `admin` (Definido en tu `.env`).
3. **Verifica Traces/Logs:** Ve al menú "Explore" de Grafana, elige "Tempo" y haz click en "Run Query". Si ves gráficas de puntos verdes, Todo está operando.

---

## ⚠️ Posibles Problemas Locales (Troubleshooting)

- **"Docker no conecta al SQL Server de mi Windows"**: Habilita las conexiones de red por TCP/IP en *SQL Server Configuration Manager*.
- **"Grafana muestra 'Data source not found'"**: Probablemente olvidaste definir `PROMETHEUS_URL` en tu archivo `.env`. Todos los enlaces del archivo maestro deben estar rellenos.
- **"Falla el Webhook de alertas"**: El token `BRIDGE_API_KEY` de Grafana (Contact Points) debe coincidir matemáticamente con la variable definida en el `.env`.

---
**Felicidades, acabas de desplegar una arquitectura de Telemetría Industrial en tu entorno.**
