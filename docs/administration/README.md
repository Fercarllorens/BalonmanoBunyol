# Administración del club

## Alcance implementado

- Panel `/admin` protegido por el rol Administrador en cliente y servidor.
- Revisión de solicitudes pendientes de Jugador y Cuerpo Técnico. Socio/Afición continúa activo al registrarse.
- Asignación de varios equipos activos a cuentas de Cuerpo Técnico. Administración tiene acceso global a todos los equipos activos y no necesita ni recibe asignaciones individuales.
- Selector de equipos agrupado por categoría, con búsqueda, casillas explícitas, chips de asignaciones actuales y equipos inactivos claramente identificados.
- Creación, edición y activación/desactivación de equipos por categoría, sin asociarlos a temporadas.
- Las categorías son Benjamín, Alevín, Infantil, Cadete, Juvenil y Senior; «Senior A» y «Senior B» son nombres posibles de equipos de categoría Senior.
- La plantilla se mantiene bajo control de Administración, que puede asignar, retirar y trasladar cuentas con rol Jugador o Cuerpo Técnico entre equipos, sin cambiar su rol principal.
- Búsqueda de usuarios por nombre o correo y edición de su rol principal.
- El cambio de rol sustituye los roles principales. Solo otro Administrador puede cambiar el rol Administrador; no puede autoasignárselo y debe quedar siempre un Administrador activo.
- Auditoría en PostgreSQL para aprobaciones/rechazos, cambios de rol, asignaciones y mantenimiento de equipos.

## Historias de usuario

- [US-ADMIN-001 — Gestionar solicitudes, roles y equipos](US-ADMIN-001-gestionar-club.md)
