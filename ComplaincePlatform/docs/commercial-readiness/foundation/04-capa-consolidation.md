# 04 — CAPA Consolidation

**Date:** 26 July 2026  
**CRP:** CRP-008

---

## Authoritative CAPA Implementation (SSOT)

| Layer | Implementation |
|-------|----------------|
| Domain | `ComplianceDeficiency` CAPA fields (`CorrectiveAction*`, `PreventiveAction*`, effectiveness, recurrence) |
| Application write | `PatchDeficiency*CapaCommandHandlers` |
| Application read | `CapaQueryService` / `ICapaQueryService` |
| Internal API | `InternalDeficienciesController` CAPA routes |
| Gateway | `DeficienciesController` `/capa`, corrective/preventive/effectiveness |
| UI | WebApp Deficiency Command Center + `/app/audit/corrective-actions` |

---

## Models Retired from Active Use (Phase A)

| Model | Status |
|-------|--------|
| `Capa` entity / `Capas` table | **Orphan** — no app writes; Control Center no longer reads; marked legacy in code comments |
| `ActionPlan` | Still written by scoring path — **Phase B** bridge |
| `AuditService.CorrectiveAction` | Dead DbSet — delete with AuditService |

---

## Phase 0 Code Changes

1. `ControlCenterDataset.ActiveDeficiencies` added  
2. `ControlCenterRepository` loads open deficiencies; `Capas` set empty  
3. Aggregation overdue/pending/alerts/activity use deficiency CAPA helpers  
4. `Capa.cs` documentation updated to SSOT pointer  

**Public APIs unchanged.**

---

## Workflow Consistency (After Phase A)

```
Finding / Evaluation → Deficiency
       ↓
Corrective / Preventive / Effectiveness PATCH (DCC)
       ↓
CapaQueryService metrics + Analytics /capa
       ↓
Control Center KPIs (now same SSOT)
```

---

## Phase B / C (Not in this PR)

1. Retarget EnterpriseComplianceGraph CAPA edges to deficiencies  
2. Remap `Incident.CapaId` → deficiency id (or rename)  
3. Bridge `ActionPlanService.EnsureForLowScoreAsync` to deficiency/task  
4. Stop DemoDataSeeder Capas inserts  
5. Schema migration to drop `Capas` when reads = 0  

---

## Verification Checklist

- [x] Control Center uses deficiency CAPA metrics  
- [x] DCC APIs untouched  
- [ ] Graph CAPA edges  
- [ ] ActionPlan bridge  
- [ ] Table drop migration  
