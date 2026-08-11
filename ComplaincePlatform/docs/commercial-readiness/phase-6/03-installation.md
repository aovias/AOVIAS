# 03 — Installation

**Phase:** 6.3  
**Sprint:** 3  
**Date:** 2026-07-26

## Delivered

| Capability | Implementation |
|------------|----------------|
| Setup wizard APIs | `Gateway/Controllers/SetupWizardController` → `api/v1/setup/*` |
| Status | `GET status` — config flag, memory cache, or `App_Data/installation.lock` |
| DB test | `POST database/test` — opens SQL connection string |
| Configure | `POST configure` — writes `App_Data/aovias.installation.json` |
| Complete | `POST complete` — writes lock + health ping list |
| Lock-down | Mutating endpoints return `RESOURCE_CONFLICT` when completed |
| WebApp | `/setup` route (outside app shell); redirects to `/login` when done |

## APIs

- `GET /api/v1/setup/status`
- `POST /api/v1/setup/database/test`
- `POST /api/v1/setup/configure`
- `POST /api/v1/setup/complete`

Secrets are **not** written into source `appsettings*.json`. Runtime file is loaded optionally at Gateway startup when present.

## Scripts / deploy notes

### Windows (PowerShell)

```powershell
# Ensure App_Data is writable by the Gateway process identity
New-Item -ItemType Directory -Force -Path .\Gateway\App_Data | Out-Null
dotnet run --project .\Gateway\Gateway.csproj
# Open WebApp /setup, complete wizard, then restart Gateway to pick up aovias.installation.json
```

### Linux

```bash
mkdir -p Gateway/App_Data
chmod 750 Gateway/App_Data
dotnet run --project Gateway/Gateway.csproj
# After wizard: systemctl restart aovias-gateway (or docker compose restart gateway)
```

### Docker

- Mount a volume for `/app/App_Data` so `installation.lock` and `aovias.installation.json` survive container recreation.
- Example compose fragment:

```yaml
services:
  gateway:
    volumes:
      - aovias-appdata:/app/App_Data
volumes:
  aovias-appdata:
```

- Run the WebApp `/setup` wizard once against the Gateway, then restart the Gateway service so configuration overlays load.

## Exit

Operator can install a fresh AOVIAS Gateway instance via wizard, lock further setup mutations, and proceed to login.
