# US-AUTH-005 — Mantener y cerrar sesión

**Como** usuario autenticado, **quiero** mantener una sesión segura y poder cerrarla, **para** controlar el acceso a mi cuenta.

## Criterios de aceptación

### Protección de credenciales de sesión

- **Dado** que inicio sesión correctamente,
- **Cuando** la API crea mi sesión,
- **Entonces** la cookie es HttpOnly y el frontend no almacena un token de sesión en localStorage ni sessionStorage.

### Protección antiforgery

- **Dado** que envío una mutación de autenticación o administración,
- **Cuando** falta un token antiforgery válido,
- **Entonces** la API rechaza la solicitud.

La cookie antiforgery de desarrollo es necesaria para las solicitudes mutables; en producción las cookies de sesión y antiforgery se marcan Secure y la aplicación debe servirse por HTTPS.

### Consultar sesión actual

- **Dado** que tengo una cookie de sesión válida,
- **Cuando** consulto mi sesión,
- **Entonces** recibo los datos mínimos de mi cuenta y roles.

### Cerrar sesión

- **Dado** que tengo una sesión activa,
- **Cuando** selecciono «Cerrar sesión» desde la barra superior,
- **Entonces** se invalida la cookie y soy redirigido a la landing pública `/`.

### Navegación superior por estado de sesión

- **Dado** que no he iniciado sesión,
- **Cuando** consulto cualquier vista principal,
- **Entonces** la barra superior ofrece siempre accesos a Iniciar sesión y Registrarse.

- **Dado** que he iniciado sesión,
- **Cuando** consulto la barra superior,
- **Entonces** encuentro acceso a Feed y Cerrar sesión.

### Página inicial con sesión activa

- **Dado** que tengo una sesión activa,
- **Cuando** abro `/`,
- **Entonces** se me redirige a `/feed`.
