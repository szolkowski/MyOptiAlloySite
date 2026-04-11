# MyOptiAlloySite

A ready-to-run Optimizely CMS 13 Alloy site on .NET 10.0, created to provide a working project for testing Optimizely CMS 13 features and developing/testing Optimizely packages in a realistic, shippable setup. Includes Docker support for macOS and Windows.

## Getting started with Docker

Prerequisites: Docker

```bash
cd MyOptiAlloySite
docker compose up --build
```

- Web app: http://localhost:5100
- SQL Server: localhost:6000
- Environment variables are in `MyOptiAlloySite/.env`

> If switching from Windows/LocalDB to Docker, delete any `App_Data/*.mdf` and `App_Data/*.ldf` files first. These contain Windows-specific paths and are not compatible with the Linux-based SQL Server container.

## Docker Compose commands

All commands should be run from the `MyOptiAlloySite/` directory (where `docker-compose.yml` lives).

| Command | Description |
| ------- | ----------- |
| `docker compose up --build` | Build images and start all services |
| `docker compose up --build --no-cache` | Full rebuild ignoring Docker layer cache (use after changing NuGet packages or Dockerfile) |
| `docker compose up -d` | Start services in detached (background) mode |
| `docker compose down` | Stop and remove containers and networks |
| `docker compose restart web` | Restart only the web container |
| `docker compose build --no-cache web` | Rebuild only the web image from scratch |

### Notes

- The `web` service mounts the project source as a volume (`.:/src`), so code changes are reflected without rebuilding. However, changes to NuGet packages or the Dockerfile require `--build --no-cache`.
- The database is ephemeral — it lives inside the `db` container and is recreated on `docker compose up` if the container was removed. Use `docker compose stop` instead of `docker compose down` to preserve the database between sessions.
- The `db` service has a healthcheck, so the `web` container waits until the database is fully ready before starting.

## Changes made for macOS Docker support

### 1. Healthcheck and service dependency

The `web` container would attempt to connect before SQL Server had created the database, causing `Login failed for user 'sa'` errors.

- Added a `healthcheck` to the `db` service that waits until the application database exists.
- Changed `web.depends_on` to `condition: service_healthy` so it waits for the healthcheck to pass.

### 2. Database creation script (`Docker/create-db.sh`)

The original script specified explicit file paths for `.mdf`/`.ldf` files inside a host-mounted directory. On macOS, Docker bind mounts don't grant the `mssql` container user write access, resulting in `OS error 31 (A device attached to the system is not functioning)`.

- Simplified `CREATE DATABASE` to not specify file paths, letting SQL Server use its default internal data directory (`/var/opt/mssql/data/`) which the `mssql` user owns.
- Added the `-b` flag to `sqlcmd` so SQL errors return a non-zero exit code (previously failures were silently reported as success).

### 3. Volume mounts (`docker-compose.yml`)

- Removed the named volume mount at `/var/opt/mssql/data` which blocked SQL Server from bootstrapping its system databases (master, model, etc.) due to root ownership.
- Made the `App_Data` bind mount read-only (`:ro`) since it only provides the `.episerverdata` import file.

### 4. Stale LocalDB files

If the site was previously run on Windows with LocalDB, the `App_Data/` directory may contain `.mdf`/`.ldf` files with internal references to Windows paths (e.g. `C:\Users\...\MSSQLLocalDB\empty.ldf`). These must be deleted before running with Docker.
