# US-PROFILE-001 — Personalizar perfil

**Como** persona autenticada, **quiero** modificar mi información, avatar y equipo preferido, **para** personalizar mi cuenta en la aplicación del club.

**Estado:** Implementada.

## Criterios de aceptación

### Abrir la personalización desde la barra

- **Dado** que tengo una sesión activa,
- **Cuando** abro el menú de usuario de la barra superior y selecciono «Personalizar perfil»,
- **Entonces** accedo a `/perfil`.

### Protección de la pantalla

- **Dado** que no he iniciado sesión,
- **Cuando** intento acceder a `/perfil`,
- **Entonces** soy redirigido a iniciar sesión y la API de perfil no revela información.

### Consultar y modificar mis datos

- **Dado** que tengo una sesión activa,
- **Cuando** consulto mi perfil,
- **Entonces** puedo editar nombre, apellidos, correo electrónico y equipo preferido.

- **Dado** que envío datos válidos para mi perfil,
- **Cuando** guardo los cambios,
- **Entonces** se actualizan mis datos y la sesión refleja el nuevo nombre y correo.

- **Dado** que el nombre completo supera 160 caracteres,
- **Cuando** intento guardar el perfil,
- **Entonces** se me pide acortar los datos antes de guardar.

- **Dado** que el correo indicado ya pertenece a otra cuenta,
- **Cuando** intento guardar el perfil,
- **Entonces** la API rechaza el cambio sin modificar mi correo actual.

### Equipo preferido

- **Dado** que mi cuenta se registra y hay equipos Senior activos,
- **Cuando** se crea la cuenta,
- **Entonces** se preselecciona el primer equipo Senior ordenado por nombre.

- **Dado** que mi cuenta se registra y no hay equipos Senior activos,
- **Cuando** abro la personalización,
- **Entonces** la preferencia queda vacía y puedo escoger un equipo activo cuando esté disponible.

- **Dado** que selecciono un equipo activo y guardo el perfil,
- **Cuando** vuelvo a consultar mis datos,
- **Entonces** mi selección permanece guardada.

- **Dado** que el equipo preferido queda inactivo,
- **Cuando** consulto mi perfil,
- **Entonces** puedo conservar y ver esa preferencia, pero no seleccionarla de nuevo para otra cuenta.

### Avatar

- **Dado** que selecciono una imagen JPEG, PNG o WebP de 5 MB o menos,
- **Cuando** se carga correctamente,
- **Entonces** aparece como mi avatar en la aplicación.

- **Dado** que selecciono un archivo no admitido, demasiado grande o cuyo contenido no coincide con su formato,
- **Cuando** intento cargarlo,
- **Entonces** se rechaza y se informa del motivo.

- **Dado** que tengo un avatar,
- **Cuando** elijo eliminarlo,
- **Entonces** se retira de mi perfil y se elimina del almacenamiento.

### Seguridad de cambios

- **Dado** que envío una mutación de perfil sin un token antiforgery válido,
- **Cuando** la API la procesa,
- **Entonces** rechaza la solicitud.

- **Dado** que intento actualizar el perfil de otra cuenta,
- **Cuando** envío una petición indicando otra identidad,
- **Entonces** la API solo actúa sobre la cuenta asociada a mi sesión.
