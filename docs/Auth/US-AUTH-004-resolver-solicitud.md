# US-AUTH-004 — Resolver solicitud de rol

**Como** administrador/a, **quiero** aprobar o rechazar solicitudes de Jugador y Cuerpo Técnico, **para** controlar la asignación de roles ampliados.

## Criterios de aceptación

### Aprobar una solicitud

- **Dado** que he iniciado sesión como Administrador y hay una solicitud Pendiente,
- **Cuando** la apruebo,
- **Entonces** el rol principal solicitado sustituye al anterior y la solicitud queda Aprobada con quién y cuándo la resolvió.

### Rechazar una solicitud

- **Dado** que he iniciado sesión como Administrador y hay una solicitud Pendiente,
- **Cuando** la rechazo,
- **Entonces** la solicitud queda Rechazada con quién y cuándo la resolvió y la cuenta conserva Socio/Afición.

### Usuario sin permisos administrativos

- **Dado** que he iniciado sesión sin rol Administrador,
- **Cuando** intento consultar o resolver solicitudes,
- **Entonces** la API deniega la operación.

### Solicitud ya resuelta

- **Dado** que la solicitud ya está Aprobada o Rechazada,
- **Cuando** intento resolverla de nuevo,
- **Entonces** la API rechaza el cambio y conserva la resolución anterior.

### Acceso a la vista de solicitudes desde la barra

- **Dado** que he iniciado sesión como Administrador,
- **Cuando** selecciono «Solicitudes» en la barra superior,
- **Entonces** navego a `/admin/solicitudes` para revisar las solicitudes.

### Acceso a la vista de solicitudes desde la barra

- **Dado** que he iniciado sesión como Administrador,
- **Cuando** selecciono «Solicitudes» en la barra superior,
- **Entonces** navego a `/admin/solicitudes` para revisar las solicitudes.
