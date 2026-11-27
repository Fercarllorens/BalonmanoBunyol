# US-TEAM-004 — Compartir mensajes en la pizarra

**Como** miembro del cuerpo técnico asignado, **quiero** compartir instrucciones y adjuntos en la pizarra, **para** preparar al equipo.

**Estado:** Implementada.

## Criterios Dado-Cuando-Entonces

- **Dado** que soy staff asignado,
- **Cuando** publico texto o adjuntos válidos en la pizarra,
- **Entonces** los miembros del mismo equipo pueden consultar el mensaje.

- **Dado** que pertenezco al equipo como jugador,
- **Cuando** consulto la pizarra,
- **Entonces** puedo leer y descargar sus adjuntos, pero no publicar.
- **Y** las imágenes se previsualizan dentro del mensaje.
- **Y** los vídeos se reproducen dentro del mensaje con controles de reproducción.
- **Y** los documentos PDF se mantienen disponibles mediante un enlace de descarga o apertura.

- **Dado** que no pertenezco al equipo,
- **Cuando** intento leer un mensaje o archivo privado,
- **Entonces** el servidor deniega el acceso.

- **Dado** que subo un archivo no permitido, con firma incorrecta o por encima del límite,
- **Cuando** intento publicarlo,
- **Entonces** se rechaza la operación.

- **Dado** que adjunto hasta cuatro archivos permitidos de 100 MB cada uno,
- **Cuando** publico el mensaje,
- **Entonces** el servidor aplica el límite por archivo, el límite total de 300 MiB y la cuota global diaria compartida de subidas.

- **Dado** que soy Administración o staff asignado al equipo,
- **Cuando** elimino un mensaje de la pizarra,
- **Entonces** deja de ser visible y sus adjuntos se eliminan de R2 mediante la cola de limpieza reintentable.
