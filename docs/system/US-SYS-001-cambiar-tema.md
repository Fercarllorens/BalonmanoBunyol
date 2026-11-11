# US-SYS-001 — Cambiar el tema de la web

**Como** usuario, **quiero** accionar el botón de cambio de tema, **para** cambiar el tema de la web.

**Estado:** Implementada en la landing y el feed público. La preferencia se guarda en el almacenamiento local del navegador.

## Criterios de aceptación

### Escenario: cambiar de claro a oscuro

- **Dado** que la aplicación está en tema claro,
- **Cuando** acciono el botón de cambio de tema,
- **Entonces** la aplicación cambia al tema oscuro y el control ofrece activar el tema claro.

### Escenario: cambiar de oscuro a claro

- **Dado** que la aplicación está en tema oscuro,
- **Cuando** acciono el botón de cambio de tema,
- **Entonces** la aplicación cambia al tema claro y el control ofrece activar el tema oscuro.

### Escenario: conservar preferencia al navegar o recargar

- **Dado** que he seleccionado un tema,
- **Cuando** navego entre la landing y el feed público o recargo la aplicación,
- **Entonces** se conserva el tema que seleccioné.
