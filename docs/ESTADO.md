# Estado de implementación

## Scaffold realizado

- Solución .NET 10 con proyectos Domain, Application, Infrastructure y Api.
- Referencias entre capas según Clean Architecture.
- PostgreSQL local por Docker Compose, EF Core/Npgsql, Identity con `ClubUser` y contexto EF.
- Entidades `Team` y `Post`; feed general paginado, selección por comunicados oficiales, publicaciones con medios, entrega de adjuntos y borrado lógico solo por autor.
- Frontend React/TypeScript/Vite con landing oficial responsive, tema claro/oscuro persistente y feed público accesible sin iniciar sesión.
- Barra superior compartida según sesión, Feed como pantalla principal autenticada y ruta separada para solicitudes administrativas.
- Navegación con barra superior según sesión: landing pública para visitantes, Feed como destino principal tras autenticación y ruta administrativa para solicitudes.
- Pantalla unificada `/auth` con Login/Registro, alta básica automática y solicitudes de rol para Jugador/Cuerpo Técnico.
- Sesión por cookie HttpOnly, antiforgery, endpoints de sesión y panel administrativo inicial para aprobar/rechazar solicitudes.
- Migraciones EF de esquema inicial y de administración/equipo/feed/perfil; aprovisionamiento opcional de Administrador por variables de entorno.
- Historias funcionales organizadas por grupo en `docs/landing/`, `docs/Auth/`, `docs/feed/` y `docs/system/`, con criterios Dado-Cuando-Entonces.
- Configuración base de PWA con service worker de shell/estáticos; las rutas `/api/` se excluyen y no se cachean respuestas de API.
- Actualización PWA: en desarrollo se desregistran workers/cachés locales; en producción se comprueba la versión del worker sin caché HTTP y se sirven recursos estáticos primero desde red, con fallback offline.
- Personalización de perfil en `/perfil`: edición de nombre, apellidos, correo y equipo preferido, avatar con almacenamiento local y validación servidor; historias en `docs/profile/`.
- Panel de Administración en `/admin`: solicitudes, equipos sin temporada, categoría Senior unificada, control de plantillas y asignaciones múltiples a Cuerpo Técnico/Administradores, búsqueda de usuarios y cambio de rol principal con auditoría y protecciones de último administrador; historias en `docs/administration/`.
- Las plantillas admiten cuentas con rol principal Jugador o Cuerpo Técnico como membresías deportivas independientes, manteniendo los permisos del cuerpo técnico asignado.
- Espacio de Equipo en `/equipo`: selector de equipos de la cuenta, plantilla con dorsal/posición, partidos y convocatoria/asistencia, pizarra privada con archivos, calendario y estadísticas deportivas manuales; historias en `docs/teams/`.
- Calendario del equipo con temporada automática julio-junio, planificación semanal con fecha efectiva y excepciones de sesión por fecha; eventos puntuales editables/cancelables.
- Medios guardados en `App_Data/post-media` local; su contenido está ignorado por Git y no se cachea en el service worker.
- Relación EF Core publicación-adjuntos fijada explícitamente por `PostId`.
- Configuración de PostgreSQL local añadida en Docker Compose; Docker no está disponible en este entorno, pero se validó la migración usando el PostgreSQL accesible en `localhost:5432`.
- La cuenta inicial Administrador está preparada para aprovisionarse desde variables `BootstrapAdmin__Email` y `BootstrapAdmin__Password`; la API comprobó la cuenta durante el arranque en el PostgreSQL configurado.
- Infraestructura de pruebas añadida: suite .NET xUnit para dominio, Feed, persistencia/modelo y almacenamiento; integración API/PostgreSQL aislada por esquema; frontend Vitest/Testing Library para Auth, API cliente, Feed y administración.
- Verificación actual: solución compilada; 38 pruebas backend pasan con PostgreSQL configurado (37 unitarias y una integración API/PostgreSQL). Las 33 pruebas frontend pasan; build y lint frontend sin errores. Sin `BALONMANO_TEST_DATABASE`, se omite la integración.
- Migraciones EF `InitialAuthSchema`, `ClubManagementAndTeamSpace` y `RemoveTeamSeasonAndUseTeamPreference` aplicadas en PostgreSQL local. La última unifica Senior A/B como categoría Senior, elimina la temporada del equipo y transforma la preferencia del perfil en referencia opcional a equipo.
- `AGENTS.md` y esta carpeta de documentación persistente.
- Vistas de referencia existentes conservadas.

## Aún no implementado

- Verificación de correo, recuperación de contraseña, invitaciones, auditoría ampliada y herramientas administrativas completas.
- Comentarios, reacciones, compartir, moderación avanzada y notificaciones.
- Gestión ampliada de fichas de jugadores/entrenadores y herramientas de consulta/auditoría administrativa.
- Área médica.
- Seed de datos de club, ampliación de pruebas Auth para rutas adicionales y CI/CD.
- Sincronización de datos federativos y despliegue productivo.

## Próximos hitos sugeridos

1. Ampliar pruebas de autorización por equipo, Administración y flujos completos de convocatorias/pizarra.
2. Añadir comentarios, reacciones, compartir y controles avanzados de moderación.
3. Diseñar y validar controles reforzados antes de implementar información médica.
4. Completar instalación PWA, iconos de producción, pruebas en dispositivos y despliegue HTTPS.
