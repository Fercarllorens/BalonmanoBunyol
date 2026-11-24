# US-SYS-002 — Navegación y barra superior según sesión

**Como** usuario del club, **quiero** que la barra superior y la página inicial se adapten a mi estado de sesión, **para** acceder fácilmente a Feed, Login, Registro y las funciones de Administración que me corresponden.

**Estado:** Implementada.

## Criterios de aceptación

### Barra para una persona sin sesión

- **Dado** que no he iniciado sesión,
- **Cuando** consulto Landing, Feed o Auth,
- **Entonces** la barra superior ofrece Iniciar sesión, Registrarse y cambio de tema.

### Barra para una persona autenticada

- **Dado** que he iniciado sesión,
- **Cuando** consulto la barra superior,
- **Entonces** puedo acceder al Feed y cerrar sesión.
- **Y** puedo abrir el menú de usuario para acceder a «Personalizar perfil» y cerrar sesión.
- **Y** el menú conserva su presentación visual como botón de cuenta (avatar, nombre y control desplegable) en escritorio y móvil.

### Acceso administrativo a solicitudes

- **Dado** que tengo rol Administrador,
- **Cuando** consulto la barra superior,
- **Entonces** encuentro el acceso «Solicitudes» a `/admin/solicitudes`.

### Acceso a la ruta de solicitudes sin permisos

- **Dado** que no tengo sesión o no tengo rol Administrador,
- **Cuando** intento acceder a `/admin/solicitudes`,
- **Entonces** no puedo consultar ni gestionar las solicitudes y se me redirige a Auth o al Feed, según mi sesión.

### Página inicial con sesión activa

- **Dado** que tengo una sesión activa,
- **Cuando** abro `/`,
- **Entonces** se me redirige a `/feed`.

### Cerrar sesión desde cualquier vista

- **Dado** que tengo una sesión activa,
- **Cuando** selecciono «Cerrar sesión» en la barra superior,
- **Entonces** la sesión se invalida y se me redirige a la landing pública `/`.
