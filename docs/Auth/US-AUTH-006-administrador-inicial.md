# US-AUTH-006 — Aprovisionar Administrador inicial

**Como** operador del despliegue, **quiero** aprovisionar la primera cuenta Administrador mediante configuración privada, **para** habilitar la revisión de solicitudes sin permitir altas públicas de administradores.

## Criterios de aceptación

### Crear Administrador inicial

- **Dado** que la cuenta configurada todavía no existe y las variables privadas de bootstrap están definidas,
- **Cuando** la API inicializa la base de datos,
- **Entonces** crea el usuario y el rol Administrador mediante ASP.NET Identity y persiste únicamente el hash de contraseña.

Para el primer arranque, el operador proporciona las credenciales acordadas de forma privada mediante `BootstrapAdmin__Email` y `BootstrapAdmin__Password`. La contraseña no se almacena en el código ni en la documentación y debe configurarse localmente antes de iniciar la API.

### No crear Administrador sin configuración

- **Dado** que no se han configurado credenciales de bootstrap,
- **Cuando** la API inicializa la base,
- **Entonces** no crea un usuario Administrador implícito.

### Sin secretos versionados

- **Dado** que se configura la cuenta Administrador,
- **Cuando** se despliega o ejecuta la aplicación,
- **Entonces** el correo y la contraseña se proporcionan mediante configuración privada del entorno y no se guardan en archivos versionados.
