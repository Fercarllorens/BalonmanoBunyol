# Privacidad y seguridad

La aplicación manejará información de jugadores, posiblemente menores, e información de salud. Esos datos requieren un diseño de acceso y retención específico antes de habilitar funciones productivas.

## Criterios de implementación

- Denegar por defecto y autorizar por rol y ámbito (club/equipo/jugador), no solo por ocultación de controles en UI.
- Mantener las notas médicas y documentos de salud separados de publicaciones, feeds y respuestas públicas.
- Dar a fisioterapeutas acceso solo a jugadores atendidos y a la información imprescindible; registrar accesos y cambios.
- Evitar incluir datos médicos o privados en logs, telemetría, notificaciones push, service worker, almacenamiento offline o caché HTTP compartida.
- Revisar permisos de tutores/representantes, consentimiento, visibilidad pública y ciclo de vida de cuentas de menores antes del lanzamiento.
- Cifrar conexiones en tránsito y aplicar cifrado/controles de acceso del proveedor para copias y almacenamiento.
- Usar cookies seguras o una estrategia de tokens evaluada para el despliegue; proteger contra CSRF/XSS y aplicar límites de petición.
- Guardar secretos únicamente en el gestor de secretos o variables seguras del entorno.

Este documento recoge criterios de ingeniería, no sustituye la evaluación legal/de protección de datos que pueda requerir el club antes de tratar información real.
