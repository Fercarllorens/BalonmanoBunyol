# Decisiones técnicas

| ID | Decisión | Estado | Motivo |
|---|---|---|---|
| D-001 | Monolito modular con Clean Architecture | Acordada | Límites claros y operación sencilla para iniciar; permite crecer gradualmente. |
| D-002 | Backend ASP.NET Core .NET 10 | Acordada | Tecnología solicitada por el usuario. |
| D-003 | Frontend React + TypeScript + Vite | Acordada | Tecnología solicitada y scaffold moderno para aplicación web. |
| D-004 | PostgreSQL + EF Core/Npgsql | Acordada | Adecuado para relaciones entre cuentas, publicaciones, comentarios, equipos y eventos; PostgreSQL no tiene coste de licencia. El hosting puede tener coste. |
| D-005 | ASP.NET Core Identity con usuarios Guid y roles | Base preparada | Identidad estándar ASP.NET Core; la matriz de roles/permisos se concretará antes de habilitar los flujos. |
| D-006 | PWA con vite-plugin-pwa, sin caché runtime de API | Base preparada | Instalación Android/iOS y actualización de recursos, evitando persistir respuestas privadas en caché. |
| D-007 | Monorepo con backend, frontend y docs | Acordada | Facilita compartir contexto y coordinar cambios de API y UI. |
| D-008 | Feed ordenado por fecha e índices iniciales | Base preparada | Consultas por cronología y equipo, con cursor en API para paginación progresiva. |
| D-009 | Sesión web mediante cookie HttpOnly y antiforgery | Acordada/implementada | Evita exponer tokens de sesión al JavaScript del frontend. En producción se fuerza HTTPS. |
| D-010 | Alta de socio activa; altas de Jugador/Staff también crean cuenta Socio y solicitud separada | Acordada/implementada | Permite acceso básico inmediato y revisión administrativa independiente del rol ampliado. |
| D-011 | Correo y contraseña como credenciales iniciales; sin verificación de correo | Acordada/implementada | Simplifica el primer flujo. La contraseña requiere 12 caracteres, mayúscula, minúscula y símbolo; el dígito no es obligatorio. |
| D-012 | Cuenta Administrador inicial se aprovisiona desde variables de entorno usando Identity | Acordada/implementada | Se almacena hash Identity, sin guardar la contraseña en el repositorio. |
| D-013 | Primera fase de feed: CRUD mínimo de publicaciones con borrado lógico y medios externos | Acordada/implementada | Publicación/listado/borrado propios; comentarios y reacciones se dejan para fases posteriores. |
| D-014 | Adjuntos de feed: 4 máximo, JPEG/PNG/WebP 10 MB, MP4/WebM 50 MB | Acordada/implementada | Formatos, tamaño y firmas comprobados en servidor. |
| D-015 | Mi feed inicialmente es una pestaña mock sin personalización | Acordada/implementada | No hay todavía grafo de seguimiento ni membresía modelada para personalizar. |
| D-016 | El perfil personal guarda nombre, apellidos, correo, avatar y equipo preferido | Acordada/implementada | La preferencia apunta a un equipo concreto; el valor inicial es el primer equipo Senior activo ordenado por nombre y queda vacío si todavía no existe ninguno. Avatares fuera de PostgreSQL, validados en servidor. |
| D-017 | Roles principales excluyentes; CuerpoTecnico y Administrador pueden tener equipos múltiples | Acordada/implementada | La administración asigna roles principales, asocia varias personas/equipos y audita acciones; los cambios de rol Administrador requieren otro administrador. |
| D-018 | Los equipos no se vinculan a una temporada; Senior es una categoría única y Senior A/B son nombres de equipo | Acordada/implementada | Administración gestiona los traslados de jugadores entre plantillas al cambiar la temporada. |
| D-019 | La pertenencia como jugador a una plantilla es independiente del rol principal | Acordada/implementada | Cuentas activas de Jugador o Cuerpo Técnico pueden tener membresía deportiva; el rol de Cuerpo Técnico conserva sus permisos asignados y Administración no se ofrece como jugador. |
| D-020 | Temporada automática del club de julio a junio y calendario mensual | Acordada/implementada | La aplicación calcula el rango con la fecha actual. Los horarios semanales tienen fecha efectiva; cambios/cancelaciones puntuales son excepciones de una fecha. |
| D-021 | Los cambios de rol Administrador bloquean la fila compartida del rol antes de validar el último administrador | Acordada/implementada | Serializa los cambios concurrentes de roles con PostgreSQL `FOR UPDATE`, evitando que dos solicitudes retiren simultáneamente los últimos administradores. |
| D-022 | La resolución de solicitudes de rol reserva atómicamente la transición desde Pendiente | Acordada/implementada | Un `UPDATE` condicional dentro de la transacción permite un único ganador entre decisiones concurrentes; cambios de rol y auditoría se confirman juntos y las decisiones perdedoras reciben conflicto. |
| D-023 | Límites de cuerpo HTTP de cargas multimedia por endpoint | Acordada/implementada | Feed: 201 MiB para cuatro vídeos de hasta 50 MiB y overhead multipart. Tablón: 401 MiB para cuatro archivos de hasta 100 MiB y overhead. No se amplía globalmente Kestrel. |
| D-024 | Los cambios del perfil y correo se guardan en una transacción Identity compartida | Acordada/implementada | Se actualizan todos los campos del usuario en una sola escritura dentro de la transacción del `ClubDbContext`; la cookie se refresca únicamente tras confirmar, evitando correos parcialmente persistidos ante fallos de concurrencia. |

## Pendiente de decisión

- Proveedor de hosting para API, frontend, PostgreSQL y ficheros.
- Proveedores para correo, notificaciones push y entrega de imágenes.
- Matriz de roles, privacidad de cuentas menores y autorización/consentimiento.
- Integración de datos oficiales de federación, estadísticas y clasificaciones.
