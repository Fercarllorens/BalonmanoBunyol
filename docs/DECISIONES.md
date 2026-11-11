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

## Pendiente de decisión

- Proveedor de hosting para API, frontend, PostgreSQL y ficheros.
- Proveedores para correo, notificaciones push y entrega de imágenes.
- Matriz de roles, privacidad de cuentas menores y autorización/consentimiento.
- Integración de datos oficiales de federación, estadísticas y clasificaciones.
