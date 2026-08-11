# 06 — Migration Validation Report

**Date:** 26 July 2026

---

## Migration Inventory

| Service | Migration files (approx) | Notes |
|---------|--------------------------|-------|
| ComplianceService | 41 | Primary domain; largest surface |
| KnowledgeBase | 5 | Under Infrastructure/Persistence/Migrations |
| IdentityService | 5 | |
| AIOrchestrator | 4 | |
| AccessGovernance | 3 | |
| Governance | 3 | |
| Integration | 3 | |
| Organization | Present under Infrastructure/Migrations | |
| Audit / Communication / Person / Professional / Workforce / Directory / WI | 2 each | |

---

## Validation Observations

| Check | Result |
|-------|--------|
| EF SQL Server provider | Consistent |
| Startup auto-migrate (Dev) | Used by several services |
| Duplicate migration names | None detected in scan |
| Pending model drift | Not fully re-scaffolded this phase — runtime services applied migrations successfully on recent start |
| Capas table | Still in snapshot; **no drop migration yet** (Phase B) |
| CorrectiveActions (Audit DB) | Still present; dead |

---

## Database Startup (Observed)

On 26 Jul 2026 local start:

- Identity, Organization, Compliance, KnowledgeBase, AI, AccessGovernance, Communication, Person, Workforce, Professional, Governance, Directory, Integration, WI reported healthy  
- Migrations applied messages observed for Organization (and typical Dev auto-migrate path)

---

## Seed Data

| Seed | Status |
|------|--------|
| seed-data NABH SQL | Present; idempotent scripts |
| Compliance.MasterData NABH Entry | Present |
| DemoDataSeeder | Enrichment; still may write Capas (Phase B stop) |
| In-service master data bootstraps | Present |

---

## Recommendations

1. Add CI step: `dotnet ef migrations has-pending-model-changes` (or equivalent) per service  
2. Do **not** drop Capas until Control Center + Graph + Demo seeder confirmed clean  
3. Document restore order: Identity → Organization → KnowledgeBase → Compliance → satellites  
4. Align LocalDB vs SQLExpress naming in a single ops doc (see 08 / 11)

---

## Phase 0 Status

**Migrations: Operational for Dev** — no broken migration blocks found. Schema cleanup migrations deferred to CAPA/Audit Phase B/C.
