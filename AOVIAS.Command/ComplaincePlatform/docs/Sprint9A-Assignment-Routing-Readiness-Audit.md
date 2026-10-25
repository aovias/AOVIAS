# Sprint 9A — Assignment & Routing Readiness Audit

**Status:** Complete (analysis only — no implementation)  
**Date:** June 2026  
**Next sprint:** Sprint 9B — Assignment & Routing Engine

---

## Executive Summary

### Can PRAMAN intelligently determine…?

| Question | Answer | Confidence |
|----------|--------|------------|
| **Who should handle this deficiency?** | **No** — manual assign or placeholder `"deficiency-engine"`; no rule engine | Low |
| **Who should own this task?** | **Partially** — user picks from org members list; OE metadata suggests role/dept but is not used for resolution | Partial |
| **Who should be notified?** | **No** — events reach Service Bus; only AI consumer reacts; header notifications are mock | Low |
| **Who should be escalated to?** | **No** — journey risk job exists; no hierarchy, no escalation targets | Low |

### Composite readiness for Sprint 9B

| Capability | Readiness |
|------------|-----------|
| Auto-assignment | **Missing** (~10%) |
| Department routing | **Partial** (~40%) |
| Role-based routing | **Partial** (~25%) |
| Escalation | **Minimal** (~10%) |
| Notifications | **Partial** (~25%) |
| Ownership model clarity | **Partial** (~50%) |
| Org hierarchy | **Missing** (~5%) |
| **Overall Sprint 9B readiness** | **~20–25%** |

**Verdict:** PRAMAN has strong remediation workflow plumbing (Finding → Deficiency → Task → Evidence → Verification) but lacks the organizational intelligence layer required for auto-routing. Catalog metadata (OE department + responsible role) is the strongest asset. Multiple parallel department models and absent reporting hierarchy are the primary blockers.

---

## Phase 1 — Organization Structure Audit

### What exists

#### Organization Service (`ComplaincePlatform/OrganizationService`)

| Entity | Key fields | Relationships |
|--------|------------|---------------|
| **Organization** | `Id`, `PlatformId`, `Name`, `Type` | Root tenant |
| **Hospital** | `OrganizationId`, `Code`, `IsOnboarded`, `NABHLevel`, geo | Many per org |
| **OrganizationMember** | `OrganizationId`, `UserId`, `DepartmentId?`, `Role`, profile | Org-scoped membership |
| **OrganizationMemberInvitation** | `OrganizationId`, `HospitalId`, `DepartmentId?`, `Role`, `Email` | Pending invites |
| **MasterDepartment** | Global catalog with `DisplayOrder` | Used in onboarding + member binding |
| **Department** + **Service** | Global legacy lookup | `/common/departments`, `/common/services` |
| **HospitalMasterDepartment** | `(HospitalId, MasterDepartmentId)` | Wizard snapshot only |

#### Identity Service (`ComplaincePlatform/IdentityService`)

| Entity | Fields |
|--------|--------|
| **AppUser** | Extends `IdentityUser<Guid>` + `PlatformId` only |
| **AspNetRoles** | Platform-wide canonical roles |

#### Canonical roles (`Shared.Infrastructure/Auth/ApplicationRoles.cs`)

`USER`, `ORG_ADMIN`, `GROUP_ADMIN`, `SUPER_ADMIN`, `REGULATORY_ADMIN`, `COMPLIANCE_VIEWER`, `AUTHORITY`, `REGULATOR`, `MANAGER`, `SYSTEM`

#### Key APIs

| Endpoint | Purpose |
|----------|---------|
| `GET /api/v1/tenant/{tenantId}/organization-detail` | Org + hospitals + members (Gateway) |
| `GET /api/v1/organization-members/organization/{orgId}` | Member list for task assignee picker |
| `GET /api/v1/common/master-departments` | Onboarding department catalog |
| `GET /internal/users/{userId}/organization-hospital` | **First** org + **first** hospital (naive context) |

### What is missing

| Gap | Impact on routing |
|-----|-------------------|
| **No user–hospital binding** on `OrganizationMember` | Cannot scope assignees to a hospital |
| **No reporting hierarchy** (`ReportsTo`, `ManagerId`) | No escalation chain |
| **No designations / job titles** | Cannot map OE `ResponsibleRole` → person |
| **No Team / workgroup entity** | Cannot route to audit/RCA teams |
| **No org chart API** | No programmatic hierarchy traversal |
| **Hospital departments not exposed at runtime** | `HospitalMasterDepartment` wizard data unused |
| **Member `DepartmentId` stripped from org-detail DTO** | Primary org payload omits department |

### Quality of mappings

| Mapping | Quality |
|---------|---------|
| Role canonicalization (`ApplicationRoles` + legacy map) | **Strong** |
| Org → Hospital → Member CRUD | **Strong** |
| Onboarding wizard → `HospitalMasterDepartments` | **Strong** (write-only) |
| Member department in org-detail read path | **Weak** — stored but not returned |
| User context resolution | **Weak** — first org + first hospital |
| Cross-service department identity | **Weak** — 4+ parallel models |

---

## Phase 2 — Department Readiness

### Parallel department models (fragmentation)

| Model | Location | Type | Used by |
|-------|----------|------|---------|
| **MasterDepartment** | OrganizationService | Global GUID catalog | Onboarding, member `DepartmentId` |
| **Department** + **Service** | OrganizationService | Global legacy | `/common/departments` |
| **HospitalMasterDepartment** | OrganizationService | Hospital join | Wizard only |
| **ComplianceDepartment** | ComplianceService | Org-scoped Code/Name | Control Center, task `DepartmentId` |
| **FindingDepartment** enum | Shared.Infrastructure | HR, Security, Clinical, Admin (4 values) | Findings |
| **Free-text `Department`** | ComplianceDeficiency | String (max 100) | DCC Sprint 8 |
| **OE `Department`** | ObjectiveElement | String label | Master data, task `DepartmentName` |

**No crosswalk table exists between these models.**

### Department coverage matrix

#### Objective Elements (NABH seed — 503 OEs)

| Department label (OE metadata) | OE count | Typical responsible role |
|------------------------------|----------|--------------------------|
| Clinical / Nursing | 110 | Chief Medical Officer |
| Front Office / Clinical | 78 | Admission In-charge |
| Pharmacy | 60 | Chief Pharmacist |
| Facility / Engineering | 55 | Facility Manager |
| Management | 45 | Administrator |
| Infection Control | 44 | Infection Control Officer |
| Quality Management | 36 | Quality Head |
| Human Resources | 30 | HR Manager |
| Quality / Patient Relations | 25 | Quality Manager |
| IT / Medical Records | 20 | IT Manager |

**Coverage:** 100% of seeded OEs have department + role metadata (503/503).

#### Deficiencies

| Field | Type | Auto-populated from OE? |
|-------|------|-------------------------|
| `Department` | Free-text string | **No** — manual via remediation PATCH only |
| `AssignedTo` | User id string | Manual assign or task sync |

#### Tasks

| Field | Type | Auto-populated from OE? |
|-------|------|-------------------------|
| `DepartmentName` | String | **Yes** — copied from OE on generation/ensure |
| `DepartmentId` | GUID | **No** — not populated in auto flows |

#### Findings

| Field | Type | Values |
|-------|------|--------|
| `Department` | Enum | HR, Security, Clinical, Admin only |

### Can PRAMAN identify responsible department?

| Object | Per OE | Per deficiency | Per task |
|--------|--------|----------------|----------|
| **Label-level department** | **Yes** (catalog metadata) | Partial (manual string) | Partial (`DepartmentName` only) |
| **Org-scoped department ID** | **No** | **No** | Partial (field exists, unused) |
| **Department head / default owner** | **No** | **No** | **No** |

**Readiness: Partial** — catalog knows *which department label* owns an OE; platform cannot resolve that to an org-specific department or person.

---

## Phase 3 — User & Role Audit

### Users

| Aspect | State |
|--------|-------|
| Identity | `AppUser` — email, password, `PlatformId`; no profile extensions |
| Org membership | `OrganizationMember` — role + optional `DepartmentId` |
| Hospital scope | **Not on member** — only on invitations |
| Assignee key | `userId` (GUID string) used consistently on tasks/deficiencies |

### Roles vs hospital job titles

| Hospital role (OE metadata) | Platform role equivalent | Mapped? |
|-----------------------------|--------------------------|---------|
| Chief Medical Officer | — | **No designation entity** |
| Chief Pharmacist | — | **No** |
| Quality Head / Manager | `MANAGER` (generic) | **Partial** — onboarding maps "Department Head" → `MANAGER` |
| HR Manager | — | **No** |
| Facility Manager | — | **No** |
| Administrator | `ORG_ADMIN`? | **Ambiguous** |
| Infection Control Officer | — | **No** |
| Nursing Head | — | **No** |
| Medical Superintendent | — | **No** |

Platform roles are **access-control roles**, not **operational designations**. There is no way to query "who is the Quality Head for Hospital X."

### Permissions

Roles gate API access via `[Authorize(Roles = ...)]` and Gateway policies (`ComplianceScope`, etc.). Permissions are **not** department-scoped or object-scoped for assignment.

### Can PRAMAN determine functional heads?

**No.** Would require either:
- Designation entity on `OrganizationMember` (e.g. `Designation = "QualityHead"`), or
- Role-to-department matrix with named default assignees, or
- Explicit `DepartmentOwnership` records (recommended for 9B).

**Readiness: Missing** for operational role resolution.

---

## Phase 4 — Current Task Assignment Audit

### ComplianceTask ownership model

```
AssignedToUserId  (string, required on manual create)
DepartmentId      (Guid?, org department — rarely set)
DepartmentName    (string, denormalized from OE)
SourceDeficiencyId (Guid?, links remediation task)
Status            Pending | InProgress | OnHold | Blocked | Completed
```

### Assignment workflows today

| Workflow | Entry point | Syncs to deficiency? |
|----------|-------------|----------------------|
| Journey task create | `POST /compliance/compliance-tasks` | **No** |
| Task execution patch | `PATCH /tasks/{id}` | **Yes** (lifecycle service) |
| Legacy task patch | `PATCH /compliance/compliance-tasks/{id}` | **No** |
| Deficiency assign API | `POST /deficiencies/{id}/assign` | Deficiency → task (**not wired in UI**) |
| Auto remediation | Outbox on `DeficiencyIdentified` | Creates task with `"deficiency-engine"` placeholder |

### Gaps

1. **Dual PATCH endpoints** — only `/tasks` triggers deficiency lifecycle sync
2. **Reassignment gap** — task owner changes after `Identified` do not update deficiency `AssignedTo`
3. **`DepartmentId` unused** in auto-provision and deficiency flows
4. **Multiple tasks per deficiency** — primary task = newest non-completed; no write-model canonical rule
5. **Placeholder assignee** `"deficiency-engine"` must be filtered from user-facing owner
6. **No routing** — assignee always manual or system placeholder

### Future extension points

- `TaskExecutionService.PatchAsync` → extend `DeficiencyTaskLifecycleService.SyncFromTaskAsync`
- `DeficiencyRemediationTaskService.ApplyMetadata` → sync department + owner on link/create
- Unified assign command with department resolution
- Single primary-task policy shared by read and write paths

**Readiness: Partial** — assignment infrastructure exists; intelligent resolution does not.

---

## Phase 5 — Deficiency Ownership Audit

### ComplianceDeficiency ownership fields (Sprint 8)

| Field | Purpose |
|-------|---------|
| `AssignedTo` | Person owner (string user id) |
| `Department` | Free-text department |
| `AssignedDate`, `TargetResolutionDate`, `ActualResolutionDate` | Ownership timeline |
| `DueDate` | Legacy due date (mirrors target) |

### Synchronization model

```
                    ┌─────────────────────┐
  POST /assign ───► │ ComplianceDeficiency │ ───► task.AssignedToUserId
                    └─────────┬───────────┘
                              │
  PATCH /tasks ───────────────┘ (only if status = Identified)
                              │
  DCC read ◄──────────────────┘ AssignedOwner = AssignedTo ?? primaryTask.AssignedToUserId
```

### Can deficiency ownership be derived from primary task?

| Concern | Assessment |
|---------|------------|
| **Read (DCC)** | Already derived with fallback |
| **Write** | Two sources of truth; sync is conditional |
| **Department** | Cannot derive — string vs GUID, no mapping |
| **Reassignment** | Task changes do not propagate after first assign |

### Conflicts

| Conflict | Detail |
|----------|--------|
| Person owner | Deficiency assign, task patch, auto-provision write independently |
| Department | String on deficiency vs GUID on task |
| Finding assignee | `Guid? AssignedToUserId` vs string elsewhere |
| API not in UI | `assignDeficiency()` exists in service but **zero UI callers** |

**Recommendation for 9B:** Treat **primary remediation task** as execution authority; persist deficiency owner as **projection** with bidirectional sync on all non-terminal statuses.

**Readiness: Partial** — model fields exist; sync incomplete; no auto-resolution.

---

## Phase 6 — Objective Element Ownership Mapping

### Framework hierarchy

```
ComplianceFramework
  └── ComplianceChapter (Code, Name, Weight — no department)
        └── ComplianceStandard (Code, Name, Weight — no department)
              └── ObjectiveElement (Department, ResponsibleRole, Frequency, EvidenceCategory)
                    └── OrganizationComplianceObligation (hospital instance)
```

Department ownership lives **only at OE level** as optional string metadata populated from master data.

### Master data pipeline

`MasterRequirementDto` → `EfMasterDataPersistence` → `ObjectiveElement.Department` + `ResponsibleRole`

Source: `seed-data/source/ObjectiveElements.csv` (503 rows, 10 department labels, 10 role labels).

### Knowledge Base (parallel)

`RequirementDetailDto.ApplicableDepartments` — free-text on KB nodes. Not used for assignment. `OrganizationComplianceObligation.ComplianceNodeId` links to KB optionally.

### Example mappings (catalog level)

| Domain | OE department label | Responsible role |
|--------|---------------------|------------------|
| Medication Management | Pharmacy | Chief Pharmacist |
| Fire Safety / FMS | Facility / Engineering | Facility Manager |
| Training / HR | Human Resources | HR Manager |
| Quality Indicators | Quality Management | Quality Head |
| IPC | Infection Control | Infection Control Officer |
| Clinical care | Clinical / Nursing | Chief Medical Officer |

### Can OEs automatically map to responsible departments?

| Level | Ready? |
|-------|--------|
| OE → department **label** | **Yes** (100% seed coverage) |
| OE → org **department ID** | **No** |
| OE → default **assignee user** | **No** |
| Chapter/Standard → department | **No** (must roll up from OE) |
| OE dept → deficiency dept on create | **No** |

**Readiness: Partial (~40%)** — metadata rich; normalization and org linkage missing.

---

## Phase 7 — Routing Rule Discovery

### No assignment routing engine exists

Searched: `AssignmentRule`, `DepartmentOwnership`, `AutoAssign`, `TaskRouting` — **zero matches**.

Existing "rule" engines serve other domains:

| Engine | Domain |
|--------|--------|
| `RuleEngine` / `RuleConfig` | Readiness scoring thresholds |
| `DeficiencyRuleEngine` | Which deficiency types fire |
| `CopilotIntentRouter` | AI question routing |
| `EventRoutingPolicy` (AI Orchestrator) | Proactive suggestion gating |

### Recommended routing strategies for Sprint 9B

#### Rule Type A — Objective Element Based

```
OE code / OE department label
    → DepartmentOwnership (org-scoped department ID)
    → RoleOwnership or default assignee pool
    → AssignmentTarget (userId)
```

**Feasibility today:** Step 1 label only; steps 2–4 missing.

#### Rule Type B — Deficiency Type / Root Cause Based

```
RootCauseCategory (Sprint 8)
    → department mapping table
    → AssignmentTarget
```

**Feasibility today:** `RootCauseCategory` enum exists; no mapping table.

| Root cause | Suggested department |
|------------|---------------------|
| PolicyMissing | Quality Management |
| EvidenceMissing | Owning OE department |
| TrainingMissing | Human Resources |
| ProcessFailure | Quality Management |
| DocumentationIssue | Owning OE department |
| VerificationFailure | Quality Management |

#### Rule Type C — Finding Source Based

```
Finding.RaisedBy / FindingDepartment
    → Quality team (audit findings)
    → Clinical (clinical findings)
```

**Feasibility today:** `FindingDepartment` enum (4 values); promotion does not apply routing.

### Recommendation matrix

| Rule type | Priority | Data ready | Engine ready | Notify ready |
|-----------|----------|------------|--------------|--------------|
| A — OE based | **P0** | Partial | No | No |
| B — RCA based | P1 | Partial | No | No |
| C — Finding source | P2 | Partial | No | No |
| D — Risk/severity override | P3 | Yes (Sprint 8 risk) | No | No |
| E — Workload balancing | P4 | No | No | No |

---

## Phase 8 — Escalation Readiness Audit

### What exists

| Component | Behavior |
|-----------|----------|
| `ComplianceEscalationJob` | Daily Hangfire; bumps journey `RiskScore` when overdue findings, unresolved NCs, missed deadlines |
| Deficiency risk fields | `RiskScore`, `AssessedRiskLevel`, `DueDate` (Sprint 8) |
| Control Center `Alert` entity | Dashboard signals (not user-targeted) |

### What is missing

| Capability | Status |
|------------|--------|
| Escalation hierarchy (Manager → Dept Head → MS → CEO) | **Missing** |
| `EscalationPolicy` / levels | **Missing** |
| Escalation on assignee non-response | **Missing** |
| Notification on escalation | **Missing** |
| Task/deficiency-level escalation | **Missing** (job is journey-level only) |

### Hierarchy readiness

| Role | Modeled? | Resolvable? |
|------|----------|-------------|
| Manager | No | No |
| Department Head | No (onboarding label only) | No |
| Quality Head | OE metadata label only | No |
| Medical Superintendent | No | No |
| Administrator | OE metadata + `ORG_ADMIN` role | Ambiguous |
| CEO | No | No |

**Readiness: Minimal (~10%)** — detection pattern exists; hierarchy and actions do not. Defer full escalation to **Sprint 10**; define schema in 9B plan.

---

## Phase 9 — Notification Readiness Audit

### Alert system (dashboard intelligence)

| Layer | Path |
|-------|------|
| Domain | `ComplianceService/Domain/Alert.cs` |
| Generation | `ControlCenterAggregationService.BuildAlerts` |
| API | `GET .../control-center/alerts` |

Alert types: ComplianceDrop, CriticalTasks, MissingEvidence, AuditRisk, OverdueCapa, etc. **UI dashboard only** — not routed to users by role/department.

### Event pipeline

```
ComplianceEventEmitter → OutboxMessages → OutboxRelayWorker → Service Bus "compliance-events"
                                                          ↓
                                    ProactiveSuggestionConsumer (AI only)
```

Relevant event types: `TaskAssigned`, `DeficiencyCreated`, `DeficiencyStatusChanged`, `FindingPromotedToDeficiency`, etc.

### User notifications

| Surface | State |
|---------|-------|
| Header bell (`header.component.ts`) | **Hardcoded mock data** |
| Toast service | UX feedback only |
| Email / SMS / push | **Not implemented** |
| User subscription preferences | **Not implemented** |

### Can routing engine trigger notifications?

| Question | Answer |
|----------|--------|
| Events published on assign/create? | **Yes** (outbox + Service Bus) |
| Consumer delivers to users? | **No** (AI consumer only) |
| SLA engine can hook events later? | **Yes** (infrastructure partial) |

**Readiness: Partial (~25%)** — event plumbing exists; user notification layer missing.

---

## Phase 10 — Routing Architecture Recommendation

> Recommendations only — **not implemented** in Sprint 9A.

### Proposed entities (Sprint 9B)

#### `DepartmentOwnership`

Maps compliance catalog to org structure.

| Field | Type | Example |
|-------|------|---------|
| `OrganizationId` | Guid | Tenant |
| `HospitalId` | Guid? | Optional hospital scope |
| `SourceType` | enum | ObjectiveElementCode, ChapterCode, OEDepartmentLabel, RootCauseCategory |
| `SourceKey` | string | `"Pharmacy"`, `"COP"`, `"AAC.5.b"` |
| `DepartmentId` | Guid | Org-scoped department FK |
| `DefaultRole` | string? | `"Chief Pharmacist"` |
| `Priority` | int | Rule precedence |

#### `RoleOwnership`

Maps operational designation to user.

| Field | Type |
|-------|------|
| `OrganizationId`, `HospitalId?` | Guid |
| `DesignationCode` | string (`QualityHead`, `NursingHead`, …) |
| `UserId` | Guid |
| `EffectiveFrom` / `EffectiveTo` | dates |

#### `AssignmentRule`

| Field | Type |
|-------|------|
| `OrganizationId`, `HospitalId?`, `FrameworkId?` | scope |
| `RuleType` | enum (ObjectiveElement, RootCause, FindingSource, RiskOverride) |
| `ConditionJson` | match criteria |
| `TargetType` | enum (User, Role, Department, DepartmentHead) |
| `TargetRef` | string |
| `Priority`, `IsActive` | int, bool |

#### `AssignmentTarget` (resolved output)

| Field | Type |
|-------|------|
| `EntityType` | Task, Deficiency, Finding |
| `EntityId` | Guid |
| `AssignedToUserId` | string |
| `DepartmentId` | Guid? |
| `RuleId` | Guid (audit trail) |
| `ResolvedAt` | datetime |

#### `EscalationPolicy` (schema only — Sprint 10 implementation)

| Field | Type |
|-------|------|
| `Level` | int |
| `TargetDesignation` | string |
| `DelayHours` | int |
| `AppliesTo` | enum (Task, Deficiency, Finding) |

### Proposed resolution service

```
IAssignmentResolutionService
  ResolveForObligation(obligationId) → AssignmentTarget
  ResolveForDeficiency(deficiencyId) → AssignmentTarget
  ResolveForFinding(findingId) → AssignmentTarget
  ApplyAssignment(target) → updates Task + Deficiency projection + emits event
```

### Department unification strategy

1. **Canonical:** `MasterDepartment` (org onboarding) ↔ `ComplianceDepartment` (compliance roll-ups) via explicit mapping table
2. **Normalize** OE string labels → master department codes
3. **Deprecate** free-text deficiency department in favor of FK + denormalized name
4. **Align** `FindingDepartment` enum to master department codes or expand enum

---

## Phase 11 — Gap Analysis

| Area | Ready | Partially Ready | Missing |
|------|-------|-----------------|---------|
| **Auto assignment** | Event hooks for post-assign | Task/deficiency assign fields; OE metadata | Rule engine, target resolution, auto-apply |
| **Routing** | OE dept labels (503/503) | Task `DepartmentName` copy | `DepartmentOwnership`, org dept FK, rule evaluation |
| **Escalation** | Journey risk job pattern | Deficiency risk/due dates | Hierarchy, policies, actions, notifications |
| **Notifications** | Outbox → Service Bus | Dashboard alerts | User notifications, subscriptions, delivery |
| **Ownership** | DCC read projection | Dual-write deficiency/task | Bidirectional sync, canonical primary task |
| **Hierarchy** | Flat org → hospitals → members | Member role + optional dept | Reporting lines, designations, hospital binding |

### Classification legend

- **Ready** — can be used by 9B without schema changes
- **Partially Ready** — exists but incomplete or fragmented
- **Missing** — must be built in 9B or pre-9B foundation sprint

---

## Phase 12 — Sprint 9B Implementation Plan

### Objective

Implement an **Assignment & Routing Engine** that resolves owners from OE metadata, org structure, and configurable rules — without redesigning Tasks or DCC.

### Prerequisites (foundation work in 9B Phase 0)

| # | Work item | Effort | Risk if skipped |
|---|-----------|--------|-----------------|
| 0.1 | Add `DepartmentId` to `MemberDetailResponseDto`, Gateway `MemberDto`, WebApp `OrganizationMember` | S | Routing cannot use member dept |
| 0.2 | Expose `GET /hospitals/{id}/departments` from `HospitalMasterDepartment` | S | Hospital-scoped routing blind |
| 0.3 | Create `MasterDepartment` ↔ `ComplianceDepartment` mapping table | M | FK resolution fails |
| 0.4 | Normalize OE `Department` labels → master department codes | M | Rule Type A brittle |
| 0.5 | Fix bidirectional owner sync in `DeficiencyTaskLifecycleService` | M | Ownership drift continues |
| 0.6 | Add `OrganizationMember.HospitalId` or junction table | L | Multi-hospital routing wrong |

### Sprint 9B phases (proposed)

#### Phase 1 — Schema & domain (Week 1)

- Migration: `DepartmentOwnership`, `RoleOwnership`, `AssignmentRule`, `AssignmentResolutionLog`
- Seed default Rule Type A mappings from OE department labels
- `IAssignmentResolutionService` interface

#### Phase 2 — Resolution engine (Week 2)

- Rule evaluator (priority-ordered, first match wins)
- Target resolvers: `UserResolver`, `RoleResolver`, `DepartmentHeadResolver`
- Unit tests for all 10 OE department labels + RCA rules

#### Phase 3 — Integration hooks (Week 2–3)

| Hook | Trigger |
|------|---------|
| Deficiency create (evaluation engine) | Auto-suggest department + owner |
| `DeficiencyRemediationTaskService.EnsureRemediationTaskAsync` | Apply resolved assignee (replace `"deficiency-engine"`) |
| Finding promotion | Rule Type C for audit findings |
| Manual override | Preserve existing assign APIs; log rule override |

#### Phase 4 — APIs (Week 3)

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/assignment-rules` | CRUD for org admins |
| POST | `/assignment-rules/{id}/test` | Dry-run resolution |
| GET | `/deficiencies/{id}/assignment-suggestion` | Preview before apply |
| POST | `/deficiencies/{id}/auto-assign` | Apply resolution |

#### Phase 5 — UI (Week 3–4)

| Surface | Change |
|---------|--------|
| DCC detail panel | "Suggested owner" + Accept/Override (minimal — not redesign) |
| Task create drawer | Pre-fill assignee from OE resolution |
| Admin settings (new) | Assignment rule management (org admin) |

**Do NOT redesign Tasks page or DCC layout.**

#### Phase 6 — Events & audit (Week 4)

- Emit `AssignmentRuleApplied` compliance event
- Log to `DeficiencyAuditTrail`: `AutoAssigned`, `AssignmentOverridden`
- Prepare notification consumer stub (full delivery = Sprint 10)

### Dependencies

| Dependency | Sprint | Notes |
|------------|--------|-------|
| Sprint 8 deficiency lifecycle | ✅ Complete | Owner/dept/risk fields |
| Sprint 7B finding promotion | ✅ Complete | Promotion hook for Rule C |
| Organization member APIs | ✅ Exists | Needs dept in DTOs |
| Sprint 10 SLA/Escalation | Future | `EscalationPolicy` schema only in 9B |
| Notification service | Future | Event stub in 9B |

### Risks

| Risk | Mitigation |
|------|------------|
| Department model fragmentation | Phase 0 mapping table before rules |
| Wrong auto-assign to user | Dry-run API + manual override + audit log |
| Multi-hospital member ambiguity | Hospital-scoped rules + member-hospital binding |
| `"deficiency-engine"` placeholder confusion | Replace on first successful resolution |
| Over-engineering | Start with Rule Type A only; B/C in 9B stretch |

### Migration strategy

1. Deploy schema + seed read-only rules (inactive)
2. Enable suggestion-only mode (UI preview, no auto-write)
3. Enable auto-assign for new deficiencies only
4. Backfill existing open deficiencies via admin batch job (optional)
5. Enable finding promotion routing last

### Success criteria for Sprint 9B

- [ ] 90%+ of new deficiencies get a suggested owner from OE department
- [ ] Remediation tasks no longer default to `"deficiency-engine"` when rule matches
- [ ] Assignment decisions auditable (rule id + override flag)
- [ ] No breaking changes to manual assign workflows
- [ ] Department FK populated on tasks created from OE resolution

---

## Deliverables Checklist

| # | Deliverable | Location |
|---|-------------|----------|
| 1 | Organization Structure Audit | Phase 1 above |
| 2 | Department Coverage Matrix | Phase 2 above |
| 3 | User & Role Audit | Phase 3 above |
| 4 | Task Assignment Audit | Phase 4 above |
| 5 | Deficiency Ownership Audit | Phase 5 above |
| 6 | Objective Element Ownership Mapping | Phase 6 above |
| 7 | Routing Rule Discovery | Phase 7 above |
| 8 | Escalation Readiness Report | Phase 8 above |
| 9 | Notification Readiness Report | Phase 9 above |
| 10 | Sprint 9B Implementation Plan | Phase 12 above |

---

## Appendix — Key file index

### Organization & identity

- `ComplaincePlatform/OrganizationService/` — org, hospital, member, departments
- `ComplaincePlatform/IdentityService/Domain/AppUser.cs`
- `ComplaincePlatform/Shared.Infrastructure/Auth/ApplicationRoles.cs`
- `ComplaincePlatform/Shared.Infrastructure/Domain/OrganizationMember.cs`

### Assignment & ownership

- `ComplaincePlatform/ComplianceService/Domain/ComplianceTask.cs`
- `ComplaincePlatform/ComplianceService/Domain/ComplianceDeficiency.cs`
- `ComplaincePlatform/ComplianceService/Application/Services/AssignDeficiencyCommandHandler.cs`
- `ComplaincePlatform/ComplianceService/Application/Services/DeficiencyRemediationTaskService.cs`
- `ComplaincePlatform/ComplianceService/Application/Services/DeficiencyTaskLifecycleService.cs`
- `ComplaincePlatform/ComplianceService/Application/Services/DeficiencyCommandCenterQueryService.cs`

### OE metadata & master data

- `ComplaincePlatform/ComplianceService/Domain/ObjectiveElement.cs`
- `ComplaincePlatform/Compliance.MasterData/DTOs/MasterRequirementDto.cs`
- `ComplaincePlatform/seed-data/source/ObjectiveElements.csv`

### Events & notifications

- `ComplaincePlatform/ComplianceService/Application/Events/ComplianceEventType.cs`
- `ComplaincePlatform/ComplianceService/Application/Services/ComplianceEventEmitter.cs`
- `ComplaincePlatform/ComplianceService/Domain/Alert.cs`
- `ComplaincePlatform/ComplianceService/Application/BackgroundJobs/ComplianceEscalationJob.cs`
- `WebApp/src/app/core/layout/header.component.ts`

### Frontend assignment

- `WebApp/src/app/features/journey/services/organization-members-api.service.ts`
- `WebApp/src/app/features/audit/deficiency-command-center/`
- `WebApp/src/app/features/tasks/` (task execution patch)

---

## Success Criteria Verification

| Criterion | Met? |
|-----------|------|
| Clear understanding of ownership model | ✅ Documented dual-write + projection model |
| Clear department mapping strategy | ✅ Unification strategy in Phase 10 |
| Routing opportunities identified | ✅ Rule Types A/B/C/D matrix |
| Escalation readiness assessed | ✅ Minimal — Sprint 10 scope |
| Notification readiness assessed | ✅ Event plumbing partial |
| No implementation assumptions | ✅ Analysis only; 9B plan separate |
| Ready for Sprint 9B | ✅ With Phase 0 foundation prerequisites |
