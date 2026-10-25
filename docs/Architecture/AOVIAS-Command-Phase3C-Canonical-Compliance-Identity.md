# AOVIAS Command — Phase 3C Canonical Compliance Identity

**Phase:** 3C — analysis and design only  
**Status:** Design complete. No production code, schema, or data changes.  
**Worktree:** `V:\DevelopmentRepo\AOVIAS.Cursor`  
**Branch:** `agent/cursor/phase3c-canonical-obligation-identity`  
**Starting HEAD (Phase 3B):** `4bff4899d0a045876e2e5a1213b0fb079b4795f0`  
**Contracts:** [Phase 1 map](./AOVIAS-Command-Module-Dependency-Map.md), [Phase 2 audit](./AOVIAS-Command-Phase2-Data-Integrity-Audit.md), [Phase 3B reconciliation](./AOVIAS-Command-Phase3B-Data-Reconciliation.md)

**Non-goals:** migrations, uniqueness changes, twin deletion, event/outbox product work (Phase 3D+).

---

## 1. Executive summary

An `OrganizationComplianceObligation` is **not** “one row per organization × objective element” in product behavior. Enrollment, journey APIs, finding promotion, onboarding skip checks, and task filters all treat it as **a hospital’s operational projection of one catalog Objective Element inside one enrolled framework**.

The unique index `UX_OrganizationComplianceObligations_OrgOE` `(OrganizationId, ObjectiveElementId)` is a **schema comment from Phase 48**, not the runtime locate key. It is why:

- a second hospital in the same organization cannot insert another row with the true `OrganizationId`;
- historical enroll/sync copied `HospitalId` into `OrganizationId` to insert a second wave (the 1006 twins).

**Recommended business key (for Phase 3D):**  
`HospitalId + FrameworkId + ObjectiveElementId`  
with `OrganizationId` and `TenantId` required, always equal to `OrganizationService.Hospital.OrganizationId`, never equal to `HospitalId`.

`ObjectiveElement.Id` is globally unique and belongs to **exactly one** framework in this database (`MaxFrameworksPerOeId = 1`). `FrameworkId` on the obligation is therefore determined by the OE catalog, but it is still part of **enroll/journey grain** and must stay in uniqueness so hospital × framework lists cannot collapse.

The 1006 twins are **not** uniformly safe to delete: **41 pairs have diverged status**, and **88 deficiencies** (plus **1 task** via deficiency) point at twin PKs vs **89 deficiencies** at MATCH PKs.

---

## 2. Current obligation identity

Entity: `OrganizationComplianceObligation` (`Domain/OrganizationComplianceObligation.cs`).

| Field | Role |
| --- | --- |
| `Id` | Surrogate PK |
| `TenantId` | EF global query filter (`TenantId == TenantScope`) |
| `OrganizationId` | Unique index half; list APIs for “org × framework”; assessment lookup |
| `HospitalId` | Journey/internal list filter; projection by hospital+OE |
| `FrameworkId` | Required; FK to `ComplianceFrameworks`; journey grain |
| `ChapterId` / `StandardId` | Denormalized from OE at sync |
| `ObjectiveElementId` | Unique index half; FK to `ObjectiveElements` |
| `ComplianceNodeId` | Nullable KB soft GUID; **always null** in live data |
| Status / scores / dates | Operational state, not identity |
| `CreatedBy` / `UpdatedBy` | Always `system` on insert from sync |
| Soft-delete | **None** |
| Navigations | `Framework`, `ObjectiveElement` only |

**CURRENT IDENTITY MODEL**

```text
OrganizationComplianceObligation
  PK           = Id (GUID)
  Unique       = (OrganizationId, ObjectiveElementId)   -- UX_OrganizationComplianceObligations_OrgOE
  Query grain  = mixed: (OrganizationId, FrameworkId) OR (HospitalId, FrameworkId) OR (HospitalId, ObjectiveElementId) OR Id
  Business key = UNDECLARED — schema says org×OE; product APIs say hospital×framework×OE
  Scope        = TenantId filter + IgnoreQueryFilters on almost all obligation code
```

PK is **not** the business key. `GetByKeyAsync` uses org+OE and has **no callers** outside the repository.

---

## 3. Current schema / indexes

Created by migration `20260619091401_Phase48_OrganizationComplianceObligation`.

| Name | Columns | Unique |
| --- | --- | --- |
| `PK_OrganizationComplianceObligations` | `Id` | yes |
| `UX_OrganizationComplianceObligations_OrgOE` | `OrganizationId`, `ObjectiveElementId` | **yes** |
| `IX_…_Tenant_Hospital_Framework` | `TenantId`, `HospitalId`, `FrameworkId` | no |
| `IX_…_Org_Framework_Maturity` | `OrganizationId`, `FrameworkId`, `MaturityLevel` | no |
| `IX_…_Org_ImplementationStatus` | `OrganizationId`, `ImplementationStatus` | no |
| `IX_…_TenantId` | `TenantId` | no |
| FKs | `FrameworkId`, `ObjectiveElementId` | restrict |

**HospitalId is absent from uniqueness** because the Phase 48 comment states “one obligation per (Organization × ObjectiveElement).” That matches `IObligationSynchronizationService` XML (“for an organization”) and `UpsertBatchAsync` / sync skip by `OrganizationId + ObjectiveElementId`.

There is **no** unique on `(HospitalId, FrameworkId, ObjectiveElementId)`. Live data **violates** that grain: 503 OE keys × 2 hospitals = 1006 duplicate hospital×framework×OE pairs.

---

## 4. Creation-path analysis

**Only insert site in application code:** `ObligationSynchronizationService` building `new OrganizationComplianceObligation { … }` then `ObligationRepository.UpsertBatchAsync`.

| Path | HospitalId | OrganizationId | TenantId | FrameworkId | OE | NodeId | Uniqueness | Behavior |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Enroll (`EnrollHospitalInFrameworkCommandHandler`) | route hospital | Phase 3A: Org hospital’s org; **pre-3A: `command.HospitalId`** | same as org arg | route FW | all OEs under FW | always null | skip existing org+FW OE ids | insert-only |
| Internal `POST /internal/v1/obligations/sync` | body hospital, **or OrganizationId if empty** | body org | body tenant or org | body FW | same | null | same | insert-only |
| Onboarding `ApplyAsync` | wizard hospital | wizard org | org | wizard FW | then assessment **updates** by org+OE | null | sync first | update by org+OE |
| Seeding/migrations | none found that INSERT obligations | — | — | — | — | — | — | catalog only |
| Admin UI | none | — | — | — | — | — | — | — |

Retry: sync is insert-if-missing; never updates existing rows. Second enroll of same hospital×FW throws already-enrolled **before** sync.

**Inconsistent identity:** pre-3A enroll and empty-hospital internal sync set `OrganizationId`/`TenantId`/`HospitalId` to the **same GUID**. Phase 3A enroll sets org from OrganizationService. Internal sync **still** can collapse hospital to org if `HospitalId` omitted.

---

## 5. Framework identity analysis

- `ObjectiveElement` unique: `(ComplianceStandardId, Code)` — **not** globally unique by code.
- Each OE `Id` maps to **one** `Chapter.FrameworkId` (SQL: max 1 framework per OE id; 1341 OEs).
- **503 codes** appear in **more than one framework** (separate OE rows, e.g. `AAC.1.a`).
- One hospital **can** enroll in two frameworks: journey `5cd1ec4a-…` has 2 journeys / 2 frameworks. **CONFIRMED BY CODE + DATA.**
- Therefore **FrameworkId is part of enroll identity**. Because OE ids do not cross frameworks, `HospitalId + ObjectiveElementId` would also separate those rows today.
- `ComplianceNodeId` is **not** used for identity (null 2662/2662). KB codes are a **fallback lookup** in assessment (`UpdateObligationFromAssessmentCommandHandler` org+code). Phase 1 item 13 (KB framework GUID fallback) remains a **lookup hazard**, not an obligation PK.

---

## 6. Hospital / organization scope

**CONFIRMED BY CODE**

- `ComplianceJourney` is `TenantId + OrganizationId + HospitalId + FrameworkId` (hospital enrolls a framework).
- Gateway `GET /api/v1/journey/obligations?hospitalId=&frameworkId=` → internal list **`HospitalId + FrameworkId`**.
- `IObligationQueryService` lists by **`OrganizationId + FrameworkId`** (no hospital). That is a **second, conflicting** grain.
- Tenant GQF: `obligation.TenantId == TenantScope`. Most obligation queries **IgnoreQueryFilters**.
- OrganizationService owns hospital→org. Phase 3A authorizer enforces that for HTTP. Obligation **rows** are not re-validated after insert.

**Canonical rule (proposed, matches Org authority + journey product):**

```text
HospitalId     is authoritative for facility / operational scope.
OrganizationId is authoritative for tenant / organization scope.
TenantId       must equal OrganizationId (current MATCH rows already do this).
OrganizationId must equal OrganizationService.Hospital.OrganizationId for HospitalId.
Neither OrganizationId nor HospitalId may be taken from an unvalidated caller GUID.
If they disagree: treat as integrity failure (do not silently prefer URL).
Validation owner: OrganizationService (lookup) + Compliance authorization boundary (Phase 3A).
```

`OrganizationId` is **not** redundant: JWT/org reports, GQF, and `IObligationQueryService` use it. It **must not** be a copy of `HospitalId`.

Hospital deactivate/delete: **BUSINESS DECISION REQUIRED** — no cascade in Compliance.

---

## 7. Consumer analysis

Backend files that query `OrganizationComplianceObligations` (excluding EF snapshots/migrations):

| Consumer | Locate / scope keys |
| --- | --- |
| `ObligationSynchronizationService` | skip: OrganizationId + FrameworkId + OE |
| `ObligationRepository` | org+FW; **GetByKey org+OE**; upsert org+OE |
| `ObligationQueryService` | OrganizationId + FrameworkId |
| `InternalJourneyObligationsController` | HospitalId + FrameworkId; detail by **Id** |
| `InternalObligationsController` | status by **Id**; validate OrganizationId + FrameworkId; sync org/hospital |
| `JourneyWorkspaceAppService.LoadObligationsByOeIdAsync` | HospitalId + OE; **GroupBy OE First()** if twins |
| `ObligationProjectionService` | Id; or HospitalId + OE **FirstOrDefault** |
| `UpdateObligationFromAssessmentCommandHandler` | OrganizationId + OE, else OrganizationId + Code |
| `OnboardingObligationAssessmentService` | sync then org+OE updates; skip if HospitalId+FW+OE assessed |
| `FindingPromotionService` | HospitalId + FrameworkId + OE (via journey) |
| `TaskQueryService` | HospitalId + OE (existence Any) |
| `DeficiencyTaskLifecycleService` | obligation **Id** from deficiency FK |
| `EvaluateObligationDeficienciesCommandHandler` | obligation **Id** + tenant filter |
| `AuditorTraceabilityService` | (hospital/org as implemented) |
| `EvidenceReviewService` / `EvidenceRegistrationService` | join via OE/hospital |
| `StageUnlockingService` / `StageUnlockCriteriaEvaluator` | journey/hospital maturity vs obligations |
| `DeficiencyEvaluationService` / `DeficiencyRuleEngine` / `DeficiencyCommandCenterQueryService` | obligation/deficiency FKs |
| `ObjectiveJourneyAppService` | related |
| Gateway `JourneyObligationsController` + handlers | hospitalId + frameworkId + obligation Id |
| Gateway onboard/deficiency apply-assessment | org + hospital + framework |
| WebApp journey | `obligationId`, OE map; does not query SQL |

**Consumers inspected:** 22 ComplianceService application/controller/repository types + 4 Gateway types + WebApp journey obligation UI (indirect). **Hangfire jobs:** no direct `OrganizationComplianceObligation` references found.

**APIs inspected:** journey obligations overview/list/detail/readiness; internal obligation status/sync/validate/onboarding apply; Gateway journey + onboard apply. **12** HTTP surfaces.

---

## 8. 1006 twin analysis

Pair definition: same `HospitalId + FrameworkId + ObjectiveElementId`; one row `OrganizationId = Hospital.OrganizationId` (MATCH); one row `OrganizationId = HospitalId` (TWIN). Same framework (NABH Full `0fe8a9c0-…`).

| Check | Result |
| --- | --- |
| Same FrameworkId / ObjectiveElementId | yes |
| Identical implementation/assessment/journey/evidence/verification | **965 / 1006** |
| Diverged those statuses | **41 / 1006** (40 hospital A `d9f5554a-…`; 1 hospital B `f17fc194-…`) |
| Deficiencies FK `ComplianceObligationId` | **88 TWIN PKs, 89 MATCH PKs** |
| Tasks via `SourceDeficiencyId` | **1 MATCH**, 0 TWIN |
| Evidence / dashboard / journey rows on twin hospital GUIDs as missing hospitals | N/A for these two real hospitals |

`JourneyWorkspaceAppService` and `ObligationProjectionService` using `HospitalId + OE` **First/FirstOrDefault** are **non-deterministic** when twins exist.

**Classification (domain-consumer, not schema-only):**

- **965:** TRUE DUPLICATE *state* — still two PKs; deficiencies may reference either.  
- **41:** AMBIGUOUS / DIVERGED — not safe to pick MATCH blindly.  
- **0:** valid distinct business records (same hospital × same OE × same framework).

Phase 3B “confirmed duplicates” is **correct for grain**, **incorrect as blanket delete-the-twin**.

---

## 9. 650 missing-hospital obligations

| HospitalId | Rows | Provenance |
| --- | ---: | --- |
| `f653e630-c9a9-4014-918d-23b33de93845` | 325 | Phase 2 audit enroll |
| `edb9321a-f922-4cbc-8c1c-1d2c0d5dc1ab` | 325 | Accidental `:5016` enroll |

Downstream (reconfirmed): journeys 0, tasks 0, evidence 0, deficiencies 0, snapshots/dashboard 0, OE ownership 0. Outbox `FrameworkEnrolled` payloads are timestamp-only.

**Cleanup after identity work:** safe *candidates* (no child FKs). Still **approval + allowlist**. Do not clean in 3C.

---

## 10. Candidate identity matrix

| Candidate identity | Evidence supporting | Evidence against | Consequence |
| --- | --- | --- | --- |
| Organization + OE | Unique index; `GetByKeyAsync`; assessment handler; sync skip; type name; Phase 48 comment | Journey APIs hospital×FW; two hospitals one org blocked; twins exist; enroll is hospital×FW | Second facility cannot comply independently |
| Hospital + OE | Projection, workspace, task filters; OE id → one framework | Enroll/list always pass FrameworkId; weaker if OE ever shared across FW | Works **today** given catalog |
| **Hospital + Framework + OE** | Journey unique grain; internal list; finding promotion; onboarding assessed check; live two-FW hospital | Unique index does not match; `IObligationQueryService` omits hospital | Matches product; requires dropping Org+OE unique |
| Organization + Framework + OE | Query service; validate API; maturity index | Same org two hospitals still collide; twins used hospital-as-org | Still org-scoped, not facility-scoped |

**Selected model:** Hospital + Framework + ObjectiveElement, plus required tenant `OrganizationId`/`TenantId` aligned to Org master.

---

## 11. Recommended canonical identity

```text
Business key     = (HospitalId, FrameworkId, ObjectiveElementId)
Surrogate PK     = Id (unchanged)
Tenant           = OrganizationId = TenantId = OrganizationService.Hospital.OrganizationId
Facility         = HospitalId (Org hospital, Active/usable per Phase 3A)
Catalog          = ObjectiveElementId (globally unique row; implies FrameworkId)
FrameworkId      = required denormalized enroll scope (must match OE’s chapter.framework)
ComplianceNodeId = optional KB correlation; NOT identity
```

---

## 12. Proposed schema / index changes (not implemented)

**Current:** PK `Id`; unique `(OrganizationId, ObjectiveElementId)`.

**Proposed:**

- PK **unchanged**.
- **Drop** `UX_OrganizationComplianceObligations_OrgOE` after data can satisfy hospital grain (two hospitals, one org, same OE).
- **Add unique** `UX_OrganizationComplianceObligations_Hospital_Framework_OE` on `(HospitalId, FrameworkId, ObjectiveElementId)`.
- Keep `IX_Tenant_Hospital_Framework`.
- Keep FKs to Framework and OE.
- Keep `OrganizationId`/`TenantId` **required**, not nullable.
- Optional check constraint (later): `OrganizationId <> HospitalId` AND `TenantId = OrganizationId` — only after twins gone.
- Do **not** add HospitalId FK to Organization DB (cross-database).

**Why HospitalId in uniqueness:** operational facility; journey APIs; prevent twins; allow multi-hospital orgs.  
**Why FrameworkId in uniqueness:** enroll/journey grain; 503 shared codes across frameworks as distinct OE ids; defense in depth.  
**Why OE is globally unique as Id:** PK of `ObjectiveElements`; not globally unique as `Code`.

---

## 13. API impact

Must change **semantics** in Phase 3D (not this phase):

- `IObligationQueryService` / validate: add **HospitalId** or stop claiming org×FW uniqueness.
- `GetByKeyAsync` / assessment handler: locate by **HospitalId (+ FrameworkId) + OE**, not org+OE (org+OE updates only the MATCH row and ignores the twin, or the reverse if org was hospital GUID).
- Internal overview DTO today sets `OrganizationId = hospitalId` — **wrong**; should return true org.
- Journey list already hospital×FW — will **double-count** until twins resolved (1006 vs 503).
- Detail-by-`Id` remains valid.

WebApp uses `obligationId` and OE maps; hospital comes from journey context.

---

## 14. Migration strategy (design only)

**Do not** “delete duplicates then add index” as a first step.

1. Freeze canonical identity (this document).  
2. Inventory conflicts: 1006 hospital×FW×OE pairs; 41 diverged; 88+89 deficiency FKs.  
3. **Approve merge policy** for 41 diverged rows (which status wins; whether to union evidence dates).  
4. **Remap** `ComplianceDeficiencies.ComplianceObligationId` (and any other FK to obligation `Id`) to the surviving row.  
5. Delete or archive **only** twin rows whose state is merged and FKs remapped.  
6. Delete/archive **650** missing-hospital rows (separate approval).  
7. Backfill remaining `OrganizationId`/`TenantId` from OrganizationService.  
8. **Then** drop Org+OE unique and add Hospital+FW+OE unique (will fail if any pair remains).  
9. Change sync/assessment/query to the new key; keep Phase 3A hospital validation.  
10. Rebuild dashboard/obligation projections; re-run Phase 2/3B integrity queries.

---

## 15. Data cleanup dependency

Phase 3D implementation **blocks** on:

- Business merge rules for **41** diverged twins.  
- Approval to remap **88** twin-backed deficiencies.  
- Approval to remove **650** artifact/accidental rows.  
- Product confirmation of multi-hospital-per-org (code already assumes hospital enroll).

---

## 16. Business decisions required

| Question | Status |
| --- | --- |
| Can two hospitals in one org both have the same OE obligation? | **CONFIRMED BY CODE** that the product enrolls **hospitals**, not orgs. Schema currently **forbids** true-org second hospital. **BUSINESS** should explicitly accept hospital-scoped obligations. |
| Can one hospital have the same OE **code** across two frameworks? | **CONFIRMED BY CODE** (distinct OE ids; hospital `5cd1ec4a-…` has two FWs). |
| Is FrameworkId part of business identity? | **CONFIRMED BY CODE** for enroll/journey; **redundant given OE id** in current catalog. Recommend keep in unique key. |
| Is ObjectiveElementId globally unique? | **CONFIRMED:** Id yes; Code no. |
| Is ComplianceNodeId required to identify an OE? | **CONFIRMED:** no (always null). |
| Is OrganizationId redundant? | **CONFIRMED:** required for tenant; must match Org master, not HospitalId. |
| Historical OrganizationId snapshot if hospital moves org? | **BUSINESS DECISION REQUIRED** (rows are insert-time copies; no update path). |
| Hospital removed/deactivated? | **BUSINESS DECISION REQUIRED**. |
| Merge policy for 41 diverged twins? | **BUSINESS DECISION REQUIRED**. |
| Which deficiency FK survivor? | **BUSINESS DECISION REQUIRED**. |

---

## 17. Risks

- Adding hospital unique **before** twin collapse **fails**.  
- Deleting twins **before** deficiency remap **orphans 88 deficiencies**.  
- Changing assessment to hospital+OE without collapsing twins still **FirstOrDefault** races.  
- Keeping Org+OE unique **prevents** legitimate multi-site orgs.  
- Internal sync hospital-default **recreates** org=hospital rows.

---

## 18. Phase 3D implementation plan

1. Human approve this identity + merge policy.  
2. Data: remap FKs → collapse twins → optional 650 delete.  
3. Schema: drop Org+OE unique; add Hospital+FW+OE unique; optional check constraints.  
4. Code: sync skip/upsert by hospital+FW+OE; assessment/projection/workspace by hospital+OE/FW; query service include hospital; fix overview DTO; internal sync require hospital.  
5. Tests: two hospitals same org same FW → two obligation sets; enroll identity; no org=hospital insert.  
6. Re-run 3B SQL classifiers.

No event/outbox redesign in 3D unless a write path still emits hospital-as-org.
