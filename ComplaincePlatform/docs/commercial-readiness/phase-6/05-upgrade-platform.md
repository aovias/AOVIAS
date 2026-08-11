# 05 — Upgrade Platform

**Phase:** 6.5  
**Sprint:** 4  
**Date:** 2026-07-26

## Delivered

| Capability | Implementation |
|------------|----------------|
| Environment validation | `scripts/ops/Validate-Environment.ps1` |
| Pre-upgrade backup | `scripts/ops/backup-databases.ps1` (called by upgrade) |
| Upgrade orchestration | `scripts/ops/upgrade.ps1` |
| Rollback | `scripts/ops/rollback.ps1` |
| Version stamp | root `VERSION` file |

## Procedure

1. Set `SA_PASSWORD` (and recommended JWT/InternalApi keys) in `.env` or the environment.
2. Run validation:
   ```powershell
   .\scripts\ops\Validate-Environment.ps1 -RequireSql
   ```
3. Run upgrade (backs up → notes migrations → optional compose → health probes):
   ```powershell
   .\scripts\ops\upgrade.ps1 -TargetVersion 1.0.1
   ```
4. On failure, restore:
   ```powershell
   .\scripts\ops\rollback.ps1 -Confirm
   ```

## Migration note

EF migrations remain owned by each service. In Development, many services auto-migrate on startup. For production, run `dotnet ef database update` per service after deploying binaries that match the restore point, or prefer restoring a backup taken from the target schema version.

## Health probes

Default upgrade health URLs include:

- `http://localhost:5212/api/v1/ops/backup/status`
- Identity / Organization / Compliance Swagger (local HTTPS)

## Exit

Operator can detect version, back up, apply upgrade notes, and verify Gateway ops status without inventing a second deploy plane.
