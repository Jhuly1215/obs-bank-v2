# ObsBank - Despliegue a Producción

Este directorio contiene los overlays y configuraciones secretas que transforman el entorno de desarrollo local en un ecosistema listamente migrado hacia Producción. 

Gracias a la última unificación arquitectónica, **todo el clúster (Loki, Tempo, MinIO, Grafana AD, Alloy)** se controla desde un único archivo sobreescritor (`docker-compose.prod.yml`), simplificando enormemente el arranque.

## Prerequisitos

- Docker Engine ≥ 26 instalado en el servidor.
- Modificar el `.env` nativo de este directorio con tus contraseñas S3 (`MINIO_ROOT_PASSWORD`), bases de datos (`SQLSERVER_CONN`) y Active Directory.
- Cuenta de Firebase con el archivo `firebase-service-account.json` disponible en `observability/certs/`.

---

## 1. Configurar el archivo `.env`

Edita **estrictamente** el archivo `deploy/prod/.env` (no el de la raíz externa) con los valores reales del entorno antes de arrancar. Ejemplo de campos críticos:

```dotenv
MINIO_ROOT_PASSWORD=...          # > 8 Caracteres Obligatorio (S3 Storage)
SQLSERVER_CONN=Server=...        # Cadena de conexión real al Transaccional
BRIDGE_API_KEY=...               # Clave secreta del webhook FCM
LDAP_BIND_PASSWORD=...           # Clave del service account Active Directory
```

---

## 2. Comandos de Arranque Oficial

Todos los comandos deben ejecutarse obligatoriamente desde la **raíz del proyecto** (`obs-bank-v2/`). Tienes dos vías para iniciarlo:

### Vía Recomendada (Script Automatizado)
Es el método más puro y a prueba de fallos humanos. Limpiará volúmenes temporales, ensamblará las capas e inyectará el `--env-file` oficial de producción:
```powershell
./deploy.prod.ps1
```

### Vía Manual (Vanilla Docker Compose)
Si te encuentras en un entorno puramente Linux o bash donde no puedes usar `.ps1`, este es el comando íntegro a correr:
```bash
docker compose --env-file deploy/prod/.env \
  -f docker-compose.yml \
  -f deploy/prod/docker-compose.prod.yml \
  up -d --build
```
*(Nota: Ya no existen múltiples `-f` regados para loki, tempo, minio. ¡`docker-compose.prod.yml` ahora absorbe todos los sistemas per se!)*

---

## 3. Puertos Expuestos (Seguridad)

En Producción, casi todos los sistemas están **ocultos** a la red LAN de tu Host (viven exclusivamente en la subred `obs`). Las excepciones son:

| Servicio | Puerto | Descripción |
|---|---|---|
| Grafana | `3000` | UI de análisis visual para directores y Ops. |
| OTLP gRPC | `4317` | Receptor oficial de trazas (Usa este en tus APIs .NET/Java externas) |
| OTLP HTTP | `4318` | Receptor alternativo HTTP para la capa OpenTelemetry |
| MinIO Console | `9001` | Manejador Web del disco virtual S3 (Ingresar con obsbank_minio_admin) |

> **Opcional:** Si deseas "abrir" los puertos del API Demo y visualizarla a fuerza bruta desde el exterior, puedes encadenar el utilitario `-f deploy/prod/docker-compose.expose-demo.yml` a tu despliegue.

---

## 4. Active Directory (LDAP) Corporativo

El archivo **`deploy/prod/ldap.prod.toml`** controla tu acceso de ventanas unificado al banco. Debes reemplazar con tu información real de SysAdmin:

- `host` → IP de tu Controlador de Dominio principal.
- `bind_dn` → DN del "Service Account" de lectura que buscará a los usuarios.
- `search_base_dns` → Organizational Unit (OU) específica donde viven los usuarios de Banco.
- `group_mappings` → Mapeo 1:1 entre tus grupos de seguridad y los roles `Admin/Editor/Viewer` en Grafana.

---

Para lecturas de alto nivel sobre esta arquitectura S3, revisa el [`README.md`](../../README.md) Maestro alojado en la Raíz del proyecto.
