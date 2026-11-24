# Diseño visual

## Lenguaje visual identificado

- Nombre/identidad: Club Balonmano Buñol, estética deportiva actual y comunitaria.
- Color principal azul de equipación `#0077B6`; rojo deportivo `#D62828`; azul marino `#023E8A`.
- Tipografía de titulares Plus Jakarta Sans y de interfaz/datos Inter.
- Tema claro con superficies blancas y fondo `#F8FAFC`; tema oscuro basado en navy/graphite.
- Layout responsive, tablas legibles y cifras deportivas con `font-variant-numeric: tabular-nums`.

## Fuentes de diseño existentes

Se conservan los HTML originales en `stitch_club_balonmano_bu_ol_hub/`:

- Acceso y registro claro/oscuro.
- Feed social claro/oscuro.
- Gestión de equipo y convocatorias claro/oscuro.
- Administración del club clara y panel en tema oscuro.
- Landing oficial clara y oscura.
- Feed social y comunidad en modo claro y oscuro.
- Feed social y comunidad en modo claro y oscuro.

Especificaciones completas:

- `stitch_club_balonmano_bu_ol_hub/balonmano_bu_ol_athletic_matrix/DESIGN.md` (base clara y dual).
- `stitch_club_balonmano_bu_ol_hub/athletic_dark_inverted/DESIGN.md` (tema oscuro alternativo).
- `stitch_club_balonmano_bu_ol_hub/landing_page_oficial_club_balonmano_bu_ol/code.html` y `screen.png` (landing clara).
- `stitch_club_balonmano_bu_ol_hub/landing_page_oficial_modo_oscuro_club_balonmano_bu_ol/code.html` y `screen.png` (landing oscura).
- `stitch_club_balonmano_bu_ol_hub/acceso_y_registro_modo_claro_c.b._bu_ol/code.html` (acceso claro).
- `stitch_club_balonmano_bu_ol_hub/acceso_y_registro_modo_oscuro_c.b._bu_ol/code.html` (acceso oscuro).
- `stitch_club_balonmano_bu_ol_hub/feed_social_y_comunidad_c.b._bu_ol/code.html` (feed clara).
- `stitch_club_balonmano_bu_ol_hub/feed_social_y_comunidad_modo_oscuro_c.b._bu_ol/code.html` (feed oscura).
- `stitch_club_balonmano_bu_ol_hub/feed_social_y_comunidad_c.b._bu_ol/code.html` (feed clara).
- `stitch_club_balonmano_bu_ol_hub/feed_social_y_comunidad_modo_oscuro_c.b._bu_ol/code.html` (feed oscura).

Antes de extraer componentes React de las vistas, unificar las diferencias cromáticas entre ambos documentos: la especificación oscura propuesta usa rojo como primary, mientras la identidad clara define azul como primary. La base actual del frontend conserva azul principal y rojo de acento.
