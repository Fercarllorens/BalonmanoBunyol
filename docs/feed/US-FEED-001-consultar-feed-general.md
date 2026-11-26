# US-FEED-001 — Consultar la feed general

**Como** usuario del club, **quiero** consultar las publicaciones de la comunidad, **para** mantenerme informado de su actividad.

## Criterios de aceptación

### Consultar publicaciones

- **Dado** que hay publicaciones activas,
- **Cuando** abro la feed general,
- **Entonces** se muestran en orden cronológico inverso con autor, fecha, texto y medios adjuntos.

### Feed sin resultados

- **Dado** que todavía no hay publicaciones,
- **Cuando** consulto la feed general,
- **Entonces** veo un estado vacío con una invitación a publicar si he iniciado sesión.

### Paginación

- **Dado** que hay más publicaciones que las mostradas,
- **Cuando** solicito más contenido,
- **Entonces** se carga la siguiente tanda sin repetir publicaciones ya vistas.

### Acceso público

- **Dado** que no he iniciado sesión,
- **Cuando** abro `/feed`,
- **Entonces** puedo consultar las publicaciones activas sin controles de publicación o borrado.
- **Y** las publicaciones muestran la inicial del autor sin sesión; los avatares solo se descargan con sesión activa.
