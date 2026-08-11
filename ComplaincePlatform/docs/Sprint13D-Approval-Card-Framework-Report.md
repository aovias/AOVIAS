# Sprint 13D – Compliance Copilot Approval Card Framework
## Report: Human Approval Before Operational Execution

**Sprint:** 13D  
**Status:** ✅ Complete  
**Date:** 2026-06-24  
**Depends On:** Sprint 13A (Core), 13B (Recommendation Engine), 13C (Draft Generation Engine)

---

## Executive Summary

Sprint 13D introduces the **Approval Card Framework** — the governance bridge between AI-generated action proposals and real workflow execution. No AI system can autonomously trigger compliance operations. Every proposed action requires explicit human approval by an authorized user before any existing workflow is invoked.

**Trust Model Enforced:**

| Capability   | AI (Copilot) | Human User |
|-------------|:------------:|:----------:|
| Read         | ✅           | ✅          |
| Explain      | ✅           | ✅          |
| Recommend    | ✅           | ✅          |
| Draft        | ✅           | ✅          |
| Approve      | ❌           | ✅          |
| Execute      | ❌           | ✅          |

---

## 1. Proposal Lifecycle

```text
Recommendation
       ↓
   [Draft] (optional)
       ↓
Action Proposal  (AI creates — status: Proposed)
       ↓
Approval Card   (Human reviews)
       ↓
   Human Decision
   ├── Approve → status: Approved → Execute → status: Executed
   │                  └─ ResultEntityId permanently linked
   ├── Reject  → status: Rejected (terminal)
   └── Ignore  → status: Expired (TTL elapsed, terminal)
```

**Key invariants:**
- AI never calls `Approve()`, `Reject()`, or `MarkExecuted()`.
- Only human user IDs (from JWT context) may advance lifecycle.
- Expired proposals cannot be approved or executed — guard enforced in domain entity.
- `ResultEntityId` + `ResultEntityType` are stored permanently for effectiveness reporting.

---

## 2. Domain Objects

### `CopilotActionProposal` (new — `Domain/Copilot/`)

```csharp
public class CopilotActionProposal : AggregateRoot
{
    Guid Id;
    Guid OrganizationId;
    Guid RecommendationId;   // source recommendation (always present)
    Guid? DraftId;           // source draft (optional)
    ProposalType ProposalType;
    string Title;
    string Description;
    string PayloadJson;      // structured action parameters
    ProposalStatus Status;
    string ProposedBy;       // "Copilot" for AI-generated
    DateTime ProposedAtUtc;
    DateTime ExpiresAtUtc;   // TTL default: 7 days

    // Approval
    Guid? ApprovedBy;
    DateTime? ApprovedAtUtc;
    Guid? RejectedBy;
    DateTime? RejectedAtUtc;
    string? RejectionReason;

    // Execution result (Sprint 13D recommendation: permanent linkage)
    Guid? ResultEntityId;    // ID of created task/finding/CAPA
    string? ResultEntityType;
    string? ExecutionNotes;
    DateTime? ExecutedAtUtc;
    Guid? ExecutedBy;
}
```

### `ProposalAuditEvent` (new — `Domain/Copilot/`)

Immutable write-once record for every lifecycle state change:

| EventType  | When                                      |
|------------|-------------------------------------------|
| Generated  | AI creates proposal from recommendation/draft |
| Created    | Custom proposal created                   |
| Viewed     | Any user opens the proposal detail        |
| Approved   | Human approves                            |
| Rejected   | Human rejects                             |
| Executed   | Approved proposal dispatched to workflow  |
| Expired    | TTL elapsed without action                |

---

## 3. Proposal Taxonomy (`ProposalType` enum)

| Value | Execution Maps To |
|-------|-------------------|
| `CreateTask (0)` | `IDeficiencyRemediationTaskService.EnsureRemediationTaskAsync()` |
| `AssignTask (1)` | `IDeficiencyRemediationTaskService.EnsureRemediationTaskAsync()` |
| `CreateCapa (2)` | `IDeficiencyRemediationTaskService` + CAPA form navigation |
| `ScheduleEffectivenessReview (3)` | CAPA module review scheduling |
| `OpenEvidenceChecklist (4)` | Evidence workspace deep link |
| `GenerateExecutiveBriefing (5)` | Copilot Drafts workspace |
| `CreateFinding (6)` | `IFindingQueryService.CreateAsync()` |
| `CreateDeficiency (7)` | Compliance deficiency workspace |
| `CreateReadinessPlan (8)` | Accreditation Readiness workspace |
| `CreateRemediationPlan (9)` | Compliance workspace |

---

## 4. Recommendation → Proposal Category Mapping

| Recommendation Category | Proposal Type Generated |
|-------------------------|-------------------------|
| `Deficiency` | `CreateCapa` |
| `Capa` | `CreateCapa` |
| `Risk` | `CreateTask` |
| `Accreditation` | `CreateReadinessPlan` |
| `Verification` | `OpenEvidenceChecklist` |
| `Evidence` | `OpenEvidenceChecklist` |
| `Executive` | `GenerateExecutiveBriefing` |
| `Analytics` | `GenerateExecutiveBriefing` |
| `Escalation` | `CreateFinding` |
| `Department` | `CreateTask` |

---

## 5. Draft → Proposal Type Mapping

| Draft Type | Proposal Type Generated |
|------------|-------------------------|
| `CapaDraft` | `CreateCapa` |
| `ExecutiveComplianceBriefing` | `GenerateExecutiveBriefing` |
| `ReadinessSummary` | `CreateReadinessPlan` |
| `EvidenceChecklist` | `OpenEvidenceChecklist` |
| `AuditPreparationBrief` | `CreateFinding` |
| `ManagementReviewSummary` | `CreateTask` |

---

## 6. Approval Policy Rules

**`ApprovalPolicyService`** governs all lifecycle transitions:

```
CanApprove:
  ✓ userId != Guid.Empty (authenticated human)
  ✓ Status == Proposed
  ✓ ExpiresAtUtc > now
  ✓ Role in { ComplianceManager, QualityOfficer, ComplianceOfficer, HospitalAdmin, Admin, SuperAdmin }

CanReject:
  Same as CanApprove

CanExecute:
  ✓ userId != Guid.Empty (authenticated human)
  ✓ Status == Approved  (must have been approved first)
  ✓ Role in { ComplianceManager, QualityOfficer, ComplianceOfficer, HospitalAdmin, Admin, SuperAdmin }
```

**No bypass path:** Policy is checked in both `CopilotProposalsController` and `ProposalExecutionService`.

---

## 7. Execution Layer (`ProposalExecutionService`)

**Critical constraints:**
1. Only executes `Approved` proposals — rejects all others.
2. Uses **existing** application services only — no new business logic.
3. Stores `ResultEntityId` and `ResultEntityType` back on the proposal.
4. Writes execution audit event after success.

**Execution dispatch:**

```csharp
ProposalType.CreateTask → IDeficiencyRemediationTaskService.EnsureRemediationTaskAsync()
                        → (fallback) acknowledged result with navigation guidance
ProposalType.CreateCapa → IDeficiencyRemediationTaskService.EnsureRemediationTaskAsync()
ProposalType.CreateFinding → IFindingQueryService.CreateAsync()
ProposalType.OpenEvidenceChecklist → acknowledged (frontend navigation)
ProposalType.CreateReadinessPlan → acknowledged (workspace navigation)
ProposalType.GenerateExecutiveBriefing → draft linking
```

---

## 8. API Reference

### ComplianceService Endpoints (`api/v1/copilot/proposals`)

| Method | Path | Description |
|--------|------|-------------|
| GET | `/proposals` | List proposals (filterable: status, type, recommendationId) |
| GET | `/proposals/pending` | Get proposals awaiting human action |
| GET | `/proposals/{id}` | Get single proposal (records Viewed audit event) |
| GET | `/proposals/{id}/audit-trail` | Full audit trail for a proposal |
| POST | `/proposals` | Create custom proposal |
| POST | `/proposals/from-recommendation/{id}` | Generate from recommendation |
| POST | `/proposals/from-draft/{id}` | Generate from draft |
| POST | `/proposals/{id}/approve` | Human approves |
| POST | `/proposals/{id}/reject` | Human rejects (requires reason) |
| POST | `/proposals/{id}/execute` | Execute approved proposal |

All endpoints proxied through Gateway at the same relative paths.

---

## 9. Database Schema

### `CopilotActionProposals` (new table)

| Column | Type | Description |
|--------|------|-------------|
| `Id` | uniqueidentifier PK | |
| `OrganizationId` | uniqueidentifier | Tenant boundary |
| `RecommendationId` | uniqueidentifier | Source recommendation |
| `DraftId` | uniqueidentifier? | Source draft (optional) |
| `ProposalType` | int | Enum integer |
| `Title` | nvarchar(250) | |
| `Description` | nvarchar(4000) | |
| `PayloadJson` | nvarchar(max) | Structured action params |
| `ProposedBy` | nvarchar(100) | "Copilot" for AI |
| `ProposedAtUtc` | datetime | |
| `ExpiresAtUtc` | datetime | Default: +7 days |
| `Status` | int | ProposalStatus enum |
| `ApprovedBy` | uniqueidentifier? | Human user ID |
| `ApprovedAtUtc` | datetime? | |
| `RejectedBy` | uniqueidentifier? | |
| `RejectedAtUtc` | datetime? | |
| `RejectionReason` | nvarchar(2000)? | |
| `ResultEntityId` | uniqueidentifier? | **Permanent linkage to created entity** |
| `ResultEntityType` | nvarchar(100)? | E.g., "ComplianceTask", "Finding" |
| `ExecutionNotes` | nvarchar(2000)? | |
| `ExecutedAtUtc` | datetime? | |
| `ExecutedBy` | uniqueidentifier? | |

**Indexes:** OrganizationId, Status, RecommendationId, ProposalType, ExpiresAtUtc, (OrgId, Status) composite.

### `CopilotProposalAuditEvents` (new table)

Immutable events, never updated, never deleted.

| Column | Type | Description |
|--------|------|-------------|
| `Id` | uniqueidentifier PK | |
| `ProposalId` | uniqueidentifier | FK to proposal |
| `OrganizationId` | uniqueidentifier | |
| `EventType` | nvarchar(50) | Created, Viewed, Approved, etc. |
| `ActorId` | nvarchar(100)? | User GUID |
| `ActorEmail` | nvarchar(200)? | |
| `Notes` | nvarchar(2000)? | |
| `ContextJson` | nvarchar(max)? | Snapshot at event time |
| `OccurredAtUtc` | datetime | |

---

## 10. Frontend Components

### `ApprovalCardComponent`

Summary card for a proposal. Displays: type, status, title, description, trace links (recommendation/draft), expiry, and action buttons (Approve / Reject / Execute).

### `ProposalListComponent`

Full-featured list with:
- Status filter chips (Pending, Approved, Executed, Rejected)
- Count badges per status
- Text search across title, description, type
- Reject dialog with mandatory reason input
- Opens `ProposalDetailComponent` panel on click

### `ProposalDetailComponent`

Slide-in panel showing:
- Full proposal details with parsed payload parameters
- Execution result (when Executed)
- Rejection reason (when Rejected)
- Audit trail chronological list
- Approve / Reject / Execute action buttons

### `ProposalHistoryComponent`

Standalone timeline component for displaying audit events. Used inside `ProposalDetailComponent`.

---

## 11. Store Extensions (`CopilotStore`)

New signals added (Sprint 13D):

| Signal | Type | Description |
|--------|------|-------------|
| `proposals` | `CopilotActionProposal[]` | All proposals for organization |
| `activeProposal` | `CopilotActionProposal \| null` | Currently selected |
| `proposalAuditTrail` | `ProposalAuditEvent[]` | Trail for active proposal |

New computed signals:

| Computed | Description |
|----------|-------------|
| `pendingApprovals` | Proposals with status Proposed, not expired |
| `approvedProposals` | Approved proposals |
| `rejectedProposals` | Rejected proposals |
| `executedProposals` | Executed proposals |
| `approvalCounts` | Total, pending, approved, rejected, executed, expired counts |

New actions: `loadProposals`, `loadPendingProposals`, `selectProposal`, `clearActiveProposal`, `generateProposalFromRecommendation`, `generateProposalFromDraft`, `approveProposal`, `rejectProposal`, `executeProposal`, `loadProposalAuditTrail`.

---

## 12. Security & Governance Model

### Organization Boundaries
- All repository queries are scoped by `OrganizationId` from `IUserContextService`.
- Global query filters in `ComplianceDbContext` provide additional tenant isolation.
- Proposals belong to the organization that generated them — cross-organization access blocked.

### Permission Checks
- `IApprovalPolicyService` validates role and status before every lifecycle transition.
- Controller layer checks policy before mutating state.
- Execution service re-checks policy independently (defense-in-depth).

### No Bypass Paths
- No `[AllowAnonymous]` on proposal endpoints.
- Policy service does not check AI actor — it checks `Guid userId != Empty` and role membership.
- Expired proposals are permanently terminal — `GuardProposed` throws before any state change.
- `MarkExecuted` requires `Status == Approved` — cannot skip approval step.

### Audit Compliance
- Every state change writes an immutable `ProposalAuditEvent` record.
- `Viewed` events are recorded for access monitoring.
- Actor ID and email stored in every audit event.
- `ContextJson` available for state snapshots.
- Audit events are never deleted (`DOWN` migration only drops tables, not individual rows).

---

## 13. Validation Scenarios

| Scenario | Expected Behavior |
|----------|-------------------|
| CAPA recommendation → proposal | `ProposalType.CreateCapa`, payload contains `DeficiencyId` |
| Evidence recommendation → proposal | `ProposalType.OpenEvidenceChecklist` |
| CAPA draft → proposal | `ProposalType.CreateCapa` |
| Readiness summary draft → proposal | `ProposalType.CreateReadinessPlan` |
| Approved proposal → execute | Dispatches to `IDeficiencyRemediationTaskService`, stores `ResultEntityId` |
| Rejected proposal → execute attempt | `CanExecute` returns `Deny("Only Approved proposals…")` |
| Expired proposal → approve attempt | `GuardProposed` throws `InvalidOperationException` |
| AI system calling approve | `userId == Guid.Empty` → `Deny("User is not authenticated.")` |
| Wrong-role user calling approve | Role check fails → `Deny("User does not have an approval-authorized role.")` |
| Cross-organization access | `OrganizationId` filter prevents return of other orgs' proposals |

---

## 14. `ResultEntityId` Permanent Linkage

**Implementation recommendation fulfilled:**

> "When a proposal is approved and executed, keep the proposal record permanently linked to the resulting entity (TaskId, FindingId, CAPA reference, etc.)."

The `CopilotActionProposal` entity stores:
- `ResultEntityId` — the Guid of the created entity.
- `ResultEntityType` — the type name (e.g., `"ComplianceTask"`, `"Finding"`).
- `ExecutionNotes` — human-readable summary of what was created.

This enables future:
- **Effectiveness reporting:** `SELECT ResultEntityType, COUNT(*) FROM CopilotActionProposals WHERE Status = Executed`
- **Adoption analytics:** Proposals generated, approved, and executed over time.
- **Governance reporting:** Full chain from Recommendation → Draft → Proposal → Execution.
- **Entity traceability:** Jump from a created task back to the Copilot proposal that triggered it.

---

## 15. Sprint 13E Readiness Assessment

Sprint 13E: **Persona Experience Layer**

| Prerequisite | Status |
|-------------|--------|
| Recommendation data model | ✅ Sprint 13B |
| Draft generation | ✅ Sprint 13C |
| Proposal framework | ✅ Sprint 13D |
| Persona model | ✅ Sprint 13A |
| Approval UI components | ✅ Sprint 13D |
| Audit trail | ✅ Sprint 13D |

### Recommended Sprint 13E Focus Areas:

1. **Persona-specific proposal views** — `ComplianceManager` sees CAPA/Task proposals, `QualityOfficer` sees Findings/Executive, etc.
2. **Notification layer** — push notifications when proposals require user's attention by role.
3. **Proposal dashboard widget** — embedded approval card in persona home screen.
4. **Deep-link integration** — direct navigation from approval card to related evidence/deficiency workspace.
5. **Proposal TTL configuration** — allow per-organization TTL customization.
6. **Batch approval** — approve multiple proposals in one action for high-volume organizations.
7. **Copilot effectiveness metrics** — dashboard showing acceptance rate, execution rate, time-to-approval by proposal type.

---

## 16. Files Created / Modified

### New Files (Backend)
- `Domain/Copilot/CopilotActionProposal.cs` — Aggregate root
- `Domain/Copilot/ProposalAuditEvent.cs` — Audit event entity
- `Domain/Copilot/Enums/ProposalStatus.cs`
- `Domain/Copilot/Enums/ProposalType.cs`
- `Application/Copilot/Contracts/ICopilotActionProposalRepository.cs`
- `Application/Copilot/Contracts/IActionProposalGenerator.cs`
- `Application/Copilot/Contracts/IApprovalPolicyService.cs`
- `Application/Copilot/Contracts/IProposalExecutionService.cs`
- `Application/Copilot/Models/ProposalPayload.cs`
- `Application/Copilot/Models/CreateProposalRequest.cs`
- `Application/Copilot/DTOs/CopilotActionProposalDto.cs`
- `Application/Copilot/DTOs/ProposalAuditEventDto.cs`
- `Application/Copilot/DTOs/ProposalRequestDtos.cs`
- `Application/Copilot/Services/ActionProposalGenerator.cs`
- `Application/Copilot/Services/ApprovalPolicyService.cs`
- `Application/Copilot/Services/ProposalExecutionService.cs`
- `Infrastructure/Copilot/EntityConfigurations/CopilotActionProposalConfiguration.cs`
- `Infrastructure/Copilot/EntityConfigurations/ProposalAuditEventConfiguration.cs`
- `Infrastructure/Copilot/Repositories/CopilotActionProposalRepository.cs`
- `Migrations/20260624160000_Sprint13D_ApprovalCardFramework.cs`
- `Controllers/V1/Copilot/CopilotProposalsController.cs`

### Modified Files (Backend)
- `Infrastructure/Persistence/ComplianceDbContext.cs` — new DbSets + ApplyConfiguration
- `Gateway/Application/Queries/CopilotQueries.cs` — 10 new queries/commands
- `Gateway/Application/Handlers/CopilotQueryHandlers.cs` — 10 new handlers
- `Gateway/Controllers/CopilotController.cs` — 10 new routes
- `Program.cs` — 4 new DI registrations

### New Files (Frontend)
- `models/proposal.model.ts`
- `components/approval-card/approval-card.component.ts + html`
- `components/proposal-list/proposal-list.component.ts + html`
- `components/proposal-detail/proposal-detail.component.ts + html`
- `components/proposal-history/proposal-history.component.ts + html`

### Modified Files (Frontend)
- `services/copilot-api.service.ts` — 9 new API methods
- `state/copilot.store.ts` — 5 new signals, 5 new computed, 9 new actions

---

## Success Criteria Checklist

| Criterion | Status |
|-----------|--------|
| ✓ AI cannot execute workflows | ✅ Policy engine blocks non-human approvals |
| ✓ Human approval required | ✅ Approved status required before execution |
| ✓ Existing commands reused | ✅ `IDeficiencyRemediationTaskService`, `IFindingQueryService` |
| ✓ Proposal lifecycle audited | ✅ `CopilotProposalAuditEvents` table with 6 event types |
| ✓ Recommendations generate proposals | ✅ `ActionProposalGenerator.GenerateFromRecommendationAsync` |
| ✓ Drafts generate proposals | ✅ `ActionProposalGenerator.GenerateFromDraftAsync` |
| ✓ Organization scoped | ✅ All queries filter by `OrganizationId` |
| ✓ Permission aware | ✅ Role-based `ApprovalPolicyService` |
| ✓ Audit compliant | ✅ Immutable event trail, never deleted |
| ✓ Ready for Sprint 13E Persona Experience Layer | ✅ All hooks in place |
