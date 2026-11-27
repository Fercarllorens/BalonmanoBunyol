# US-FEED-003 — Borrar una publicación propia

**Como** usuario autenticado, **quiero** borrar mis publicaciones, **para** retirar contenido que ya no quiero compartir.

## Criterios de aceptación

### Borrado lógico propio

- **Dado** que soy la persona autora de una publicación activa,
- **Cuando** confirmo su borrado,
- **Entonces** se marca como eliminada y deja de aparecer en la feed general.
- **Y** sus adjuntos dejan de servirse y se encolan para su eliminación física de R2.

### No borrar publicaciones ajenas

- **Dado** que no soy la persona autora de una publicación,
- **Cuando** intento borrarla,
- **Entonces** la API deniega la operación y la publicación continúa visible.

### Eliminación idempotente

- **Dado** que la publicación ya se ha eliminado,
- **Cuando** se vuelve a solicitar su borrado,
- **Entonces** no vuelve a aparecer ni se alteran sus datos de autoría.
