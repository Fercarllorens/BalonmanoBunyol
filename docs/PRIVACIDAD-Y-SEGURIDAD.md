# Privacidad y seguridad

La aplicación manejará información de jugadores, posiblemente menores, e información de salud. Esos datos requieren un diseño de acceso y retención específico antes de habilitar funciones productivas.

## Criterios de implementación

- Denegar por defecto y autorizar por rol y ámbito (club/equipo/jugador), no solo por ocultación de controles en UI.
- Mantener las notas médicas y documentos de salud separados de publicaciones, feeds y respuestas públicas.
- No permitir incluir ni consultar documentos o información médica en publicaciones de la comunidad.
- Autorizar la eliminación de publicaciones en backend y solo para su autor; conservar borrado lógico y metadatos de autoría.
- Validar en servidor tamaño, contenido MIME y firma del archivo adjunto; usar nombres aleatorios de almacenamiento y servir los archivos desde rutas controladas.
- Proteger el acceso a todos los avatares, incluidos los mostrados junto a publicaciones públicas, con sesión autenticada; para visitantes mostrar iniciales. Validar JPEG, PNG y WebP por firma y limitar el tamaño a 5 MB. No incluirlos en caché PWA ni HTTP compartida.
- Proteger todas las operaciones del panel con rol Administrador en backend y antiforgery; auditar cambios de rol, decisiones de solicitudes y asignaciones de equipo.
- Serializar los cambios de rol Administrador mediante bloqueo transaccional compartido antes de comprobar que permanece al menos una cuenta administradora activa.
- Las plantillas, pizarra, partidos, asistencia, eventos y estadísticas son privados por equipo: comprobar membresía de Jugador o asignación activa del staff en backend. Los adjuntos de pizarra requieren la misma comprobación y firma/MIME/tamaño válidos.
- No permitir incluir ni consultar documentos o información médica en publicaciones de la comunidad.
- Autorizar la eliminación de publicaciones en backend y solo para su autor; conservar borrado lógico y metadatos de autoría.
- Validar en servidor tamaño, contenido MIME y firma del archivo adjunto; usar nombres aleatorios de almacenamiento y servir los archivos desde rutas controladas.
- Dar a fisioterapeutas acceso solo a jugadores atendidos y a la información imprescindible; registrar accesos y cambios.
- Evitar incluir datos médicos o privados en logs, telemetría, notificaciones push, service worker, almacenamiento offline o caché HTTP compartida.
- El feed de publicaciones no acepta contenido médico ni documentos de salud; los adjuntos se validan en servidor por formato, firma y tamaño, y se sirven desde almacenamiento controlado.
- Los medios del feed no se cachean en la PWA; las publicaciones se eliminan lógicamente y solo su autor puede solicitarlas borrar.
- Revisar permisos de tutores/representantes, consentimiento, visibilidad pública y ciclo de vida de cuentas de menores antes del lanzamiento.
- Cifrar conexiones en tránsito y aplicar cifrado/controles de acceso del proveedor para copias y almacenamiento.
- Usar cookies seguras o una estrategia de tokens evaluada para el despliegue; proteger contra CSRF/XSS y aplicar límites de petición.
- Guardar secretos únicamente en el gestor de secretos o variables seguras del entorno.

Este documento recoge criterios de ingeniería, no sustituye la evaluación legal/de protección de datos que pueda requerir el club antes de tratar información real.
