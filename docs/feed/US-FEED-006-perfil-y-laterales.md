# US-FEED-006 — Perfil y módulos laterales de la feed

**Como** usuario, **quiero** ver mi información de perfil y contexto deportivo, **para** reconocer mi cuenta y orientarme en la actividad del club.

## Criterios de aceptación

### Datos reales de perfil

- **Dado** que he iniciado sesión,
- **Cuando** consulto mi panel de perfil,
- **Entonces** veo el nombre y correo de mi cuenta autenticada.

### Datos deportivos no conectados

- **Dado** que la jornada, clasificación, estadísticas y agenda todavía no están integradas,
- **Cuando** se muestran sus tarjetas laterales,
- **Entonces** se identifican como contenido de demostración y no se presentan como información confirmada.

### Diseño adaptable

- **Dado** que consulto la feed en una pantalla móvil,
- **Cuando** se adapta la disposición,
- **Entonces** el compositor y las publicaciones tienen prioridad y los módulos laterales pasan debajo o se reducen.
