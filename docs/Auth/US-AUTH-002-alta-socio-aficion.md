# US-AUTH-002 — Alta de Socio/Afición

**Como** persona aficionada, **quiero** crear una cuenta de Socio/Afición, **para** acceder inmediatamente a las funciones básicas de la aplicación.

## Criterios de aceptación

### Alta correcta

- **Dado** que no existe una cuenta con mi correo,
- **Cuando** envío nombre, apellidos, correo y una contraseña válida como Socio/Afición,
- **Entonces** se crea una cuenta activa con rol Socio/Afición y una sesión iniciada.

### Correo ya registrado

- **Dado** que ya existe una cuenta con mi correo,
- **Cuando** intento registrarme de nuevo,
- **Entonces** no se crea otra cuenta ni se revela información sensible de la cuenta existente.

### Contraseña que no cumple política

- **Dado** que la contraseña no cumple la política publicada en el formulario,
- **Cuando** envío el registro,
- **Entonces** la cuenta no se crea y recibo indicaciones para corregirla.
