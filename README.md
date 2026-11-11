# Club Balonmano Buñol

Aplicación web y PWA para la comunidad, actividad deportiva y gestión del Club Balonmano Buñol.

## Estructura

- `backend/`: API ASP.NET Core .NET 10 con Clean Architecture.
- `frontend/`: React + TypeScript + Vite, preparada como PWA instalable.
- `docs/`: requisitos, arquitectura y memoria persistente del proyecto.
- `stitch_club_balonmano_bu_ol_hub/`: vistas HTML y especificaciones visuales de referencia, conservadas.
- `docker-compose.yml`: PostgreSQL local para desarrollo.

## Arranque local

1. Si aún no tienes configuración local, copia `.env.example` a `.env` y sustituye los valores de ejemplo por los que hayas elegido. El archivo `.env` está excluido de Git.
2. Desde la raíz del repositorio, iniciar PostgreSQL: `npm run db:up`.
3. En otra terminal desde la raíz, iniciar la API: `npm run api`.
4. En una tercera terminal desde la raíz, iniciar el frontend: `npm run web`.
5. Abrir `http://localhost:5173/`; Login y Registro están en `http://localhost:5173/auth`.

Consultar el estado de PostgreSQL con `docker compose ps postgres` y sus mensajes con `npm run db:logs`. Detenerlo con `npm run db:down` (los datos persisten en el volumen local `postgres_data`).

La API arranca en `http://localhost:5210` según `launchSettings.json`; Vite reenvía `/api` a ese puerto. El script carga las variables de `.env` sin mostrarlas en consola y construye `ConnectionStrings__ClubDatabase` usando las variables de PostgreSQL. La API no tiene una contraseña predeterminada en `appsettings.json`: si falta la conexión configurada, falla al arrancar con un mensaje claro.

El `.env` también proporciona `BootstrapAdmin__Email`, `BootstrapAdmin__Password` y, opcionalmente, `BootstrapAdmin__DisplayName`. Al arrancar con PostgreSQL disponible, la API aplica las migraciones y aprovisiona los roles y la cuenta Administrador mediante ASP.NET Identity; la contraseña se almacena como hash. No añadir credenciales reales a `.env.example`, documentación ni archivos versionados.

Los comandos npm de la raíz invocan el script PowerShell con la política de ejecución limitada al proceso; no hace falta cambiar la política global del equipo.

## Comprobaciones

- Backend: `dotnet build BalonmanoBunyol.slnx`
- Frontend: `cd frontend; npm run build`
- Lint frontend: `cd frontend; npm run lint`

Consulta [AGENTS.md](AGENTS.md) y [docs/README.md](docs/README.md) antes de continuar el desarrollo.
