# US-TEAM-003 — Gestionar convocatoria y asistencia

**Como** miembro del cuerpo técnico asignado, **quiero** programar un partido y convocar jugadores, **para** organizar la jornada del equipo.

**Estado:** Implementada.

## Criterios Dado-Cuando-Entonces

- **Dado** que soy staff asignado al equipo,
- **Cuando** creo un partido y selecciono jugadores de su plantilla,
- **Entonces** se guarda el partido y una convocatoria con esos jugadores.
- **Y** las convocatorias de partidos futuros se mantienen visibles en el espacio del equipo aunque existan más de veinte partidos históricos activos.

- **Dado** que estoy convocado,
- **Cuando** confirmo o rechazo mi asistencia,
- **Entonces** mi respuesta queda registrada.

- **Dado** que consulto el calendario mensual de un mes pasado de la temporada,
- **Cuando** navego a ese mes,
- **Entonces** puedo seguir consultando los partidos históricos aunque no aparezcan en la sección de próximos partidos.

- **Dado** que no pertenezco a la plantilla del equipo,
- **Cuando** intento añadir mi cuenta a la convocatoria,
- **Entonces** la API rechaza la operación.
