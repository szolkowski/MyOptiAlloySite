# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

**Local (Windows with LocalDB):**
```bash
dotnet run --project MyOptiAlloySite
```

**Docker (any OS):**
```bash
cd MyOptiAlloySite
docker compose up --build
```
- Web app: http://localhost:5100
- SQL Server: localhost:6000
- Environment variables are in `MyOptiAlloySite/.env`
- If switching from Windows/LocalDB to Docker, delete `App_Data/*.mdf` and `App_Data/*.ldf` first

**SCSS compilation** is handled via `compilerconfig.json` (source in `Assets/scss/`, output to `wwwroot/css/`).

There are no test projects in this solution.

## Architecture

This is an **Optimizely CMS 13** (EPiServer) site built on .NET 10.0 using the Alloy MVC template.

### Content Model (`Models/`)

All content types live under `Models/`:
- **Pages** inherit from `SitePageData` (base providing meta, media, layout properties)
- **Blocks** inherit from `SiteBlockData`
- **ViewModels** are in `Models/ViewModels/`

Content routing is handled by Optimizely's `MapContent()` — there are no traditional route registrations.

### Controllers & Rendering

- `PageControllerBase<T>` is the abstract base for all page controllers (generic over `SitePageData`)
- View Components in `Components/` handle block rendering (e.g., `PageListBlockViewComponent`, `ContactBlockViewComponent`)
- `AlloyContentAreaItemRenderer` customizes how content area items render with Bootstrap-based display options (full/wide/half/narrow)
- `TemplateCoordinator` and `SiteViewEngineLocationExpander` control template selection and view lookup paths

### Service Registration

`Extensions/ServiceCollectionExtensions.AddAlloy()` registers all Alloy-specific services: display options, display resolutions, MVC filters, and view engine customization. Called from `Startup.cs`.

### Key Files

- `Globals.cs` — constants for group names, content area tags, display widths, and static paths
- `Startup.cs` — middleware pipeline and service configuration
- `Business/Rendering/` — custom content rendering pipeline
- `Business/Channels/` — multi-device display resolution definitions

### Docker Setup

- `docker-compose.yml` with two services: `db` (SQL Server 2025 Linux) and `web`
- `Docker/create-db.sh` — database initialization script with retry loop
- `Docker/web.dockerfile` — .NET SDK image with `dotnet run`
- `Docker/db.dockerfile` — SQL Server with custom entrypoint
- `Directory.Build.props` separates `bin`/`obj` output for host vs container builds to avoid conflicts
