# Estado de implementación

## Scaffold realizado

- Solución .NET 10 con proyectos Domain, Application, Infrastructure y Api.
- Referencias entre capas según Clean Architecture.
- PostgreSQL local por Docker Compose, EF Core/Npgsql, Identity con `ClubUser` y contexto EF.
- Entidades iniciales `Team` y `Post`, repositorio/caso de uso de lectura del feed y rutas health/API/feed.
- Frontend React/TypeScript/Vite con landing oficial responsive, tema claro/oscuro persistente y feed público accesible sin iniciar sesión.
- Barra superior compartida según sesión, Feed como pantalla principal autenticada y ruta separada para solicitudes administrativas.
- Navegación con barra superior según sesión: landing pública para visitantes, Feed como destino principal tras autenticación y ruta administrativa para solicitudes.
- Pantalla unificada `/auth` con Login/Registro, alta básica automática y solicitudes de rol para Jugador/Cuerpo Técnico.
- Sesión por cookie HttpOnly, antiforgery, endpoints de sesión y panel administrativo inicial para aprobar/rechazar solicitudes.
- Migración inicial de Identity, equipos, publicaciones y solicitudes de rol; aprovisionamiento opcional de Administrador por variables de entorno.
- Historias funcionales organizadas por grupo en `docs/landing/`, `docs/Auth/` y `docs/system/`, con criterios Dado-Cuando-Entonces.
- Configuración base de PWA con service worker de shell/estáticos; las rutas `/api/` se excluyen y no se cachean respuestas de API.
- Configuración de PostgreSQL local añadida en Docker Compose; el comando Docker no está disponible en el entorno actual, por lo que no se pudo iniciar el contenedor.
- La cuenta inicial Administrador está preparada para aprovisionarse desde variables `BootstrapAdmin__Email` y `BootstrapAdmin__Password`; no se ha creado en PostgreSQL porque no hay servidor disponible en este entorno.
- `AGENTS.md` y esta carpeta de documentación persistente.
- Vistas de referencia existentes conservadas.

## Aún no implementado

- Verificación de correo, recuperación de contraseña, invitaciones, auditoría ampliada y herramientas administrativas completas.
- Escritura/moderación de posts, comentarios, reacciones, carga de medios y notificaciones.
- Gestión de jugadores/fichas/entrenadores, convocatorias, partidos, eventos y estadísticas.
- Pizarra de equipo y área médica.
- Seed de datos de club, pruebas automatizadas de Auth y CI/CD.
- Sincronización de datos federativos y despliegue productivo.

## Próximos hitos sugeridos

1. Definir modelo de usuarios, equipos, jugadores, temporada y política de cuentas menores.
2. Completar pruebas automatizadas de Auth y configurar PostgreSQL/variables bootstrap locales.
3. Implementar autorización por ámbito/equipo y herramientas completas de gestión.
4. Conectar feed con API y crear flujo real de publicaciones y comentarios.
5. Añadir equipos/calendario y administración.
6. Diseñar y validar controles reforzados para información médica.
7. Completar instalación PWA, iconos de producción, pruebas en dispositivos y despliegue HTTPS.
