# AOVIAS Command — Phase 3G Runtime Verification & Consistency Decisions

**Phase:** 3G — proof and decision gate (not broad remediation)  
**Worktree:** `V:\DevelopmentRepo\AOVIAS.Cursor`  
**Branch:** `agent/cursor/phase3g-runtime-verification`  
**Starting HEAD:** `342acfdc30885f9513835438444b021c63eafebf` (Phase 3F)  
**Canonical obligation identity:** unchanged `(HospitalId, FrameworkId, ObjectiveElementId)`  
**Local database:** `localhost\SQLEXPRESS` / `Compliance` (Trusted_Connection)

---

## 1. Executive summary

Phase 3G runtime-proofed the Phase 3F in-process dashboard drain **without starting a second Compliance Hangfire server**. The process already bound to `:5016`/`:7087` is `V:\DevelopmentRepo\AOVIAS\...\ComplianceService.exe` (human worktree, started 2026-08-31 09:46). Cursor therefore drained via an opt-in SQL test host (`PHASE3G_DRAIN=1`) with **no Hangfire**.

| Goal | Result |
| --- | --- |
| 7 `FrameworkEnrolledDomainEvent` | **Not projectable.** Payload is `{ OccurredOn }` only. Rows left unprocessed (`ProjectionProcessedOn` null, `RetryCount=1`, error preserved). |
| 607 `TaskOverdue` | **Processed.** All 749 TaskOverdue rows now have `ProjectionProcessedOn`. Incremental counts written under **HospitalId-as-OrganizationId** keys. |
| Dashboard API | **200** for Hospital A snapshot/summary with Development JWT. Snapshot overdue **134** comes from the existing **org+hospital+framework** complete projection (`LastEventType=ComplianceFrameworkSnapshotRecalculated`), not from the TaskOverdue hospital-keyed rows. |
| Service Bus | Not configured, not required for this path. |
| WI / Directory / Profile / JWT / Identity / Org outbox | Models classified from **code + existing docs**. Immediacy SLAs that are not written remain **BUSINESS DECISION REQUIRED**. |

Compliance outbox uses **`ProjectionProcessedOn`**, not Identity’s `ProcessedOn`. Section 5 of the phase brief maps to `ProjectionProcessedOn` on this table.

---

## 2. Runtime environment

| Item | Value |
| --- | --- |
| SQL | `DESKTOP-N5M6STN\SQLEXPRESS` / `Compliance` |
| JWT | Development `JwtSettings` (Issuer/Audience `https://localhost:7135`) |
| Identity | Cursor `IdentityService` `:7135` / `:5062` |
| Compliance HTTP | Human-tree host `:7087` / `:5016` — **not recycled** (would add a second Hangfire server) |
| Gateway | `:5000` |
| WebApp | `:4200` listening — **UI not logged in this session** |
| Drain | `DashboardProjectionLocalSqlDrainTests` against Express; Hangfire not started |
| Unrelated dirty | `IdentityService.csproj` — not modified/committed |

Unauthenticated `GET /api/v1/compliance/snapshots/hospital/{HospitalA}` → **401**. Authenticated Hospital A snapshot and dashboard summary → **200**.

---

## 3. Enrollment projection verification

**IDs (not deleted):**

| Id | OccurredOn (UTC) | AggregateId | Payload |
| --- | --- | --- | --- |
| `12AD239A-92C7-4F82-8BF8-83BFF724A473` | 2026-04-06 | `C9B881B5-0000-0000-C564-FDB900000000` | `{"OccurredOn":"..."}` |
| `D93F72DB-B284-4CA6-9D4B-0B2A67F89601` | 2026-06-16 | `A79C971F-0000-0000-CDE5-901600000000` | OccurredOn only |
| `B97440D4-3786-4FF6-8D05-5970877DD6DC` | 2026-06-20 | `888EDCFD-0000-0000-CDE5-901600000000` | OccurredOn only |
| `1E2003C3-893D-4AF3-B956-285BB4AA8F95` | 2026-08-02 | `8978B673-0000-0000-CDE5-901600000000` | OccurredOn only |
| `ECE7977E-C7FF-40F8-ADB2-0E6ECA31C7A6` | 2026-08-31 05:31 | `405C4B35-0000-0000-4B0C-6F2400000000` | OccurredOn only |
| `D3DAC760-ED1F-43E3-9420-4D5F9DB847C5` | 2026-08-31 06:14 | `26D98AB9-0000-0000-4B0C-6F2400000000` | OccurredOn only |
| `7CCEF0D8-064C-4313-BEF1-C6CF663371FA` | 2026-08-31 06:14 | `405C4B35-0000-0000-4B0C-6F2400000000` | OccurredOn only |

**Cause:** `OutboxDomainEventDispatcher.AddOutboxMessageAsync` serialized `IDomainEvent` (interface has only `OccurredOn`). HospitalId/FrameworkId never reached JSON. AggregateId is a **hash** of hospital+framework — not reversible.

**Before drain:** 7 eligible (`RetryCount=0`, `ProjectionProcessedOn` null).  
**After drain:** 7 remaining, `RetryCount=1`, `ProjectionProcessedOn` **null**, `Error` set (payload missing HospitalId/FrameworkId). **Not marked processed.**

**Fix applied (forward-only):** serialize `domainEvent.GetType()`. Future enrolls will carry HospitalId/FrameworkId. Historical seven **cannot** be reconstructed from payload.

Journeys exist (Hospital A `d9f5554a-…` + NABH `0fe8a9c0-…` + org `2759fad4-…`). Dashboard already has a complete row for that triple from snapshot recalculation — enrollment **read model** for Hospital A is present independently of these seven outbox rows.

---

## 4. TaskOverdue projection verification

**Before:** 749 total TaskOverdue; **607** pending (`RetryCount<5`, `ProjectionProcessedOn` null); 142 already projected.  
**After:** pending **0**; processed **749**; failed **0**. Events not deleted.

Incremental updater writes `DashboardProjection` keyed by envelope `TenantId` (see §9). Hospital A hospital-keyed row: `OverdueTasksCount=228`, `IsComplete=0`. Live SQL overdue tasks for Hospital A: **358**.

---

## 5. ProcessedOn evidence

Compliance column: **`ProjectionProcessedOn`**.

| Type | Processed | Unprocessed | Notes |
| --- | --- | --- | --- |
| TaskOverdue | 749 | 0 | Timestamp set only after `ProcessOutboxMessageAsync` without throw |
| FrameworkEnrolled | 0 | 7 | Guard throws; retry/error stored; **not** marked processed |

---

## 6. Dashboard API verification

Host: `https://localhost:7087` (human-tree Compliance, same SQL).

| Call | Result |
| --- | --- |
| Snapshot, no JWT | **401** |
| `GET /api/v1/compliance/snapshots/hospital/{HospitalA}?frameworkId={NABH}` | **200** `overdueTasks: 134`, `totalJourneys: 1`, `isSnapshotAvailable: true` |
| `GET /api/v1/dashboard/summary?hospitalId={HospitalA}` | **200** `totalTasks: 503`, `pendingTasks: 377` (live summary path, not projection table) |

Snapshot **134** matches the complete projection `(OrganizationId=2759fad4-…, HospitalId=d9f5554a-…, FrameworkId=0fe8a9c0-…)` `LastEventType=ComplianceFrameworkSnapshotRecalculated`. It does **not** read the TaskOverdue hospital-keyed row (228).

**API VERIFIED** (snapshot + summary). **UI VERIFICATION PENDING** (WebApp `:4200` up; not exercised logged-in).

---

## 7. Idempotency verification

Unit: same outbox id twice → one rebuild / one overdue increment (Phase 3F).  
Runtime: after drain, `ProcessOutboxMessageAsync` twice on a processed TaskOverdue → `DataVersion` unchanged.

---

## 8. TaskOverdue catalog-FK analysis

**Classification: EXPECTED INVARIANT**

- EF: required `HasOne(ComplianceStandard)` / `HasOne(ObjectiveElement)`, `OnDelete(Restrict)`.
- SQL: `FK_ComplianceTasks_ComplianceStandards_ComplianceStandardId` present.
- Local data: **0** overdue tasks missing a `ComplianceStandards` row.
- In-memory diagnostic: a task without a catalog graph is dropped by required-nav join; overdue count **0**; handler does **not** throw.

Production SQL cannot insert an overdue task without a standard. The Phase 3F in-memory caveat is a **test-provider artifact**, not a live data hole. No projection semantic change.

---

## 9. TaskOverdue TenantId analysis

**Classification: INCORRECT** (CONFIRMED BY CODE)

`IComplianceEventEmitter.EmitAsync(..., Guid tenantId, ...)` stores `ComplianceEventEnvelope.TenantId`.  
`TaskOverdueEscalationJob` passes `task.HospitalId`.  
`ResolveComplianceEventScopes` uses `envelope.TenantId` as **OrganizationId**.

Elsewhere (journeys, obligations, snapshot API) tenant = **organization**. Nested payload `HospitalId` is what `RefreshTasksAsync` filters on (organization parameter unused in that method).

No consumer in this drain required org-on-envelope. **Do not change the job** until product confirms envelope TenantId = OrganizationId (then incremental keys would match the API).

---

## 10. Workforce consistency decision

**Live operational model: SNAPSHOT** (CONFIRMED BY CODE)

Evidence: `SnapshotSchedulerHostedService` default **60 minutes**; `POST .../intelligence/refresh`; snapshot tables; `IWorkforceSnapshotService.RefreshOrganizationAsync`.  
Dormant: `IntelligenceEventConsumerHostedService` returns immediately when Service Bus connection is empty; handlers exist but `EmploymentCreated` payload is `{ employmentId }` only.

Whether product **requires immediate** WI after employment mutations is **not** stated in `06-Business-Rules.md`. That SLA remains **BUSINESS DECISION REQUIRED**. Implementation of event-driven WI is **not** approved by this phase.

---

## 11. ProfessionalProfile transfer decision

**BUSINESS DECISION REQUIRED** for “must follow employment immediately.”

**Current code (CONFIRMED):** profile copies `OrganizationId`/`HospitalId` from employment **at create**. `EmploymentAppService.TransferAsync` updates employment + `TransferRecord`; does **not** call ProfessionalService. No architecture rule says profile org/hospital is a projection of employment.

---

## 12. Employment termination decision

**SNAPSHOT / rebuild freshness** for WI (~60 min) and Directory (~360 min) is the **implemented** path.  
`EmploymentTerminated` is registered by WI/Directory but **never written**.

Immediate WI/Directory/profile updates after soft-delete: **BUSINESS DECISION REQUIRED**. Do not invent IMMEDIATE.

---

## 13. Directory consistency decision

**Live operational model: REBUILD** (CONFIRMED BY CODE)

`IndexRebuildIntervalMinutes` default **360**; `POST /directory/rebuild`; rebuild HTTP-pulls persons/employments/profiles. Person list query uses `!IsDeleted` — **soft deletes drop off on rebuild**. Person **updates** appear on next rebuild (no `PersonUpdated` writer).

Event consumer is SB-gated and unused locally. Hybrid is **code-present, not live**. Switching to event-first: **BUSINESS DECISION REQUIRED**.

---

## 14. JWT consistency decision

**NEXT TOKEN** (CONFIRMED BY CODE)

`AuthenticationService` loads `GetUserAuthContextAsync` at **token issuance**. Session refresh rotates tokens and re-reads auth-context. No membership outbox consumer invalidates JWTs. ADR-015 treats JWT hospital as login/refresh source of truth for the shell — not push-on-membership.

Immediate claim refresh without new token: **BUSINESS DECISION REQUIRED** if product wants it.

---

## 15. Identity SecurityEventRaised

**UNUSED** as a cross-service contract; **AUDIT-ONLY** locally (CONFIRMED BY CODE).

Writer: `SecurityAppServices` → outbox. Processor: `IdentityOutboxProcessor` logs and sets `ProcessedOn` (mark-only). Not on `OutboxRelayWorker`. Phase 3E backlog (~3498) is drain/hosting, not a bus consumer.

Do not enable Service Bus for this event without a SIEM/consumer BDR.

---

## 16. Organization outbox

**OBSOLETE as a bus contract** for current login/membership (CONFIRMED BY CODE).

Writers: `MembershipCreated`, `MembershipTransferred` (`{}`), `OrganizationHierarchyChanged`, `DepartmentCreated`, trial events. **No** Organization outbox processor. Membership → JWT is HTTP auth-context at next token.

Do not delete writers in this phase.

---

## 17. Service Bus status

`OutboxRelayWorker` `ConnectionStrings:ServiceBus` = `""`. Worker cannot start. WI/Directory consumers disable themselves when empty.

**CODE VERIFIED** (relay exists). **TEST VERIFIED** (not a bus test). **RUNTIME BUS PENDING**.

In-process dashboard drain does not need the bus.

---

## 18. Final decision matrix

| Question | Current behavior | Evidence | Decision | Implementation phase |
| --- | --- | --- | --- | --- |
| Enrollment dashboard | Recognize `FrameworkEnrolled`; historical 7 lack IDs | Dispatcher serialized `IDomainEvent`; SQL payloads | CONFIRMED BY CODE — fix serializer; historical 7 not replayable | 3G serializer; 3H new enrolls only |
| TaskOverdue dashboard | Drain + incremental; API uses org-scoped complete projection | SQL 749 processed; snapshot 134 | CONFIRMED BY CODE — processing works; API key mismatch | 3H if TenantId=org |
| WI freshness | 60 min snapshot + manual refresh | `SnapshotSchedulerHostedService`, refresh API | SNAPSHOT (CONFIRMED BY CODE). Immediate = BDR | none unless BDR |
| Employment → ProfessionalProfile | Create copies org/hospital; transfer does not | `ProfessionalProfileAppService` / `TransferAsync` | BUSINESS DECISION REQUIRED | none |
| Employment termination | Soft-delete; no `EmploymentTerminated` | Workforce delete tests | SNAPSHOT/rebuild live; IMMEDIATE = BDR | none |
| Directory | Rebuild 360 min; events unused | `DirectoryOptions`, projector | REBUILD (CONFIRMED BY CODE). Event-first = BDR | none |
| JWT | Auth-context at issue/refresh | `AuthenticationService` | NEXT TOKEN (CONFIRMED BY CODE) | none |
| SecurityEventRaised | Mark-only Identity outbox | `IdentityOutboxProcessor` | UNUSED / AUDIT-ONLY | none |
| Organization outbox | Writers, no processor | Org services | OBSOLETE (bus) | none |
| TaskOverdue TenantId | HospitalId in tenant slot | Escalation job + envelope | INCORRECT | 3H only if BDR |

---

## 19. Remaining implementation backlog

1. New enrollments: confirm outbox JSON contains HospitalId/FrameworkId after dispatcher fix.  
2. Historical 7 FrameworkEnrolled: leave in table or archive by **human** policy — cannot reconstruct.  
3. Optional: emit TaskOverdue `TenantId` = journey `TenantId` (org) so incremental keys match snapshot API.  
4. Background drain + mandatory tenant filters: `IgnoreQueryFilters` on journey tenant lookup (applied in 3G) so a future Cursor Compliance host can rebuild enrolls.  
5. WI/Directory/profile/JWT immediacy — only after BDRs.  
6. Runtime bus / emulator — not required for dashboard in-process path.

---

## 20. Phase 3H recommendations

1. Run one enroll through Cursor Compliance; verify outbox payload completeness and dashboard rebuild on **org** key.  
2. Decide TaskOverdue envelope tenant = OrganizationId vs document hospital-keyed projections as non-API.  
3. Close BDRs (WI immediacy, Directory event-first, profile-on-transfer, termination immediacy, JWT push).  
4. Do not start a second Compliance Hangfire against this Express instance while the AOVIAS host is running.  
5. Do not treat empty local Service Bus as a dashboard failure.
