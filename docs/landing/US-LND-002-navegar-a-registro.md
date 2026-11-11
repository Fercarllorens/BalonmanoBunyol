# US-LND-002 — Navegar a Registro

**Como** usuario no registrado, **quiero** navegar a la vista de registro, **para** poder registrarme.

**Estado:** Implementado como acceso a `/auth?tab=register`. Los criterios funcionales de registro se especifican en `docs/Auth/`.

## Criterios de aceptación

### Escenario: navegar a Registro

- **Dado** que estoy en la landing,
- **Cuando** selecciono el acceso «Registrarme»,
- **Entonces** navego a `/auth` con la pestaña de Registro seleccionada.
