# US-AUTH-003 — Solicitar rol de Jugador o Cuerpo Técnico

**Como** jugador/a o integrante del Cuerpo Técnico, **quiero** solicitar mi rol durante el registro, **para** poder usar primero las funciones básicas y obtener las ampliadas tras la validación del club.

## Criterios de aceptación

### Cuenta básica y solicitud pendiente

- **Dado** que no existe una cuenta con mi correo,
- **Cuando** me registro como Jugador o Cuerpo Técnico,
- **Entonces** se crea una cuenta activa Socio/Afición y una solicitud independiente con estado Pendiente.

### Acceso básico mientras se revisa

- **Dado** que mi solicitud está Pendiente,
- **Cuando** inicio sesión,
- **Entonces** puedo usar las funciones básicas de Socio/Afición, pero no las funciones exclusivas del rol solicitado.

### Rechazo de solicitud

- **Dado** que Administración rechaza mi solicitud,
- **Cuando** vuelvo a consultar mi cuenta,
- **Entonces** conservo el rol Socio/Afición y el acceso básico.
