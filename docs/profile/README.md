# Perfil — Personalización de cuenta

La funcionalidad permite a una persona autenticada gestionar sus datos de cuenta, foto de avatar y equipo preferido.

## Alcance implementado

- Menú de usuario en la barra superior con acceso a «Personalizar perfil» y «Cerrar sesión».
- Pantalla protegida en `/perfil` para modificar nombre, apellidos, correo y equipo preferido.
- La preferencia apunta a un equipo activo concreto; si no hay equipos Senior, puede quedar sin preferencia.
- Al registrarse, se asigna por defecto el primer equipo Senior activo, ordenado por nombre.
- Carga y eliminación de avatar JPEG, PNG o WebP de hasta 5 MB.
- Datos guardados mediante Identity y avatar en almacenamiento local del servidor, fuera de PostgreSQL.
- Las API de perfil identifican la cuenta desde la cookie de sesión y no cachean sus respuestas.

## Historias de usuario

- [US-PROFILE-001 — Personalizar perfil](US-PROFILE-001-personalizar-perfil.md)
