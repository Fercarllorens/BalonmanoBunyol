# US-ADMIN-001 — Gestionar solicitudes, roles y equipos

**Como** administrador/a del club, **quiero** resolver solicitudes, gestionar roles, crear equipos y asignarlos a entrenadores, **para** mantener actualizada la organización del club.

**Estado:** Implementada.

## Criterios de aceptación

### Acceso al panel

- **Dado** que no tengo el rol Administrador,
- **Cuando** intento acceder a `/admin` o a su API,
- **Entonces** la interfaz me devuelve al Feed y la API deniega el acceso.

### Solicitudes pendientes

- **Dado** que hay una solicitud pendiente de Jugador o Cuerpo Técnico,
- **Cuando** un Administrador la aprueba,
- **Entonces** se resuelve la solicitud y el rol solicitado sustituye al rol principal anterior.

- **Dado** que hay una solicitud pendiente,
- **Cuando** un Administrador la rechaza,
- **Entonces** la cuenta conserva su rol principal y la solicitud queda resuelta como rechazada.

### Equipos y entrenadores

- **Dado** que soy Administrador,
- **Cuando** creo un equipo indicando nombre y categoría,
- **Entonces** el equipo aparece en la lista de equipos.
- **Y** el equipo no queda vinculado a una temporada.

- **Dado** que creo o edito un equipo llamado «Senior A» o «Senior B»,
- **Cuando** selecciono su categoría,
- **Entonces** la categoría almacenada es «Senior».

- **Dado** que una persona tiene rol Cuerpo Técnico,
- **Cuando** guardo una selección de varios equipos activos,
- **Entonces** todos esos equipos quedan asignados a su cuenta.
- **Y** cada persona dispone de un desplegable compacto que muestra cuántos equipos tiene asignados y abre la lista con casillas, búsqueda y marcas actuales.
- **Y** puedo identificar claramente qué equipos ya están asignados y cuáles están inactivos.
- **Y** los equipos inactivos que ya están asignados permanecen visibles y se conservan al editar otras asignaciones, salvo que los retire explícitamente.
- **Y** si guardo la lista vacía, se retiran todas las asignaciones de la persona y la acción queda registrada en auditoría.

- **Dado** que una cuenta tiene rol Administrador,
- **Cuando** consulto el apartado de asignación de equipos o intento asignarle equipos por API,
- **Entonces** no aparece como asignable y la API rechaza la operación porque Administración ya dispone de acceso global.
- **Y** las asociaciones históricas de esa cuenta se conservan.

- **Dado** que cambia la composición de una plantilla,
- **Cuando** Administración asigna o retira un jugador desde el panel de equipos,
- **Entonces** la membresía queda actualizada bajo control administrativo.
- **Y** se pueden asignar cuentas activas con rol Jugador o Cuerpo Técnico sin modificar su rol principal.
- **Y** las cuentas Administrador no se ofrecen como jugadores de plantilla.

- **Dado** que una plantilla se prepara para una nueva temporada,
- **Cuando** Administración edita la composición de sus equipos,
- **Entonces** la identidad del equipo permanece y solo cambian las asignaciones de jugadores indicadas explícitamente.

- **Dado** que una cuenta no tiene rol Cuerpo Técnico ni Administrador,
- **Cuando** intento asignarle equipos mediante la API,
- **Entonces** la operación es rechazada.

- **Dado** que un equipo tiene personas asignadas,
- **Cuando** lo desactivo,
- **Entonces** se conserva la asignación histórica y deja de ofrecerse para nuevas asignaciones.

### Búsqueda y cambios de rol

- **Dado** que soy Administrador,
- **Cuando** busco una persona por nombre o correo y cambio su rol principal,
- **Entonces** los roles principales anteriores se sustituyen por el nuevo.
- **Y** cualquier solicitud de rol pendiente de esa cuenta se resuelve en la misma operación: Aprobada si coincide con el rol asignado y Rechazada si es distinto.
- **Y** la solicitud resuelta deja de aparecer en la lista de pendientes y la resolución manual queda auditada.
- **Y** la transición desde Pendiente se reserva atómicamente antes de modificar roles; si otra decisión ya la reservó, esta operación recibe conflicto y no cambia roles ni añade auditoría.

- **Dado** que intento cambiar mi propio rol Administrador o retirar el rol al último Administrador activo,
- **Cuando** envío el cambio,
- **Entonces** la API lo rechaza.
- **Y** los cambios concurrentes de rol se serializan para impedir que se retiren a la vez los dos últimos Administradores.

- **Dado** que una cuenta tiene rol Administrador,
- **Cuando** otro Administrador modifica su rol,
- **Entonces** el cambio queda registrado en auditoría.
