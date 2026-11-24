# US-FEED-004 — Filtrar comunicados del club

**Como** usuario de la aplicación, **quiero** filtrar publicaciones para ver comunicados de Administración y Cuerpo Técnico, **para** localizar información oficial del club.

## Criterios de aceptación

### Aplicar filtro

- **Dado** que la feed contiene publicaciones de varios perfiles,
- **Cuando** selecciono «Comunicados»,
- **Entonces** solo se muestran publicaciones cuyos autores tienen rol Administrador o Cuerpo Técnico.

### Quitar filtro

- **Dado** que está activo el filtro «Comunicados»,
- **Cuando** selecciono «Todas las publicaciones»,
- **Entonces** se muestran de nuevo todas las publicaciones activas.

El filtro se basa en los roles persistidos del autor, no en texto de perfil aportado por el cliente.
