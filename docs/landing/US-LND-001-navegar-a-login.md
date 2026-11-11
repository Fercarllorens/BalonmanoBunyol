# US-LND-001 — Navegar a Login

**Como** usuario registrado, **quiero** navegar a la vista de login, **para** poder iniciar sesión.

**Estado:** Implementado como acceso a `/auth?tab=login`. Los criterios funcionales de autenticación se especifican en `docs/Auth/`.

## Criterios de aceptación

### Escenario: navegar a Login

- **Dado** que estoy en la landing,
- **Cuando** selecciono el acceso «Iniciar sesión»,
- **Entonces** navego a `/auth` con la pestaña de Login seleccionada.
