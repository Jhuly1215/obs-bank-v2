# Integración Active Directory (LDAP) en ObsBank-v2

Grafana utiliza el protocolo LDAP para autenticar a los funcionarios del Banco contra el Controlador de Dominio corporativo.

---

## 🏗️ Arquitectura de Plantillas (Automation)

Dado que Grafana no soporta variables de entorno nativas dentro de su archivo `ldap.toml`, ObsBank-v2 utiliza un sistema de **Pre-procesamiento de Plantillas**:

1. **Plantilla**: El archivo `observability/grafana/ldap.toml.template` (o `deploy/prod/ldap.prod.toml.template`) contiene marcadores como `${LDAP_SERVER_HOST}`.
2. **Sidecar (Config-Init)**: Un contenedor ligero de Docker (`config-init`) procesa esta plantilla al arrancar el sistema e inyecta los valores reales desde el archivo `.env`.
3. **Resultado**: El archivo final `/etc/grafana/ldap.toml` se genera dinámicamente cada vez que inicias el stack.

---

## ⚙️ Configuración vía .env

Ya no necesitas editar los archivos `.toml` manualmente. Todo se controla desde el **`.env`**:

```env
# --- ACTIVE DIRECTORY / LDAP ---
LDAP_SERVER_HOST=SrvAD.banco.local
LDAP_SERVER_PORT=389
LDAP_BIND_DN=CN=SvcGrafana,OU=ServiceAccounts,DC=banco,DC=local
LDAP_BIND_PASSWORD=ContraseñaSegura123
LDAP_SEARCH_BASE=OU=Usuarios,DC=banco,DC=local
LDAP_GROUP_ADMIN=CN=Monitoreo-Admins,OU=Grupos,DC=banco,DC=local
LDAP_GROUP_VIEWER=CN=Funcionarios-Banco,OU=Grupos,DC=banco,DC=local
```

---

## 🔍 Verificación

Si tienes problemas para iniciar sesión:
1. Revisa los logs de Grafana: `docker compose logs grafana`.
2. Verifica que el archivo se generó correctamente dentro del contenedor:
   ```bash
   docker compose exec grafana cat /etc/grafana/ldap.toml
   ```

---
> **Seguridad**: El uso de plantillas evita que las rutas de tu Active Directory o las contraseñas de cuentas de servicio queden guardadas en los archivos de configuración estáticos del repositorio.
