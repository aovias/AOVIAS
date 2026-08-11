# PRAMAN Sprint 4 — H3 Evidence Ownership Consolidation

**Date:** 2026-06-23  
**Scope:** Analysis only — no application redesign, no menu changes, Journey/CM/StageUnlockingService/Deficiency Engine untouched  
**Goal:** Establish **Evidence** as the single authoritative proof workspace and validate the Upload Anywhere model

---

## Executive Summary

The backend already implements a **unified evidence registration pipeline** (`EvidenceRegistrationService`) and a **centralized review pipeline** (`EvidenceReviewService` + `EvidenceVerificationStateMachine`). All write paths — Journey, Tasks, Evidence Binder, and legacy URL registration — converge on registration; all approval paths converge on review.

Evidence ownership is **architecturally correct on the server** but **fragmented on the client**. The frontend has four upload transports, inconsistent status vocabulary, duplicate validation, and verification actions scattered across Library, Binder, and Review Queue — while Journey and Tasks correctly display state without performing verification.

**Verdict:** Evidence is ~80% consolidated at the API/domain layer. Sprint 4 validates ownership; Sprint 5 (H4 Review Queue Consolidation) should unify client surfaces and retire legacy verify API shapes.

| Criterion | Status |
|-----------|--------|
| Evidence = authoritative proof workspace | ✓ Backend; partial UX |
| Upload Anywhere → Registration Service | ✓ All server paths converge |
| Single evidence lifecycle (OE link) | ✓ `EvidenceVerificationStatus` + state machine |
| Single verification ownership | ✓ `EvidenceReviewService`; 2 legacy API shapes remain |
| Tasks reference evidence, don't own it | ✓ |
| Journey launches workflows, doesn't own proof | ✓ |
| No duplicate upload pipelines (server) | ✓ |
| No duplicate approval workflows (server) | ✓ |
| Event-driven projection chain preserved | ✓ |
| Ready for Sprint 5 — Review Queue | ✓ With refactor plan below |

---

# Deliverable 1 — Evidence Ownership Audit

## Domain Model

Evidence is modeled as a **two-entity link**:

| Entity | File | Role |
|--------|------|------|
| `Evidence` | `ComplianceService/Domain/Evidence.cs` | File aggregate (storage, metadata, versions, optional `TaskId`) |
| `ObjectiveElementEvidence` | `ComplianceService/Domain/ObjectiveElementEvidence.cs` | OE linkage + **authoritative verification state** |
| `EvidenceVersion` | `ComplianceService/Domain/EvidenceVersion.cs` | Version history |
| `EvidenceReviewAuditEntry` | `ComplianceService/Domain/EvidenceReviewAuditEntry.cs` | Immutable review audit trail (Phase 4) |
| `OrganizationComplianceObligation` | `ComplianceService/Domain/OrganizationComplianceObligation.cs` | Projected obligation (`EvidenceStatus`, `VerificationStatus`) |

Supporting: `FindingEvidence` (findings linkage, separate from OE proof path).

## Upload Paths

| Entry Point | HTTP Route | Service Handler | Registration Method |
|-------------|-----------|-----------------|---------------------|
| **Journey OE** (multipart) | `POST /api/v1.0/journeys/{jid}/objective-elements/{oeId}/evidence` | `JourneyWorkspaceAppService.RegisterObjectiveElementEvidenceFromUploadAsync` | `RegisterFromUploadAsync` |
| **Journey OE** (URL/JSON) | Same route (JSON body) | `RegisterObjectiveElementEvidenceAsync` | `RegisterFromUrlAsync` |
| **Journey OE** (bulk) | `POST …/evidence/bulk` | `BulkUploadObjectiveElementEvidenceAsync` | `RegisterFromUploadAsync` (×N) |
| **Journey OE** (version) | `POST …/evidence/{id}/versions` | Version upload handler | `EvidenceService` + re-link |
| **Tasks** | `POST /api/v1.0/tasks/{taskId}/evidences?frameworkId=` | `TaskExecutionService.UploadEvidenceAsync` | `RegisterFromUploadAsync` via `ForTask` context |
| **Evidence Binder** | `POST /api/v1.0/evidence-binder/objective-elements/{oeId}/evidences?frameworkId=` | `EvidenceBinderService.RegisterEvidenceFromUploadAsync` | `RegisterFromUploadAsync` via `ForJourneyObjectiveElement` |
| **Legacy task URL** | `POST /api/v1.0/compliance/tasks/{taskId}/evidence` | `EvidenceController.Create` | `RegisterFromUrlAsync` |
| **Scoring import** | Internal | `ScoringService` | `LinkExistingEvidenceAsync` |

**Evidence Library has no dedicated upload endpoint.** Uploads resolve journey context via `GET /evidence/upload-context` and post through Journey OE multipart (client-side).

## Review / Approval Paths

| Entry Point | HTTP Route | Backend Handler | Review Pipeline |
|-------------|-----------|-----------------|-----------------|
| **Evidence Library / Review Queue** | `POST /api/v1.0/evidence/{evidenceId}/approval` | `EvidenceLibraryController` | `EvidenceReviewService.ApplyReviewAsync` |
| **Evidence Binder** (client) | Same approval endpoint via `EvidenceApiService.approveOrReject` | — | Same |
| **Journey legacy verify** | `POST …/journeys/…/evidence/{id}/verify` | `JourneyWorkspaceAppService.VerifyObjectiveElementEvidenceAsync` | `EvidenceService.SetVerificationAsync` → `ApplyReviewAsync` |
| **V1.1 legacy verify** | `POST /api/v1.1/evidence/objective-elements/{oeId}/verify/{evidenceId}` | `EvidenceController` (V1.1) | Same facade |

All approval paths converge on `EvidenceReviewService.ApplyReviewAsync`. Journey verify is a **thin legacy wrapper** (binary accept/reject only; no "Needs More").

## Storage Paths

Built in `EvidenceRegistrationService.BuildStorageRelativePath`:

- **Journey-scoped:** `App_Data/evidence_uploads/{journeyId}/{objectiveElementId}/{evidenceId}/`
- **Non-journey (task):** `App_Data/evidence_uploads/{hospitalId}/{objectiveElementId}/{evidenceId}/`
- **Versions:** `…/versions/` subfolder

## Projection Updates (Post-Upload / Post-Review)

**On upload** (`LinkAndCompleteAsync`):

1. Create/reset `ObjectiveElementEvidence` link → `Pending`
2. `ObligationProjectionService.ProjectByObjectiveElementAsync` (`EvidenceUploaded`)
3. If task-linked: `DeficiencyTaskLifecycleService.SyncFromEvidenceAsync` (`Uploaded` → `EvidenceSubmitted`)
4. Emit `EvidenceUploaded`, `VerificationRequired` events
5. Enqueue snapshot recalc (journey context)

**On review** (`EvidenceReviewService.ApplyReviewAsync`):

1. State machine transition on OE link(s)
2. Write `EvidenceReviewAuditEntry` rows
3. Obligation projection (`EvidenceApproved` / `EvidenceRejected` / `EvidenceNeedsMore`)
4. Deficiency sync (`Approved` → `ReviewPending`; `Rejected` → `InProgress`)
5. Reopen task if rejected
6. Emit compliance events + snapshot queue

## Frontend Surfaces Audited

| Surface | File | Upload | Verification Actions |
|---------|------|--------|---------------------|
| Evidence Library | `WebApp/.../evidence/pages/evidence.component.ts` | Modal → Journey multipart | Approve/Reject (Workflow tab) |
| Review Queue | `.../evidence-review-queue.component.ts` | — | Approve/Reject/Needs More |
| Evidence Binder | `.../evidence/binder/` | Drawer inline → Binder API | Approve/Reject (list) |
| OE Execution Wizard | `.../oe-execution-wizard/` | Inline dropzone → Journey multipart | Display only |
| Journey workspace | `journey-workspace.store.ts`, tabs | Hidden file input → Journey multipart | Display only |
| Tasks | `task-drawer.component.ts` | Dropzone → Task API | Display only; completion blocked server-side |

---

# Deliverable 2 — Evidence Ownership Matrix

| Responsibility | Evidence | Journey | Tasks | Compliance Maturity |
|----------------|:--------:|:-------:|:-----:|:-------------------:|
| Evidence Library (repository) | ✓ own | — | — | — |
| Evidence Binder (coverage/mapping) | ✓ own | navigate | — | — |
| Review Queue | ✓ own | — | — | — |
| Verification / approval | ✓ own | — (display) | — (display) | — |
| Version history | ✓ own | view/download | view | — |
| Approval / review audit trail | ✓ own | — | — | — |
| Evidence metadata lifecycle | ✓ own | — | — | — |
| Evidence analytics / dashboard | ✓ own | — | — | — |
| Upload entry (launch workflow) | ✓ register | ✓ launch | ✓ launch | — |
| OE execution context | — | ✓ own | — | — |
| Remediation task work | — | — | ✓ own | — |
| Obligation projection | consume | consume | consume | consume |
| Deficiency evaluation | trigger | — | consume | — |
| Stage unlock | trigger | consume | — | consume |
| Governance / CMI | — | — | — | ✓ own |

**Principle validated:** Tasks and Journey **reference** evidence; they do not own verification, lifecycle, or repository state.

---

# Deliverable 3 — Upload Path Inventory

## Server — Converged ✓

```text
┌─────────────┐  ┌─────────────┐  ┌──────────────────┐  ┌─────────────┐
│   Journey   │  │    Tasks    │  │ Evidence Binder  │  │ Legacy URL  │
│  multipart  │  │  multipart  │  │    multipart     │  │  register   │
└──────┬──────┘  └──────┬──────┘  └────────┬─────────┘  └──────┬──────┘
       │                │                   │                   │
       └────────────────┴───────────────────┴───────────────────┘
                                    │
                    EvidenceRegistrationService
                    (RegisterFromUpload / RegisterFromUrl / LinkExisting)
                                    │
              Evidence → ObjectiveElementEvidence → Projection → Deficiency → Events → Snapshot
```

## Client — Fragmented (deviations)

| Path | Transport | Endpoint | FormData Builder | Validation |
|------|-----------|----------|------------------|------------|
| Library upload | `EvidenceMultipartApiClient` (XHR) | Journey OE `/evidence` | `buildSingleEvidenceFormData` | `validateEvidenceFilesForMode` |
| OE Wizard single/bulk | Same | Journey OE + `/bulk` | Single + bulk builders | Shared helper |
| Journey workspace | Same | `postEvidenceMultipart` | Inline in service | **Inline duplicate** (store) |
| Tasks | Same client | `/tasks/{id}/evidences` | Inline FormData | **None** |
| **Binder** | **`HttpClient.post`** | `/evidence-binder/.../evidences` | Inline only | Upload component only |

### Deviations Documented

1. **Binder bypasses `EvidenceMultipartApiClient`** — different auth/envelope/error handling path.
2. **Evidence type strings inconsistent:** `'Document'` (Binder/Wizard), `'document'` (Tasks), `'PolicyDocument'` (Journey workspace).
3. **Library upload requires journey context** — no standalone library upload API; depends on `upload-context`.
4. **Legacy URL endpoint** still active at `/compliance/tasks/{taskId}/evidence`.
5. **Mock/demo paths:** `journey-workspace.store` mock mode; `oe-detail-drawer` local append (no API).

### Future Entry Points (not yet implemented)

- Deficiency Workspace → should call `EvidenceRegistrationService` with `ComplianceDeficiencyId` anchor
- AI Recommendations → should call `RegisterFromUrlAsync` or `LinkExistingEvidenceAsync`

`EvidenceRegistrationContext` already supports `ComplianceDeficiencyId` anchor validation.

---

# Deliverable 4 — Evidence Lifecycle Model

## Authoritative Lifecycle (OE Link)

**Enum:** `EvidenceVerificationStatus` on `ObjectiveElementEvidence`  
**State machine:** `EvidenceVerificationStateMachine.cs`

```text
Draft ──→ Submitted ──→ Pending ──→ Approved (Verified)
                          │    ├──→ Rejected ──→ Pending/Submitted (re-upload)
                          │    └──→ Needs More Evidence ──→ Pending/Submitted
                          └── (direct from Draft)
```

| State | API Label | Blocks Task Completion |
|-------|-----------|------------------------|
| Draft | `draft` | No |
| Submitted | `submitted` / `pendingReview` | Yes |
| Pending | `pendingReview` | Yes |
| Verified | `approved` | No |
| Rejected | `rejected` | Yes |
| NeedsMoreEvidence | `needsMoreEvidence` | Yes |

**Re-upload behavior:** Rejected or NeedsMore links reset to `Pending` on re-registration.

## Secondary Lifecycle (Evidence Aggregate)

`Evidence.LifecycleStatus` (`Draft`, `Submitted`, `Verified`, `Rejected`) exists on the file aggregate but is **not the authoritative review state**. Obligation projection and UI read from `ObjectiveElementEvidence.VerificationStatus`.

## Obligation-Level Projection

| Enum | Values | Source |
|------|--------|--------|
| `EvidenceStatus` | Missing, Partial, Submitted, Verified | `ObligationProjectionRules.ComputeEvidenceStatus` |
| `VerificationStatus` | NotVerified, Pending, Verified, Rejected | `ObligationProjectionRules.ComputeVerificationStatus` |

## Deficiency Lifecycle (Evidence-Driven)

`DeficiencyTaskLifecycleService.SyncFromEvidenceAsync`:

| Trigger | Deficiency Transition |
|---------|----------------------|
| Uploaded | → `EvidenceSubmitted` |
| Approved | → `ReviewPending` |
| Rejected | → `InProgress` |
| NeedsMoreEvidence | No transition |

## Gap: Dual Status on Aggregate

`Evidence.LifecycleStatus` is not kept in sync by the registration/review pipeline in all paths. **Recommendation:** Treat OE link status as SSOT; deprecate aggregate lifecycle writes in Sprint 5+ (non-breaking: stop surfacing aggregate status in DTOs first).

---

# Deliverable 5 — Binder vs Library Analysis

## Intended Separation

| Surface | Intended Role | Current Implementation |
|---------|---------------|------------------------|
| **Evidence Library** | Repository — search, filter, dashboard, analytics, detail, download | ✓ Matches intent (~3,900-line monolith) |
| **Evidence Binder** | Coverage & mapping — chapter/OE navigation, checklist, AI mapping, history | ✓ Mostly matches; overlaps on upload + approve |
| **Review Queue** | Verification worklist — paginated reviewer queue | ✓ Dedicated route; Library also has Review Queue tab |

## Overlap Identified

| Capability | Library | Binder | Review Queue |
|------------|:-------:|:------:|:------------:|
| Upload evidence | ✓ (via Journey API) | ✓ (Binder API) | — |
| Approve/Reject | ✓ Workflow tab | ✓ Evidence list | ✓ Full actions |
| Needs More | ✗ | ✗ | ✓ |
| OE-scoped navigation | Filter only | ✓ Primary UX | Row context |
| Coverage analytics | ✓ Dashboard | ✓ Summary cards | — |
| Checklist / AI mapping | ✗ | ✓ | ✗ |
| Audit trail | History tab | History component | — |

## Duplicate APIs

| Concern | Library | Binder |
|---------|---------|--------|
| List evidence for OE | `/evidence/list` (filtered) | `/evidence-binder/objective-elements/{id}/evidences` |
| Upload | Journey OE endpoint (client) | `/evidence-binder/.../evidences` |
| Approve | `/evidence/{id}/approval` | Same (shared client) |
| Summary | `/evidence/summary`, `/dashboard` | `/evidence-binder/summary` |

Both upload paths converge on `EvidenceRegistrationService` server-side — **no duplicate server pipeline**, but **duplicate client routes and UX**.

## Recommendation

| Surface | Keep | Remove / Redirect |
|---------|------|-------------------|
| **Library** | Repository, analytics, detail drawer, download, upload modal (entry point) | Inline Review Queue tab → link to `/evidence/review-queue` only (Sprint 5) |
| **Binder** | Chapter/OE navigation, checklist, AI mapping, coverage summary | Inline approve/reject → redirect to Review Queue or Library Workflow (Sprint 5) |
| **Review Queue** | Sole verification action surface for reviewers | — |

**No overlap target:** Library = find/browse; Binder = map coverage; Review Queue = verify.

---

# Deliverable 6 — Verification Ownership Report

## Evidence Owns ✓

| Capability | Owner Service | API |
|------------|---------------|-----|
| Review Queue | `EvidenceReviewService.GetReviewQueueAsync` | `GET /evidence/review-queue` |
| Approve / Reject / Needs More | `EvidenceReviewService.ApplyReviewAsync` | `POST /evidence/{id}/approval` |
| Audit trail | `EvidenceReviewService.GetAuditTrailAsync` | `GET /evidence/{id}/audit-trail` |
| Version tracking | `EvidenceService` | `POST …/versions`, detail DTO |
| State machine | `EvidenceVerificationStateMachine` | Internal |
| Reviewer comments | `ObjectiveElementEvidence.ReviewComment`, audit entries | DTOs |

## Journey / Tasks / CM — Correctly Non-Owning ✓

- **Journey:** Displays `verificationStatus` on OE evidence rows; wizard review step counts verified/pending/rejected; **no UI verification actions** (legacy `verifyObjectiveElementEvidence` exists in service but unused).
- **Tasks:** Display-only status chips; server blocks task completion when evidence pending/rejected/needs-more (`EVIDENCE_PENDING_REVIEW`).
- **Compliance Maturity:** No verification actions; consumes projected obligation state only.

## Legacy API Shapes (Consolidation Target — Sprint 5)

| Legacy | Limitation | Target |
|--------|------------|--------|
| `POST …/evidence/{id}/verify` (Journey) | Binary accept/reject only | Deprecate; clients use `/evidence/{id}/approval` |
| `POST /api/v1.1/evidence/.../verify/{id}` | Same | Deprecate |
| `EvidenceService.SetVerificationAsync` | Wrapper | Keep as internal facade; no new consumers |

## Client Verification Fragmentation

| Surface | Approve | Reject | Needs More |
|---------|:-------:|:------:|:----------:|
| Review Queue | ✓ | ✓ | ✓ |
| Library Workflow | ✓ | ✓ | ✗ |
| Binder list | ✓ | ✓ | ✗ |
| Journey | — | — | — |
| Tasks | — | — | — |

**Finding:** Verification ownership is correct at domain level; **client action surfaces are split** across three Evidence module components.

---

# Deliverable 7 — Projection Integration Report

## Event Chain (Validated ✓)

```text
Evidence Upload
       │
       ▼
EvidenceRegistrationService.LinkAndCompleteAsync
       │
       ├── ObjectiveElementEvidence (Pending)
       │
       ├── ObligationProjectionService
       │         └── EvaluateObligationDeficienciesCommandHandler
       │                   └── IDeficiencyEvaluationService
       │
       ├── DeficiencyTaskLifecycleService (if task-linked)
       │
       ├── ComplianceEventEmitter (EvidenceUploaded, VerificationRequired)
       │
       └── SnapshotRecalculationQueue
                 └── Stage unlock via IStageUnlockingService (in projection path)
```

## Review Chain (Validated ✓)

```text
Evidence Approval/Rejection/NeedsMore
       │
       ▼
EvidenceReviewService.ApplyReviewAsync
       │
       ├── State machine + audit entries
       ├── ObligationProjectionService (trigger varies by decision)
       ├── DeficiencyTaskLifecycleService
       ├── Task reopen on reject
       ├── ComplianceEventEmitter
       └── SnapshotRecalculationQueue
```

## Manual Synchronization

**None found.** All obligation, deficiency, task, readiness, and stage-unlock updates flow through projection and event handlers. No client-side sync of verification state to obligations.

## Projection Rules Summary

`ObligationProjectionRules.ComputeEvidenceStatus`:

- No links → `Missing`
- Any verified link → `Verified`
- All awaiting review → `Submitted`
- Otherwise → `Partial`

Stage unlock consumes projected obligation metrics via existing `IStageUnlockingService` — **not modified in this sprint**.

---

# Deliverable 8 — API Consolidation Report

## DTO Locations

| Area | ComplianceService | Gateway |
|------|-------------------|---------|
| Lifecycle / approval | `EvidenceLifecycleDtos.cs` | Passthrough (MediatR proxy) |
| Review queue | `EvidenceReviewDtos.cs` | Passthrough |
| Library | `EvidenceLibraryDtos.cs` | Passthrough |
| Journey OE evidence | `JourneyWorkspaceDtos.cs` (`OeEvidenceItemDto`) | Passthrough |
| Binder | `EvidenceBinderDtos.cs` | Passthrough |
| Registration contracts | `IEvidenceRegistrationService.cs` | N/A (internal) |

Gateway does **not** duplicate evidence DTO types — JSON passthrough.

## Duplicate / Overlapping Contracts

| Issue | Locations | Impact |
|-------|-----------|--------|
| **Verification status vocabulary** | Journey: `verified`/`pending`/`rejected`; Library: `approved`/`pendingReview`/`rejected`/`needsMoreEvidence` | UX inconsistency; same backend enum |
| **Two verify request shapes** | `VerifyOeEvidenceRequest { Accepted, Remarks }` vs `EvidenceApprovalRequest { action, rowVersion }` | Legacy vs canonical |
| **Evidence metadata on list items** | `EvidenceListItemDto`, `OeEvidenceItemDto`, `EvidenceBinderEvidenceItemDto`, `TaskEvidenceItem` | Parallel list shapes; fields overlap |
| **Aggregate vs link status** | `Evidence.LifecycleStatus` vs `ObjectiveElementEvidence.VerificationStatus` | Potential confusion in DTOs |
| **IsApproved bool** | Library DTOs expose `isApproved` alongside `status` | Redundant derived field |

## Cleanup Plan (Non-Breaking)

| Priority | Action | Sprint |
|----------|--------|--------|
| P1 | Add shared `EvidenceStatusLabel` mapper in frontend (mirror `EvidenceVerificationStateMachine.ToApiLabel`) | 5 |
| P1 | Unify Binder upload client onto `EvidenceMultipartApiClient` | 5 |
| P2 | Mark Journey `/verify` and V1.1 verify as `[Obsolete]`; document migration to `/approval` | 5 |
| P2 | Add `needsMoreEvidence` to Library Workflow tab OR remove approve from Binder | 5 |
| P3 | Consolidate list DTOs behind `EvidenceSummaryDto` for cross-module reads | 6 |
| P3 | Stop emitting `Evidence.LifecycleStatus` in external DTOs | 6 |
| P4 | Retire legacy URL upload endpoint when task client confirmed on multipart only | 6 |

**No breaking changes in Sprint 4.**

---

# Deliverable 9 — UX Consistency Report

## Status Display Matrix

| Context | Term for Verified | Term for Pending | Term for Rejected | Needs More |
|---------|-------------------|------------------|-------------------|------------|
| Evidence Library | `approved` | `pendingReview` | `rejected` | `needsMoreEvidence` |
| Review Queue | `approved` | `pendingReview` / `submitted` | `rejected` | `needsMoreEvidence` |
| Binder | Raw `status` string | Same | Same | Not shown |
| Journey OE list | `verified` | `pending` | `rejected` | Not consistently shown |
| OE Wizard review | "verified" count | "pending" count | "rejected" count | — |
| Tasks drawer | `status` chip | Same | Same | Filter exists |

## Navigation Deep-Link Gaps

| Source | Query Params | Library Handler | Result |
|--------|-------------|-----------------|--------|
| OE Wizard quick action | `source=oe-wizard`, `objectiveElementId` | `applyRouteScopedFilters()` | ✓ Works |
| Journey execution context | `source=journey-execution`, `objectiveElementId` | Ignores `journey-execution` | ✗ Filter/upload prefill broken |

**Fix (Sprint 5):** Accept `journey-execution` in Library route handler (1-line alignment with `journey-execution-navigation.util.ts`).

## Consistent Evidence Approved Everywhere?

**Partially.** Backend projection ensures obligation `VerificationStatus.Verified` propagates to Journey and Tasks. UI labels differ (`approved` vs `verified`) but represent the same enum value.

## Upload UX Patterns

| Surface | Pattern |
|---------|---------|
| Library | Modal overlay |
| Binder | Drawer inline buttons |
| OE Wizard | Inline dropzone |
| Journey tabs | Hidden file input |
| Tasks | Dropzone in drawer |

Acceptable for Upload Anywhere — entry points differ, pipeline converges.

---

# Deliverable 10 — Refactor Plan

## Sprint 4 (This Sprint) — Complete ✓

- [x] Evidence ownership audit
- [x] Ownership matrix
- [x] Upload path inventory + deviation documentation
- [x] Lifecycle model validation
- [x] Binder vs Library analysis
- [x] Verification ownership report
- [x] Projection integration audit
- [x] API consolidation review
- [x] UX consistency report
- [x] Refactor plan for Sprint 5+

**No code changes required** — architecture already supports authoritative Evidence ownership at the domain layer.

## Sprint 5 — H4 Review Queue Consolidation (Recommended)

| # | Task | Layer | Risk |
|---|------|-------|------|
| 1 | Make Review Queue the **sole verification action surface**; remove approve/reject from Binder list | Frontend | Low |
| 2 | Library Review Queue tab → navigation link only (or embed shared queue component) | Frontend | Low |
| 3 | Add "Needs More" to Library Workflow OR remove Workflow actions entirely | Frontend | Low |
| 4 | Unify Binder upload onto `EvidenceMultipartApiClient` + shared FormData builder | Frontend | Medium |
| 5 | Shared `EvidenceStatusBadgeComponent` + label mapper (`approved`/`verified` alias) | Frontend | Low |
| 6 | Fix `journey-execution` deep-link in Library | Frontend | Low |
| 7 | Standardize `evidenceType` strings client-side | Frontend | Low |
| 8 | Add task upload validation via `validateEvidenceFilesForMode` | Frontend | Low |
| 9 | Obsolete Journey `/verify` endpoint; update docs | Backend | Low |
| 10 | Extract shared review action hook/service for Library/Queue/Binder | Frontend | Medium |

## Sprint 6+ — Follow-On

| Task | Notes |
|------|-------|
| Deficiency workspace upload entry | Wire to `EvidenceRegistrationContext.ComplianceDeficiencyId` |
| AI recommendation evidence link | `RegisterFromUrlAsync` / `LinkExistingEvidenceAsync` |
| Consolidate evidence list DTOs | Non-breaking additive fields first |
| Retire `Evidence.LifecycleStatus` from external contracts | Internal deprecation |
| Retire legacy URL upload endpoint | After task client audit |
| Split Library monolith | Optional maintainability |

## Explicit Non-Goals (Per CRITICAL RULES)

- ✗ Redesign application
- ✗ Change menu structure
- ✗ Modify Journey ownership
- ✗ Modify Compliance Maturity ownership
- ✗ Modify StageUnlockingService
- ✗ Modify Deficiency Engine
- ✗ Introduce duplicate upload or verification pipelines

---

# Phase Integration Summary

## Phase 5 — Task Integration ✓

```text
Open Task → Upload Evidence → EvidenceRegistrationService (ForTask)
    → Verification (Evidence module) → Deficiency Evaluation → Task Updated
```

Tasks store `TaskId` on `Evidence` for linkage; verification state lives on OE link. Task completion guard enforced server-side.

## Phase 6 — Journey Integration ✓

```text
Open OE → Upload → EvidenceRegistrationService (ForJourneyObjectiveElement)
    → Evidence Module → Verification
```

Journey never writes verification status directly from UI.

---

# Success Criteria Checklist

| Criterion | Result |
|-----------|--------|
| Evidence becomes authoritative proof workspace | ✓ Domain/API; UX consolidation deferred to Sprint 5 |
| Upload Anywhere model validated | ✓ Server converged; client Binder transport noted |
| Tasks remain remediation work items | ✓ |
| Journey remains execution workspace | ✓ |
| Single evidence lifecycle | ✓ OE link + state machine |
| Single verification ownership | ✓ EvidenceReviewService |
| No duplicate upload pipelines (server) | ✓ |
| No duplicate approval workflows (server) | ✓ |
| Existing architecture preserved | ✓ No code changes |
| Ready for Sprint 5 — Review Queue Consolidation | ✓ Plan documented |

---

# Primary File Index

## Backend

| Area | Path |
|------|------|
| OE link + verification enum | `ComplianceService/Domain/ObjectiveElementEvidence.cs` |
| Evidence aggregate | `ComplianceService/Domain/Evidence.cs` |
| Registration pipeline | `ComplianceService/Application/Services/EvidenceRegistrationService.cs` |
| Review pipeline | `ComplianceService/Application/Services/EvidenceReviewService.cs` |
| State machine | `ComplianceService/Application/EvidenceReview/EvidenceVerificationStateMachine.cs` |
| Obligation projection | `ComplianceService/Application/Services/ObligationProjectionService.cs` |
| Projection rules | `ComplianceService/Application/Services/ObligationProjectionRules.cs` |
| Deficiency sync | `ComplianceService/Application/Services/DeficiencyTaskLifecycleService.cs` |
| Journey evidence | `ComplianceService/Application/Services/JourneyWorkspaceAppService.cs` |
| Task upload | `ComplianceService/Application/Services/TaskExecutionService.cs` |
| Binder upload | `ComplianceService/Application/Services/EvidenceBinderService.cs` |
| Library / review APIs | `ComplianceService/Controllers/V1/EvidenceLibraryController.cs` |

## Frontend

| Area | Path |
|------|------|
| Evidence routes | `WebApp/src/app/features/evidence/routes.ts` |
| Library | `WebApp/src/app/features/evidence/pages/evidence.component.ts` |
| Review Queue | `WebApp/src/app/features/evidence/pages/evidence-review-queue.component.ts` |
| Binder | `WebApp/src/app/features/evidence/binder/` |
| Shared API | `WebApp/src/app/features/evidence/services/evidence-api.service.ts` |
| Multipart client | `WebApp/src/app/features/journey/services/evidence-multipart-api.client.ts` |
| Journey upload | `WebApp/src/app/features/journey/services/journey-data.service.ts` |
| OE Wizard | `WebApp/src/app/features/journey/components/oe-execution-wizard/` |
| Task upload | `WebApp/src/app/features/tasks/task-api.service.ts`, `task-drawer.component.ts` |
| Upload validation | `WebApp/src/app/features/journey/utils/oe-evidence-upload.constants.ts` |
| Execution nav | `WebApp/src/app/features/journey/utils/journey-execution-navigation.util.ts` |

---

*End of Sprint 4 — H3 Evidence Ownership Consolidation Report*
