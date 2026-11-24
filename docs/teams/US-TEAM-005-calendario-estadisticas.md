# US-TEAM-005 — Consultar calendario y estadísticas deportivas

**Como** miembro del equipo, **quiero** consultar el calendario mensual de la temporada y las estadísticas deportivas, **para** seguir la actividad y el rendimiento de la plantilla.

**Estado:** Implementada.

## Criterios Dado-Cuando-Entonces

- **Dado** que pertenezco al equipo,
- **Cuando** abro Calendario,
- **Entonces** veo un calendario mensual navegable desde julio hasta junio de la temporada actual, calculada automáticamente.
- **Y** las sesiones planificadas semanalmente, los partidos y los eventos aparecen en las fechas correspondientes.
- **Y** cada día muestra a primera vista la hora y el nombre de sus actividades, sin tener que seleccionarlo.
- **Y** cuando hay más de tres actividades, el día indica cuántas quedan fuera de la vista previa.

- **Dado** que soy staff asignado,
- **Cuando** añado o modifico un evento puntual,
- **Entonces** aparece para los miembros del equipo.
- **Y** puedo editarlo o cancelarlo desde el calendario.

- **Dado** que soy staff asignado,
- **Cuando** guardo una planificación semanal con fecha efectiva, días, horarios y lugar opcional,
- **Entonces** se generan las sesiones recurrentes hasta la siguiente planificación o el fin de temporada.
- **Y** las planificaciones anteriores conservan su vigencia hasta su fecha final.

- **Dado** que una sesión está generada por una planificación semanal,
- **Cuando** la modifico o cancelo desde el calendario,
- **Entonces** el cambio afecta solo a esa fecha y puedo restaurar la sesión planificada.

- **Dado** que soy staff asignado,
- **Cuando** actualizo partidos, minutos, goles o paradas de un jugador,
- **Entonces** sus estadísticas deportivas quedan actualizadas.

- **Dado** que consulto las estadísticas del equipo,
- **Cuando** se presenta la ficha individual,
- **Entonces** no se muestran datos médicos ni carga de salud.
