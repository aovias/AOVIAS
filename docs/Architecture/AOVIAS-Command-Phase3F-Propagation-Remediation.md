# AOVIAS Command — Phase 3F P1 Propagation Remediation

**Phase:** 3F  
**Branch:** `agent/cursor/phase3f-p1-propagation-remediation`  
**Starting HEAD:** `78f593d6b1d635497c533ad24c15f8d84a3144a9` (Phase 3E)  
**Canonical obligation identity:** unchanged `(HospitalId, FrameworkId, ObjectiveElementId)`

---

## 1. Phase 3E findings addressed

| ID | Finding | 3F action |
| --- | --- | --- |
| P1-1 | `FrameworkEnrolledDomainEvent` not a dashboard candidate | **Implemented** (recognize existing event; no second event type) |
| P1-2 | 607 `TaskOverdue` unprojected (`RetryCount=0`) | **Implemented** (drain + TaskOverdue incremental fields) |
| P1-3 | Workforce payloads / terminated / ProfessionalProfile | **Not implemented** — BDR |
| P1-4 | PersonUpdated / PersonDeleted vs Directory rebuild | **Not implemented** — BDR |

---

## 2. P1 fixes implemented

### Enrollment → dashboard

- `InProcessOutboxProcessor.IsDashboardProjectionCandidate` includes `FrameworkEnrolled`.
- `DashboardProjectionEngine.RequiresFullRebuild` includes `FrameworkEnrolled` (same as `ComplianceEnrolled`).
- Scope resolution deserializes `FrameworkEnrolledDomainEvent` and loads tenant from `ComplianceJourneys` (payload has hospital + framework, not organization).
- No duplicate enroll event type.

### TaskOverdue → dashboard

Root cause: Hangfire `TaskOverdueEscalationJob` writes outbox but **nothing periodically called** `InProcessOutboxProcessor`. Command paths only drain on deficiency/command. Rows sat at `RetryCount=0`, not handler poison.

Fixes:

- `OutboxProjectionDrainHostedService` (every 30s after 15s warmup).
- Job calls `ProcessPendingAsync` after emit.
- `IsTaskEvent` includes `TaskOverdue` so incremental projection refreshes overdue counts.

---

## BUSINESS DECISION REQUIRED (not implemented)

No product document (Phase 1 map, Phase 2–3E, constitution, or 06-Business-Rules) picks a consistency model for the following. Phase 3F **stopped** here and added gap-lock tests only.

| Item | Choices | Current code (unchanged) |
| --- | --- | --- |
| WI freshness | Event-driven vs ~60 min `SnapshotSchedulerHostedService` / manual refresh | Snapshot path only; `EmploymentCreated` payload is `{ employmentId }` |
| Employment transfer → ProfessionalProfile | Profile follows employment immediately vs independent vs event | Transfer updates Employment + TransferRecord; profile org/hospital set only at profile create |
| Employment termination immediacy | Immediate WI/Directory vs next snapshot/rebuild | Soft-delete; **no** `EmploymentTerminated` outbox |
| Directory consistency | Event-driven vs rebuild (~360 min) vs hybrid | Rebuild/scheduler; `PersonUpdated` / `PersonDeleted` **not** written |
| JWT hospital claims | Immediate refresh vs next login | Next token (Phase 3E INT) |
| Identity `SecurityEventRaised` outbox | Local telemetry vs bus | Mark-only, no relay |
| Organization outbox writers | Need processor vs none | Writers exist; no processor |

**Do not** treat snapshot delay as a bug until product picks IMMEDIATE.

---

## 4. Event contracts changed

- **No new event types.**
- Enrollment: still `FrameworkEnrolledDomainEvent` JSON (`HospitalId`, `FrameworkId`, `RequestedByUserId`, `OccurredOn`). Organization comes from journey lookup.
- TaskOverdue: still `ComplianceEvent.TaskOverdue` envelope; nested payload still includes `HospitalId`.

---

## 5. Consumers changed

- `DashboardProjectionEngine` / `DashboardProjectionUpdater` only.
- No Service Bus consumers. No mark-only processors rewritten.

---

## 6. Projection changes

- Enroll → full rebuild.
- TaskOverdue → incremental task summary.
- Field-ownership map documents both.

---

## 7. Snapshot decisions

WI remains ~60 min snapshot until BDR. Directory remains rebuild/scheduler until BDR. Unchanged.

---

## 8. Idempotency strategy

- Rebuild/incremental skip when `LastProcessedOutboxMessageId == message.Id`.
- Tests process the same outbox id twice.

---

## 9. Historical event replay

Eligible on local Express (2026-08-31): **7** `FrameworkEnrolledDomainEvent` + **607** `TaskOverdue` (`RetryCount < 5`).

Replay mechanism: in-process drain (hosted service / next TaskOverdue job / any `ProcessPendingAsync`). **Not executed in this session** (Compliance host not started against the DB). Events were **not** deleted.

---

## 10. Tests

- `DashboardProjectionPropagationTests` (4): enroll candidate, enroll rebuild idempotency, TaskOverdue incremental idempotency, processor drain.
- Workforce `EmploymentAppServiceTests`: Created payload `{ employmentId }` only; transfer payload `{}`; delete writes no `EmploymentTerminated`.
- Person: update/delete write no `PersonUpdated` / `PersonDeleted`.
- Phase 3A `HospitalAccessAuthorizerTests` / `EnrollHospitalAccessTests`.
- Phase 3D `ObligationCanonicalIdentityTests`.

Session results: Compliance filter **22/22**; Workforce employment tests **7/7**; Person gap-lock tests **2/2**. (`PersonAppServiceTests.CreateAsync_RejectsDuplicateEmail` still fails independently with NRE on mock `IWebHostEnvironment` — not changed in 3F.)

---

## 11. Runtime verification

| Layer | Result |
| --- | --- |
| CODE | VERIFIED |
| TEST | VERIFIED (see completion report) |
| Historical drain against SQL Express | **PENDING** until Compliance process runs drain |
| Service Bus | **PENDING** (still unconfigured; not required for these in-process fixes) |

---

## 12. Deferred items / 13. Remaining business decisions

- Directory event vs rebuild (~360 min)
- WI event vs snapshot (~60 min)
- Employment transfer → ProfessionalProfile
- JWT refresh vs next-login
- Identity `SecurityEventRaised` purpose / 3498 backlog
- Organization outbox writers vs no processor
- Whether TaskOverdue envelope `TenantId` (currently job passes `HospitalId`) should become real org id (task entity has no OrganizationId; incremental counts use `HospitalId`)

---

## 14. Phase 3G recommendations

1. Run Compliance so drain projects 614 historical rows; measure `ProjectionProcessedOn`.
2. Close WI/Directory BDRs then either document snapshot SLA in product UX or add payloads + relay.
3. Optionally align TaskOverdue `TenantId` with journey tenant if alerts join needs it.
4. Do not require Azure Service Bus for local dashboard projection.
