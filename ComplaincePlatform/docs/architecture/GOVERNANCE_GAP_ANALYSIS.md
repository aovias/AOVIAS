# PRAMAN Governance Gap Analysis — EIWAM Phase 6

| Field | Value |
|-------|-------|
| **Phase** | EIWAM Phase 6 — Governance & Accountability |
| **Prior work** | M6.5 Enterprise Governance & Responsibility Framework |
| **Status** | Investigation complete — implementation authorized |
| **Bounded context** | GovernanceService (evolve in place) |

## Executive summary

EIWAM Phase 6 largely overlaps with **M6.5**, which introduced `GovernanceService`, explainable accountability (responsibilities, committees, approval/escalation chains, timeline), `Shared.Governance` SDK, Gateway partial proxy, and Angular read shells. Phase 6 **evolves** this foundation — it does not create a competing service.

**Governance answers accountability (who owns the outcome). AccessGovernance answers authorization (who may act).** These must never be conflated.

---

## Investigation scope

| Area | Locations reviewed |
|------|-------------------|
| Governance domain | `GovernanceService/Domain/` |
| Application services | `GovernanceService/Application/Services/GovernanceAppServices.cs` |
| REST APIs | `GovernanceService/Controllers/V1/GovernanceControllers.cs` |
| SDK | `Shared.Governance/` |
| Gateway | `Gateway/Application/Handlers/GovernanceProxyHandlers.cs` |
| Compliance ownership | `ComplianceService/Domain/DepartmentOwnership.cs`, deficiency/task owner fields |
| Audit ownership | `AuditService/Domain/` |
| Access governance boundary | `AccessGovernanceService` responsibility writes throw |
| Angular | `WebApp/src/app/features/governance/` |

---

## Already implemented

### Core domain (M6.5)

| Phase 6 spec | Implementation |
|--------------|----------------|
| ResponsibilityAssignment | `GovernanceAssignment` under `Responsibility` aggregate |
| GovernanceRole | `ResponsibilityType` lookup (master data) |
| Committee | `Committee` + `CommitteeMembership`, `CommitteeMeeting`, `CommitteeDecision` |
| ApprovalChain / ApprovalStep | `ApprovalChain` + `ApprovalStep` |
| EscalationChain / EscalationRule | `EscalationChain` + `EscalationLevel` |
| GovernanceTimeline | `GovernanceTimelineEntry` + `GovernanceTimelineProjector` |

### Responsibility engine

- CRUD responsibilities (`GET/POST/PUT/DELETE /responsibilities`)
- Assign governance (`POST /responsibilities/{id}/assign`)
- Primary owner enforcement, overlap detection, effective dating
- Polymorphic assignee: `AssigneeType` + `AssigneeId` (string lookup-driven)
- Owner lookup: `GET /internal/governance/owners`

### Committees

- Create committee, add member, schedule meeting, record decision
- Term validation on committee creation
- Outbox: `CommitteeCreated`, `CommitteeMemberAdded`, `CommitteeDecisionRecorded`

### Approval / escalation

- Create and publish approval/escalation chains
- Sequential + conditional approval evaluation (foundation)
- Escalation level advance with loop prevention
- Internal APIs: `/internal/governance/approval/evaluate`, `/internal/governance/escalation/resolve`

### Timeline and analytics

- `GET /governance/timeline` with resource/org filters
- `GET /governance/analytics/summary`

### Boundary with AccessGovernance

- Gateway `/api/v1/responsibilities` → GovernanceService (canonical)
- AccessGovernance `ResponsibilityAppService` throws on write/list
- M6.4 `ResponsibilityAssignments` table deprecated (read-only)

### Tests

- 9 unit tests in `GovernanceService.Tests/GovernanceGovernanceTests.cs`

---

## Reusable (extend, do not duplicate)

| Asset | Reuse pattern |
|-------|---------------|
| Person / Employment / Organization | HTTP validation clients (read-only); no PII storage in Governance |
| AccessGovernance roles | Optional assignee validation when `AssigneeType = Role` |
| Compliance `DepartmentOwnership` | Migrate read path to `IGovernanceClient.GetOwnersAsync` |
| Compliance `EscalationPolicy` | Future: resolve via `IEscalationResolver` |
| Outbox pattern | Same as OrganizationService / AccessGovernance Phase 5 |
| Gateway MediatR proxy | Mirror Organization Phase 4 / Access Governance Phase 5 handlers |

---

## Needs enhancement

| Area | Current | Phase 6 target |
|------|---------|----------------|
| **Responsibility types** | Partial seed (ProcessOwner, EvidenceOwner, CapaOwner) | Full healthcare catalog (NABH Coordinator, Clinical Lead, AI Supervisor, etc.) |
| **Assignment targets** | Person/Employment via assignee type string | Validate Position, ProfessionalCategory, Department, Committee assignees |
| **Primary reassignment** | Primary uniqueness on assign | Emit `OwnershipChanged` outbox + timeline |
| **Committees** | Create/list only at Gateway | GET `{id}`, PUT, chair/secretary on create/update |
| **Meetings** | Schedule + decide only | Lifecycle (Scheduled/Held/Completed), minutes, attendance JSON |
| **Decisions** | Summary + resource link | Owner, implementation status, linked CAPA/Risk refs |
| **Approval engine** | Sequential break after first step | Parallel step groups; `Completed`/`Approved` when all satisfied |
| **Escalation** | Level advance only | `DelayHours`, severity filter, manager-chain via WorkforceService |
| **Timeline** | `PayloadJson = "{}"` | Structured JSON per event type |
| **Outbox processor** | Heartbeat stub | Mark processed (AccessGovernance pattern) |
| **GovernanceAuditLog** | Schema only | Write on all mutations |
| **Gateway** | ~40% endpoint coverage | Full proxy for assign, committees, chains, evaluate, resolve |
| **Angular** | Read-only shells | CRUD forms, assign, ownership explorer |
| **Shared.Governance** | Package complete | Register in ComplianceService |

---

## Missing

| Capability | Impact |
|------------|--------|
| **GOVERNANCE_GAP_ANALYSIS.md** | This document (mandatory gate) |
| **Phase 6 domain model framing** | `GOVERNANCE_DOMAIN_MODEL.md` still labeled M6.5 only |
| **Integration validation clients** | Person/Workforce/Organization HTTP clients in GovernanceService |
| **ApprovalCompleted event** | Not emitted on workflow completion |
| **CommitteeMeetingCompleted event** | Not emitted |
| **OwnershipChanged event** | Not emitted on primary reassignment |
| **Compliance consumer pilot** | No `AddPramanGovernance()` in ComplianceService |
| **governance-consumer-playbook.md** | Module adoption guide |
| **Integration tests** | Gateway round-trip, Compliance owner lookup |
| **AuditService ownership** | No owner fields — future governed-resource hooks only |

---

## Technical debt

1. **Dual responsibility storage**: M6.4 `ResponsibilityAssignments` in AccessGovernance DB vs Governance `GovernanceAssignments` — Governance is canonical; legacy table read-only.
2. **Compliance embedded ownership**: `DepartmentOwnership`, deficiency `CorrectiveActionOwner`, task `AssignedToUserId` — local until incremental migration.
3. **Compliance local escalation**: `EscalationPolicy` / `EscalationService` parallel to Governance `EscalationChain` — document coexistence during migration.
4. **Approval evaluator incomplete**: Never returns `Completed=true, Approved=true` for satisfied workflows.
5. **Gateway route inconsistency**: Some routes under `/governance/approval-chains` vs spec `/approval/evaluate`.
6. **Angular placeholders**: Committee members/meetings pages, ownership explorer non-functional.
7. **Outbox no RabbitMQ**: Platform-wide — processor marks processed only.
8. **Empty lookup categories**: ApprovalLevel, EscalationType, MeetingType seeded as categories without values.

---

## Compliance ownership field inventory

| Entity / Service | Ownership fields | Phase 6 stance |
|----------------|------------------|----------------|
| `DepartmentOwnership` | PrimaryOwnerUserId, BackupOwnerUserId | Pilot migration to governance owners API |
| `ComplianceDeficiency` | AssignedTo, CorrectiveActionOwner, PreventiveActionOwner, EffectivenessReviewer | Future governed-resource assignments |
| `ComplianceTask` | AssignedToUserId | Future task owner responsibility type |
| `ControlledDocument` / `Incident` / `ClinicalIndicator` | OwnerUserId | Link-only via Responsibility |
| `DocumentApproval` | ApproverRole, ApproverUserId | Workflow — use ApprovalChain |
| `EvidenceReview` | ReviewerUserId | Reviewer responsibility type |
| `EscalationService` | Local policy levels | Coexist; migrate incrementally |

## AuditService

No owner/reviewer/approver fields in domain — accountability for audit findings will use governed-resource references (`AuditFinding`, `CorrectiveAction`) in Phase 6+ integration.

---

## Permission vs responsibility matrix

| Question | Service | Mechanism |
|----------|---------|-----------|
| Can user edit this policy? | AccessGovernance | Permission + `RequirePermission` |
| Who owns this policy outcome? | GovernanceService | Responsibility + GovernanceAssignment |
| Who must approve CAPA closure? | GovernanceService | ApprovalChain template |
| Can user click Approve button? | AccessGovernance | `ApprovalAuthority` or permission |
| Who receives SLA escalation? | GovernanceService | EscalationChain |

---

## Definition of Done checklist

- [ ] Gap analysis and domain model documents complete
- [ ] Responsibilities independent of permissions (documented + enforced)
- [ ] Committees first-class with meetings and decisions
- [ ] Approval/escalation engines deepened
- [ ] Gateway full proxy coverage
- [ ] Outbox processor + audit log + timeline payloads
- [ ] Compliance governance consumer pilot
- [ ] Angular CRUD screens
- [ ] Expanded tests + sequence diagrams

---

## Related documents

- [GOVERNANCE_DOMAIN_MODEL.md](./GOVERNANCE_DOMAIN_MODEL.md)
- [governance-api-flows.md](./governance-api-flows.md)
- [ACCESS_GOVERNANCE_GAP_ANALYSIS.md](./ACCESS_GOVERNANCE_GAP_ANALYSIS.md)
