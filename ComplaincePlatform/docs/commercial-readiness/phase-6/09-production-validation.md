# 09 — Production Validation

**Phase:** 6.9  
**Sprint:** 6  
**Date:** 2026-07-26

## Purpose

Close Phase 6 with a repeatable **production validation checklist** for commercial pilot readiness: install → upgrade → rollback → backup/restore → license → tenant isolation → config → DR smoke.

## Checklist script

| Artifact | Path |
|----------|------|
| Validation checklist | [`scripts/ops/validate-production.ps1`](../../../scripts/ops/validate-production.ps1) |

```powershell
# Print checklist (AUTO vs MANUAL)
.\scripts\ops\validate-production.ps1

# Print checklist + run local AUTO checks (paths + focused unit tests)
.\scripts\ops\validate-production.ps1 -RunAutomated
```

### Mode legend

| Tag | Meaning |
|-----|---------|
| **[AUTO]** | Repo/script can verify without a live production stack (file presence, unit tests) |
| **[MANUAL]** | Operator drill on a real/demo stack (compose or customer environment) |

## Validation areas

| Area | AUTO coverage (Sprint 6) | MANUAL drill |
|------|--------------------------|--------------|
| Fresh install | Compose / docs presence | Wizard → health → admin login |
| Upgrade | Doc presence when Sprint 4 landed | Backup → migrate → smoke |
| Rollback | — | Restore + pin previous package |
| Backup / restore | Foundation strategy doc | SQL + blob restore + smoke |
| License activation | `LicenseAppService` unit tests | Issue → activate → heartbeat → grace |
| Tenant isolation | `OrganizationAccessHandler` unit tests | Cross-org JWT → 403 |
| Config validation | `deployment.metadata.json` / env example | Vault inject + Production secrets |
| DR smoke | Doc presence | Failover restore RPO/RTO record |

## Automated tests added

| Project | Focus |
|---------|--------|
| `OrganizationService.Tests` | `LicenseAppServiceTests` — issue, activate, grace, expired, wrong-org |
| `tests/Gateway.UnitTests` | `OrganizationAccessHandlerTests` — match, mismatch, SUPER_ADMIN, missing claim |

```powershell
dotnet test OrganizationService.Tests --filter "FullyQualifiedName~LicenseAppService"
dotnet test tests/Gateway.UnitTests --filter "FullyQualifiedName~OrganizationAccessHandler"
```

## Related runbooks

| Topic | Doc |
|-------|-----|
| Backup / restore strategy | [`../foundation/11-backup-recovery.md`](../foundation/11-backup-recovery.md) |
| Licensing platform | [`01-licensing-platform.md`](01-licensing-platform.md) |
| Multi-tenant | [`02-multi-tenant-platform.md`](02-multi-tenant-platform.md) |
| Installation | [`03-installation.md`](03-installation.md) |
| Configuration | [`04-configuration-platform.md`](04-configuration-platform.md) |
| Upgrade | [`05-upgrade-platform.md`](05-upgrade-platform.md) |
| Backup product | [`06-backup-recovery.md`](06-backup-recovery.md) |
| Commercial ops | [`07-commercial-operations.md`](07-commercial-operations.md) |
| Deploy automation | [`08-deployment-automation.md`](08-deployment-automation.md) |

## Honest deferrals (not blocking checklist existence)

1. **Payment gateway / Stripe** — out of Phase 6; activation keys only.
2. **Full Kubernetes production** — Helm skeleton only (future); Compose + scripts are the Phase 6 path.
3. **Playwright E2E** — deferred (no WebApp CI infra for full install wizard); unit/API-level isolation + license tests shipped.
4. **End-to-end DR automation product** — strategy + manual drill; no customer self-serve restore API.

## Exit

Operators can run the checklist script, execute MANUAL drills on a demo stack, and developers can re-run focused license + tenant isolation unit tests in CI.
