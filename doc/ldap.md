# Integración Active Directory / LDAP en ObsBank-v2

Este documento describe el estado real actual de la integración LDAP en ObsBank-v2, tomando como base la rama `base` del repositorio.

Punto clave: actualmente el proyecto no genera el archivo `ldap.toml` mediante `config-init`. En la práctica, Grafana usa un archivo `ldap.toml` estático montado directamente desde el repositorio.

---

## 1. Objetivo de la integración LDAP

Grafana usa LDAP para autenticar usuarios contra un directorio de identidad.

En un entorno final del banco, la intención es que Grafana autentique contra el Active Directory corporativo de Ecofuturo. Sin embargo, en el estado actual del proyecto, la configuración efectiva apunta a un servicio OpenLDAP local de prueba llamado `openldap`.

Esto significa que la integración LDAP está preparada a nivel de Grafana, pero todavía no está completamente alineada con una configuración real de Active Directory corporativo.

---

## 2. Estado actual de la implementación

Actualmente existen tres piezas relevantes:

1. Grafana tiene LDAP habilitado mediante variables de entorno.
2. Grafana monta un archivo estático `deploy/prod/config/ldap.toml`.

Por tanto, el comportamiento actual es:

```text
deploy/prod/config/ldap.toml
        ↓ volumen Docker
/etc/grafana/ldap.toml
        ↓ leído por Grafana
Autenticación LDAP
```

No es correcto decir que el proyecto actualmente genera `ldap.toml` dinámicamente desde variables de entorno.

---

## 3. Archivos involucrados

Los archivos relevantes para LDAP son:

```text
obs-bank-v2/
├── docker-compose.yml
├── deploy.prod.ps1
├── deploy/
│   └── prod/
│       ├── docker-compose.prod.yml
│       ├── env.example
│       └── config/
│           └── ldap.toml
└── doc/
    └── ldap.md
```

### Descripción de cada archivo

| Archivo | Uso actual |
|---|---|
| `docker-compose.yml` | Define Grafana, OpenLDAP y el montaje real de `ldap.toml`. |
| `deploy/prod/config/ldap.toml` | Archivo LDAP realmente usado por Grafana. Actualmente apunta a `openldap`. |
| `deploy/prod/docker-compose.prod.yml` | Override de producción. No reemplaza el volumen LDAP base. |
| `deploy/prod/env.example` | Contiene variables LDAP parciales, pero no controla el `ldap.toml` efectivo de Grafana en el estado actual. |
| `deploy.prod.ps1` | Levanta el stack con `docker-compose.yml` y `deploy/prod/docker-compose.prod.yml`. |

---

## 4. Configuración efectiva de Grafana

En el `docker-compose.yml`, Grafana tiene habilitado LDAP con estas variables:

```yaml
GF_AUTH_LDAP_ENABLED: "true"
GF_AUTH_LDAP_CONFIG_FILE: "/etc/grafana/ldap.toml"
GF_AUTH_LDAP_ALLOW_SIGN_UP: "true"
```

Esto significa:

- LDAP está habilitado.
- Grafana busca su configuración en `/etc/grafana/ldap.toml`.
- Si un usuario LDAP válido inicia sesión, Grafana puede crear la cuenta automáticamente.

El archivo `/etc/grafana/ldap.toml` no se genera dinámicamente. Se monta desde el host usando este volumen:

```yaml
volumes:
  - ./deploy/prod/config/ldap.toml:/etc/grafana/ldap.toml:ro
```

Por tanto, el archivo efectivo es:

```text
deploy/prod/config/ldap.toml
```

---

## 5. Configuración real del archivo `ldap.toml`

El archivo actualmente usado por Grafana tiene una configuración de prueba basada en OpenLDAP:

```toml
[[servers]]
host = "openldap"
port = 389
use_ssl = false
start_tls = false
ssl_skip_verify = false
timeout = 10

bind_dn = "cn=admin,dc=planetexpress,dc=com"
bind_password = "GoodNewsEveryone"

search_filter = "(uid=%s)"
search_base_dns = ["ou=people,dc=planetexpress,dc=com"]

[servers.attributes]
name = "givenName"
surname = "sn"
username = "uid"
email = "mail"
member_of = "memberOf"

[[servers.group_mappings]]
group_dn = "cn=admin_staff,ou=people,dc=planetexpress,dc=com"
org_role = "Admin"
grafana_admin = true

[[servers.group_mappings]]
group_dn = "cn=ship_crew,ou=people,dc=planetexpress,dc=com"
org_role = "Viewer"
```

Esta configuración no apunta al Active Directory corporativo. Apunta al contenedor `openldap` dentro de la red Docker del proyecto.

---

## 6. Servicio OpenLDAP actual

El proyecto incluye un servicio `openldap` en `docker-compose.yml`.

Su configuración base es:

```yaml
openldap:
  image: osixia/openldap:1.4.0
  container_name: obs-bank-v2-ldap-1
  ports:
    - "${LDAP_PORT:-389}:389"
  environment:
    - LDAP_DOMAIN=${LDAP_DOMAIN:-planetexpress.com}
    - LDAP_ADMIN_PASSWORD=${LDAP_BIND_PASSWORD:-GoodNewsEveryone}
  networks: [ obs ]
```

En el override de producción `deploy/prod/docker-compose.prod.yml`, el servicio `openldap` no se elimina ni se deshabilita. Más bien, se le agregan variables y un volumen adicional:

```yaml
openldap:
  environment:
    - LDAP_DOMAIN=${LDAP_DOMAIN:-planetexpress.com}
    - LDAP_ADMIN_PASSWORD=${LDAP_BIND_PASSWORD:-GoodNewsEveryone}
    - LDAP_TLS=false
    - LDAP_REMOVE_CONFIG_AFTER_SETUP=false
    - LDAP_RFC2307BIS_SCHEMA=true
  volumes:
    - ./deploy/prod/config/ldap:/container/service/slapd/assets/config/bootstrap/ldif/custom
  networks: [ obs ]
```

Esto implica que, al levantar el stack productivo actual con ambos archivos Compose, el servicio `openldap` sigue formando parte del stack.

Si `deploy/prod/config/ldap.toml` mantiene:

```toml
host = "openldap"
```

entonces Grafana sigue autenticando contra ese OpenLDAP local, no contra un Active Directory externo.

---

## 9. Variables LDAP en `.env`

Estas variables afectan principalmente al contenedor `openldap`:

```env
LDAP_DOMAIN=planetexpress.com
LDAP_BIND_PASSWORD=GoodNewsEveryone
LDAP_PORT=389
```

## 10. Diferencia entre OpenLDAP de prueba y Active Directory real

La configuración actual corresponde a OpenLDAP de prueba:

| Elemento | Configuración actual |
|---|---|
| Host LDAP | `openldap` |
| Puerto | `389` |
| Dominio | `planetexpress.com` |
| Bind DN | `cn=admin,dc=planetexpress,dc=com` |
| Filtro de búsqueda | `(uid=%s)` |
| Base DN | `ou=people,dc=planetexpress,dc=com` |
| Grupo Admin | `cn=admin_staff,ou=people,dc=planetexpress,dc=com` |
| Grupo Viewer | `cn=ship_crew,ou=people,dc=planetexpress,dc=com` |

Para Active Directory real, normalmente se necesitaría una configuración diferente:

| Elemento | Ejemplo AD |
|---|---|
| Host LDAP | `ad.ecofuturo.local` o IP del controlador de dominio |
| Puerto | `389` o `636` |
| Bind DN | Cuenta de servicio LDAP |
| Filtro de búsqueda | `(sAMAccountName=%s)` |
| Base DN | OU real de usuarios del banco |
| Grupo Admin | DN real del grupo AD de administradores Grafana |
| Grupo Viewer | DN real del grupo AD de visores Grafana |

---

## 11. Roles configurados en Grafana

El archivo actual define dos mapeos de grupo:

```toml
[[servers.group_mappings]]
group_dn = "cn=admin_staff,ou=people,dc=planetexpress,dc=com"
org_role = "Admin"
grafana_admin = true

[[servers.group_mappings]]
group_dn = "cn=ship_crew,ou=people,dc=planetexpress,dc=com"
org_role = "Viewer"
```

Esto significa:

| Grupo LDAP | Rol asignado |
|---|---|
| `admin_staff` | Admin |
| `ship_crew` | Viewer |

Además, el grupo `admin_staff` tiene:

```toml
grafana_admin = true
```

Eso no solo lo vuelve administrador de la organización en Grafana, sino administrador global de Grafana.

Para producción real, esto debe revisarse con cuidado. No es recomendable otorgar `grafana_admin = true` a un grupo amplio.

---

## 12. Ejecución actual del stack

El despliegue productivo se realiza con:

```powershell
./deploy.prod.ps1
```

Internamente ejecuta:

```powershell
docker compose --env-file deploy/prod/.env `
  -f docker-compose.yml `
  -f deploy/prod/docker-compose.prod.yml `
  up -d --build
```

Este comando:

1. Carga variables desde `deploy/prod/.env`.
2. Usa `docker-compose.yml` como base.
3. Aplica `deploy/prod/docker-compose.prod.yml` como override.
4. Levanta los servicios.

Pero no genera `ldap.toml`.

---

## 13. Verificación del archivo LDAP real usado por Grafana

Para revisar qué archivo está leyendo Grafana:

```bash
docker compose exec grafana cat /etc/grafana/ldap.toml
```

Si estás usando el flujo productivo:

```bash
docker compose --env-file deploy/prod/.env   -f docker-compose.yml   -f deploy/prod/docker-compose.prod.yml   exec grafana cat /etc/grafana/ldap.toml
```

El contenido debería coincidir con:

```text
deploy/prod/config/ldap.toml
```

Si aparece:

```toml
host = "openldap"
```

entonces Grafana está autenticando contra el OpenLDAP local.

---

## 14. Verificación de la configuración final de Docker Compose

Para ver la configuración final después de fusionar ambos Compose:

```bash
docker compose --env-file deploy/prod/.env   -f docker-compose.yml   -f deploy/prod/docker-compose.prod.yml   config
```

Luego buscar la sección de `grafana`.

Se debería encontrar un volumen equivalente a:

```yaml
- ./deploy/prod/config/ldap.toml:/etc/grafana/ldap.toml:ro
```

Si ese volumen sigue apareciendo, el archivo usado por Grafana es el estático.

---

## 15. Troubleshooting

### 15.1. Grafana no permite iniciar sesión

Revisar logs:

```bash
docker compose logs grafana
```

En producción:

```bash
docker compose --env-file deploy/prod/.env   -f docker-compose.yml   -f deploy/prod/docker-compose.prod.yml   logs grafana
```

Errores comunes:

| Error | Posible causa |
|---|---|
| `LDAP user not found` | El usuario no existe en el LDAP configurado o el `search_base_dns` no corresponde. |
| `Invalid username or password` | Credenciales incorrectas o usuario no encontrado. |
| `Failed to bind` | `bind_dn` o `bind_password` incorrecto. |
| `connection refused` | El host o puerto LDAP no está disponible. |
| Usuario entra sin rol esperado | El usuario no pertenece al grupo mapeado o el DN del grupo no coincide. |

---

### 15.2. OpenLDAP no tiene usuarios de prueba

El contenedor OpenLDAP necesita datos cargados para poder autenticar usuarios.

El override productivo monta:

```yaml
./deploy/prod/config/ldap:/container/service/slapd/assets/config/bootstrap/ldif/custom
```

Por tanto, si se usan usuarios o grupos de prueba, deben existir archivos LDIF válidos en esa ruta.

No basta con que `ldap.toml` tenga grupos como:

```toml
cn=admin_staff,ou=people,dc=planetexpress,dc=com
cn=ship_crew,ou=people,dc=planetexpress,dc=com
```

Esos grupos deben existir realmente en el LDAP.

---

## 16. Cómo debería configurarse para Active Directory real

Si el objetivo es conectar Grafana al Active Directory real del banco, hay dos caminos posibles.

---

### Opción A: Mantener archivo estático

Editar directamente:

```text
deploy/prod/config/ldap.toml
```

y reemplazar la configuración mock por valores reales de AD.

Ejemplo referencial:

```toml
[[servers]]
host = "ad.ecofuturo.local"
port = 389
use_ssl = false
start_tls = false
ssl_skip_verify = false
timeout = 10

bind_dn = "CN=svc_grafana_ldap,OU=ServiceAccounts,DC=ecofuturo,DC=com,DC=bo"
bind_password = "CAMBIAR_PASSWORD_REAL"

search_filter = "(sAMAccountName=%s)"
search_base_dns = ["OU=Usuarios,DC=ecofuturo,DC=com,DC=bo"]

[servers.attributes]
name = "givenName"
surname = "sn"
username = "sAMAccountName"
email = "mail"
member_of = "memberOf"

[[servers.group_mappings]]
group_dn = "CN=GG-Grafana-Admins,OU=Groups,DC=ecofuturo,DC=com,DC=bo"
org_role = "Admin"

[[servers.group_mappings]]
group_dn = "CN=GG-Grafana-Viewers,OU=Groups,DC=ecofuturo,DC=com,DC=bo"
org_role = "Viewer"
```

Problema de esta opción: si se guarda una contraseña real en ese archivo, se corre el riesgo de versionar secretos.

---

## 19. Comandos útiles

### Ver logs de Grafana

```bash
docker compose logs grafana
```

### Ver logs de OpenLDAP

```bash
docker compose logs openldap
```

### Ver el archivo LDAP dentro de Grafana

```bash
docker compose exec grafana cat /etc/grafana/ldap.toml
```

### Ver la configuración final fusionada de Compose

```bash
docker compose --env-file deploy/prod/.env   -f docker-compose.yml   -f deploy/prod/docker-compose.prod.yml   config
```

### Levantar producción

```powershell
./deploy.prod.ps1
```

---

## 20. Conclusión

El estado actual de ObsBank-v2 es el siguiente:

| Punto | Estado actual |
|---|---|
| LDAP habilitado en Grafana | Sí |
| Archivo usado por Grafana | `/etc/grafana/ldap.toml` |
| Fuente real del archivo | `deploy/prod/config/ldap.toml` |
| LDAP actual configurado | OpenLDAP local |
| Active Directory real | No conectado todavía |
| `openldap` en producción Compose | Sigue incluido |
| Variables `.env` LDAP | Aplicadas al `ldap.toml` efectivo |
