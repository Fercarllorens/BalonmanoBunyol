# US-LND-003 — Acceder al feed público

**Como** usuario, **quiero** navegar a la feed sin iniciar sesión, **para** ver la información a la que se puede acceder sin iniciar sesión.

**Estado:** Implementada como vista pública de ejemplo en `/feed`.

## Criterios de aceptación

### Escenario: consultar el feed sin autenticación

- **Dado** que no he iniciado sesión y me encuentro en la landing,
- **Cuando** selecciono un acceso al feed público del club,
- **Entonces** navego a `/feed` y puedo consultar el contenido público sin ser redirigido a Login.

### Escenario: volver a la landing como usuario anónimo

- **Dado** que estoy consultando el feed público,
- **Y** no he iniciado sesión,
- **Cuando** navego a la raíz `/`,
- **Entonces** regreso a la landing.

### Escenario: navegación a la raíz con sesión activa

- **Dado** que estoy autenticado,
- **Cuando** navego a la raíz `/`,
- **Entonces** la aplicación me dirige al Feed de `/feed`.
