# AOVIAS Command — Phase 3B Data Reconciliation

**Phase:** 3B — classify existing Compliance data; no destructive cleanup  
**Status:** Reconciliation complete. Production rows were **not** modified.  
**Inspected:** 2026-08-31 against `localhost\SQLEXPRESS` databases `Compliance`, `Organization`, `Identity`  
**Worktree:** `V:\DevelopmentRepo\AOVIAS.Cursor`  
**Branch:** `agent/cursor/phase3b-data-reconciliation`  
**Phase 3A HEAD used:** `3f3e3ec898da0dc2858eb6e30ebb8a392df775ef`  
**Contracts:** [Phase 1 dependency map](./AOVIAS-Command-Module-Dependency-Map.md), [Phase 2 integrity audit](./AOVIAS-Command-Phase2-Data-Integrity-Audit.md)

**Non-goals:** event/outbox product remediation (Phase 3C+), mass-delete, rewriting `OrganizationId` without approval.

---

## A. Executive summary

OrganizationService is the authority for “does this hospital exist?” and “which organization owns it?”. In this database, **no** Organization hospital has `Id = OrganizationId` (15 hospitals, 0 equal). Therefore **`OrganizationId == HospitalId` on Compliance rows is never a legitimate organization identity** in the current Org master. It is either:

1. **INVALID** — hospital GUID copied into `OrganizationId`/`TenantId` (historical enroll/sync), or  
2. **AUDIT / accidental enroll** — hospital GUID that does not exist in Organization at all.

There is a **second, correct** obligation set for the two real hospitals that also have the invalid twin: unique index `UX_OrganizationComplianceObligations_OrgOE` is `(OrganizationId, ObjectiveElementId)` **without** `HospitalId`. Sync skips existing OEs by `OrganizationId + FrameworkId` only. Copying hospital into organization was an effective workaround so a second wave of obligations could insert. The later wave used the **true** OrganizationId (MATCH). Both waves remain.

**No child→parent cross-hospital graph** was found (deficiency→obligation, task→deficiency, evidence→task, CAPA→finding, finding→journey).

Phase 2 artifact `f653e630-c9a9-4014-918d-23b33de93845` is **325 obligations only** (plus a `FrameworkEnrolled` outbox row whose payload is timestamp-only). It was **not** deleted.

---

## B. Entity inventory

Hospital-scoped Compliance tables that actually contain rows (union of `HospitalId`):

| Table | Distinct HospitalId | Rows |
| --- | ---: | ---: |
| OrganizationComplianceObligations | 4 | 2662 |
| ComplianceTasks | 5 | 1534 |
| ComplianceSnapshots | 4 | 1725 |
| Evidences | 4 | 473 |
| ControlledDocuments | 1 | 197 |
| ComplianceDeficiencies | 1 | 177 |
| DeficiencyEscalationHistories | 1 | 167 |
| AnalyticsDailySnapshots | 5 | 88 |
| ActivityEvents | 3 | 68 |
| ClinicalIndicators | 1 | 23 |
| ScheduledAudits | 2 | 19 |
| Incidents | 1 | 18 |
| AuditSessions | 2 | 17 |
| Capas | 1 | 17 |
| Risks | 1 | 15 |
| ObjectiveJourneys | 2 | 14 |
| EscalationPolicies / SlaPolicies | 1 | 8 |
| DashboardProjections | 3 | 7 |
| ComplianceJourneys | 5 | 6 |
| WizardOnboardingAssessments | 4 | 4 |
| Alerts | 3 | 4 |
| OrganizationMaturityStageStates | 1 | 4 |
| ObjectiveElementOwnerships | 0 | 0 |

Other tables with a `HospitalId` column but **zero rows** in this snapshot (still in the diagnostic union if they gain data later): DepartmentOwnerships, RecurringTaskDefinitions, Regulations, LessonsLearned, FrameworkChapterSnapshots, AssignmentRoutingLogs (1 row), etc.

**Catalog (global, not hospital-scoped):** `ComplianceFrameworks`, `ObjectiveElements`, chapters/standards. Obligation `FrameworkId`/`ObjectiveElementId` all resolve (0 missing). `ComplianceNodeId` is **null on all 2662** obligations (cross-DB KB soft reference; Phase 2 observation, not a hospital leak).

---

## C. Hospital reference reconciliation

**Distinct Compliance `HospitalId` values:** **8**

| HospitalId | Class | Org.Hospital.OrganizationId | Org.Status | Notes |
| --- | --- | --- | --- | --- |
| `d9f5554a-bf83-4262-b386-a31587b0ba06` | VALID_HOSPITAL | `2759fad4-…` | Active | Hospital A (Phase 2 login) |
| `f17fc194-157b-4bca-940e-8762086c4a51` | VALID_HOSPITAL | `bcb783db-…` | Active | Hospital B |
| `5cd1ec4a-62e3-41bc-a331-f686e79791bf` | VALID_HOSPITAL | `b7632d36-…` | Active | Tasks/snapshots/analytics |
| `07738d43-2df7-4292-b7f7-38206e0d5e55` | VALID_HOSPITAL | `3b16e8cc-…` | Active | Very small footprint (2 refs) |
| `564a2c5c-ae08-42a8-a19e-0f3e409d55e3` | UNKNOWN | `9b8ef3fe-…` | **blank** | Exists, not deleted; status empty |
| `68cae20e-6dd9-4e14-b0a9-ed6978b1afe8` | UNKNOWN | `4a57cef0-…` | **blank** | Same |
| `f653e630-c9a9-4014-918d-23b33de93845` | MISSING_HOSPITAL | — | — | Phase 2 audit enroll |
| `edb9321a-f922-4cbc-8c1c-1d2c0d5dc1ab` | MISSING_HOSPITAL | — | — | Accidental enroll vs old `:5016` |

| Class | Distinct hospitals |
| --- | ---: |
| VALID_HOSPITAL | 4 |
| UNKNOWN (blank Status, not deleted) | 2 |
| MISSING_HOSPITAL | 2 |
| INACTIVE_HOSPITAL | 0 |
| DELETED_HOSPITAL | 0 |

Org master: 13 Active + 2 blank Status; **0** `IsDeleted = 1`.

Tasks/Evidence/Risks/CAPA/Deficiencies: **0** rows pointing at MISSING hospitals. Artifact hospitals did **not** propagate into those operational tables.

---

## D. Organization / Hospital consistency

Authority: `Organization.Hospitals.OrganizationId`.

### Obligations (2662)

| Org ownership vs Org master | Rows | OrgId == HospId |
| --- | ---: | ---: |
| MATCH (Compliance.OrganizationId = hospital’s true org) | 1006 | 0 |
| MISMATCH (OrganizationId is the hospital GUID, true org differs) | 1006 | 1006 |
| MISSING_HOSPITAL | 650 | 650 |

MISMATCH is **not** “Hospital A stored under Organization B”. It is **Hospital H stored as OrganizationId = H** while Org says Hospital H belongs to Organization O.

### Journeys (6)

All **MATCH**. None use `OrganizationId = HospitalId`. Artifact hospitals have **no** journey row.

### Dashboard projections (7)

| Pattern | Rows | Class |
| --- | ---: | --- |
| True org + hospital | 1 | MATCH |
| `OrganizationId = 00000000-…` | 3 | MISMATCH / empty org (hospital still valid) |
| `OrganizationId = HospitalId` | 3 | MISMATCH (same copy-hospital pattern) |

### Ownership matrix

0 rows — no org mismatch to report.

---

## E. Soft-GUID referential integrity

| Relationship | Result | Class |
| --- | --- | --- |
| Obligation → Framework / OE (in-DB FK) | 2662/2662 exist | VALID |
| Obligation → ComplianceNodeId | 2662 null | MISSING (by design until KB join is required) |
| Obligation → Hospital (Org HTTP/soft) | 2012 valid hospitals; 650 missing | VALID / ORPHAN |
| Deficiency → Obligation | 177/177; 0 cross-hospital | VALID |
| Task → SourceDeficiency | 0 orphans; 0 cross-hospital | VALID |
| Evidence → Task | 0 orphans; 0 cross-hospital | VALID |
| CAPA → Finding | 0 orphans; 0 cross-hospital via journey | VALID |
| Finding → Journey | 35/35; 0 tenant mismatch | VALID |
| Task.AssignedToUserId → Identity user | 10 of 1534 missing | ORPHAN / STALE |
| OE ownership → User / Department | 0 ownership rows | N/A |
| TenantId vs OrganizationId on MATCH obligations | equal | VALID |
| TenantId on INVALID twins | equals HospitalId | STALE tenant scope |

Internal obligation sync (`POST /internal/v1/obligations/sync`) still defaults `HospitalId`/`TenantId` to `OrganizationId` when those fields are empty. That path can **recreate** `OrganizationId == HospitalId` if callers omit hospital. Phase 3A enroll no longer does this.

---

## F. Duplicate candidates

Unique index `(OrganizationId, ObjectiveElementId)` has **0** duplicate keys.

Same **Hospital + Framework + ObjectiveElement** with **two rows** (different OrganizationId):

| Hospital | Framework | Duplicate OE keys | Rows in those keys |
| --- | --- | ---: | ---: |
| `f17fc194-…` | NABH Full `0fe8a9c0-…` | 503 | 1006 |
| `d9f5554a-…` | same | 503 | 1006 |

**Confirmed duplicates:** **1006 extra rows** (503 × 2 hospitals). Pair is always MATCH org-id + INVALID org=hospital twin. Same `CreatedBy = system`. Hospital B twins are ~11 seconds apart (`2026-08-02 05:00:47` vs `05:00:58`). Hospital A twins same calendar day (`2026-06-20 05:25` vs `10:31`).

**Suspected:** none beyond that pair pattern.  
**Ambiguous:** none — not version history (no status divergence required to explain two inserts).

Artifact hospitals: **325 distinct OEs, 325 rows** — not duplicated.

---

## G. Orphaned records

| Parent missing | Count | Class |
| --- | ---: | --- |
| Obligation hospital missing in Org | 650 | ORPHAN (audit/accidental) |
| Deficiency without obligation | 0 | — |
| Task without deficiency when linked | 0 | — |
| Evidence without task when linked | 0 | — |
| Finding without journey | 0 | — |
| Task assignee not in Identity | 10 | ORPHAN user |
| Journey without hospital | 0 | — |

---

## H. Cross-tenant findings

**P0 — stored OrganizationId is another live tenant’s org while hospital belongs elsewhere:** **0** obligation rows.

**P0 — child hospital ≠ parent hospital:** **0**.

**P1 — identity corruption / duplicate operational projection:** 1006 obligation twins + 6 dashboard rows with empty or hospital-copied OrganizationId. Queries that filter `OrganizationId = JWT org` **miss** the twin set; queries that filter `HospitalId` **see both twins** (double-count risk).

**P2 — UNKNOWN hospital status** (blank Org.Status) still referenced by journeys/tasks/snapshots: 2 hospitals. Not cross-tenant.

---

## I. Phase 2 audit artifact

HospitalId **`f653e630-c9a9-4014-918d-23b33de93845`**. Enrolled framework **`1ac431be-7ca8-4e7b-e98a-08deb4cbd8ae`** at **2026-08-31T05:31:36Z** (Phase 2).

| Entity | Rows |
| --- | ---: |
| OrganizationComplianceObligations | **325** (`OrganizationId = HospitalId = TenantId`) |
| ComplianceJourneys | 0 |
| Tasks, Evidence, Risks, CAPA, Deficiencies | 0 |
| Snapshots, dashboard, analytics, SLA, maturity, OE ownership | 0 |
| Outbox `FrameworkEnrolledDomainEvent` | payload has **no hospital GUID** (timestamp `2026-08-31T05:31:35Z` aligns) |

**Isolated?** Yes for hospital-scoped operational tables. Outbox enroll event is global/type-level, not hospital-keyed in payload.

**Also present (not Phase 2 official artifact):** `edb9321a-…` — **325** obligations, same pattern, created **2026-08-31T06:14:43Z** (live hit on pre-3A `:5016`). Treat as separate cleanup candidate. **Do not delete either** in 3B.

**Later cleanup (approved phase):** delete or archive obligations (and any outbox rows tied by time) for those two HospitalIds only after confirming no reporting dependency.

---

## J. Data provenance

| Pattern | CreatedBy | Dates | Provenance |
| --- | --- | --- | --- |
| MATCH obligations (1006) | `system` | 2026-06-20, 2026-08-02 | ObligationSynchronizationService after enroll / internal sync with real org id |
| INVALID org=hospital (1006) | `system` | 2026-06-20, 2026-08-02 | Same service with `organizationId: command.HospitalId` (pre-3A enroll) or internal sync hospital default |
| Artifact 325 + accidental 325 | `system` | 2026-08-31 | Invalid enroll (Phase 2 + old binary) |
| Journeys | (journey audit) | 2025-11-13 … 2026-08-02 | Enrollment aggregate; org ids MATCH |
| Dashboard empty org | — | — | Projection builder without tenant; **AMBIGUOUS** product bug vs incomplete job |

**Legitimate historical:** MATCH obligations + all MATCH journeys + operational tasks/evidence on VALID hospitals.  
**AUDIT ARTIFACT:** `f653e630`.  
**TEST/ACCIDENTAL:** `edb9321a`.  
**SEEDED:** NABH catalog (not hospital-scoped).  
**MIGRATION:** not evidenced as a SQL migration rewriting org ids; application write path.

---

## K. Cleanup candidates

### Finding C1 — Phase 2 artifact obligations

- **Entity:** `OrganizationComplianceObligations`  
- **Record count:** 325  
- **Why suspicious:** HospitalId not in OrganizationService  
- **Evidence:** SQL class MISSING_HOSPITAL; Phase 2 HTTP 201  
- **Source/provenance:** AUDIT ARTIFACT  
- **Expected correct state:** no Compliance hospital-scoped rows for a nonexistent hospital  
- **Recommended action:** archive/delete obligations (+ time-aligned enroll outbox) in a **later approved** cleanup  
- **Risk of deletion:** low operational (no tasks/journeys); reporting if anyone keyed that GUID  
- **Requires business approval:** YES  
- **Safe automated cleanup:** NO (explicit ID allowlist + backup)

### Finding C2 — Accidental `:5016` enroll

- **Entity:** `OrganizationComplianceObligations`  
- **Record count:** 325  
- **Why suspicious:** same as C1; HospitalId `edb9321a-…`  
- **Evidence:** created 2026-08-31 06:14 during Phase 3A live miss  
- **Source/provenance:** ACCIDENTAL / TEST  
- **Expected correct state:** none  
- **Recommended action:** same as C1, separate ticket  
- **Risk of deletion:** low  
- **Requires business approval:** YES  
- **Safe automated cleanup:** NO

### Finding C3 — Org=hospital obligation twins on real hospitals

- **Entity:** `OrganizationComplianceObligations`  
- **Record count:** 1006 (keep 1006 MATCH)  
- **Why suspicious:** OrganizationId ≠ Org.Hospital.OrganizationId; duplicates Hospital+FW+OE  
- **Evidence:** pair timestamps; unique index on Org+OE  
- **Source/provenance:** PRODUCTION-LIKE historical sync  
- **Expected correct state:** one obligation per hospital×OE with OrganizationId = Org master  
- **Recommended action:** after approval, delete or remap the twin where `OrganizationId = HospitalId` **only if** MATCH twin exists for same OE  
- **Risk of deletion:** **HIGH** if MATCH row is incomplete or dashboards use TenantId=hospital; double-count if kept  
- **Requires business approval:** YES  
- **Safe automated cleanup:** NO

### Finding C4 — Dashboard OrganizationId empty or equals HospitalId

- **Entity:** `DashboardProjections`  
- **Record count:** 6 of 7  
- **Why suspicious:** cannot join JWT org; hospital itself is valid  
- **Evidence:** SQL vs Org.Hospitals  
- **Source/provenance:** UNKNOWN / projection job  
- **Expected correct state:** OrganizationId = hospital’s org  
- **Recommended action:** rebuild projections (Hangfire) after org column rule is confirmed — **UPDATE**, not delete  
- **Risk of deletion:** N/A if rebuild; do not DELETE projections blindly  
- **Requires business approval:** YES  
- **Safe automated cleanup:** NO

### Finding C5 — Task assignees missing in Identity

- **Entity:** `ComplianceTasks.AssignedToUserId`  
- **Record count:** 10  
- **Why suspicious:** soft GUID orphan  
- **Evidence:** left join AspNetUsers  
- **Source/provenance:** STALE user  
- **Expected correct state:** null or retarget  
- **Recommended action:** investigate users; do not mass-unassign  
- **Requires business approval:** YES  
- **Safe automated cleanup:** NO

Identification SELECTs (not executed as DML): `scripts/dev/phase3b-proposed-cleanup.sql`.

---

## L. Business decisions required

1. **Unique obligation grain:** org×OE vs hospital×OE? Current unique index forces hospital-as-org or only one hospital per org per OE. Phase 3A writes true OrganizationId — a **second hospital in the same org cannot insert** a second set of obligations. Product must choose a schema change vs keep a documented workaround.  
2. **Twin deletion:** keep MATCH only, or merge status columns from INVALID twin first?  
3. **Artifact + accidental 650 rows:** delete, quarantine, or keep as forensic samples?  
4. **Blank hospital Status** in Organization: treat as Active or Inactive for future enroll?  
5. **ComplianceNodeId** always null: accept or backfill from KB?  
6. **Dashboard** empty OrganizationId: rebuild vs backfill?  
7. **Internal sync** defaulting hospital to organizationId: change in a later engineering task (code), not 3B data.

### Do not touch (3B)

- Phase 2 artifact rows (until approved cleanup)  
- MATCH obligations and MATCH journeys  
- All tasks/evidence/risks/CAPA/deficiencies on VALID hospitals  
- Catalog frameworks/OEs  
- Mass `OrganizationId` rewrite without a grain decision (item 1)

---

## Diagnostics run

```text
sqlcmd -S localhost\SQLEXPRESS -E -i scripts/dev/phase3b-reconciliation-readonly.sql
```

Results snapshot: `scripts/dev/phase3b-reconciliation-results.txt` (read-only output).

**Production data modified:** NO (`DELETE`/`UPDATE`/`TRUNCATE`/`DROP`/`ALTER` were not executed).
