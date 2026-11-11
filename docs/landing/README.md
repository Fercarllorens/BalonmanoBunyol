# Landing

## Objetivo

Página pública principal del Club Balonmano Buñol, implementada a partir de las referencias visuales clara y oscura. Presenta el club, su historia, galería, categorías deportivas, colaboradores y un partido destacado.

## Navegación

- La raíz `/` muestra la landing pública cuando no hay sesión. Si el usuario está autenticado, se redirige a la pantalla principal de la sesión en `/feed`.
- El feed de publicaciones públicas se consulta en `/feed` sin iniciar sesión.
- La barra superior mantiene Login/Registro visibles al salir de sesión y Feed/Cerrar sesión al autenticarse.

## Historias de usuario

- [US-LND-001 — Navegar a Login](US-LND-001-navegar-a-login.md) — acceso a la pantalla de autenticación; requisitos detallados en [Auth](../Auth/README.md).
- [US-LND-002 — Navegar a Registro](US-LND-002-navegar-a-registro.md) — acceso a la pestaña de registro; requisitos detallados en [Auth](../Auth/README.md).
- [US-LND-003 — Navegar al feed público](US-LND-003-acceder-al-feed-publico.md) — implementada. Para cuentas autenticadas, `/feed` es la vista inicial.

Todos los criterios usan el formato español **Dado – Cuando – Entonces**.
