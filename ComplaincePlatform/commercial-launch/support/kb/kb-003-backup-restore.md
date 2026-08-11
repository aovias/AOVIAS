# KB-003 — Backup and Restore

## Purpose

Guide customer ops / AOVIAS support through database backup and restore using shipped scripts.

## Backup

```powershell
cd <CompliancePlatform-root>
.\scripts\ops\backup-databases.ps1
```

Record: timestamp, output path, which databases, operator name.

Also confirm application file state if used:

- Gateway `App_Data/` (installation.lock, aovias.installation.json) — include in host backup policy  
- Evidence / blob storage paths per customer config  

## Restore

```powershell
.\scripts\ops\restore-database.ps1
# Supply parameters for backup file + target DB as documented in script help
```

**Warning:** Restore overwrites target database. Confirm environment (never restore prod backup onto wrong tenant without approval).

## Validation after restore

1. Gateway health / login works.  
2. `GET /api/v1/licenses/status?organizationId=` still sensible (license rows restored).  
3. Spot-check journey + evidence for a known ID.  
4. Run subset of `.\scripts\ops\validate-production.ps1`.

## Escalate when

- Backup script fails SQL auth / permissions  
- Partial restore across microservices DBs (consistency risk)  
- Customer lacks retention meeting their policy  

Related: Phase 6 backup docs, [`../../onboarding/deployment-validation.md`](../../onboarding/deployment-validation.md).
