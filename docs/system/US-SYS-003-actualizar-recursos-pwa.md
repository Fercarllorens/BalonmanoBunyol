# US-SYS-003 — Actualizar recursos PWA sin conservar estilos antiguos

**Como** persona usuaria de la aplicación, **quiero** que el navegador obtenga las versiones nuevas de los estilos y scripts al actualizar la web, **para** no seguir viendo recursos obsoletos al regresar.

**Estado:** Implementada.

## Criterios Dado-Cuando-Entonces

- **Dado** que ejecuto la aplicación en desarrollo,
- **Cuando** se inicia el frontend,
- **Entonces** no queda un service worker local sirviendo bundles estáticos desde una caché antigua.

- **Dado** que se publica una nueva versión de la PWA,
- **Cuando** el navegador comprueba el service worker,
- **Entonces** solicita el script de worker sin reutilizar una copia de caché HTTP y activa el worker nuevo al instalarse.

- **Dado** que un recurso estático (CSS, JavaScript o fuente) está disponible en línea,
- **Cuando** la aplicación lo solicita,
- **Entonces** se utiliza la versión actual de la red y se actualiza su copia offline.

- **Dado** que un recurso estático no puede descargarse,
- **Cuando** la aplicación está sin conexión,
- **Entonces** puede utilizar la última copia disponible en caché.

- **Dado** que una respuesta pertenece a una API,
- **Cuando** se procesa con el service worker,
- **Entonces** no se almacena en la caché PWA.
