# Equipos — Espacio de trabajo

## Alcance implementado

- Ruta privada `/equipo` con selector de equipo únicamente cuando la cuenta tiene varias asignaciones.
- La lista de equipos disponibles se calcula desde las asignaciones del staff o la pertenencia como jugador. Administración puede consultar todos los equipos activos.
- Plantilla con nombre, avatar, dorsal y posición; pueden pertenecer a ella cuentas activas de Jugador o Cuerpo Técnico sin modificar su rol principal. Administración asigna desde `/admin`, el staff asignado puede añadir o retirar desde su equipo.
- Próximos partidos y convocatoria; cada jugador convocado puede confirmar o indicar que no está disponible.
- Pizarra privada con mensajes y archivos JPEG/PNG/WebP, MP4/WebM o PDF, validados en servidor; imágenes y vídeos se previsualizan en el mensaje y los documentos siguen disponibles como enlaces.
- Calendario mensual de la temporada automática julio-junio, con planificación recurrente semanal de entrenamientos, partidos y gestión de eventos puntuales.
- Estadísticas deportivas editables por staff: partidos, minutos, goles y paradas.
- No se muestran datos médicos ni carga de salud en este apartado.

## Fuentes de diseño

- [Equipo y convocatorias — claro](../../stitch_club_balonmano_bu_ol_hub/gesti_n_de_equipo_y_convocatorias_c.b._bu_ol/code.html)
- [Equipo y convocatorias — oscuro](../../stitch_club_balonmano_bu_ol_hub/gesti_n_de_equipo_y_convocatorias_modo_oscuro_c.b._bu_ol/code.html)

## Historias de usuario

- [US-TEAM-001 — Consultar el espacio de un equipo](US-TEAM-001-espacio-y-selector.md)
- [US-TEAM-002 — Gestionar la plantilla del equipo](US-TEAM-002-plantilla.md)
- [US-TEAM-003 — Gestionar convocatoria y asistencia](US-TEAM-003-convocatorias.md)
- [US-TEAM-004 — Compartir mensajes en la pizarra](US-TEAM-004-pizarra.md)
- [US-TEAM-005 — Consultar calendario y estadísticas](US-TEAM-005-calendario-estadisticas.md)
