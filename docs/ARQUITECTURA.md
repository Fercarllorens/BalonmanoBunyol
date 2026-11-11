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
  BalonmanoBunol.Api/            # HTTP, composición de dependencias y configuración
```

Dirección de dependencias: `Api -> Application`, `Api -> Infrastructure`, `Infrastructure -> Application`, `Infrastructure -> Domain`, `Application -> Domain`. Domain no depende de las demás capas.

Stack: ASP.NET Core .NET 10, EF Core 10, Npgsql y ASP.NET Core Identity con claves `Guid`. Configuración PostgreSQL vía `ConnectionStrings:ClubDatabase`.

Auth utiliza cookie `HttpOnly`, cookie antiforgery y endpoint de sesión. La migración inicial se aplica al arrancar la API. Roles iniciales: `SocioAficion`, `Jugador`, `CuerpoTecnico` y `Administrador`. Las solicitudes de rol se guardan aparte; el usuario obtiene Socio/Afición en el alta y solo tras aprobación se le asigna el rol ampliado.

Primeros elementos: health endpoint (`/api/health`), feed paginado con cursor de fecha (`/api/v1/feed`), entidades `Team` y `Post`, repositorio de posts y contexto PostgreSQL que incluye las tablas Identity.

## Frontend

React + TypeScript + Vite. La organización es por funcionalidad (`landing`, `auth`, `feed`, `system`, `teams`, `matches`, `events`, `administration`, `medical`), con componentes compartidos y capa cliente HTTP. La ruta `/` muestra la landing pública, `/feed` es pública y `/auth` contiene Login/Registro. Las llamadas API usan cookie de sesión y token antiforgery; Auth y datos privados quedan fuera de caché.

La landing `/` es la entrada pública; las cuentas con sesión activa se redirigen allí a `/feed`. La barra global cambia según sesión; la ruta `/admin/solicitudes` se expone a Administradores en frontend y permanece protegida por rol en API.

La landing `/` es la entrada pública; las cuentas con sesión activa se redirigen allí a `/feed`. La barra global cambia según sesión; la ruta `/admin/solicitudes` se expone a Administradores en frontend y permanece protegida por rol en API.

La PWA usa un manifiesto y service worker propios: se almacena el shell y recursos estáticos, mientras todas las rutas `/api/` quedan explícitamente fuera del service worker. No se cachean respuestas de API. iOS y Android requieren servir la aplicación por HTTPS en producción.

## Datos y despliegue

- PostgreSQL es la base relacional principal.
- Medios (imágenes/vídeos/documentos) se almacenarán en un servicio de objetos; la base de datos conservará metadatos y referencias.
- `docker-compose.yml` permite levantar una base local de desarrollo.
- Contraseñas/secretos de producción se inyectarán por configuración segura del entorno.
- Las migraciones EF Core deberán añadirse cuando se cierre el primer modelo persistente.
