# US-AUTH-004 — Resolver solicitud de rol

**Como** administrador/a, **quiero** aprobar o rechazar solicitudes de Jugador y Cuerpo Técnico, **para** controlar la asignación de roles ampliados.

## Criterios de aceptación

### Aprobar una solicitud

- **Dado** que he iniciado sesión como Administrador y hay una solicitud Pendiente,
- **Cuando** la apruebo,
- **Entonces** el rol principal solicitado sustituye al anterior y la solicitud queda Aprobada con quién y cuándo la resolvió.

- **Dado** que tengo una solicitud pendiente y Administración me cambia manualmente al rol solicitado,
- **Cuando** se guarda el cambio de rol,
- **Entonces** la solicitud queda resuelta como Aprobada por ese Administrador y deja de aparecer entre las pendientes.
- **Y** si Administración me asigna manualmente un rol distinto al solicitado, la solicitud queda resuelta como Rechazada por ese Administrador y el rol asignado se conserva.

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

- **Dado** que dos Administradores intentan resolver a la vez la misma solicitud Pendiente,
- **Cuando** una decisión reserva Pendiente y confirma su transacción,
- **Entonces** solo esa decisión queda resuelta y auditada: si aprueba, cambia el rol; si rechaza, el rol se conserva.
- **Y** la otra decisión recibe conflicto y no cambia roles ni genera auditoría de resolución.
- **Y** una reserva revertida no determina qué decisión resulta ganadora.

### Acceso a la vista de solicitudes desde la barra

- **Dado** que he iniciado sesión como Administrador,
- **Cuando** selecciono «Solicitudes» en la barra superior,
- **Entonces** navego a `/admin/solicitudes` para revisar las solicitudes.

### Acceso a la vista de solicitudes desde la barra

- **Dado** que he iniciado sesión como Administrador,
- **Cuando** selecciono «Solicitudes» en la barra superior,
- **Entonces** navego a `/admin/solicitudes` para revisar las solicitudes.
