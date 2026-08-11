# 03 — Domain Consolidation Report

**Date:** 26 July 2026  
**Rule:** Prefer refactoring; preserve APIs

---

## Inventory of Duplication Classes

| Area | Duplicates | SSOT | Action |
|------|------------|------|--------|
| CAPA | `Capa` entity, Deficiency CAPA fields, `ActionPlan`, AuditService `CorrectiveAction` | `ComplianceDeficiency` CAPA fields | Phase A done (CC KPIs); Phase B/C pending |
| Audit session | AuditService `AuditSession` vs Compliance `Domain.Auditor.AuditSession` | Compliance Auditor | Deprecate AuditService behind facade |
| Findings | AuditService `AuditFinding` vs Compliance `Finding` | Compliance Finding | Retire with AuditService |
| Departments | Org `Department` vs `MasterDepartment` / workforce | Documented dual catalogs | No merge in Phase 0 (intentional layering) |
| Journey state | `JourneyState` vs `ComplianceLifecycleState` | Lifecycle state machine | Track for cleanup |
| Audit logs | Per-service audit log vs Compliance AuditLog | Context-specific | Keep separate |

---

## Consolidation Principles Applied

1. **One write path per concept** — CAPA writes only via deficiency patches.  
2. **Read models may lag** — Capas table kept empty in CC dataset; table not dropped yet.  
3. **No public API break** — `/deficiencies/*/capa` unchanged.  
4. **Facades before deletes** — AuditService generate API kept until AI client retargeted.

---

## Duplicate DTOs / Commands / Handlers

| Finding | Decision |
|---------|----------|
| Gateway deficiency CAPA handlers proxy Compliance | Keep (correct BFF) |
| Copilot CreateCapa does not insert Capas | Keep as facade; document |
| Control Center previously aggregated Capas | **Fixed** → ActiveDeficiencies |

---

## DI / Mapping Updates (Phase 0)

- `ControlCenterRepository` loads `ComplianceDeficiencies` into `ActiveDeficiencies`  
- `ControlCenterAggregationService` CAPA KPIs/alerts/events use deficiency helpers  
- `Capa` class XML docs mark legacy orphan  

No repository interface renames (backward compatible).

---

## Remaining Consolidation Backlog

| ID | Work | Sprint |
|----|------|--------|
| DC-01 | Graph/Incident Capas → DeficiencyId | CR-T3 Phase B |
| DC-02 | Stop ActionPlan writes; bridge to deficiency/task | CR-T3 |
| DC-03 | Audit generate facade on Compliance | CR-T3 |
| DC-04 | Drop Capas / CorrectiveActions tables after zero reads | Post CR-T3 |
| DC-05 | JourneyState enum cleanup | ARCH-1 |

---

## Compatibility Statement

Existing WebApp DCC and Gateway deficiency CAPA routes remain the operational CAPA surface. Control Center now reports the same truth.
