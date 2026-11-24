# Organización del frontend por funcionalidades

Cada funcionalidad debe agrupar su interfaz, hooks, modelos y llamadas API en una carpeta propia:

```text
src/
  app/                 # composición global, rutas y proveedores
  components/          # componentes visuales compartidos
  features/
    landing/
    auth/
    profile/              # datos de cuenta, avatar y preferencias
    feed/                # publicaciones, filtros y widgets de feed
    system/
    teams/
    matches/
    events/
    administration/
    teams/                 # plantilla, convocatoria, pizarra, calendario y estadísticas por equipo
    medical/
  lib/                 # cliente HTTP y utilidades compartidas
```

Landing, feed y autenticación se encuentran en `src/features/<funcionalidad>/`. La composición de rutas y sesión se coordina desde `src/App.tsx`.
