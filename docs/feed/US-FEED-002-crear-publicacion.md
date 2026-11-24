# US-FEED-002 — Crear publicación

**Como** usuario autenticado, **quiero** publicar texto, imágenes o vídeos, **para** compartir información con la comunidad del club.

## Criterios de aceptación

### Publicación con texto

- **Dado** que he iniciado sesión y he escrito un texto válido,
- **Cuando** publico,
- **Entonces** la publicación aparece en la primera posición de la feed con mi nombre y el momento de creación.

### Publicación con medios

- **Dado** que he iniciado sesión y he seleccionado medios permitidos,
- **Cuando** publico la entrada,
- **Entonces** los medios quedan asociados a la publicación y se muestran en ella.

### Restricciones de adjuntos

- **Dado** que intento adjuntar más de cuatro archivos, un formato no permitido, una imagen mayor de 10 MB o un vídeo mayor de 50 MB,
- **Cuando** intento publicar,
- **Entonces** la API rechaza la publicación y la interfaz comunica el límite aplicable.

### Contenido vacío

- **Dado** que no he escrito texto ni adjuntado medios,
- **Cuando** intento publicar,
- **Entonces** no se envía la solicitud y se me indica que añada contenido.

### Usuario sin sesión

- **Dado** que no he iniciado sesión,
- **Cuando** consulto la feed,
- **Entonces** puedo leer contenido público, pero no publicar.
