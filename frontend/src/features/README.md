# Organización del frontend por funcionalidades

Cada funcionalidad debe agrupar su interfaz, hooks, modelos y llamadas API en una carpeta propia:

```text
src/
  app/                 # composición global, rutas y proveedores
  components/          # componentes visuales compartidos
  features/
    landing/
    auth/
    feed/
    system/
    teams/
    matches/
    events/
    administration/
    medical/
  lib/                 # cliente HTTP y utilidades compartidas
```

El prototipo inicial de inicio/feed está en `src/App.tsx`. Se extraerá a esas funcionalidades al comenzar la implementación de rutas y pantallas conectadas a la API.
