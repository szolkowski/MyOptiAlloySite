# MyOptiAlloySite

A ready-to-run Optimizely CMS 13 Alloy site on .NET 10.0, created to provide a working project for testing Optimizely CMS 13 features and developing/testing Optimizely packages in a realistic, shippable setup. Includes Docker support for macOS and Windows.

## Getting started with Docker

Prerequisites: Docker

```bash
cd MyOptiAlloySite
docker compose up --build
```

- Web app: http://localhost:5100
- SQL Server: localhost:6000 (user `sa`, password from `.env`)
- Environment variables are in `MyOptiAlloySite/.env`

> If switching from Windows/LocalDB to Docker, delete any `App_Data/*.mdf` and `App_Data/*.ldf` files first. These contain Windows-specific paths and are not compatible with the Linux-based SQL Server container.

### Named resources

Everything that outlives a single run has an explicit, stable name, so `docker` commands don't depend on generated prefixes:

| Resource | Name |
| -------- | ---- |
| Compose project | `myoptialloy` |
| Web container / image | `myoptialloy-web` / `myoptialloy-web:local` |
| DB container / image | `myoptialloy-db` / `myoptialloy-db:local` |
| SQL Server data volume | `myoptialloy-sqldata` |
| Network | `myoptialloy-net` |

So `docker logs -f myoptialloy-web` and `docker exec -it myoptialloy-db bash` work directly.

### Apple Silicon (arm64) Macs

SQL Server 2025 is published for `linux/amd64` only, so the `db` service is pinned to that platform and runs under Docker Desktop's Rosetta emulation. Expect the database container to start more slowly than on an x86 host. The `web` service uses the multi-arch .NET SDK image and runs natively on arm64.

## Docker Compose commands

All commands should be run from the `MyOptiAlloySite/` directory (where `docker-compose.yml` lives).

| Command | Description |
| ------- | ----------- |
| `docker compose build --no-cache web` | Rebuild only the web image from scratch |
| `docker compose up --build` | Build images and start all services |
| `docker compose up -d` | Start services in detached (background) mode |
| `docker compose down` | Stop and remove containers and networks (database is preserved) |
| `docker compose down -v` | Same, but also **deletes the `myoptialloy-sqldata` volume and the database** |
| `docker compose restart web` | Restart only the web container |
| `docker logs -f myoptialloy-web` | Follow the application log |

### Notes

- The `web` service mounts the project source as a volume (`.:/src`), so code changes are reflected without rebuilding. However, changes to NuGet packages or the Dockerfile require `--build --no-cache`.
- The database persists in the named volume `myoptialloy-sqldata` and survives `docker compose down`. To start from a clean database, run `docker compose down -v` — the `db` container recreates it and the site re-imports the sample content on next startup.
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

- A named volume mounted at `/var/opt/mssql/data` blocked SQL Server from bootstrapping its system databases (master, model, etc.): that path does not exist in the image, so Docker created it owned by `root` and the `mssql` user (uid/gid 10001) could not write to it.
- The volume is instead mounted one level up at `/var/opt/mssql`, which *does* exist in the image as `drwxrwx--- root:mssql`. Docker seeds an empty named volume from the image path including its ownership, so SQL Server can bootstrap normally and the data survives `docker compose down`.
- Made the `App_Data` bind mount read-only (`:ro`) since it only provides the `.episerverdata` import file.

### 5. Lowercase image names (`docker-compose.yml`)

The `image:` keys were `MyOptiAlloySite/db` and `MyOptiAlloySite/web`. Docker requires lowercase repository names, so these failed with `invalid reference format: repository name must be lowercase`. Renamed to `myoptialloy-db:local` and `myoptialloy-web:local`.

### 6. amd64 platform pin (`docker-compose.yml`, `Docker/db.dockerfile`)

The SQL Server 2025 image has no arm64 variant. With Docker Desktop's containerd image store enabled, pulling it on an Apple Silicon Mac needs an explicit platform, so the `db` service sets `platform: linux/amd64` and its Dockerfile pins `FROM --platform=linux/amd64`. Both are no-ops on x86 hosts.

### 4. Stale LocalDB files

If the site was previously run on Windows with LocalDB, the `App_Data/` directory may contain `.mdf`/`.ldf` files with internal references to Windows paths (e.g. `C:\Users\...\MSSQLLocalDB\empty.ldf`). These must be deleted before running with Docker.
