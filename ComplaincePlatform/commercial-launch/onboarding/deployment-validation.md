# Deployment Validation (Customer Onboarding)

**Product:** AOVIAS Command v1.0.0  
**Audience:** Ops / Implementation engineers  
**Scripts root:** [`scripts/ops/`](../../scripts/ops/)

Run these checks after install or upgrade **before** handing the environment to hospital users.

## 1. Environment prerequisites

```powershell
.\scripts\ops\Validate-Environment.ps1
```

Confirms machine / container readiness (SQL connectivity assumptions, required tooling). Fix failures before proceeding.

## 2. Production validation checklist

```powershell
# Print AUTO vs MANUAL checklist
.\scripts\ops\validate-production.ps1

# Local automated checks (tests + file presence; no live prod stack required)
.\scripts\ops\validate-production.ps1 -RunAutomated

# Optional load (requires k6 on PATH + running Gateway)
.\scripts\ops\validate-production.ps1 -RunLoad -LoadBaseUrl "http://localhost:5000"
```

Covered themes: fresh install, upgrade, rollback, backup/restore, license activation, tenant isolation, config validation, DR smoke.

## 3. Upgrade path (existing customer)

```powershell
# Always backup first
.\scripts\ops\backup-databases.ps1

.\scripts\ops\upgrade.ps1
# Dry-run / migration validation as documented in Phase 6 upgrade guide
```

Rollback if needed:

```powershell
.\scripts\ops\rollback.ps1
```

## 4. Backup & restore smoke

```powershell
.\scripts\ops\backup-databases.ps1
.\scripts\ops\restore-database.ps1   # use params for target DB / backup file
```

Record backup location, timestamp, and restore result in the implementation ticket.

## 5. Security headers (pilot hardening)

```powershell
.\scripts\ops\scan-security-headers.ps1
```

## 6. License & commercial smoke

| Check | How |
|-------|-----|
| Issue / provision | `POST /api/v1/commercial/provision` or `POST /api/v1/licenses/issue` |
| Activate | Settings → Platform → Licensing, or `POST /api/v1/licenses/activate` |
| Status | `GET /api/v1/licenses/status?organizationId=` |
| Heartbeat | `POST /api/v1/licenses/heartbeat` |
| Customer health | `GET /api/v1/commercial/customer-health?organizationId=` |
| Diagnostics pack | `GET /api/v1/commercial/diagnostics?organizationId=` |

## 7. Setup wizard lock

Confirm `GET /api/v1/setup/status` shows completed and mutating setup endpoints return conflict after lock (`App_Data/installation.lock`).

## Related docs

- Phase 6: [`docs/commercial-readiness/phase-6/03-installation.md`](../../docs/commercial-readiness/phase-6/03-installation.md)
- Phase 6: [`docs/commercial-readiness/phase-6/09-production-validation.md`](../../docs/commercial-readiness/phase-6/09-production-validation.md)
- Phase 9 published: [`docs/commercial-readiness/phase-9/published/deployment-guide.md`](../../docs/commercial-readiness/phase-9/published/deployment-guide.md) (Sprint 2)
