# Feed del club

## Objetivo y fases

Primera fase de la feed social inspirada en los diseños `feed_social_y_comunidad_*` claros y oscuros. El trabajo inicial cubre creación, consulta y borrado lógico de publicaciones de la feed general, con texto y medios adjuntos.

- Feed general real, cronológico y paginado.
- Publicar requiere sesión. El feed público se puede consultar sin autenticación.
- Adjuntos: máximo cuatro por publicación; JPEG/PNG/WebP hasta 10 MB por imagen y MP4/WebM hasta 50 MB por vídeo.
- Borrado lógico disponible solo para la persona autora; la API aplica la autorización.
- Filtro «Comunicados» para Administradores y Cuerpo Técnico.
- «Mi feed» se presenta como pestaña visual, pero no personaliza el contenido en esta fase.
- Perfil de la cuenta autenticada con nombre y correo reales.
- Las publicaciones muestran el avatar del perfil de su autor cuando existe; si no, usan su inicial.
- Partido, clasificación, estadísticas, agenda, patrocinadores y enlaces laterales son contenido mock de presentación.
- Comentarios, reacciones, compartir y seguimiento quedan para fases posteriores.

## Historias de usuario

- [US-FEED-001 — Consultar la feed general](US-FEED-001-consultar-feed-general.md)
- [US-FEED-002 — Crear publicación con adjuntos](US-FEED-002-crear-publicacion.md)
- [US-FEED-003 — Borrar una publicación propia](US-FEED-003-borrar-publicacion.md)
- [US-FEED-004 — Filtrar comunicados del club](US-FEED-004-filtrar-comunicados.md)
- [US-FEED-005 — Acceder a Mi feed](US-FEED-005-mi-feed.md)
- [US-FEED-006 — Consultar perfil y contexto lateral](US-FEED-006-perfil-y-laterales.md)

Todos los criterios están en español con formato **Dado – Cuando – Entonces**.

## Pruebas automatizadas

- Backend: `dotnet test BalonmanoBunyol.slnx` desde la raíz.
- Frontend: `npm --prefix frontend test` desde la raíz.
- `npm test` en la raíz ejecuta ambas suites y usa la configuración PostgreSQL de `.env` local para activar integración cuando está disponible.
- Para activar la integración ejecutando `dotnet test` directamente, establece `BALONMANO_TEST_DATABASE` con la cadena de conexión de una base dedicada. Se crea y elimina un esquema temporal aleatorio durante la prueba.
