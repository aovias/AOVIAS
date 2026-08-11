# 06 — Backup & Recovery (Operational Tooling)

**Phase:** 6.6  
**Sprint:** 4  
**Date:** 2026-07-26

Supersedes the “no automated tooling” gap called out in foundation `11-backup-recovery.md` for pilot ops scripts (product UI restore remains out of scope).

## Delivered

| Capability | Implementation |
|------------|----------------|
| SQL backup script | `scripts/ops/backup-databases.ps1` |
| SQL restore script | `scripts/ops/restore-database.ps1` |
| Rollback helper | `scripts/ops/rollback.ps1` |
| Manifest | `Gateway/App_Data/backups/manifest.json` |
| Ops API | `GET /api/v1/ops/backup/status` |

## Backup

```powershell
.\scripts\ops\backup-databases.ps1 `
  -Databases Identity,Organization,Compliance `
  -Server "localhost,1433"
```

- Uses `sqlcmd` when installed; otherwise writes **placeholder markers** and documents the `BACKUP DATABASE` T-SQL.
- Updates `Gateway/App_Data/backups/manifest.json` with last backup metadata.

## Restore

```powershell
.\scripts\ops\restore-database.ps1 -Database Compliance -Replace
# or explicit file:
.\scripts\ops\restore-database.ps1 -Database Compliance -BackupFile .\Gateway\App_Data\backups\Compliance_20260726-120000.bak -Replace
```

## Ops status API

`GET /api/v1/ops/backup/status` returns the manifest (creates it if missing). Intended for upgrade scripts and platform admin diagnostics. Anonymous read is allowed for local health probes; commercial hardening should re-lock to `AdminOnly` behind the gateway edge.

## RPO / RTO (pilot)

| Metric | Target |
|--------|--------|
| RPO | ≤ 24h (nightly full via scheduled task calling backup script) |
| RTO | Operator-driven restore of Identity → Organization → Compliance |

## Exit

Pilot operators can back up and restore core SQL databases and inspect last-backup metadata via Gateway.
