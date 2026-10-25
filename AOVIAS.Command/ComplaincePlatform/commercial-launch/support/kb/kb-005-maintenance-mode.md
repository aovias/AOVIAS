# KB-005 — Maintenance Mode

## Behavior

When maintenance is active, Gateway short-circuits most traffic with **HTTP 503** and a Retry-After hint (`MaintenanceModeMiddleware`).

Activation methods:

1. Environment variable: `MAINTENANCE_MODE=true`  
2. Lock file: `Gateway/App_Data/maintenance.lock`  

WebApp may show `/maintenance` experience when APIs return maintenance responses.

## When to use

- Planned upgrades (`scripts/ops/upgrade.ps1`)  
- Database restore windows  
- Emergency containment  

## Enable (ops)

```powershell
# Option A — lock file
New-Item -ItemType File -Force -Path .\Gateway\App_Data\maintenance.lock

# Option B — env on the Gateway process / container
$env:MAINTENANCE_MODE = "true"
# restart Gateway so env is picked up if required by host model
```

## Disable

```powershell
Remove-Item -Force .\Gateway\App_Data\maintenance.lock -ErrorAction SilentlyContinue
# Ensure MAINTENANCE_MODE is not set / false, then restart if needed
```

Verify login and a simple `GET /api/v1/licenses/status` (authenticated) succeed.

## Customer communication

Use templates in [../customer-comms-templates.md](../customer-comms-templates.md). Update status page (see [../service-status-page-template.md](../service-status-page-template.md)).

## Escalate when

- Maintenance stuck on after lock removed  
- Exempt health endpoints unexpectedly blocked (middleware regression)  
- Upgrade failed mid-window — follow rollback: `scripts/ops/rollback.ps1`
