# 05 — Audit Domain Consolidation

**Date:** 26 July 2026  
**CRP:** CRP-009, CRP-042

---

## SSOT

| Concern | Authoritative owner |
|---------|---------------------|
| Auditor workspace / sessions / observations / reports | **ComplianceService** `Domain.Auditor.*` + `AuditorController` |
| Findings → promote deficiency | **ComplianceService** Findings |
| Scheduled audits (calendar) | **ComplianceService** `ScheduledAudit` |
| Activity / entity audit logs | Per-service / Compliance `AuditLog` (not assessment) |
| Legacy generate-session API | AuditService (to be facaded) |

---

## Parallel System (Legacy)

| Artifact | Path | Action |
|----------|------|--------|
| AuditService host | `AuditService/` | Deprecate after AI client retarget |
| `POST /api/v1.0/audit/generate` | `AuditController` | Preserve via Compliance facade |
| AIOrchestrator `AuditClient` | Calls AuditService | Retarget to facade |
| Gateway `ProxyToAuditServiceAsync` | Unused by controllers | Remove after retarget |
| ProjectReference ComplianceService | NonConformanceMapper only | Eliminate with retirement |
| WeatherForecast | Controllers + model | **Deleted in Phase 0** |

---

## Terminology Normalization

| Term | Meaning |
|------|---------|
| Audit Session (product) | Compliance Auditor session |
| Finding | Compliance Finding / journey audit finding |
| Audit Log | Change/activity log (not NABH assessment) |
| Scheduled Audit | Calendar readiness item |
| CAPA | Deficiency remediation (see 04) — not AuditService CorrectiveAction |

---

## Phase 0 Completed

- Removed WeatherForecast from AuditService and IdentityService  
- Documented SSOT + retirement sequence  
- **Did not** delete AuditService host (would break AI workflow)

---

## Required Next Steps (CR-T3)

1. Implement Compliance facade:  
   `POST /api/v1.0/audit/generate`, `GET /{id}`, `GET organizations/{orgId}` → map to `Domain.Auditor.AuditSession`  
2. Point `AIOrchestratorService` AuditClient at Compliance/Gateway  
3. Remove unused Gateway AuditService proxy helper  
4. Stop deploying AuditService  
5. Drop Audit DB after traffic zero  

---

## UI Alignment

WebApp `/app/auditor/**` already targets Compliance via Gateway `AuditorV1ProxyController`. No UI change required for SSOT. Remaining: wire `roleGuard` (CRP-010) in WebApp.
