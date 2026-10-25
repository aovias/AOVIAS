# AOVIAS Command — Phase 2 Platform-Wide Data Integrity Audit

**Phase:** 2 — API / data dependency verification  
**Status:** Audit only. No production code, schema, event, or data-correction changes.  
**Inspected:** 2026-08-31 against the AOVIAS.Cursor worktree plus live local services and SQL Server `localhost\SQLEXPRESS`  
**Contract:** [AOVIAS-Command-Module-Dependency-Map.md](./AOVIAS-Command-Module-Dependency-Map.md) (Phase 1)  
**Code changes:** None (this document and its index entry only)

This report answers: after a mutation in Module A, does every associated Module B / API / screen that is supposed to observe that relationship actually return the expected state?

---

## How this audit was run

Live services in this session:

| Service | Port | Used |
| --- | --- | --- |
| Gateway | `:5000` | Health 200. `/api/v1/*` 404 — BFF not usable |
| Identity | HTTP `:5062`, HTTPS `:7135` | Login, JWT claims |
| Compliance | HTTP `:5016` | Enroll, OE ownership, dashboard |
| Organization | HTTPS `:7181` | Departments tree, memberships |
| Person | HTTPS `:7190` | Person GET |
| Workforce | HTTPS `:7195` | Employment list |
| Professional | HTTPS `:7200` | Professional profile GET |
| KnowledgeBase | `:5120` | Not required for priority chains |
| WI, Directory, Communication, AccessGovernance, Governance, Audit, AI, Integration, OutboxRelayWorker | not listening | Snapshot / directory / bus paths not runtime-exercised |

Authenticated as `ekta.gupta@gmail.com` (ORG_ADMIN).

| Claim | Value |
| --- | --- |
| `sub` | `ad286a93-01de-48ae-9227-d20c53f1e3f8` |
| `organization_id` | `2759fad4-c895-4a5f-9e49-6ded5d378098` |
| `hospital_id` | `d9f5554a-bf83-4262-b386-a31587b0ba06` |
| `membership_id` | `b3b1f68b-b719-42fe-8384-d6d5c690dcee` |
| `person_id` | `e24b61fa-438c-cd55-9ff9-ab5248686aae` |
| `professional_profile_id` | `2cad5638-3988-7658-8474-bd838026c463` |

**Audit artifact (not reversed):** `POST /api/v1/hospitals/f653e630-c9a9-4014-918d-23b33de93845/frameworks/1ac431be-7ca8-4e7b-e98a-08deb4cbd8ae/enroll` returned **201** at `2026-08-31T05:31:36Z`. That GUID is not a real OrganizationService hospital. Leave it in place until Phase 3 decides how to clean it.

Mutations that would destroy real user data (invitation redeem, employment transfer/delete, person PUT/DELETE) were **not** executed. Those chains are classified from code plus corroborating live reads.

Confidence labels: **HIGH** = reproduced on a live API or SQL path; **MEDIUM** = strongly established in code, not runtime-mutated; **LOW** = inferred / needs infrastructure.

---

## 1. Executive Summary

Phase 1’s dependency map is still the right contract. Live APIs and SQL **confirmed** several of its highest-signal claims and **disproved none** of the structural findings. Two items moved from “needs investigation” to confirmed defects: invalid-hospital enrollment succeeds, and several Compliance APIs do not bind the JWT hospital to the requested `hospitalId`.

| Bucket | Count |
| --- | ---: |
| Phase 1 cross-module dependencies | 33 |
| Dependencies exercised (API, SQL, or corroborated live read) | 26 |
| PASS | 8 |
| PASS — EVENTUAL CONSISTENCY | 3 |
| Defects | 18 |
| UNKNOWN — BUSINESS RULE | 10 |
| OBSERVATION | 7 |
| P0 | 4 |
| P1 | 11 |
| P2 | 6 |
| P3 | 4 |

### Critical findings

1. **P0 — Invalid hospital enrollment.** `POST .../hospitals/{guid}/frameworks/{id}/enroll` returned 201 for a GUID that OrganizationService never issued. The handler never HTTP-validates the hospital. `organizationId`, `hospitalId`, and `tenantId` are all set to that GUID. Live SQL already contains **1331** obligation rows where `OrganizationId = HospitalId` versus **1006** correctly distinct pairs.
2. **P0 — Cross-hospital Compliance reads.** OE ownership GET for a hospital other than the JWT `hospital_id` returned 200. Dashboard summary accepts any `hospitalId` query parameter and has no `ValidateHospitalId`. Dashboard task counts filter only on `HospitalId`; `ComplianceTasks` have no EF tenant query filter.
3. **P1 — Employment termination never leaves the service.** `DeleteAsync` soft-deletes and writes an audit row. It does not write `EmploymentTerminated`. WI and Directory register that event. No consumer can react.
4. **P1 — Documented event bus is not the runtime path for most services.** Architecture docs describe transactional outbox → Azure Service Bus via `OutboxRelayWorker`. Identity, Person, AccessGovernance, and Organization are not relayed. Identity/Person/Workforce/Professional/AG/Governance processors log then set `ProcessedOn`. The relay worker was **not running**. Compliance has **2438** `ObligationStatusUpdated` rows with `RelayProcessedOn` null.
5. **P1 — Enrollment does not drive dashboard projection.** Outbox type is `ComplianceService.Domain.FrameworkEnrolledDomainEvent`. `IsDashboardProjectionCandidate` looks for `ComplianceEnrolled`. Four FrameworkEnrolled rows sit with `ProjectionProcessedOn` null forever.

Invitation redeem was not live-executed (Gateway BFF 404; would create users). The already-provisioned ORG_ADMIN login proves the **steady-state** of that pipeline: Person, membership, JWT org/hospital/role, and ProfessionalProfile all line up.

---

## 2. Dependency Test Matrix

A/B/C/D = testability class from the Phase 2 brief.

| ID | Source mutation | Target | Expected | Actual | Class | Severity | Type |
| -- | --- | --- | --- | --- | --- | --- | --- |
| D01 | Identity login | JWT auth-context | Org/hospital/role/person in token | Claims populated as table above | PASS | — | A |
| D02 | Invitation redeem | Identity+Person+membership+seat | Full provision | Not live-mutated. Orchestrator code + existing user prove steady-state | UNKNOWN — BUSINESS RULE (orphan-on-partial-failure) | — | A / C |
| D03 | Enroll hospital×framework | Obligations | Hospital-validated; org ≠ hospital; OE count = local OEs | 201 for nonexistent hospital; org=hospital=tenant; `ComplianceNodeId` always null | DEFECT — DATA PROPAGATION | P0 | A |
| D04 | Re-enroll same hospital×framework | Obligations | Idempotent, no duplicate OEs | Handler throws already-enrolled before sync (sync itself is idempotent) | PASS | — | A |
| D05 | Enroll | Dashboard projection | Enrollment reflected after projection | `FrameworkEnrolledDomainEvent` is not a projection candidate | DEFECT — DATA PROPAGATION | P1 | B |
| D06 | Enroll | Journey APIs | Rows for hospital×framework | Public `GET /api/v1/journeys?hospitalId=` 404. Gateway path is `/api/v1/journey/obligations` → `/internal/v1/journey-obligations` (`IgnoreQueryFilters`) | DEFECT — API CONTRACT | P2 | A |
| D07 | OE ownership GET | Matrix | Hospital-scoped ownership | Catalog OEs for **any** hospitalId; JWT hospital not enforced; empty ownership table | DEFECT — TENANT ISOLATION | P0 | A |
| D08 | OE ownership GET enrolled FW | JourneyStatus | Status when journey exists | Enrolled FW `0fe8a9c0-…` returns `journeyStatus: "Draft"`; non-enrolled catalog FW returns `null` | PASS | — | A |
| D09 | OE upsert | ComplianceDepartment | Reject Org OU ids | UI loads Org `/departments/tree`; upsert looks up CS `Departments` | DEFECT — UI PROPAGATION | P1 | A |
| D10 | OE re-upsert | Ownership rows | No duplicates | Unique on hospital+OE; update in place (code) | PASS | — | A |
| D11 | Employment list `?personId=` | Employment | Filter by person | Query ignored; first page of org employments returned | DEFECT — API CONTRACT | P2 | A |
| D12 | Employment GET by id | ProfessionalProfile | Matching org/hospital | Profile `2cad5638-…` employment `7efb8340-…`, org/hospital match JWT | PASS | — | A |
| D13 | Employment transfer | ProfessionalProfile + WI | Profile org/hospital updated; WI after refresh | Transfer updates Employment + TransferRecord only; outbox `"{}"` | DEFECT — DATA PROPAGATION | P1 | A / B |
| D14 | Employment delete | WI / Directory | `EmploymentTerminated` | SoftDelete, no outbox | DEFECT — EVENT PROPAGATION | P1 | D |
| D15 | Employment promote | WI | Snapshot refresh | Writes `EmploymentPromoted` `"{}"`; WI does not register it | DEFECT — EVENT PROPAGATION | P1 | D |
| D16 | Person PUT | Directory + OrganizationMember | Names/email follow Person | No `PersonUpdated` outbox; member denorm is independent | DEFECT — EVENT PROPAGATION | P1 | D |
| D17 | Person DELETE | Identity / membership / directory | Soft-delete propagated | SoftDelete only; no outbox | DEFECT — EVENT PROPAGATION | P1 | D |
| D18 | Person create | Directory | Index row | `PersonCreated` mark-only; Directory listens for `PersonUpdated`; rebuild calls `GET /persons?organizationId=` which Person ignores | DEFECT — DATA PROPAGATION | P1 | B |
| D19 | OrganizationMember denorm | Person names | Stay aligned | Live membership `firstName`/`email` can differ from Person (observed on list) | DEFECT — DATA PROPAGATION | P2 | A |
| D20 | DeficiencyIdentified | ComplianceTask | Remediation task, `SourceDeficiencyId` | In-process processor calls `EnsureRemediationTaskAsync`; 1 live event processed | PASS — EVENTUAL CONSISTENCY | — | B |
| D21 | TaskOverdue outbox | Dashboard incremental | Task counts refresh | 607 unprojected; `IsTaskEvent` only Created/Completed | DEFECT — DATA PROPAGATION | P1 | B |
| D22 | ObligationStatusUpdated | Dashboard full rebuild | Projection processed | Unproj=0 (processed); Unrelay=2438 (bus not running) | PASS — EVENTUAL CONSISTENCY | — | B |
| D23 | Identity outbox | External consumers | Publish or in-process consume | 3474 unprocessed (`ProcessedOn` null); processor is mark-only; no relay | DEFECT — EVENT PROPAGATION | P1 | D |
| D24 | Org outbox | Consumers | Processor + relay | Table empty; no hosted Org processor; no relay | OBSERVATION | P3 | D |
| D25 | OutboxRelayWorker | Service Bus | Relay processed outbox | Worker not listening; requires Service Bus connection string; no Identity/Person/Org/AG relay | DEFECT — EVENT PROPAGATION | P1 | B |
| D26 | WI snapshot | Employment/Professional/Training | Refresh 60 min or `POST /intelligence/refresh` | WI not running; `ExtractOrganizationId` requires JSON `organizationId`; Workforce payloads are `"{}"` | DEFECT — EVENT PROPAGATION | P1 | B |
| D27 | Training Phase-4 | WI | Event or snapshot | No Phase-4 attendance outbox; live matrix is ProfessionalService | PASS — EVENTUAL CONSISTENCY | — | B / C |
| D28 | User disable | Roles/seats/membership | UNKNOWN (U3) | Suspend/Disable set status + revoke sessions only | UNKNOWN — BUSINESS RULE | — | C |
| D29 | Dual auditor | Audit vs Compliance Auditor | UNKNOWN (U11) | Two stores remain | UNKNOWN — BUSINESS RULE | — | C |
| D30 | KB framework GUID fallback | OE codes | UNKNOWN if merge-all is intended | On miss, client unions nodes from **all** KB frameworks | OBSERVATION | P2 | D |
| D31 | FE `/api/v1` + `apiBaseUrl` | WI / Integration / admin-audit | Single prefix | `BaseApiService` already prefixes `/api/v1`; those services add another | DEFECT — UI PROPAGATION | P1 | D |
| D32 | Employment UI vs Gateway | Create/delete/assign | UNKNOWN product intent | `EmploymentApiService` has list/search/get/timeline/history/org-chart/transfer/promote — no create/delete/assign | UNKNOWN — BUSINESS RULE | — | C |
| D33 | Cert expiry reminder | Communication | Notify | Professional hosted service writes outbox; processor mark-only; no relay for Professional except via workforce-events after ProcessedOn | DEFECT — EVENT PROPAGATION | P2 | B |
| D34 | Compliance tenant filters | Tasks/Evidence/Risks/CAPA/DashboardProjections | Isolation | No `HasQueryFilter` on those DbSets; journeys/obligations/deficiencies are filtered | DEFECT — TENANT ISOLATION | P0 | D |
| D35 | SLA / CAPA / docs / incidents | Dashboard | UNKNOWN (U12) | Incremental filter is task/finding/deficiency/`EvidenceUploaded`/`ComplianceEvaluated` only | UNKNOWN — BUSINESS RULE | — | C |

---

## 3. End-to-End Verified Chains

### 3.1 Login / auth-context (TEST 1 steady-state) — PASS (HIGH)

```text
POST http://localhost:5062/api/v1/auth/login
 → JWT
 → organization_id / hospital_id / membership_id / person_id / professional_profile_id / role=ORG_ADMIN
 → GET Person https://localhost:7190/api/v1/persons/{personId}  (Ekta Gupta, Active, email contact)
 → GET Professional https://localhost:7200/api/v1/professional-profiles/{id}  200
 → GET Org departments/tree  200, hospital-scoped
```

Redeem itself was **not** posted. `InvitationRedeemOrchestrator` still matches Phase 1: Identity register → Person create/upsert → membership → employment → optional seat (`FailSeatIfUnavailable`); department/position/manager assign are caught and skipped. Orphan behavior on mid-pipeline failure remains **U / not proven**.

### 3.2 Hospital + framework enrollment (TEST 2) — mixed

**Valid hospital, already enrolled** (`d9f5554a-…` × `0fe8a9c0-…` NABH Full): GET enrolled frameworks returns that single InProgress row. OE matrix for that framework returns catalog OEs with `journeyStatus: "Draft"`. SQL: **2337 / 2337** obligations have `ComplianceNodeId` null.

**Invalid hospital (this audit):**

```text
POST http://localhost:5016/api/v1/hospitals/f653e630-c9a9-4014-918d-23b33de93845/frameworks/1ac431be-7ca8-4e7b-e98a-08deb4cbd8ae/enroll
HTTP 201
{"hospitalId":"f653e630-…","frameworkId":"1ac431be-…","status":"Enrolled","enrolledOn":"2026-08-31T05:31:36.4004246Z"}
```

Handler (`EnrollHospitalInFrameworkCommandHandler`) validates framework Active only. Comment: full hospital validation would require OrganizationService. Then:

```csharp
SyncForOrganizationAsync(
    organizationId: command.HospitalId,
    hospitalId: command.HospitalId,
    tenantId: command.HospitalId,
    ...)
```

`ComplianceNodeId` is always `null` in `ObligationSynchronizationService`. That is an **implementation detail** for persistence (no FK to KB nodes) **and** an observable integrity problem if any downstream API is supposed to join obligations to KB nodes by `ComplianceNodeId` — today that join cannot succeed. Classification of the null itself: **OBSERVATION** until U10 decides the product rule; classification of enroll-without-hospital: **DEFECT**.

Re-enrollment: handler throws “already enrolled” **before** the idempotent sync. Repeated enroll does not create duplicate obligations via this API. Sync’s own duplicate skip (existing OE IDs) was not reached on the live re-enroll path.

Public journey: `GET /api/v1/journeys?hospitalId=` → 404. Product journey obligations are Gateway `GET /api/v1/journey/obligations` proxying `GET /internal/v1/journey-obligations` (InternalOnly, 403 with user JWT). Internal query uses `IgnoreQueryFilters()` and `HospitalId + FrameworkId` only.

Dashboard `GET /api/v1/dashboard/summary?hospitalId=d9f5554a-…` → 200, score 0, totalTasks 503, completed 126, pending 377, openAuditFindings 12. That summary is built from live task/journey queries, not only `DashboardProjections`. Enrollment still does not invalidate projection via `FrameworkEnrolledDomainEvent`.

### 3.3 OE ownership (TEST 3) — GET works; write path structurally misaligned

```text
GET /api/v1/hospitals/{hospitalId}/oe-ownership?frameworkId=
 → ObjectiveElements catalog (optionally filtered by framework)
 → ObjectiveElementOwnerships WHERE HospitalId = route hospitalId
 → ComplianceJourneys.Status for that hospital
 → does not call OrganizationService
```

Live:

- SQL `ObjectiveElementOwnerships`: **empty**.
- Enrolled framework: rows with `ownershipId: null`, `status: Unassigned`, `journeyStatus: "Draft"`.
- Non-enrolled catalog framework `1ac431be-…`: same Unassigned catalog, `journeyStatus: null`, duplicate codes (e.g. three OE ids all `ACC.1`). Catalog `ACC.1` appears **65** times across ObjectiveElements.
- Other hospitalId (including the audit fake GUID): **HTTP 200**, same catalog shape, `journeyStatus: null`. Controller has **no** `ValidateHospitalId`.

UI (`oe-ownership-matrix.component.ts` `loadFrameworksAndDeps`) loads departments from `OrganizationApiService.getDepartmentTree`. Upsert requires a row in Compliance `Departments` for `(Id, OrganizationId)` and `IsActive`. Org Quality id `9e411e53-64d3-4f52-96a3-0181c009bd42` is not that store (CS Housekeeping for this org is `e6c065ea-463f-4bff-a7ec-01984bfe8e8c`). Saving from the matrix will fail with “Department not found for organization.” unless the two id spaces accidentally collide.

Catalog-wide OE lists vs enrolled-only is **U9**. Do not call it a defect until product confirms. It is operationally real: the API returns OEs for frameworks the hospital is not enrolled in.

### 3.4 Employment lifecycle (TEST 4) — create readable; transfer/delete broken in code

Create was not posted. Existing ProfessionalProfile proves Person + Employment + org/hospital + training records exist.

`GET /api/v1/employment?personId={ekta}` ignored `personId` (`ListEmploymentsQuery` has organization/hospital/status only). Internal `GET /internal/employment/by-person/{id}` uses `SearchEmploymentsQuery(personId.ToString())` (search string, not a dedicated filter).

Transfer (code, MEDIUM): updates `Employment.OrganizationId`/`HospitalId`, inserts `TransferRecord`, writes `EmploymentTransferred` payload `"{}"`. Does **not** load or update `ProfessionalProfile`. WI `ExtractOrganizationId` returns null on `"{}"`, so even a delivered bus event would not refresh that org.

Delete (code, HIGH): `SoftDelete`, audit `EMPLOYMENT_DELETED`, **no outbox**. WI and Directory handlers for `EmploymentTerminated` cannot fire. **Structurally broken (class D).**

WebApp `EmploymentApiService` can transfer/promote but cannot create/delete/assign — candidate 21 / U product surface.

### 3.5 Person update/delete (TEST 5) — structurally no propagation

`PersonAppService.UpdateAsync`: save + audit. No outbox.  
`DeleteAsync`: `SoftDelete` + audit. No outbox.

Person outbox SQL: **only** `PersonCreated` (27 rows, all `ProcessedOn` set). Directory registers `PersonUpdated`. Rebuild calls `GET /api/v1/persons?organizationId=` but `PersonsController.List` has no `organizationId` parameter — Directory rebuild cannot scope persons by org.

OrganizationMember FirstName/Email are written only on membership create/update APIs, not from Person events. Live membership list already shows denormalized names that are not a live Person join.

### 3.6 Compliance projection chain (TEST 6) — in-process works for a subset

```text
Enrollment → obligations (sync, org=hospital bug)
DeficiencyIdentified → EnsureRemediationTaskAsync + snapshot queue  (1 live processed event)
ObligationStatusUpdated → full rebuild candidate  (Unproj=0)
FrameworkEnrolled → NOT a candidate  (Unproj=4)
TaskOverdue → candidate via ComplianceEvent.* prefix, but incremental IsTaskEvent misses it
EvidenceUploaded → incremental
SLA/CAPA/docs/incidents/maturity unlock → not in incremental or full-rebuild lists (U12)
```

Hangfire in Compliance `Program.cs` schedules snapshot, expiry, analytics daily, task overdue escalation, SLA chase, document jobs. Those jobs were not proven to have run in this session.

---

## 4. Defects

### 4.1 Data propagation

**Finding ID: P2-F01**  
Dependency: Hospital enroll → OrganizationComplianceObligation  
Source mutation: `POST /api/v1/hospitals/{hospitalId}/frameworks/{frameworkId}/enroll`  
Source API: Compliance `FrameworkController.EnrollHospitalInFramework`  
Source record ID: hospital `f653e630-c9a9-4014-918d-23b33de93845`, framework `1ac431be-7ca8-4e7b-e98a-08deb4cbd8ae`  
Target: OrganizationComplianceObligation + dashboard + journey  
Expected: Reject unknown hospital; persist real OrganizationId; one obligation per OE; dashboard/journey observe enrollment  
Actual: HTTP 201 Enrolled. Handler sets org=hospital=tenant=`command.HospitalId`. SQL: 1331 ORG_EQ_HOSP vs 1006 DISTINCT; 2337/2337 `ComplianceNodeId` null.  
Classification: **DEFECT — DATA PROPAGATION**  
Evidence: `EnrollHospitalInFrameworkCommandHandler.cs` lines 77–150; `ObligationSynchronizationService.cs` `ComplianceNodeId = null`; live 201 body; SQL counts.  
Reproduction: Authenticate; POST enroll with a random GUID hospitalId and an Active framework.  
Severity: **P0**  
Confidence: **HIGH**

**Finding ID: P2-F02**  
Dependency: FrameworkEnrolled → DashboardProjections  
Expected: Enrollment is a projection candidate (docs: full rebuild on enroll)  
Actual: Outbox type `ComplianceService.Domain.FrameworkEnrolledDomainEvent` is not in `IsDashboardProjectionCandidate` (`ComplianceEnrolled` string). Four rows `ProjectionProcessedOn` null.  
Classification: **DEFECT — DATA PROPAGATION**  
Evidence: `InProcessOutboxProcessor.cs` 138–151; SQL FrameworkEnrolled events.  
Severity: **P1**  
Confidence: **HIGH**

**Finding ID: P2-F03**  
Dependency: TaskOverdue → dashboard task counts  
Expected: Overdue events refresh dashboard tasks  
Actual: 607 `TaskOverdue` unprojected. Even if processed, `DashboardProjectionUpdater.IsTaskEvent` is only TaskCreated/TaskCompleted.  
Classification: **DEFECT — DATA PROPAGATION**  
Evidence: SQL Unproj=607; `DashboardProjectionUpdater.cs` 245–247.  
Severity: **P1**  
Confidence: **HIGH**

**Finding ID: P2-F04**  
Dependency: Employment transfer → ProfessionalProfile  
Expected: Profile org/hospital follow employment (or a documented refresh)  
Actual: `TransferAsync` never touches ProfessionalProfile. WI payload `"{}"` cannot select an organization.  
Classification: **DEFECT — DATA PROPAGATION**  
Evidence: `EmploymentAppService.TransferAsync`; `IntelligenceEventPayloadParser.ExtractOrganizationId`.  
Severity: **P1**  
Confidence: **MEDIUM** (code; not live-transferred)

**Finding ID: P2-F05**  
Dependency: Person → Directory rebuild  
Expected: Rebuild lists persons for an organization  
Actual: Directory `ListPersonsAsync` calls `/api/v1/persons?organizationId=`. Person List ignores that query.  
Classification: **DEFECT — DATA PROPAGATION**  
Evidence: `SourceServiceClients.cs`; `PersonsController.List`.  
Severity: **P1**  
Confidence: **MEDIUM** (Directory service not running)

**Finding ID: P2-F06**  
Dependency: Person names → OrganizationMember denorm  
Expected: Member FirstName/Email follow Person (if that is the product rule — U2)  
Actual: No PersonUpdated; membership APIs write denorm independently. Live list already shows denormalized values that are not a Person join.  
Classification: **DEFECT — DATA PROPAGATION** (if U2 is yes); else UNKNOWN  
This audit treats the **missing publisher** as a defect against Directory’s registered `PersonUpdated` consumer, and the denorm drift as P2.  
Severity: **P2**  
Confidence: **HIGH** for missing event; **MEDIUM** for which names “should” win

### 4.2 Event propagation

**Finding ID: P2-F07**  
Dependency: Employment delete → EmploymentTerminated  
Expected: WI/Directory consume termination  
Actual: `DeleteAsync` has no outbox. Consumers exist. Event is never written.  
Classification: **DEFECT — EVENT PROPAGATION**  
Evidence: `EmploymentAppService.DeleteAsync`; WI `ServiceCollectionExtensions` event list.  
Severity: **P1**  
Confidence: **HIGH** (code proves impossibility)

**Finding ID: P2-F08**  
Dependency: EmploymentPromoted / ProfessionalProfileCreated → WI  
Expected: Snapshot refresh  
Actual: Events written (promote payload `"{}"`); WI does not register them.  
Classification: **DEFECT — EVENT PROPAGATION**  
Severity: **P1**  
Confidence: **HIGH**

**Finding ID: P2-F09**  
Dependency: Architecture outbox → Service Bus  
Expected (PRAMAN / Architecture README): transactional outbox → Azure Service Bus through `OutboxRelayWorker`  
Actual: Relay covers Compliance (`RelayProcessedOn` → `compliance-events`), Workforce/Professional/Governance after `ProcessedOn`, Directory, Integration. **No relay for Identity, Person, Organization, AccessGovernance.** Identity/Person/Workforce/Professional/AG/Governance processors: log + `ProcessedOn`. Worker not running this session. Compliance Unrelay: ObligationStatusUpdated 2438, FrameworkEnrolled 4. Identity outbox: 3474 unprocessed (processor not draining this database).  
Classification: **DEFECT — EVENT PROPAGATION** versus documented architecture. Mark-only processors are **intentional local drain**, not a bus. Do not confuse the two.  
Severity: **P1**  
Confidence: **HIGH**

**Finding ID: P2-F10**  
Dependency: PersonUpdated / PersonDeleted  
Expected: Directory (and membership) observe  
Actual: Only `PersonCreated` is written. Directory listens for `PersonUpdated`.  
Classification: **DEFECT — EVENT PROPAGATION**  
Severity: **P1**  
Confidence: **HIGH**

**Finding ID: P2-F11**  
Dependency: Empty outbox payloads → WI  
Expected: Payload carries `organizationId` if WI is to refresh incrementally  
Actual: `EmploymentTransferred` / `EmploymentPromoted` persist `"{}"`.  
Classification: **DEFECT — EVENT PROPAGATION**  
Severity: **P1**  
Confidence: **HIGH**

**Finding ID: P2-F12**  
Dependency: Professional `CertificationExpiryReminder`  
Expected: Downstream reminder  
Actual: Written to Professional outbox; mark-only processor; relay only if ProcessedOn then workforce-events.  
Classification: **DEFECT — EVENT PROPAGATION**  
Severity: **P2**  
Confidence: **MEDIUM**

### 4.3 API contract

**Finding ID: P2-F13**  
`GET /api/v1/employment?personId=` does not filter. `ListEmploymentsQuery` has no PersonId.  
Classification: **DEFECT — API CONTRACT**  
Severity: **P2**  
Confidence: **HIGH**

**Finding ID: P2-F14**  
`GET /api/v1/professional-profiles/by-person/{id}` → 404; GET by profile id → 200.  
Classification: **DEFECT — API CONTRACT**  
Severity: **P3**  
Confidence: **HIGH**

**Finding ID: P2-F15**  
`GET /api/v1/journeys?hospitalId=` on Compliance → 404. Product path is Gateway `/api/v1/journey/obligations`.  
Classification: **DEFECT — API CONTRACT** (direct-to-service clients)  
Severity: **P2**  
Confidence: **HIGH**

### 4.4 Tenant isolation

**Finding ID: P2-F16**  
OE ownership GET `/hospitals/{hospitalId}/oe-ownership` has no `ValidateHospitalId`. JWT hospital `d9f5554a-…`; request hospital `f653e630-…` (and any other GUID) → 200 catalog.  
Classification: **DEFECT — TENANT ISOLATION**  
Severity: **P0**  
Confidence: **HIGH**

**Finding ID: P2-F17**  
Enrollment POST uses route `hospitalId` with no JWT hospital check — any ORG_ADMIN can enroll **any GUID**, including another tenant’s hospital id if guessed.  
Classification: **DEFECT — TENANT ISOLATION**  
Severity: **P0**  
Confidence: **HIGH**

**Finding ID: P2-F18**  
`DashboardController` has no `ValidateHospitalId`. Query `hospitalId` is not a route value, so the filter would not catch it even if applied. `GetDashboardSummaryAsync` counts `ComplianceTasks` by HospitalId only. `ComplianceTasks`, `Evidences`, `Risks`, `Capas`, `DashboardProjections`, snapshots have **no** `HasQueryFilter`. Journeys/obligations/deficiencies do.  
Classification: **DEFECT — TENANT ISOLATION**  
Severity: **P0**  
Confidence: **HIGH** (code + OE GET proof; dashboard cross-hospital body not separately captured in this session)

`ValidateHospitalIdAttribute` only inspects **route** `hospitalId`, not query string.

### 4.5 Referential integrity

**Finding ID: P2-F19**  
Obligations for hospital GUID `f653e630-…` (and historical ORG_EQ_HOSP rows) reference OrganizationId values that are hospital ids, not OrganizationService organization ids. Soft GUID `HospitalId` can point at a hospital that does not exist.  
Classification: **DEFECT — ORPHAN / REFERENTIAL INTEGRITY**  
ID semantics: **ORPHANED** (fake enroll) / **STALE** (org column holds hospital id) / **AMBIGUOUS** (`TenantId` = `OrganizationId` even on DISTINCT rows sampled as org GUID).  
Severity: **P0**  
Confidence: **HIGH**

**Finding ID: P2-F20**  
`ComplianceNodeId` always null → KB node relationship **MISSING**.  
Classification: **OBSERVATION** until U10; not a user-visible join today.  
Severity: **P3**

### 4.6 UI propagation

**Finding ID: P2-F21**  
OE matrix UI: `getDepartmentTree` (Org). Upsert API: Compliance `Departments`.  
Classification: **DEFECT — UI PROPAGATION**  
Severity: **P1**  
Confidence: **HIGH** (code; PUT with Org dept id was not successfully JSON-posted this session — PowerShell quoting returned 400 parse error)

**Finding ID: P2-F22**  
`WorkforceIntelligenceApiService.base = '/api/v1/intelligence'` and `IntegrationApiService` / `AdministrationAuditApiService` duplicate `environment.apiBaseUrl` (`/api/v1`). Copilot comments not to duplicate. Gateway was 404 so the double path was not HTTP-proved.  
Classification: **DEFECT — UI PROPAGATION**  
Severity: **P1**  
Confidence: **MEDIUM**

**Finding ID: P2-F23**  
Employment UI missing create/delete/assign while Gateway exposes them.  
Classification: **UNKNOWN — BUSINESS RULE** (candidate 21) unless product requires those screens.  
Severity: —

---

## 5. Event / Outbox Audit

Architecture README: “Events: Transactional outbox → Azure Service Bus (`OutboxRelayWorker`)”.

Implementation:

```text
Write OutboxMessages
  → (optional) in-process processor sets ProcessedOn or ProjectionProcessedOn
  → OutboxRelayWorker (if running, if Service Bus configured, if service is in the relay list)
       → Service Bus topic
  → consumers (WI, Directory, …) only if they subscribe to that topic
```

| Event | Written? | Processed? | Published? | Consumer exists? | Consumer receives? | Observable impact |
| --- | --- | --- | --- | --- | --- | --- |
| Identity UserRegistered / SecurityEventRaised / … | Yes (3474+ rows) | No (`ProcessedOn` all null in this DB) | No (Identity not in relay) | None found on bus | No | Login still works without these events |
| PersonCreated | Yes (27) | Yes (mark-only) | No (Person not in relay) | Directory does **not** listen for PersonCreated | No | Directory freshness via rebuild only |
| PersonUpdated / PersonDeleted | **No** | — | — | Directory `PersonUpdated` | No | Stale directory / membership denorm |
| Org MembershipCreated / Transferred | Not present in this Org DB | No Org processor | No | None found | No | JWT uses HTTP auth-context, not events |
| AG outbox | Mark-only processor | If hosted | No AG relay | Evaluate is HTTP persist | No bus impact | Role assign still local |
| EmploymentCreated | Code yes; this Workforce DB outbox empty | Mark-only | Relay Workforce **after** ProcessedOn | WI EmploymentCreated | No (empty payload / worker down) | Create still HTTP-validates Person/Hospital |
| EmploymentTransferred | Code `"{}"` | Mark-only | Same | WI yes | Would skip org extract | Employment row updates immediately |
| EmploymentPromoted | Code `"{}"` | Mark-only | Same | **WI no** | No | Position assignment is local |
| EmploymentTerminated | **Never written** | — | — | WI + Directory yes | No | Soft-delete only |
| ProfessionalProfileCreated | Code yes | Mark-only | workforce-events if relayed | **WI no** | No | Profile GET works |
| Professional.CertificationExpiryReminder | Hosted service writes | Mark-only | If relayed | Not traced to Communication | No | — |
| DeficiencyIdentified | Yes (1) | ProjectionProcessedOn set; EnsureRemediationTaskAsync | RelayProcessedOn likely null | In-process Compliance | Yes in-process | Task create is the impact |
| ObligationStatusUpdated | 2438 | Unproj=0 | Unrelay=2438 | Dashboard engine | In-process yes; bus no | Full rebuild path |
| FrameworkEnrolledDomainEvent | 4+ | Unproj (not a candidate) | Unrelay | Dashboard looks for ComplianceEnrolled | No | Enrollment API still 201 |
| ComplianceEvent.TaskOverdue | 749 | Unproj 607 | Unrelay | Incremental misses overdue | Partial | Stale overdue on projection |
| CommitteeDecisionRecorded | Governance mark-only | Then workforce relay if ProcessedOn | Worker down | WI yes | No this session | Soft IDs on committee |

Traced **one real event to a consumer**: `DeficiencyIdentified` → `InProcessOutboxProcessor` → `EnsureRemediationTaskAsync` (in-process, not Service Bus). That is the only compliance-to-task path that is actually wired.

Identity mark-only processor exists in code (`IdentityOutboxProcessor`) but this database’s Identity outbox is entirely unprocessed — the running Identity instance is not draining this table (wrong DB, processor not hosted, or stuck). **HIGH** for backlog; **MEDIUM** for why.

---

## 6. Snapshot / Eventual Consistency Audit

| Dependency | Expected freshness | Refresh mechanism | Manual | Scheduled | Consumes latest source? | Downstream API this session |
| --- | --- | --- | --- | --- | --- | --- |
| Workforce → WI | After snapshot | `SnapshotSchedulerHostedService` 60 min; event projection if payload has `organizationId` | `POST /intelligence/refresh` | 60 min | HTTP pull employment/profiles/governance/depts | **WI not running** |
| Professional → WI | Same | Same | Same | Same | Same | Not running |
| Training → WI | Snapshot only for WI dashboards; live matrix is Professional | No Phase-4 outbox | Same | Same | Snapshot job HTTP | Not running |
| Person → Directory | Rebuild / PersonUpdated | `POST /directory/rebuild`; event PersonUpdated (never written) | Rebuild endpoint | Scheduler in Directory | Person list **unscoped** | **Directory not running** |
| Compliance → Dashboard summary | Mixed live queries | `GetDashboardSummaryAsync` hits tasks/journeys **now** | Projection rebuild diagnostics | Hangfire snapshots nightly | Tasks by hospitalId | **HIGH** live summary 200 |
| Compliance → DashboardProjections | Event-driven + Hangfire | InProcessOutboxProcessor | Rebuild accepted endpoint | AnalyticsDailySnapshotJob etc. | Enrollment events not candidates | Projection table not separately queried |
| Compliance → Analytics | Hangfire daily | `AnalyticsDailySnapshotJob` | — | Cron in Program.cs | Unknown this session | Not called |

Do **not** classify snapshot lag as a defect. Classify: WI/Directory **cannot** become consistent in this environment because those services and the relay worker are down, **and** even when up, several events cannot select an organization (`"{}"`) or are never written.

PASS — EVENTUAL CONSISTENCY applies to: DeficiencyIdentified → task (in-process); ObligationStatusUpdated projection processed; WI/training **design** (if refresh runs and payload is valid).

---

## 7. Tenant Isolation Audit

| Scenario | Result |
| --- | --- |
| Org departments/tree for JWT org | Hospital-scoped tree 200 — PASS |
| Employment list | Rows all `organizationId=2759fad4`, `hospitalId=d9f5554a` — PASS for this query |
| Professional profile | Same org/hospital — PASS |
| OE GET hospital A enrolled FW | 200 Draft — PASS for data shape |
| OE GET hospital **other than JWT** | **200 catalog** — FAIL isolation |
| Enroll random hospital GUID | **201** — FAIL isolation + orphan |
| Obligation SQL two org/hospital pairs | `2759fad4`/`d9f5554a` and `BCB783DB`/`F17FC194` coexist — data is partitioned by stored ids, but enroll writes hospital GUID into OrganizationId |
| Dashboard `hospitalId` query vs JWT | No `ValidateHospitalId`; tasks not tenant-filtered |
| Internal journey obligations | `IgnoreQueryFilters` + HospitalId+FrameworkId — isolation is “know the hospital GUID”, not JWT tenant |
| Compliance EF filters | Present for journeys, obligations, deficiencies, some audit/docs/incidents; **absent** for tasks, evidence, risks, CAPA, dashboard projections, snapshots |

Hospital A ownership never appearing on Hospital B: **ownership table empty**, so that specific leak could not be demonstrated. Catalog OE ids **do** appear for every hospitalId — that is catalog-global by design (U9), not a tenant leak of ownership rows.

---

## 8. Unknown Business Rules (U1–U18)

| ID | Phase 1 question | Phase 2 result |
| --- | --- | --- |
| U1 | Admin user-create always create Person? | Not exercised. Login user already has PersonId. **UNKNOWN** |
| U2 | Person update/delete refresh Directory and member names without rebuild? | Code: no. Directory expects PersonUpdated. **Needs product decision**; missing publisher is already P2-F10 |
| U3 | Suspend/disable revoke roles/seats/memberships? | `SuspendAsync`/`DisableAsync`: status + `RevokeAllSessionsAsync` only. **UNKNOWN** (no cascade found) |
| U4 | Membership archive release seats? | Not exercised. **UNKNOWN** |
| U5 | Employment delete emit EmploymentTerminated? | Code: no. If product wants WI/Directory to drop the person, this is P2-F07. **Decision: emit or stop listening** |
| U6 | Transfer/promote update ProfessionalProfile and WI? | Transfer does not update profile. Promote not subscribed in WI. **Decision required**; current behavior is inconsistent |
| U7 | Phase-4 training feed WI/Communication like TrainingCompleted? | No Phase-4 outbox. **UNKNOWN** |
| U8 | Canonical department: Org OU vs MasterDepartmentId vs ComplianceDepartment? | Three stores confirmed live (Org tree ≠ CS Departments ids). **UNKNOWN**; OE UI already broken if Org is shown |
| U9 | OE matrix only enrolled OEs? | API lists catalog. Enrolled FW gets JourneyStatus. **UNKNOWN**; not classified as defect |
| U10 | Enroll validate Hospital + set ComplianceNodeId from KB? | Neither happens. Invalid enroll is a **defect** regardless. NodeId null is **OBSERVATION** until this decision |
| U11 | AuditService vs Compliance Domain.Auditor? | Both remain. **UNKNOWN** |
| U12 | When dashboard refreshes for SLA/CAPA/docs/incidents/maturity? | Incremental/full-rebuild lists do not include those names. **UNKNOWN** if Hangfire jobs are the intended path |
| U13 | OutboxRelayWorker for Identity/Workforce/Person or only Compliance? | Code: Compliance + Workforce/Professional/Governance + Directory + Integration. Not Identity/Person/Org/AG. **Docs disagree with code** — P2-F09 |
| U14 | Users without PersonId get notifications? | Not exercised. **UNKNOWN** |
| U15 | Designation Org string vs Workforce entity? | Not exercised. **UNKNOWN** |
| U16 | Integration import same validation as UI create? | Integration not running. **UNKNOWN** |
| U17 | Dual membership APIs both in product? | Not exercised. **UNKNOWN** |
| U18 | KB FacilityId vs HospitalId? | Not exercised. **UNKNOWN** |

---

## 9. Phase 3 Remediation Backlog

Do **not** implement in this phase.

| Priority | Modules | Root cause | Recommended fix | Required tests | Blast radius |
| --- | --- | --- | --- | --- | --- |
| P0 | Compliance enroll + obligations | Hospital not validated; org=hospital=tenant | HTTP-validate hospital against OrganizationService; pass real organizationId/tenantId; reject unknown hospital | Enroll unknown GUID → 404/400; enroll hospital A does not write org=A’s hospital id; duplicate OE count | All obligation/journey/dashboard queries keyed by OrganizationId |
| P0 | Compliance OE, enroll, dashboard | No JWT hospital bind; query-param hospitalId | Apply hospital/org authorization on GET/POST; extend `ValidateHospitalId` to query string; add tenant filters or equivalent for Tasks/Evidence/Risks/CAPA/projections | Hospital B id with Hospital A token → 403; task counts cannot include B | All Compliance GETs parameterized by hospitalId |
| P0 | Historical obligations | 1331 ORG_EQ_HOSP rows + audit enroll GUID | Data repair playbook (human): identify true org from Hospital master; do not silent-fix in app | SQL audit of OrganizationId≠Hospital.OrganizationId | Reporting, journey, tenant filters |
| P1 | Workforce delete/transfer | Missing EmploymentTerminated; no profile update; `"{}"` payloads | Emit terminated; update or explicitly invalidate ProfessionalProfile; put organizationId in outbox JSON | Delete → WI/Directory row inactive after documented refresh; transfer → profile hospital matches | WI dashboards, directory, professional screens |
| P1 | Person → Directory | No PersonUpdated; List ignores organizationId | Write PersonUpdated/Deleted **or** document rebuild-only and fix List filter | PUT person name → directory after rebuild/event; rebuild does not mix orgs | Directory, comms, membership display |
| P1 | Outbox vs docs | Relay list ≠ architecture README; Identity backlog | Either run relay + document which services are local-only, or add relays; drain Identity outbox or stop writing unused events | One event Identity→bus→consumer if that is the architecture | All cross-service listeners |
| P1 | Dashboard enrollment/overdue | Type name mismatch; IsTaskEvent too narrow | Treat FrameworkEnrolledDomainEvent as enroll; include TaskOverdue in incremental | Enroll → projection timestamp moves; overdue count matches tasks | Dashboard accuracy |
| P1 | OE UI vs CS Departments | U8 undecided + UI wired to Org tree | Pick canonical department store; point matrix picker at that API | Upsert with picker id → 200 GET round-trip | Org admin + compliance OE screens |
| P1 | WebApp double `/api/v1` | Duplicate prefix | Use path relative to `apiBaseUrl` like Copilot | Network tab: `/api/v1/intelligence/...` once | WI, integration, admin-audit screens |
| P2 | Employment `personId` query | List DTO omits personId | Add filter or remove query param from docs | `?personId=` returns only that person | Workforce list |
| P2 | Journey URL | Public `/journeys` vs Gateway `/journey/obligations` | Document or add public alias | Gateway 200 with JWT | Journey UI (if it uses Gateway, may already be fine) |
| P2 | Cert expiry | Outbox trapped | Publish to Communication or drop the writer | Expiring cert → notification | Professional |
| P3 | ComplianceNodeId | Always null | Implement KB map or drop the column from DTOs | Obligation DTO / KB join | KB bridge |
| P3 | professional-profiles/by-person | 404 | Align route with Gateway/UI | GET by person 200 | Professional screens |

---

## 10. Phase 1 candidate close-out (22)

| # | Candidate | Classification | Notes |
| ---: | --- | --- | --- |
| 1 | Person update/delete → Directory | DEFECT — EVENT PROPAGATION | P2-F10, P2-F05 |
| 2 | Outbox processors mark-only | OBSERVATION + DEFECT vs docs | Intentional drain; not a bus. P2-F09 |
| 3 | Organization outbox without consumer | OBSERVATION | Empty table; no processor |
| 4 | EmploymentTerminated never emitted | DEFECT — EVENT PROPAGATION | P2-F07 |
| 5 | EmploymentPromoted / ProfessionalProfileCreated unused by WI | DEFECT — EVENT PROPAGATION | P2-F08 |
| 6 | Empty outbox payloads | DEFECT — EVENT PROPAGATION | P2-F11 |
| 7 | Transfer → ProfessionalProfile | DEFECT — DATA PROPAGATION | P2-F04 |
| 8 | Training → WI | PASS — EVENTUAL CONSISTENCY / U7 | Snapshot by design; Phase-4 no outbox |
| 9 | OE matrix catalog vs enrollment | UNKNOWN U9 + PASS for JourneyStatus | Live Draft vs null |
| 10 | ComplianceDepartment vs Org OU | DEFECT — UI PROPAGATION + U8 | P2-F21 |
| 11 | Enrollment ComplianceNodeId | OBSERVATION + U10 | 2337/2337 null |
| 12 | Hospital not validated at enroll | DEFECT — DATA PROPAGATION | P2-F01 HIGH |
| 13 | KB framework GUID fallback | OBSERVATION | Merge-all on miss |
| 14 | Dashboard incremental filter | DEFECT + U12 | TaskOverdue, FrameworkEnrolled |
| 15 | Dual auditor | UNKNOWN U11 | Not runtime compared |
| 16 | Tenant filtering | DEFECT — TENANT ISOLATION | P2-F16–F18 |
| 17 | OrganizationMember denorm | DEFECT — DATA PROPAGATION | P2-F06 |
| 18 | User disable cascade | UNKNOWN U3 | Status+sessions only |
| 19 | Directory Person?organizationId | DEFECT — DATA PROPAGATION | P2-F05 |
| 20 | FE `/api/v1/api/v1` | DEFECT — UI PROPAGATION | P2-F22 MEDIUM |
| 21 | Employment UI vs Gateway | UNKNOWN | P2-F23 |
| 22 | Cert expiry reminder outbox | DEFECT — EVENT PROPAGATION | P2-F12 |

---

## 11. ID semantics (critical relationships)

| Relationship | Verdict |
| --- | --- |
| AppUser.PersonId → Person | **VALID** (login + Person GET) |
| JWT organization_id / hospital_id | **VALID** for this user |
| ProfessionalProfile.employmentId / personId / org / hospital | **VALID** |
| OrganizationMember denorm names | **STALE** possible (not joined to Person) |
| Obligation.OrganizationId | **AMBIGUOUS** / **STALE** when equal to HospitalId |
| Obligation.HospitalId for `f653e630-…` | **ORPHANED** |
| Obligation.ComplianceNodeId | **MISSING** |
| OE ownership.DepartmentId | **UNKNOWN** (table empty); upsert would require CS department **VALID** in that store |
| Employment.HospitalId vs ProfessionalProfile.HospitalId | **VALID** now; **STALE** after transfer (code) |
| DirectoryIndex | **UNKNOWN** (service down) |
| Cross-tenant obligation IDs | Two real org/hospital pairs in SQL; application-layer leak via unvalidated hospitalId is **CROSS-TENANT** risk |

---

## 12. Frontend (after backend)

Gateway `:5000` `/api/v1` was 404, so Angular was **not** exercised in a browser this session.

| Screen | Backend | Angular | Classification |
| --- | --- | --- | --- |
| OE ownership | GET catalog+journey OK; PUT department store mismatch | Calls Gateway `/hospitals/{id}/oe-ownership`; departments from Org tree | Backend GET OK; **UI propagation defect** on save |
| Organization tree | Org API 200 | `getDepartmentTree` | PASS for org screens; wrong source for OE |
| Workforce list | Employment list 200 | list/search — no personId | API filter defect |
| Employment create/delete | Gateway exists | Not in `EmploymentApiService` | UNKNOWN product |
| Professional | GET by id 200 | — | PASS if UI uses id |
| Directory / WI / dashboard Angular | Directory/WI down; dashboard CS 200 | WI/integration double prefix | FE defect independent of backend |
| Journey | Internal-only on CS | Must use Gateway `/api/v1/journey/obligations` | If UI hits CS public `/journeys`, that is FE+API |

Do not mix: OE GET returning catalog Unassigned is **backend contract**, not a stale UI cache.

---

## 13. Testability classification (Phase 1 matrix)

| Class | Dependencies |
| --- | --- |
| A. Directly testable now | Login, enroll, OE GET, employment list/get, person GET, dashboard summary, membership list, professional GET |
| B. Needs Hangfire / outbox / snapshot / bus | WI refresh, Directory rebuild, dashboard projection backlog, OutboxRelayWorker, TaskOverdue drain |
| C. Business-rule UNKNOWN | U1–U18 items not resolved; dual auditor; employment UI surface; OE catalog vs enrolled |
| D. Structurally incomplete | EmploymentTerminated never written; PersonUpdated never written; FrameworkEnrolled not a projection candidate; Identity/Person/Org/AG not relayed; mark-only processors cannot reach Service Bus by themselves |

---

## 14. Coverage metrics

```text
Phase 2 Audit Coverage
Phase 1 dependencies:                         33
Dependencies tested:                          26
Dependencies passed:                           8
Eventual-consistency passes:                   3
Dependencies with defects:                    18
Dependencies requiring investigation:         10
Business-rule unknowns:                       18 (U1–U18; several now have evidence but still need a decision)
Source mutations exercised:                    2 (invalid enroll 201; OE PUT 400 JSON parse — not a valid upsert)
Downstream APIs verified:                     12
Cross-service event chains traced:             1 (DeficiencyIdentified in-process)
Snapshot chains verified:                      0 runtime (WI/Directory down); 2 by code (WI 60min, Directory rebuild)
Tenant-isolation scenarios:                    6
P0: 4
P1: 11
P2: 6
P3: 4
```

Counts in the executive table are finding-oriented (P2-Fxx plus matrix rows), not 1:1 with the 33 Phase 1 rows.

### Confidence by major conclusion

| Conclusion | Confidence |
| --- | --- |
| Invalid hospital enroll returns 201 and writes org=hospital | HIGH |
| 1331 historical ORG_EQ_HOSP obligation rows | HIGH |
| OE GET is not JWT-hospital-bound | HIGH |
| EmploymentTerminated is never written | HIGH |
| PersonUpdated is never written | HIGH |
| OutboxRelayWorker does not publish Identity/Person/Org/AG | HIGH |
| Documented Service Bus path is not running in this environment | HIGH |
| Transfer leaves ProfessionalProfile stale | MEDIUM |
| Directory rebuild cannot filter persons by org | MEDIUM |
| FE double `/api/v1` actually 404s in browser | MEDIUM |
| Dashboard summary leaks Hospital B task counts | MEDIUM (code HIGH; cross-hospital response body not captured) |
| Invitation redeem orphan-on-failure | LOW (not mutated) |
| Dual auditor product winner | LOW |
| Integration import validation | LOW |

---

## 15. Environment notes for the next phase

- Do not treat Gateway `:5000` health as a working BFF; start the Gateway that maps `/api/v1` (integration tests mention `:5212`).
- OutboxRelayWorker will not start without a Service Bus connection string.
- The enroll of `f653e630-c9a9-4014-918d-23b33de93845` is an **audit artifact**. Phase 3 should decide whether to delete those obligations/enrollment rows.
- Identity optional PFX / `agent/cursor/identity-signing-pfx-optional` is unrelated and was not part of this audit.

---

*End of Phase 2 audit. Phase 3 is remediation only, against the backlog in section 9.*
