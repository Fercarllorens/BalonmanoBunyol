# Contexto de trabajo para agentes

Antes de realizar cambios, consulta el índice `docs/README.md` y los documentos que afecten a la tarea. Esta documentación es el contexto persistente del proyecto entre sesiones.

## Directrices

- Producto: plataforma del Club Balonmano Buñol; la interfaz y el contenido de producto están en español.
- Backend .NET 10 y React + TypeScript en frontend.
- Mantener Clean Architecture: Domain no depende de Application, Infrastructure ni Api; Application depende de Domain; Infrastructure implementa contratos de Application; Api es el punto de composición.
- PostgreSQL como base de datos principal mediante EF Core/Npgsql.
- Mantener el frontend instalable como PWA para Android/iOS; no cachear datos personales, médicos ni respuestas privadas de API.
- Consultar las vistas existentes en `stitch_club_balonmano_bu_ol_hub/` y sus `DESIGN.md` antes de cambiar el diseño.
- Proteger especialmente datos médicos y datos de menores: acceso mínimo necesario, permisos por rol/ámbito y auditoría en las funcionalidades correspondientes.
- No guardar secretos ni credenciales productivas en archivos versionados.
- Actualizar `docs/` cuando se acuerden decisiones o se implementen cambios relevantes de arquitectura o producto.
- Mantener historias de usuario y criterios Given-When-Then en español (Dado-Cuando-Entonces) dentro de `docs/<grupo-funcional>/`; crear y enlazar un apartado por cada nueva sección/grupo funcional y usar `docs/system/` para comportamiento transversal.

## Índice

- `docs/README.md`: índice y estado actual.
- `docs/PRODUCTO.md`: requisitos aportados por el usuario.
- `docs/ARQUITECTURA.md`: estructura y dependencias.
- `docs/DECISIONES.md`: decisiones registradas.
- `docs/DISEÑO.md`: sistema visual y enlaces a las vistas fuente.
- `docs/PRIVACIDAD-Y-SEGURIDAD.md`: criterios para áreas sensibles.
- `docs/ESTADO.md`: implementación y próximos hitos.
- `docs/landing/`: historias y criterios de aceptación de la landing.
- `docs/Auth/`: historias y criterios de autenticación y registro.
- `docs/system/`: historias y criterios de aceptación del sistema general.
