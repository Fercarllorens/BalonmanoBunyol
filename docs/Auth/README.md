# Auth — Autenticación y registro

## Alcance implementado

- Pantalla unificada Login/Registro en `/auth`.
- Login mediante correo y contraseña; el perfil proviene de la cuenta.
- Alta Socio/Afición con cuenta activa inmediatamente.
- Alta Jugador/Cuerpo Técnico con cuenta Socio/Afición activa y solicitud de rol separada.
- Revisión administrativa de solicitudes: aprobación añade el rol; rechazo conserva Socio/Afición.
- Barra superior compartida: enlaces de Login/Registro al salir de sesión, y Feed/Cerrar sesión al autenticarse; Administrador dispone de «Solicitudes».
- Tras iniciar sesión, la vista principal es `/feed`; al cerrar sesión se vuelve a `/`.
- Barra superior compartida: enlaces de Login/Registro al salir de sesión, y Feed/Cerrar sesión al autenticarse; Administrador dispone de «Solicitudes».
- Tras iniciar sesión, la vista principal es `/feed`; al cerrar sesión se vuelve a `/`.
- Sesión basada en cookie HttpOnly, antiforgery y endpoint para consultar la sesión.
- Cuenta inicial Administrador aprovisionable por variables de entorno; Identity almacena el hash de contraseña.

## Historias de usuario

- [US-AUTH-001 — Iniciar sesión](US-AUTH-001-iniciar-sesion.md)
- [US-AUTH-002 — Registrar Socio/Afición](US-AUTH-002-alta-socio-aficion.md)
- [US-AUTH-003 — Solicitar rol de Jugador o Cuerpo Técnico](US-AUTH-003-solicitar-rol.md)
- [US-AUTH-004 — Resolver solicitud de rol](US-AUTH-004-resolver-solicitud.md)
- [US-AUTH-005 — Mantener y cerrar sesión](US-AUTH-005-sesion-y-cierre.md)
- [US-AUTH-006 — Aprovisionar Administrador inicial](US-AUTH-006-administrador-inicial.md)
- La navegación de solicitudes desde la barra y su ruta `/admin/solicitudes` se especifica en US-AUTH-004.
- La navegación de solicitudes desde la barra y su ruta `/admin/solicitudes` se especifica en US-AUTH-004.

Los criterios de aceptación se escriben en español con formato **Dado – Cuando – Entonces**. Las historias de navegación de la landing permanecen en `docs/landing/`.

## Operación local

La API aplica migraciones y crea los roles al arrancar. Para crear el primer Administrador, define en el `.env` local `BootstrapAdmin__Email`, `BootstrapAdmin__Password` y opcionalmente `BootstrapAdmin__DisplayName`; `scripts/start-api.ps1` carga esos valores sin imprimirlos. No introduzcas credenciales reales en `.env.example`, archivos versionados ni documentación.

El origen frontend local accede a `/api` mediante proxy Vite. La API ofrece `GET /api/v1/auth/csrf`; todas las mutaciones de Auth y Administración requieren el token en `X-CSRF-TOKEN`.
