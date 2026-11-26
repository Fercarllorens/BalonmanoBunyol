# Arquitectura

## Enfoque

Monolito modular con Clean Architecture. Proporciona límites claros y despliegue simple para la primera versión; los módulos pueden evolucionar de forma independiente sin introducir prematuramente microservicios.

## Backend

Solución `BalonmanoBunyol.slnx`:

```text
backend/src/
  BalonmanoBunol.Domain/         # Entidades y reglas de negocio sin dependencias externas
  BalonmanoBunol.Application/    # Casos de uso, DTOs y contratos
  BalonmanoBunol.Infrastructure/ # EF Core, PostgreSQL, Identity y adaptadores
  BalonmanoBunol.Api/            # Controladores HTTP por funcionalidad, middleware y composición
```

Dentro de cada proyecto las carpetas se organizan por funcionalidad cuando aplica (`Administration`, `Auth`, `Profile`, `Social`, `Teams`). En Api, `Controllers/` agrupa los controladores por esa misma funcionalidad; `Program.cs` conserva configuración, middleware e inicialización. En Infrastructure, `Persistence` contiene el contexto EF Core y las migraciones, mientras `Identity` conserva los tipos propios de ASP.NET Identity. No es obligatorio que todas las capas tengan todas las carpetas: cada capa contiene solo los elementos de la funcionalidad que le corresponden.

Dirección de dependencias: `Api -> Application`, `Api -> Infrastructure`, `Infrastructure -> Application`, `Infrastructure -> Domain`, `Application -> Domain`. Domain no depende de las demás capas.

Stack: ASP.NET Core .NET 10, EF Core 10, Npgsql y ASP.NET Core Identity con claves `Guid`. Configuración PostgreSQL vía `ConnectionStrings:ClubDatabase`.

Auth utiliza cookie `HttpOnly`, cookie antiforgery y endpoint de sesión. La migración inicial se aplica al arrancar la API. Roles iniciales: `SocioAficion`, `Jugador`, `CuerpoTecnico` y `Administrador`. Las solicitudes de rol se guardan aparte; el usuario obtiene Socio/Afición en el alta y solo tras aprobación se le asigna el rol ampliado.

Primeros elementos: health endpoint (`/api/health`), feed paginado por cursor, publicaciones de texto/medios y borrado lógico. Los medios se guardan mediante `IPostMediaStorage` fuera de PostgreSQL, en almacenamiento local de desarrollo; los metadatos y claves se guardan en la base. El endpoint de adjuntos solo devuelve medios de publicaciones activas. La relación publicaciones-adjuntos se configura explícitamente por `PostId` y la migración `FeedPostsV2` incorpora borrado lógico y tabla de adjuntos.

## Frontend

React + TypeScript + Vite. La organización es por funcionalidad (`landing`, `auth`, `feed`, `system`, `teams`, `matches`, `events`, `administration`, `medical`), con componentes compartidos y capa cliente HTTP. La ruta `/` muestra la landing pública, `/feed` es pública y `/auth` contiene Login/Registro. Las llamadas API usan cookie de sesión y token antiforgery; Auth y datos privados quedan fuera de caché.

La landing `/` es la entrada pública; las cuentas con sesión activa se redirigen allí a `/feed`. La barra global cambia según sesión; la ruta `/admin/solicitudes` se expone a Administradores en frontend y permanece protegida por rol en API.

La landing `/` es la entrada pública; las cuentas con sesión activa se redirigen allí a `/feed`. La barra global cambia según sesión; la ruta `/admin/solicitudes` se expone a Administradores en frontend y permanece protegida por rol en API.

La PWA usa un manifiesto y service worker propios: se almacena el shell y recursos estáticos, mientras todas las rutas `/api/`, `/auth` y `/feed` quedan explícitamente fuera del service worker. No se cachean respuestas de API ni el feed. iOS y Android requieren servir la aplicación por HTTPS en producción.

La funcionalidad de perfil almacena nombre, apellidos, equipo preferido opcional y clave de avatar en `ClubUser`. El equipo preferido referencia `teams`; al registrarse se asigna el primer equipo activo de categoría Senior ordenado por nombre, o queda vacío si no existe ninguno. Los avatares se guardan fuera de PostgreSQL, en `App_Data/profile-avatars` localmente, y se sirven mediante endpoint autenticado. Las mutaciones requieren antiforgery y las respuestas de perfil llevan `Cache-Control: no-store`.

El panel `/admin` requiere rol Administrador en rutas y endpoints. La gestión usa tablas `team_assignments` (múltiples equipos por miembro de Cuerpo Técnico/Administrador) y `administration_audit_entries`; las operaciones administrativas quedan auditadas. Los roles principales son excluyentes entre SocioAficion, Jugador, CuerpoTecnico y Administrador. La membresía deportiva `team_player_memberships` es independiente del rol principal y puede asociarse a cuentas activas de Jugador o Cuerpo Técnico; Administración queda excluida como jugador.

El espacio `/equipo` resuelve equipos desde `team_assignments` para Cuerpo Técnico y `team_player_memberships` para Jugadores; Administración puede consultar todos los equipos activos. Los equipos se identifican por nombre y categoría. La temporada del calendario se calcula automáticamente de julio a junio. Las planificaciones recurrentes se guardan en `team_weekly_training_plans`/`team_weekly_training_slots` con fecha efectiva, y `team_training_exceptions` conserva cambios o cancelaciones de una fecha concreta. Eventos puntuales y partidos/convocatorias se almacenan por separado. El servicio valida acceso por equipo en cada operación. La pizarra/adjuntos y estadísticas se almacenan en tablas separadas. Los medios, avatares y adjuntos de pizarra comparten contratos de almacenamiento independientes por colección: desarrollo usa filesystem local; el despliegue demo puede seleccionar el adaptador S3-compatible de Cloudflare R2 con `FileStorage:Provider=R2`. Los objetos R2 son privados y se leen mediante endpoints con autorización. No se reutiliza el feed público ni se incluyen datos médicos.

## Datos y despliegue

- PostgreSQL es la base relacional principal.
- Medios (imágenes/vídeos/documentos) se almacenarán fuera de PostgreSQL; desarrollo usa almacenamiento local y producción podrá sustituirlo por un servicio de objetos.
- Los avatares se guardan en `App_Data/profile-avatars` local; su formato, firma y tamaño máximo de 5 MB se validan en backend.
- `docker-compose.yml` permite levantar una base local de desarrollo.
- Contraseñas/secretos de producción se inyectarán por configuración segura del entorno.
- El despliegue demo de una sola URL usa Docker: Vite compila a `backend/src/BalonmanoBunol.Api/wwwroot`, ASP.NET Core sirve la PWA y la API, y `render.yaml` declara el web service Render Free. Las claves de Neon y R2 se inyectan como variables de entorno; no se versionan.
- `/api/health` confirma que el proceso responde y `/api/ready` comprueba además la conexión a PostgreSQL para los checks del proveedor.
- El arranque serializa las migraciones PostgreSQL con un advisory lock de sesión. En Render se respetan los encabezados `X-Forwarded-*` para TLS y cookies seguras.
- Las migraciones EF Core se generan con `dotnet-ef`; `FeedPostsV2` incorpora borrado lógico, tabla de adjuntos e índices de consulta.

## Pruebas

- Backend: `dotnet test BalonmanoBunyol.slnx`; la prueba de integración PostgreSQL se omite si no se establece `BALONMANO_TEST_DATABASE`. `npm test` en la raíz la configura desde `POSTGRES_DB`, `POSTGRES_USER` y `POSTGRES_PASSWORD` del `.env` local, si existe, y ejecuta el resto de pruebas. La prueba crea y elimina un esquema con nombre aleatorio.
- Frontend: `npm --prefix frontend test`; `npm --prefix frontend run test:watch` para ejecución interactiva.
- Antes de incorporar un cambio funcional, añadir pruebas unitarias, de componente o de integración según la capa afectada y ejecutar la suite relevante.
