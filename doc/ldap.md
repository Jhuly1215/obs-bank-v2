# Integración y Configuración de Autenticación LDAP (Active Directory)

Este documento detalla la arquitectura de autenticación **LDAP (Lightweight Directory Access Protocol)** implementada en Grafana para el entorno local, su funcionamiento técnico y el procedimiento de migración hacia un entorno de Producción soportado por un Controlador de Dominio Windows (Active Directory).

---

## 1. Naturaleza de la Integración (Mock local vs. Producción)

La integración configurada en el entorno local **utiliza implementaciones nativas del protocolo LDAP**, garantizando paridad arquitectónica con entornos productivos.

*   **Capa de Aplicación (Grafana):** El módulo de autenticación LDAP de Grafana opera bajo configuraciones formales (`GF_AUTH_LDAP_ENABLED=true`), ejecutando operaciones de tipo `Bind` y `Search` reales sobre la red de contenedores.
*   **Capa de Directorio (Mock Server):** Para suprimir la dependencia de conectividad con un Directorio Activo corporativo durante el desarrollo local, el stack orquesta un contenedor OpenLDAP efímero (imagen `rroemhild/test-openldap`). Este servidor de prueba expone el puerto `10389` e incluye un árbol de datos precargado (`dc=planetexpress,dc=com`) estructurado con Unidades Organizativas (`ou=people`) y cuentas de usuario funcionales (ej. `uid=fry`, `uid=bender`).

Esta dualidad asegura que la transición al entorno productivo requiera exclusivamente modificaciones en la cadena de conexión, sin impacto en la lógica de autorización de la plataforma de observabilidad.

---

## 2. Topología de la Configuración Local

La integración actual se fundamenta en los siguientes componentes:

1. **Docker Compose (`docker-compose.yml`)**: Provisiona el servicio local `openldap` e inicializa las variables de entorno habilitadoras en el servicio `grafana` (`GF_AUTH_LDAP_CONFIG_FILE=/etc/grafana/ldap.toml`).
2. **Archivo de Configuración y Mapeo (`observability/grafana/ldap.toml`)**:
   - Resuelve el endpoint interno del contenedor y su puerto expuesto para el protocolo (`host = "openldap"`, `port = 10389`).
   - Define el *Service Account* (Cuenta de servicio) de lectura (`bind_dn = "cn=admin,dc=planetexpress,dc=com"`) y sus credenciales estáticas.
   - Aplica filtros de búsqueda originarios OID (`search_filter = "(uid=%s)"`) sobre el *Base DN* poblado con usuarios de prueba.
   - Establece reglas de *Group Mapping* para asignar roles internos de Grafana en base a la pertenencia de grupos LDAP (RBAC):
     - El grupo `cn=admin_staff` otorga privilegios **`Admin`**.
     - El grupo `cn=ship_crew` otorga privilegios **`Viewer`**.

> **Validación Operativa (Testing Local):**
> - Credenciales estándar de observador: `fry` / `fry`
> - Credenciales con privilegios administrativos: `leela` / `leela`

---

## 3. Procedimiento de Migración a Producción (Active Directory)

Para enrutar la autenticación de Grafana hacia el controlador de dominio empresarial y habilitar el Single Sign-On (SSO) con las credenciales formales del personal, es imperativo gestionar los siguientes requisitos con el departamento de Infraestructura, Redes o Seguridad Informática (Active Directory):

-   **Endpoint del Controlador de Dominio (Domain Controller):** Dirección IP o FQDN del servidor (Ej. `10.5.1.20` o `dc01.banco.local`).
-   **Service Account (Bind DN):** Cuenta de servicio sin atributos de escritura o expiración de contraseña, destinada exclusivamente a buscar e iterar sobre el árbol del directorio. (Ej. `CN=grafana_svc,OU=ServiceAccounts,DC=mibanco,DC=com`).
-   **Base DN de Búsqueda:** Nodo principal u origin (OU) donde residen los objetos de tipo *Usuario* designados para el consumo de la plataforma (Ej. `OU=Empleados,DC=mibanco,DC=com`).
-   **Grupos de Seguridad (Security Groups):** Distinguished Names (DN) exactos de los grupos de Windows destinados a separar roles de visualización operativa y administración de dashboards.

### 🔴 Configuración Técnica de Cambio de Entorno

Una vez gestionadas las directivas de red corporativa y reglas de Firewall requeridas para habilitar la comunicación entre las instancias de Docker locales y el servidor Active Directory, deben aplicarse las siguientes modificaciones:

#### 3.1. Reestructuración de la Orquestación
Debe eliminarse o comentarse el servicio de simulación `openldap` en el archivo `docker-compose.yml`, dado que la consulta LDAP se emitirá directamente a la red corporativa.

#### 3.2. Modificación del Descriptor TOML
El archivo local `observability/grafana/ldap.toml` debe ser depurado y reconfigurado con los parámetros corporativos formales:

```toml
[[servers]]
# Endpoint corporativo Active Directory
host = "10.5.1.20"
port = 389 
use_ssl = false # Validar TLS. Cambiar a 'true' y configurar puerto '636' si se exige LDAPS.

# Credenciales de la Cuenta de Servicio (Bind)
bind_dn = "CN=grafana_svc,OU=ServiceAccounts,DC=mibanco,DC=com"
bind_password = "PasswordDeServicioDefinitivo"

# Ruteo y Búsqueda (Filtro estándar base Active Directory sAMAccountName)
search_filter = "(sAMAccountName=%s)"
search_base_dns = ["OU=Empleados,DC=mibanco,DC=com"]

[servers.attributes]
# Taxonomía y sintaxis estándar de atributos AD
name = "givenName"
surname = "sn"
username = "sAMAccountName"
member_of = "memberOf"
email =  "mail"

[[servers.group_mappings]]
# Mapeo Rol Visor: Pertenencia a grupo corporativo "Monitoreo_Nivel1"
group_dn = "CN=Monitoreo_Nivel1,OU=SecurityGroups,DC=mibanco,DC=com"
org_role = "Viewer"

[[servers.group_mappings]]
# Mapeo Rol Administrador: Pertenencia a grupo corporativo "Arquitectura_Sistemas"
group_dn = "CN=Arquitectura_Sistemas,OU=SecurityGroups,DC=mibanco,DC=com"
org_role = "Admin"
```

Una vez consolidados ambos requerimientos, la instancia de Grafana debe ser reiniciada desde Docker para recargar la variante actualizada de `ldap.toml`. Finalizado el despliegue, las validaciones de inicio de sesión de todos los ingenieros o responsables de TI operarán de forma transparente contra el Active Directory central de la institución bancaria.
