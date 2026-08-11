# PRAMAN Sprint 5A — Evidence Verification Consolidation & UX Consistency

**Date:** 2026-06-23  
**Scope:** Verification UI consolidation + status standardization — no Library/Binder redesign, no menu changes, no backend pipeline changes  
**Goal:** Review Queue becomes the **sole verification action surface**; all modules show consistent evidence status labels

---

## Executive Summary

Sprint 5A eliminates verification fragmentation on the client while preserving the authoritative backend services (`EvidenceRegistrationService`, `EvidenceReviewService`, `EvidenceVerificationStateMachine`).

| Change | Result |
|--------|--------|
| Review actions removed from Library Workflow tab | Read-only + "Open in Review Queue" |
| Review actions removed from Binder evidence list | Read-only + "Open in Review Queue" |
| Review Queue | Sole surface for Approve / Reject / Return For Revision |
| `EvidenceStatusPresentationMapper` | Backend + frontend canonical labels |
| `EvidenceStatusBadgeComponent` | Shared semantic badge across modules |
| Legacy verify endpoints | Marked `[Obsolete]` — wrappers unchanged |
| Deep-link navigation | Library, Binder, Tasks, Journey → Review Queue |

---

# Deliverable 1 — Verification Surface Inventory

| Surface | Before | After (Sprint 5A) |
|---------|--------|-------------------|
| **Review Queue** | Approve, Reject, Needs More | ✓ **Authoritative** — Approve, Reject, Return For Revision |
| **Evidence Library** | Approve/Reject in Workflow tab | Status display + history only; link to Review Queue |
| **Evidence Binder** | Approve/Reject on list rows | Status display only; link to Review Queue |
| **Journey OE tab** | Raw `verificationStatus` string | Badge + Review link |
| **Tasks drawer** | Raw status chip | Badge + Review link |
| **Journey `/verify` API** | Binary accept/reject wrapper | `[Obsolete]` — delegates to `EvidenceReviewService` |
| **V1.1 `/verify` API** | Same | `[Obsolete]` |

---

# Deliverable 2 — Evidence Status Inventory

| Raw API / Legacy | Canonical Key | Display Label (Evidence) | Display Label (Obligation) |
|------------------|---------------|--------------------------|----------------------------|
| `approved`, `verified`, `accepted`, `reviewed` | `approved` | Approved | Verified |
| `pendingReview`, `pending`, `submitted` | `pendingReview` / `submitted` | Pending Review / Submitted | Pending Review |
| `rejected`, `declined` | `rejected` | Rejected | Rejected |
| `needsMoreEvidence`, `returnForRevision` | `needsMoreEvidence` | Needs More Evidence | Needs More Evidence |
| `draft`, `missing` | `draft` | Draft | Draft |
| `uploaded`, `partial` | `uploaded` | Uploaded | — |

**Prior inconsistencies resolved:** Journey `verified` → **Approved** on evidence items; obligation-level `Verified` retained for projection signals.

---

# Deliverable 3 — EvidenceStatusPresentationMapper

| Layer | Path |
|-------|------|
| **Backend** | `ComplianceService/Application/Presentation/EvidenceStatusPresentationMapper.cs` |
| **Frontend** | `WebApp/src/app/shared/models/evidence-status.model.ts` |

Backend consolidates API key generation via `EvidenceVerificationStateMachine.ToApiLabel` in:
- `EvidenceLibraryQueryHelper.MapVerificationStatus`
- `EvidenceBinderQueryService`
- `TaskQueryService`

---

# Deliverable 4 — EvidenceStatusBadgeComponent

**Path:** `WebApp/src/app/shared/ui/evidence-status-badge/evidence-status-badge.component.ts`

- Semantic design tokens (`--color-success`, `--color-warning`, `--color-danger`, `--color-info`)
- Light/dark via `color-mix` and `prefers-color-scheme`
- `role="status"` for accessibility
- `context` input: `'evidence' | 'obligation'`

**Adopted in:** Library, Review Queue, Binder list, Journey evidence tab, Tasks drawer.

---

# Deliverable 5 — Legacy Endpoint Audit

| Endpoint | Layer | Consumer | Risk | Action |
|----------|-------|----------|------|--------|
| `POST …/journeys/…/evidence/{id}/verify` | ComplianceService + Gateway | `journey-data.service.ts` (unused in UI) | Low | `[Obsolete]` — wrapper to `EvidenceReviewService` |
| `POST /api/v1.1/evidence/objective-elements/{oeId}/verify/{id}` | ComplianceService V1.1 | Legacy integrations | Low | `[Obsolete]` |
| `POST /api/v1.0/evidence/{id}/approval` | Evidence Library API | Review Queue (authoritative) | None | **Canonical** |

**No breaking changes.** Existing clients continue to work; new UI uses Review Queue only.

---

# Deliverable 6 — Navigation Consistency Report

**Utility:** `WebApp/src/app/shared/utils/evidence-review-navigation.util.ts`

| Source | Navigation |
|--------|------------|
| Evidence Library Workflow tab | `/app/evidence/review-queue?evidenceId=&objectiveElementId=&search=` |
| Evidence Binder list | Same pattern |
| Tasks drawer | `/app/evidence/review-queue?evidenceId=` |
| Journey evidence tab | Same |
| Journey execution nav util | `reviewQueueQueryForOe()` added |

Review Queue reads query params on init and highlights matching `evidenceId` row.

**Also fixed:** Library accepts `source=journey-execution` deep-link (Sprint 4 gap).

---

# Deliverable 7 — API Consistency Report

| Issue | Status |
|-------|--------|
| Duplicate `MapVerificationStatus` in Library/Binder/Tasks | ✓ Delegates to `EvidenceStatusPresentationMapper.ToApiKey` |
| Duplicate label generation in UI | ✓ Centralized in `evidence-status.model.ts` |
| `verified` vs `approved` API drift | ✓ Normalized at presentation layer |
| Breaking DTO changes | ✗ None — API keys unchanged |

**Future (Sprint 5B+):** Optional additive `statusLabel` field on list DTOs from backend mapper.

---

# Deliverable 8 — Validation Report

## Verification Path (Single Pipeline) ✓

```text
Evidence Uploaded → Submitted/Pending Review
       ↓
Review Queue (POST /evidence/{id}/approval)
       ↓
EvidenceReviewService.ApplyReviewAsync
       ↓
EvidenceVerificationStateMachine
       ↓
ObligationProjectionService
       ↓
EvaluateObligationDeficienciesCommandHandler
       ↓
DeficiencyTaskLifecycleService (if task-linked)
       ↓
Snapshot / Stage Unlock (event-driven)
```

**Confirmed:** No alternative verification path remains in active UI. Library and Binder cannot invoke `approveOrReject`.

## Build / Test

| Check | Result |
|-------|--------|
| `npm run build` | ✓ |
| `EvidenceStatusPresentationMapperTests` | ✓ |
| `evidence-status.model.spec.ts` | Added |

## Screenshot Checklist (Manual QA)

- [ ] Library table — Approved badge matches Review Queue for same item
- [ ] Library Workflow tab — no Approve/Reject buttons; Review Queue link visible for pending items
- [ ] Binder drawer — no Approve/Reject; Review Queue link for pending items
- [ ] Review Queue — Approve / Reject / Return For Revision functional
- [ ] Journey OE evidence tab — badge + Review link
- [ ] Tasks drawer — badge + Review link
- [ ] Journey execution → Evidence Library deep-link applies OE filter

---

# Success Criteria

| Criterion | Status |
|-----------|--------|
| Review Queue = sole verification surface | ✓ |
| Library & Binder = read-only verification consumers | ✓ |
| Status labels standardized | ✓ |
| EvidenceStatusPresentationMapper introduced | ✓ |
| Shared status component implemented | ✓ |
| Legacy endpoints audited + deprecated | ✓ |
| EvidenceReviewService remains authoritative | ✓ |
| No menu changes | ✓ |
| No screen redesigns | ✓ |
| Ready for Sprint 6 — Deficiency Command Center | ✓ |

---

# Files Changed (Summary)

**New**
- `ComplianceService/Application/Presentation/EvidenceStatusPresentationMapper.cs`
- `WebApp/src/app/shared/models/evidence-status.model.ts`
- `WebApp/src/app/shared/models/evidence-status.model.spec.ts`
- `WebApp/src/app/shared/ui/evidence-status-badge/evidence-status-badge.component.ts`
- `WebApp/src/app/shared/utils/evidence-review-navigation.util.ts`
- `ComplianceService.Tests/EvidenceStatusPresentationMapperTests.cs`

**Modified**
- `evidence.component.ts` — removed review actions; badge + Review Queue nav
- `evidence-review-queue.component.ts` — badge; deep-link; Return For Revision
- `evidence/binder/evidence-list.component.ts`, `evidence-drawer.component.ts`, `evidence-binder.store.ts`
- `task-drawer.component.ts`, `journey-evidence-tab.component.ts`
- `execution-screen.component.ts`, `journey-execution-navigation.util.ts`
- Backend status mappers + `[Obsolete]` on legacy verify endpoints

---

*End of Sprint 5A Report*
