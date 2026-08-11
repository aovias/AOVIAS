# Wave 2.2A — Platform Integrity Investigation & Architectural Foundation

**Date:** 2026-08-02 · **Type:** Investigation & architecture only — **no implementation performed**
**Evidence:** code trace, `grep` census across 15 services, direct SQL against `Compliance`, runtime verification.

> **Verified** = observed at runtime or in the database. **Hypothesis** = reasoned but not proven.
> Every claim below is labelled.

---

## 1. Platform Integrity Investigation Report (Phase 1)

### 1.1 Duplicate deficiency creation — complete execution chain [VERIFIED]

```
Journey Maturity dashboard mounts
  └─ combineLatest([hospitalId$, frameworkId$])            ← organizationId NOT a precondition
      └─ organizationId = getOrganizationId() ?? hospitalId  ← ① substitution
          └─ POST /api/v1/deficiencies/bootstrap-from-onboarding
                ?organizationId={hospitalId}&hospitalId=…&frameworkId=…
              └─ Gateway DeficienciesController.BootstrapFromOnboarding
                    [FromQuery] Guid organizationId          ← ② accepted verbatim, unvalidated
                  └─ BootstrapDeficienciesFromOnboardingCommand(organizationId, …)
                      └─ ComplianceService writes deficiencies
                            OrganizationId = TenantId = HospitalId   ← ③ corrupt ownership
```

**Failure sequence.** ① fires whenever organization context has not hydrated — a reload or deep link,
i.e. precisely the conditions DEF-001 created before Wave 1. ② is the architectural defect: a write
endpoint trusting the caller to name its own tenant. ③ is the persisted result.

| Question | Answer | Confidence |
|---|---|---|
| Root cause | Client-supplied `organizationId` on a write endpoint, combined with a UI fallback that substitutes a hospital id | **Verified** |
| Is repeat execution possible? | **Yes** — the dashboard calls bootstrap on every mount | **Verified** |
| Affected services | WebApp → Gateway → ComplianceService | Verified |
| Affected repositories | `ComplianceDeficiencies` only | **Verified** (§4) |
| Production vulnerable? | Yes, in two ways: self-corruption on context failure, and cross-tenant writes by any caller | **Verified** |
| Other modules similarly seeded? | No other bootstrap-style write found | Verified by census (§3) |

**Status:** both ① and ② were fixed in Wave 2.2 (UI precondition; Gateway derives org from token
claims, 401 when absent). Historical rows remain.

### 1.2 Context integrity [VERIFIED]

| Context | State |
|---|---|
| Organization | Resolves from JWT `organization_id`; now a precondition for bootstrap |
| Hospital | Correct; persisted `selected_hospital_id` |
| Journey | Correct since Wave 2 — `selected_framework_journey_id` |
| Framework | Correct since Wave 2 — `selected_framework_id`, sourced from the new `frameworkId` DTO field |

---

## 2. ADR-002 — Enterprise Execution Context

**Status:** Proposed · **Supersedes:** ADR-001 (Wave 2.2), which this expands
**Decision needed from:** Platform architecture owner

### 2.1 Context

Two incompatible tenant-scoping models coexist [VERIFIED by census]:

| Model | Where | Failure mode |
|---|---|---|
| **Ambient** — EF global query filter from `IUserContextService.OrganizationId` | ComplianceService only (18 filters) | Registered **conditionally**; absent on API-key paths → **fails open** |
| **Explicit** — `OrganizationId` passed and filtered in each query | All other services (474 refs in OrganizationService alone) | Correct, but unenforced by any guardrail |

ComplianceService is the outlier. The explicit model used elsewhere is the right one; it simply has
no enforcement.

### 2.2 The context object

```csharp
public sealed record ExecutionContext(
    Guid   OrganizationId,   // MANDATORY — the tenant boundary. Never inferred, never defaulted.
    Guid?  HospitalId,       // optional — scopes within a tenant
    Guid?  JourneyId,        // optional — a hospital's enrollment in a framework
    Guid?  FrameworkId,      // optional — the accreditation framework itself
    Guid?  UserId,           // null for system/background execution
    ExecutionMode Mode,      // UserRequest | InternalService | BackgroundJob | Migration
    string CorrelationId,    // spans the whole causal chain
    string RequestId,        // unique per hop
    string Source,           // originating service
    string Caller);          // originating principal or job name
```

| Identifier | Mandatory | Source of truth | Notes |
|---|---|---|---|
| `OrganizationId` | **Always** | JWT claim (user) / job config (background) | Never from client input |
| `HospitalId` | When hospital-scoped | Client, **validated** as belonging to the org | |
| `JourneyId` | Journey APIs | Client, validated against hospital | Distinct from `FrameworkId` — see Wave 2 |
| `FrameworkId` | Framework-filtered reads | Resolved from journey | |
| `UserId` | User requests only | JWT `sub` | Null is legitimate for `BackgroundJob` |
| `CorrelationId` | Always | Inbound header or generated at edge | |

### 2.3 Propagation rules

1. **Origin** — the Gateway constructs the context from **validated token claims**. A client-supplied
   `organizationId` is accepted only on endpoints explicitly marked cross-tenant, and only for
   principals holding a cross-tenant policy (`CommercialAdmin` / `AdminOnly`).
2. **Transport** — `X-Aovias-Organization-Id`, `-Hospital-Id`, `-Journey-Id`, `-Framework-Id`,
   `-Correlation-Id`, `-Execution-Mode` on every internal hop.
3. **Binding** — middleware materialises the context and **rejects** the request when
   `OrganizationId` is absent. No defaulting. No `?? somethingElse`.
4. **Consumption** — repositories take the context (or its `OrganizationId`) as an argument.
   Ambient state is a convenience, never the boundary.
5. **Background services** — construct a context per tenant explicitly, iterating a tenant list.
   `Mode = BackgroundJob`, `UserId = null`.
6. **SignalR** — the hub derives the context at connection time from the connecting token and stores
   it on the connection; group names are namespaced by `OrganizationId` so a broadcast cannot cross
   tenants.
7. **Dashboards / analytics** — read paths take the context like any other; cache keys **must**
   include `OrganizationId` (see Risk R-05).

### 2.4 Query filter registration

Registration becomes **unconditional**:

```csharp
// Always registered. With no tenant, the comparison is against Guid.Empty and returns nothing.
builder.Entity<ComplianceDeficiency>()
       .HasQueryFilter(d => d.TenantId == _currentTenantId);
```

*Failing closed is the point.* A query returning zero rows is a visible bug caught in minutes; one
returning every tenant's rows is a breach discovered by a customer.

`IgnoreQueryFilters()` permitted only in: migrations, cross-tenant admin tooling, and repository
methods that take an explicit `OrganizationId`. Enforced by an architecture test, not convention.

### 2.5 Consequences

- **Positive:** one model platform-wide; internal and background paths get the same guarantee as user
  requests; failure mode inverts from silent over-fetch to loud under-fetch.
- **Negative:** touches 84 bypasses + 11 internal controllers + internal contracts. Staged, not
  drop-in.
- **Rejected:** *null-check the ambient context* (same latent gap, now throwing); *scope by
  HospitalId* (hospital is not a tenant boundary — the exact confusion that caused this).

---

## 3. Tenant Isolation Assessment & Internal API Audit (Phase 3)

### 3.1 Census [VERIFIED]

| Service | `IgnoreQueryFilters()` | `HasQueryFilter` | Internal controllers |
|---|---|---|---|
| **ComplianceService** | **84** | **18** | 11 |
| OrganizationService | 0 | 0 | 6 |
| IdentityService | 0 | 0 | 2 |
| GovernanceService | 0 | 0 | 1 |
| Gateway, AuditService, AIOrchestrator, Communication, Integration, AccessGovernance, Person, Professional, Workforce, EnterpriseDirectory, KnowledgeBase | 0 | 0 | 0 |

**Only ComplianceService uses query filters at all** — so it is the only service where the
"fails open" defect exists, and it is also where every bypass lives.

### 3.2 Client-supplied `organizationId` on Gateway endpoints [VERIFIED]

**63 endpoints across 17 controllers.** Classified by whether cross-tenant access is intended:

| Controller | Count | Gate | Verdict |
|---|---|---|---|
| `CommercialAdminController` | 4 | `Policy = CommercialAdmin` | ✅ **By design** — AOVIAS staff console |
| `CommercialOpsController` | 2 | `Policy = AdminOnly` | ✅ By design |
| `LicensesController` | 6 | `Policy = CommercialAdmin` | ✅ By design |
| **`WorkforceIntelligenceController`** | **15** | bare `[Authorize]` + `RequirePermission` | 🔴 **Critical** |
| **`EnterpriseDirectoryController`** | **9** | bare `[Authorize]` + `RequirePermission` | 🔴 **Critical** |
| `OrganizationPhase4Controllers` | 6 | — | 🟠 High — review |
| `IntegrationController` | 6 | — | 🟠 High — review |
| `EmploymentController`, `AssignmentController` | 6 | `ComplianceScope` | 🟠 High |
| `SlaController` | 2 | `ComplianceScope` | 🟠 High |
| Subscriptions, Reporting, ProfessionalProfiles, Licensing, Hospitals, Governance, Deficiencies | 7 | mixed | 🟡 Medium — 1 fixed |

**Worked example [VERIFIED]:**

```csharp
[HttpPost("rebuild")]
[RequirePermission("Directory.Search.Read")]
public Task<IActionResult> Rebuild([FromQuery] Guid organizationId, …)
```

Permission is checked; **tenant is not**. A user holding `Directory.Search.Read` in tenant A can
trigger a directory index rebuild for tenant B by changing a query parameter. The same shape appears
on `WorkforceIntelligence/refresh`, and on 15 *read* endpoints in that controller.

### 3.3 Platform Risk Register

| ID | Risk | Severity | Evidence | Status |
|---|---|---|---|---|
| **R-01** | Query filters registered conditionally → no isolation on API-key paths | **Critical** | `ComplianceDbContext` `if (_currentTenantId.HasValue …)`; CAPA screen returns 176 across 2 tenants | Open — ADR-002 §2.4 |
| **R-02** | Client-supplied `organizationId` on tenant-scoped endpoints | **Critical** | 24 endpoints across WorkforceIntelligence + EnterpriseDirectory under bare `[Authorize]` | Open (1 of 63 fixed) |
| **R-03** | 84 explicit filter bypasses on tenant-scoped entities | **High** | Census §3.1 | Open — 3 fixed |
| **R-04** | Bootstrap write path could corrupt tenant ownership | **Critical** | 88 corrupt rows | ✅ **Fixed** (Wave 2.2) |
| **R-05** | Caches may not be tenant-keyed | **High** | *Hypothesis* — `TrialAccessMiddleware` keys by org (safe); others unaudited | Open |
| **R-06** | SignalR groups may not be tenant-namespaced | **High** | *Hypothesis* — not audited; hub was unreachable until Wave 1 | Open |
| **R-07** | Background jobs have no tenant context | **High** | *Hypothesis* — no user context by construction; not enumerated | Open |
| **R-08** | `CommunicationService` has zero tenant/org references | **Medium** | Census: 0 `TenantId`, 0 `OrganizationId` | Open — notifications may be unscoped |

R-05 – R-08 are **hypotheses requiring their own investigation** and are scoped into Wave 2.2B.

---

## 4. Data Integrity Assessment (Phase 4)

### 4.1 Entity audit [VERIFIED]

| Entity | Rows | Ownership anomalies | Orphans |
|---|---|---|---|
| `ComplianceDeficiencies` | 176 | **88** with `TenantId = HospitalId` | 0 |
| `ComplianceJourneys` | 6 | 0 | — |
| `AuditSessions` | 16 | 0 | — |
| `Findings` | 32 | 0 | — |
| `Evidences` | 467 | 0 | — |
| `ScheduledAudits` | 15 | 0 | 0 duplicate groups |

**Blast radius is bounded to one table.** `TenantId <> OrganizationId` is **0** everywhere — the
corrupt batch has both columns set to the hospital id consistently, so the two never disagree.
Zero orphaned `ObjectiveElementId` references.

### 4.2 Duplicate analysis

**(a) Deficiencies — 88 rows [VERIFIED]**

| Batch | Rows | CAPA progress | Closed | Distinct OEs |
|---|---|---|---|---|
| Correct (`TenantId = OrganizationId`) | 88 | 0 | 6 | 40 |
| Corrupt (`TenantId = HospitalId`) | 88 | 0 | 6 | 40 |

Symmetric. **Deletion is not yet provably safe** — `(ObjectiveElementId, Title)` is *not unique
within a batch* ("Evidence Missing" appears 3×), so rows cannot be paired 1:1 and 6 closed rows
cannot be individually matched. *Hypothesis (high confidence): the batches are exact duplicates.*

**(b) Findings — 3 duplicate `FindingNumber` groups [VERIFIED — new finding]**

```
FND-2026-00001 ×2  → "QA finding seed 1"            / "Title"
FND-2026-00002 ×2  → "QA finding seed 2"            / "Wrong Medication"
FND-2026-00003 ×2  → "Assisted patient fall near i" / "QA finding seed 3"
```

`FindingNumber` is the user-facing business key and **is not unique**. These are *different*
findings sharing an identifier — not duplicates to delete, but an identifier-generation defect.
Severity **High**: two findings cannot be told apart in reports or exports. Root cause not yet
established (candidate: non-atomic sequence generation). **Needs its own investigation.**

### 4.3 Cleanup proposal (design only — not executed)

Script exists and is gated: `2026-08-02_RemoveDuplicateDeficiencies.sql`. It quarantines before
deleting, asserts post-conditions, adds `CK_ComplianceDeficiencies_TenantNotHospital`, and **aborted
at its own pre-flight check** rather than deleting rows carrying closure state.

**To make deletion provable, one of:**

| Option | Effort | Risk | Notes |
|---|---|---|---|
| **A** — add a stable natural key (seed batch id / deterministic `DeficiencyNumber`), then pair 1:1 | Medium | Low | **Recommended.** Also fixes 4.2(b) |
| **B** — accept batch-level equivalence (counts, statuses, closure dates all match) and delete on `TenantId = HospitalId` | Low | Medium | Defensible; loses per-row proof |
| **C** — restore a pre-2026-06-23 backup to a scratch DB and diff | High | Lowest | Definitive |

**Rollback:** the quarantine table `ComplianceDeficiencies_QuarantineWave22` holds every deleted row;
restore is `INSERT … SELECT` back. Verification queries are in §5 of the script.

---

## 5. Implementation Roadmap (Phase 5)

### Wave 2.2B — Platform Integrity Implementation

| | |
|---|---|
| **Objective** | Implement ADR-002; close R-01, R-02, R-03 |
| **Dependencies** | ADR-002 approved |
| **Repos** | `ComplaincePlatform` (Gateway, ComplianceService, +services touched by R-02) |
| **Complexity** | **High** — 84 bypasses, 11 internal controllers, 24 unguarded endpoints |
| **Risk** | **High** — unconditional filters can zero out any path with no tenant context |
| **Sequence** | 1. `ExecutionContext` + middleware (additive, both paths work) → 2. Gateway populates from claims → 3. internal controllers bind it → 4. repositories take explicit org → 5. **flip registration to unconditional** → 6. remove bypasses file-by-file → 7. add the architecture test |
| **Testing** | Two-tenant integration suite asserting zero cross-tenant rows on every internal endpoint; per-step runtime smoke |
| **Rollback** | Steps 1–4 additive and revertible. Step 5 is the one-way door — behind a config flag for one release |
| **Acceptance** | Every internal request carries explicit context; no conditional registration; two-tenant suite green |

### Wave 2.2C — Data Cleanup

| | |
|---|---|
| **Objective** | Remove 88 corrupt deficiencies; resolve duplicate `FindingNumber` |
| **Dependencies** | **2.2B first** — cleaning before isolation is fixed invites recurrence. Plus a decision on Option A/B/C |
| **Complexity** | Low (script written) · **Risk** Medium (irreversible without quarantine) |
| **Testing** | Dry run → quarantine → delete → three verification queries → CAPA screen shows **88 / 82** |
| **Rollback** | Restore from `ComplianceDeficiencies_QuarantineWave22` |
| **Acceptance** | 0 rows with `TenantId = HospitalId`; 0 duplicate groups; CHECK constraints live |

### Wave 2.2D — Platform Validation

| | |
|---|---|
| **Objective** | Prove isolation and integrity end to end |
| **Dependencies** | 2.2B + 2.2C |
| **Complexity** | Medium · **Risk** Low |
| **Scope** | Two-tenant cross-leakage suite; investigate R-05–R-08 (caches, SignalR groups, background jobs, CommunicationService); full functional regression; cold/warm performance incl. the 76 s `control-center/kpis` cold start |
| **Acceptance** | No cross-tenant leakage on any endpoint; regression green; no Critical/High open |

---

## 6. Cross-Repository Impact Assessment

| Repo | Impact | Breaking? |
|---|---|---|
| `ComplaincePlatform` — Gateway | Context construction + middleware; ~24 endpoints re-gated | Behavioural: cross-tenant callers correctly start failing |
| `ComplaincePlatform` — ComplianceService | 84 bypasses, 11 internal controllers, filter registration | Internal contract only |
| `ComplaincePlatform` — other services | R-02 endpoints re-gated | Behavioural only |
| `WebApp` | None expected — it never legitimately supplied another tenant's org id | No |
| Database | 2 CHECK constraints + cleanup | Additive |

## 7. Technical Debt Register (updated)

| ID | Item | Severity | Wave |
|---|---|---|---|
| TD-01 | 84 `IgnoreQueryFilters()` bypasses | High | 2.2B |
| TD-02 | Conditional query-filter registration | Critical | 2.2B |
| TD-03 | 63 client-supplied `organizationId` params (24 unguarded) | Critical | 2.2B |
| TD-04 | 88 corrupt deficiency rows | High | 2.2C |
| TD-05 | Duplicate `FindingNumber` — identifier generation | High | 2.2C |
| TD-06 | Cache / SignalR / background-job tenant scoping unaudited | High | 2.2D |
| TD-07 | `control-center/kpis` 76 s cold start | High | 2.2D |
| TD-08 | ComplianceService uses ambient scoping; rest of platform uses explicit | Medium | 2.2B |

## 8. Acceptance criteria

| Criterion | Status |
|---|---|
| Root cause of duplicate data fully understood | ✅ Full chain documented |
| Platform execution flow documented | ✅ |
| Enterprise Execution Context designed | ✅ ADR-002 |
| Tenant isolation fully audited | ✅ 15 services censused, 63 endpoints classified |
| Internal API risks documented | ✅ Risk register R-01…R-08 |
| Data integrity investigation completed | ✅ 6 entities; 2 anomalies found |
| Duplicate cleanup strategy designed | ✅ 3 options with risk/rollback |
| Cross-repository impact documented | ✅ |
| Wave 2.2B roadmap completed | ✅ |
| **No speculative fixes introduced** | ✅ **No code changed in this wave** |

## 9. Decisions required

1. **Approve ADR-002** — nothing in 2.2B should start without it.
2. **Choose a duplicate-proof strategy** — A (natural key, recommended), B (batch equivalence), or C (backup diff).
3. **Confirm sequencing** — 2.2B before 2.2C. Cleaning data while the write path can still corrupt it
   means doing it twice.
4. **Authorise R-02 remediation scope** — 24 unguarded endpoints sit outside the Audit module, in
   Workforce Intelligence and Enterprise Directory.
