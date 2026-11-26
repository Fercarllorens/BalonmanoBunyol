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

## Despliegue demo gratuito (Render + Neon + R2)

El repositorio incluye un `Dockerfile` multi-stage y `render.yaml` para servir el build de la PWA y la API desde el mismo origen. En local, Vite genera `backend/src/BalonmanoBunol.Api/wwwroot` y `dotnet publish` incluye esos archivos. En Docker, se construye el frontend dentro del contexto de publicación y se copia al artefacto sin versionar el directorio generado. Las imágenes/adjuntos se guardan en Cloudflare R2 cuando `FileStorage__Provider=R2`; el valor predeterminado local conserva los archivos en disco para desarrollo.

1. Crea PostgreSQL en Neon y un bucket privado en Cloudflare R2 con una clave limitada a ese bucket.
2. Conecta el repositorio en Render mediante el Blueprint `render.yaml`; el servicio web es gratuito y puede dormir al estar inactivo. Configura los valores marcados `sync: false` en el panel de Render, sin guardarlos en Git.
3. Usa el endpoint S3 de la cuenta R2, el nombre del bucket y las claves de acceso R2. `R2__Endpoint` debe ser `https://<ACCOUNT_ID>.r2.cloudflarestorage.com`.
4. `BootstrapAdmin__Email` y `BootstrapAdmin__Password` crean una cuenta de demostración al iniciar. Usa datos inventados; no cargues datos reales del club ni de menores.
5. Espera al despliegue, abre `/api/health` y `/api/ready`, después `/` y ensaya inicio de sesión, carga de archivos y rutas PWA. Tras un periodo sin visitas Render Free puede tardar cerca de un minuto en despertar.

El Blueprint no crea una base de datos Render gratuita porque expira a los 30 días. Crea Neon Free aparte y configura su cadena PostgreSQL como `ConnectionStrings__ClubDatabase` en el panel de Render con este formato, usando los valores que proporciona Neon:

```text
Host=<HOST_DIRECTO_NEON>;Database=<DATABASE>;Username=<USERNAME>;Password=<PASSWORD>;SSL Mode=Require
```

Debe ser la conexión **directa** de Neon: en la consola abre **Connect**, desactiva **Connection pooling** y copia el host sin el sufijo `-pooler`. La aplicación rechaza hosts Neon que incluyan `-pooler` porque las migraciones usan un bloqueo de sesión PostgreSQL, que PgBouncer en modo transacción no conserva. Neon escala a cero al estar inactivo, por lo que la primera conexión tras una pausa puede tardar varios segundos.

Las cuotas gratuitas y disponibilidad dependen de los proveedores y pueden cambiar. Neon Free limita recursos y escala la base a cero; Render Free tiene sistema de archivos efímero, por lo que todos los medios deben ir a R2. El bucket R2 es privado; la API preserva el acceso público únicamente a los adjuntos del feed que ya se exponen como públicos. La demo no sustituye un despliegue productivo ni una estrategia de copias de seguridad.

Para un bucket R2 privado, crea un API token de Cloudflare con permisos **Object Read & Write** limitado al bucket demo. Copia el S3 API endpoint (`https://<ACCOUNT_ID>.r2.cloudflarestorage.com`), bucket, Access Key ID y Secret Access Key a las variables protegidas de Render. No habilites acceso público: la API transmite los objetos por los endpoints existentes, conservando el acceso público únicamente para adjuntos del feed que ya sean públicos.

Consulta [AGENTS.md](AGENTS.md) y [docs/README.md](docs/README.md) antes de continuar el desarrollo.
