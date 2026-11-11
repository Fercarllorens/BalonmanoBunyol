# Producto: Club Balonmano Buñol

## Visión

Aplicación web del Club Balonmano Buñol que combina comunidad deportiva, información pública del club y herramientas privadas de organización y gestión. Tras el despliegue debe poder instalarse como PWA en Android e iOS.

## Requisitos aportados por el usuario

- Feed de publicaciones con interacción social.
- Estadísticas deportivas, clasificaciones, partidos y resultados.
- Calendario de próximos eventos y partidos.
- Espacio por equipo/categoría gestionado por entrenadores y oficiales.
- Fichas y listado de jugadores de cada equipo.
- Calendario del equipo, partidos disputados y próximos eventos.
- Pizarra privada del equipo para directrices, jugadas e información de entrenadores a jugadores.
- Área de administración/gestión del club para jugadores, fichas, entrenadores y otras gestiones.
- Área para fisioterapeutas donde registrar el estado médico de jugadores revisados.
- Interfaz con tema claro y oscuro, responsive y coherente con los diseños de referencia existentes.
- Backend .NET 10, frontend React y Clean Architecture.
- Carpeta de conocimiento versionable para preservar requisitos, decisiones e implementación entre sesiones.

## Landing pública

- La raíz de la web presenta una landing oficial basada en las referencias clara y oscura existentes.
- La landing incluye identidad del club, partido destacado, historia y valores, galería, categorías, captación de jugadores y colaboradores.
- Login y Registro se mantienen visibles como «Próximamente» hasta su siguiente iteración.
- El feed de publicaciones públicas es navegable sin iniciar sesión.
- Los grupos funcionales mantienen sus historias y criterios Dado-Cuando-Entonces bajo su carpeta documental; el comportamiento transversal se documenta en `docs/system/`.

## Perfiles previstos

- Visitante/seguidor: contenido público, noticias, calendario, resultados y clasificaciones.
- Jugador/familia: espacios y publicaciones según pertenencia y permisos.
- Entrenador/oficial: administración operativa de sus equipos, convocatorias y pizarra.
- Fisioterapeuta: acceso limitado a información médica necesaria para la atención.
- Gestor/administrador del club: gestión global de la entidad y asignaciones.

Los permisos concretos, flujos de registro, visibilidad de publicaciones y consentimiento de representantes legales quedan por detallar antes de implementar esas funcionalidades.

## Autenticación y altas

- El Login usa correo y contraseña; el perfil de la cuenta es intrínseco y no se selecciona al iniciar sesión.
- Registro Socio/Afición crea una cuenta activa inmediatamente.
- Registro Jugador o Cuerpo Técnico crea cuenta básica Socio/Afición y una solicitud separada del rol ampliado.
- Si Administración aprueba, se asigna el rol solicitado; si rechaza, la cuenta conserva Socio/Afición.
- No se exige verificación de correo en esta iteración.
- La cuenta inicial de Administración se aprovisiona desde variables de entorno, mediante ASP.NET Identity y contraseña almacenada como hash.
