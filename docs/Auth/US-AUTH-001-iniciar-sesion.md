# US-AUTH-001 — Iniciar sesión

**Como** usuario registrado, **quiero** iniciar sesión con correo y contraseña, **para** acceder a las funciones permitidas para mi cuenta.

## Criterios de aceptación

### Credenciales válidas

- **Dado** que tengo una cuenta activa,
- **Cuando** envío mi correo y contraseña correctos,
- **Entonces** se crea una sesión segura y puedo continuar como el perfil asociado a mi cuenta.
- **Y entonces** la aplicación me lleva directamente al Feed en `/feed`.
- **Y entonces** la aplicación me lleva directamente al Feed en `/feed`.

### Credenciales no válidas

- **Dado** que el correo o la contraseña no son correctos,
- **Cuando** intento iniciar sesión,
- **Entonces** recibo un mensaje genérico y no se crea una sesión.

### Cuenta bloqueada o inactiva

- **Dado** que mi cuenta está bloqueada o inactiva,
- **Cuando** intento iniciar sesión,
- **Entonces** no obtengo una sesión autenticada.

### No se elige perfil en Login

- **Dado** que estoy en la pantalla Login,
- **Cuando** introduzco mis credenciales,
- **Entonces** se utilizan los roles asignados a mi cuenta y no se muestra selector de perfil.

### Usuario autenticado entra en la landing o Auth

- **Dado** que ya tengo una sesión activa,
- **Cuando** accedo a `/` o a `/auth`,
- **Entonces** se me dirige directamente a `/feed`.

### Usuario autenticado entra en la landing o Auth

- **Dado** que ya tengo una sesión activa,
- **Cuando** accedo a `/` o a `/auth`,
- **Entonces** se me dirige directamente a `/feed`.
