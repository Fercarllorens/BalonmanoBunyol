# US-TEAM-002 — Gestionar la plantilla

**Como** administrador o miembro del cuerpo técnico asignado, **quiero** añadir y retirar jugadores del equipo y completar dorsal y posición, **para** mantener su plantilla actualizada.

**Estado:** Implementada.

## Criterios Dado-Cuando-Entonces

- **Dado** que tengo rol Administrador,
- **Cuando** añado o retiro un jugador en Administración,
- **Entonces** la pertenencia al equipo se actualiza.

- **Dado** que soy miembro de Cuerpo Técnico asignado al equipo,
- **Cuando** añado o retiro un jugador desde `/equipo`,
- **Entonces** la plantilla de ese equipo se actualiza.

- **Dado** que no soy Administrador ni staff asignado a ese equipo,
- **Cuando** intento cambiar la plantilla por API,
- **Entonces** la operación es denegada.

- **Dado** que la cuenta activa tiene rol Jugador o Cuerpo Técnico,
- **Cuando** se le asigna al equipo con dorsal y posición,
- **Entonces** aparece una vez en la plantilla con esos datos.
- **Y** la pertenencia a la plantilla es independiente de las asignaciones como cuerpo técnico y conserva el rol principal.
- **Y** las cuentas Administrador no se ofrecen como jugadores en los selectores de plantilla.

- **Dado** que el cuerpo técnico busca un jugador,
- **Cuando** hay resultados,
- **Entonces** aparecen en un desplegable anclado al buscador sin mover dorsal, posición ni el botón de asignación.
- **Y** al seleccionar una cuenta el resultado se cierra y la cuenta seleccionada queda visible junto al buscador.
- **Y** puedo recorrerlos con flechas, seleccionar con Enter, cerrar con Escape o pulsar fuera.
- **Y** el formulario conserva su disposición mientras se buscan y seleccionan cuentas.
