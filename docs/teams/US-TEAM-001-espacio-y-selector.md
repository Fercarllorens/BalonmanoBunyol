# US-TEAM-001 — Consultar el espacio y seleccionar equipo

**Como** jugador o miembro del cuerpo técnico, **quiero** abrir los equipos a los que pertenezco, **para** consultar la actividad de cada uno.

**Estado:** Implementada.

## Criterios Dado-Cuando-Entonces

- **Dado** que tengo una asignación a un equipo,
- **Cuando** abro `/equipo`,
- **Entonces** se muestra el espacio de ese equipo.

- **Dado** que pertenezco a varios equipos,
- **Cuando** abro `/equipo`,
- **Entonces** puedo seleccionar cuál consultar y el contenido corresponde al equipo elegido.
- **Y** el selector se abre al pulsarlo, marca el equipo actual y permite elegir otro con ratón, teclado o toque.

- **Dado** que solo pertenezco a un equipo,
- **Cuando** abro `/equipo`,
- **Entonces** se muestra directamente y no aparece selector.

- **Dado** que tengo rol Administrador,
- **Cuando** abro el selector,
- **Entonces** puedo elegir cualquier equipo activo, aunque no tenga asignación individual.

- **Dado** que no pertenezco a un equipo,
- **Cuando** abro `/equipo`,
- **Entonces** se me informa que aún no tengo equipos asignados.

- **Dado** que no pertenezco al equipo indicado,
- **Cuando** consulto su API directamente,
- **Entonces** la API no entrega la plantilla, la pizarra ni los datos del equipo.
