# US-SYS-004 — Limitar y limpiar las subidas de archivos

**Como** responsable de la demo, **quiero** limitar el volumen diario de archivos y eliminar los objetos que ya no están vinculados a contenido, **para** contener el consumo de almacenamiento.

## Criterios Dado-Cuando-Entonces

- **Dado** que el total de archivos subidos hoy todavía cabe en la cuota configurada,
- **Cuando** una persona autorizada sube archivos al feed, pizarra o avatar,
- **Entonces** la API reserva atómicamente sus bytes en el contador global del día UTC antes de enviarlos al almacenamiento externo.

- **Dado** que una carga superaría la cuota global diaria,
- **Cuando** se intenta guardar,
- **Entonces** la API rechaza la carga con HTTP 429 y no escribe los archivos en R2.

- **Dado** que una carga ya ha iniciado el envío a almacenamiento,
- **Cuando** falla la operación posterior,
- **Entonces** los bytes reservados siguen contabilizados para evitar que reintentos o fallos de red permitan sobrepasar el límite de transferencia.

- **Dado** que se ha eliminado una publicación, mensaje de pizarra o avatar sustituido,
- **Cuando** se confirma el cambio en PostgreSQL,
- **Entonces** el archivo deja de ser consultable y su objeto R2 queda encolado para borrado con reintentos; borrar no devuelve cuota diaria.

- **Dado** que la cola de borrado contiene trabajos pendientes,
- **Cuando** la aplicación se reinicia o hay más de una instancia,
- **Entonces** los trabajos siguen persistidos y cada objeto se elimina de forma idempotente.
