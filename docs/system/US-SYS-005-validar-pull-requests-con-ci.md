# US-SYS-005 — Validar las pull requests con CI

**Como** persona que revisa cambios del repositorio, **quiero** que GitHub construya y pruebe backend y frontend en cada pull request, **para** detectar regresiones antes de integrarlas.

## Criterios Dado-Cuando-Entonces

- **Dado** que se abre o actualiza una pull request,
- **Cuando** GitHub Actions ejecuta el workflow de CI,
- **Entonces** restaura y compila la solución .NET, ejecuta las pruebas backend y ejecuta las pruebas, build y lint del frontend.

- **Dado** que la suite backend contiene pruebas de integración PostgreSQL,
- **Cuando** se ejecuta CI,
- **Entonces** dispone de un PostgreSQL efímero y ejecuta también dichas pruebas sin credenciales productivas.

- **Dado** que cualquier comprobación de build, lint o test falla,
- **Cuando** finaliza el workflow,
- **Entonces** la comprobación de CI queda fallida en la pull request.
