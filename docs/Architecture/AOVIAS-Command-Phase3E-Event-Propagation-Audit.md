# AOVIAS Command — Phase 3E Event & Outbox Propagation Audit

**Phase:** 3E — contract establishment (no broad remediation)  
**Inspected:** 2026-08-31, worktree `AOVIAS.Cursor`  
**Starting HEAD:** `7027e898eddd59b2f431ed379d5e69a6329ea142` (Phase 3D Gate B)  
**Code/data changes:** none (documentation only)  
**Canonical Compliance identity (do not regress):** `(HospitalId, FrameworkId, ObjectiveElementId)`

Contracts: [Phase 1 map](./AOVIAS-Command-Module-Dependency-Map.md), [Phase 2 audit](./AOVIAS-Command-Phase2-Data-Integrity-Audit.md), [12-Event-Architecture](./12-Event-Architecture.md).

---

## 1. Executive summary

The README line “transactional outbox → Azure Service Bus (`OutboxRelayWorker`)” describes **one intended Compliance/workforce relay path**. Runtime is **not** a single bus:

| Layer | Reality |
| --- | --- |
| Cross-module mutations that must succeed now | **Synchronous HTTP** (Identity→Person, login→org auth-context, employment→Person/Hospital, enroll→obligations) |
| Same-database side effects | **In-process outbox** (Compliance `DeficiencyIdentified` → remediation task; selected dashboard candidates) |
| Read models | **Snapshot / Hangfire / rebuild** (WI ~60 min, Directory ~360 min, dashboard Hangfire + incremental) |
| Cross-service bus | **Implemented, not configured, not running** locally (`ServiceBus` connection string empty; worker throws if started) |
| Most hosted “processors” | **Mark-only** (`ProcessedOn = UtcNow` after a log line). For Workforce/Professional/Governance/Directory/Integration this is a **gate** before relay (`RelayProcessedOn` requires `ProcessedOn`), not a publisher. Identity/Person/Organization/AccessGovernance have **no relay at all**. |

Phase 3E does **not** treat every outbox row as a defect. Security events that nobody consumes can stay local. Snapshot staleness can be a contract. Missing `EmploymentTerminated` / `PersonUpdated` is a defect **only if** Directory/WI are expected to be event-driven rather than rebuild/snapshot.

**Verified complete chain this session:**  
`DeficiencyIdentified` outbox row has `ProjectionProcessedOn` set; `ComplianceTasks` count still **1534** after Phase 3D. That path is in-process, not Service Bus.

**Not verified (infrastructure):** any Service Bus hop. Local `OutboxRelayWorker` `ConnectionStrings:ServiceBus` is `""`.

---

## 2. Propagation principles

Use the **narrowest** mechanism that matches the consistency requirement:

| Code | When to use |
| --- | --- |
| **A. Synchronous HTTP** | Caller cannot complete without target existence/authorization (Person, Hospital, enroll org lookup). |
| **B. Outbox + bus** | Another **service/database** must react without sharing a transaction; consumer must be idempotent. |
| **C. In-process** | Same service, same DB; side effect in the same reliability domain (task from deficiency). |
| **D. Snapshot / rebuild** | Aggregates/dashboards that are expensive; document SLA (minutes/hours). |
| **E. Pull-on-read** | Freshness required at request time (JWT claims, live training matrix). |
| **F. None** | Informational IDs, dual-write legacy, or independent admin surfaces. |

**Do not** publish CRUD to the bus by default. **Do not** replace snapshots with events unless SLA is IMMEDIATE and a consumer exists.

Mark-only processors are **INCOMPLETE** relative to docs if a **registered bus consumer** exists for that event. They are **INTENTIONAL drains** if no consumer is required (local audit/security telemetry).

---

## 3. 33-dependency propagation matrix

Phase 1 catalogued **33** cross-module dependencies. Expected mechanism is the **contract** Phase 3F should implement against, not a wish to bus everything.

Legend — **Status:** OK = chain matches contract; GAP = contract needs a hop that is missing/broken; BDR = business must choose contract; INT = intentional snapshot/pull.

| # | Source | Mutation | Target | Expected | Current | Actual behavior | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Identity | User register | Person | A HTTP | HTTP `POST /internal/persons` | Person created; `PersonId` on user | OK |
| 2 | Identity | Login | Org membership | E pull-on-read | HTTP auth-context | JWT from **Active** membership at token issue | OK / INT |
| 3 | Organization | Invitation redeem | Identity+Person+Membership | A HTTP | Gateway orchestrator | Sync provision | OK |
| 4 | Onboarding | Signup/onboard | Identity+Org+Compliance+KB | A HTTP | `OnboardWizardCommandHandler` | Sync enroll + journeys | OK |
| 5 | Person | Create | Directory | D snapshot **or** B bus | Outbox `PersonCreated` mark-only; Directory rebuild HTTP | Event never consumed; rebuild/scheduler is the live path | INT / GAP if event-first |
| 6 | Person | Update | Directory / member denorm | BDR | No `PersonUpdated` writer; Directory **registers** handler | Names/email do not follow Person until rebuild | BDR / GAP |
| 7 | Person | Soft-delete | Identity/membership/Directory | BDR | No outbox; no cascade | SoftDelete local only | BDR |
| 8 | Organization | Membership create/transfer | Identity JWT | E next token | Auth-context HTTP | Immediate token refresh **not** implemented | INT (login boundary) |
| 9 | Organization | License seats | Gateway entitlements | A persist + E middleware | `LicenseEntitlementMiddleware` | Next request sees seats | OK |
| 10 | AccessGovernance | Role assign/revoke | Evaluate API | A persist + E | Evaluate reads DB; AG outbox mark-only | Permissions change without bus | OK / F for outbox |
| 11 | Identity | Suspend/disable user | AG/seats/membership | BDR | Login blocked; cascade UNKNOWN | No event cascade found | BDR |
| 12 | Communication | In-app notify | Person | E HTTP contact | `GET /internal/users/{id}/contact` | Needs PersonId | OK |
| 13 | Workforce | Employment create | Person+Hospital | A HTTP | Internal GET then persist + outbox `EmploymentCreated` | Create fails if lookups fail; outbox not bus-published | OK (HTTP); GAP (WI event) |
| 14 | Workforce | Transfer | Professional + WI | BDR | Employment + `TransferRecord` + outbox `"{}"`; **no** ProfessionalProfile update | WI would ignore `{}` even if bus ran | BDR / GAP |
| 15 | Workforce | Promote | Position + WI | D + optional B | `EmploymentPromoted` outbox; WI **does not** register that type | Position local; WI snapshot only | INT / GAP if event-first |
| 16 | Workforce | Soft-delete | WI / Directory | B if event-first else D | No `EmploymentTerminated` written; consumers exist | Directory/WI handlers never fire | GAP / BDR |
| 17 | Professional | Profile create | Workforce | A HTTP | Validates employment | OK | OK |
| 18 | Professional | Training attendance | WI matrix | D snapshot; live matrix E | Phase-4 **no** outbox; Professional matrix live EF; WI ~60 min | INT | INT |
| 19 | Organization | Dept/OU | Compliance OE ownership | F / BDR | Split stores; OE uses Compliance departments | Mapping UNKNOWN | BDR |
| 20 | WI | Snapshot refresh | WF+Professional+Gov+Org | D + E pull | `POST /intelligence/refresh` + scheduler | Dashboards read WI DB | OK / INT |
| 21 | Organization | Hospital create | Journey/OE matrix | E parameterized | Matrix by `hospitalId`; enroll is separate HTTP | Catalog OEs without enroll | OK |
| 22 | Compliance | Enroll | Obligations | A + C | `ObligationSynchronizationService` canonical key; `FrameworkEnrolledDomainEvent` outbox | Obligations created in-process; dashboard **does not** treat FrameworkEnrolled as candidate | OK (obligations); GAP (dashboard event) |
| 23 | Onboarding/KB | Assessment apply | Obligations/deficiencies | A / C | Obligation projection + bootstrap | Same service | OK |
| 24 | Compliance | Deficiency identified | Tasks | C in-process | `InProcessOutboxProcessor` → `EnsureRemediationTaskAsync` | **Verified** (`ProjectionProcessedOn` set; tasks 1534) | OK |
| 25 | Compliance | Evidence upload | Obligation + dashboard | C incremental | Projection rerun; `EvidenceUploaded` incremental | In-process if type matches candidate/`ProcessOutboxMessageAsync` | OK / partial |
| 26 | Compliance | Finding promote | Deficiency/CAPA | C / A | Same service APIs | OK | OK |
| 27 | Compliance | OE ownership upsert | Notifications | B or C | Publisher mapping `OeOwnershipAssigned`; Communication SB consumer needs `compliance-events` | Local persist; notify if bus | GAP (bus) / OK (persist) |
| 28 | Compliance | Obligation/score | Dashboard/analytics | C + D Hangfire | Candidates processed in-process; Hangfire snapshots | Enrollment type mismatch; TaskOverdue 607 unprojected | GAP (naming + TaskOverdue) |
| 29 | KnowledgeBase | Gap analysis | Copilot/UI | E HTTP | KB persist; UI reads KB | OK | OK |
| 30 | Governance | Committee decision | Compliance IDs + WI | F soft GUID; optional B | Soft IDs; WI listens `CommitteeDecisionRecorded` if bus | No bus | INT / GAP if event-first |
| 31 | AuditService | Legacy audit | Compliance auditor | F dual SoT | Two models | Not event-coupled | F |
| 32 | AIOrchestrator | Assist | Copilot | A HTTP | Named HttpClient | OK | OK |
| 33 | Integration | Import/webhooks | Platform writes | A HTTP | `PlatformWriteClient` | Per-connector | OK / review |

---

## 4. Outbox inventory

### 4.1 Processors (code)

| Service | Processor | Publishes? | Local consumer? | Marks ProcessedOn? | Relayed by worker? |
| --- | --- | --- | --- | --- | --- |
| Identity | `IdentityOutboxProcessor` | No | No | Yes (+ retry cap 5) | **No** |
| Person | `PersonOutboxProcessor` | No | No | Yes | **No** |
| Organization | **none** | — | — | — | **No** |
| AccessGovernance | `AccessGovernanceOutboxProcessor` | No | No | Yes | **No** |
| Workforce | `WorkforceOutboxProcessor` | No | No | Yes | Yes **if** ProcessedOn **and** SB + `WorkforceConnection` |
| Professional | `ProfessionalOutboxProcessor` | No | No | Yes | Same workforce-events relay |
| Governance | `GovernanceOutboxProcessor` | No | No | Yes | Same |
| Compliance | `InProcessOutboxProcessor` | No bus | **Yes** (task, snapshot queue, dashboard engine) | `ProjectionProcessedOn` | `OutboxRelayBackgroundService` on `RelayProcessedOn` |
| Communication | `CommunicationOutboxProcessor` | No | Outbound is a **different** hosted service | Yes | No |
| Directory | `DirectoryOutboxProcessor` | No | No | Yes | directory-events if SB |
| WI | `IntelligenceOutboxProcessor` | No | No | Yes | No |
| Integration | `IntegrationOutboxProcessor` | No | No | Yes | integration relay if SB |
| AI | `AIOutboxProcessorHostedService` | Separate SB publisher if configured | Optional consumer | varies | No Identity-style table on `AIOrchestrator` DB |

### 4.2 Local SQL Express evidence (2026-08-31)

| Database | Rows | Notes |
| --- | ---: | --- |
| Identity | 3498 | **All** `ProcessedOn` null (processor not draining this DB, or Identity not hosted against it). Types: `SecurityEventRaised` 3403, plus compromise/session/register/risk |
| Person | 27 | All `PersonCreated`; all processed (mark-only) |
| Organization | 0 | Table exists; writers (`MembershipCreated`, `MembershipTransferred` `{}`, hierarchy, department) unused here |
| AccessGovernance | 0 | |
| Workforce | 0 | Transfer/create outbox not present in this DB |
| Professional | 0 | |
| Governance | 0 | |
| Compliance | 3582 | `RelayProcessedOn` null on **3575**. Unprojected **614** (607 `TaskOverdue` + 7 `FrameworkEnrolledDomainEvent`). `DeficiencyIdentified` **1**, projected. |
| EnterpriseDirectory / Communication / WI / Integration | DBs not on this instance | Code-inspected only |
| AIOrchestrator | No `OutboxMessages` table | |

### 4.3 Payload notes

| Event | Payload | Usable by WI `ExtractOrganizationId`? |
| --- | --- | --- |
| `EmploymentCreated` | `{"employmentId":"..."}` | **No** |
| `EmploymentTransferred` | `{}` | **No** — literal empty JSON in `TransferAsync` |
| `PersonCreated` | `{"personId":"..."}` | N/A for WI; Directory person-updated wants org in tests |
| `FrameworkEnrolledDomainEvent` | JSON of record (`HospitalId`, `FrameworkId`, `RequestedByUserId`, `OccurredOn`) | N/A; dashboard filter still misses type name |
| Org `MembershipTransferred` | `{}` | — |

Empty `"{}"` on transfer is **not** a serializer bug; it is a hardcoded string.

---

## 5. Relay architecture

**Intended single relay for bus:** `OutboxRelayWorker` (`Program.cs`).

| Hosted service | Source DB | Topic | Predicate |
| --- | --- | --- | --- |
| `OutboxRelayBackgroundService` | Compliance | `compliance-events` | `RelayProcessedOn IS NULL` (does **not** wait for projection) |
| `WorkforceOutboxRelayBackgroundService` | Workforce, Professional, Governance connection strings | `workforce-events` | `ProcessedOn != null AND RelayProcessedOn IS NULL` |
| `DirectoryOutboxRelayBackgroundService` | EnterpriseDirectory | `directory-events` | same ProcessedOn gate |
| `IntegrationOutboxRelayBackgroundService` | Integration | (see file) | same |

**Not relayed:** Identity, Person, Organization, AccessGovernance, Communication, WI, AI.

Worker **requires** `ConnectionStrings:ServiceBus`. Local `appsettings.json` has `""` → process **cannot start**. Workforce/Directory connection strings in worker appsettings point at **LocalDB** names, not this Express instance.

**Consumers (code, disabled without SB):**

- WI → `workforce-events`
- Communication → `compliance-events`
- Integration → `workforce-events`, `directory-events`, `compliance-events`
- AI proactive consumer → `compliance-events` topic (config)

Idempotency: Service Bus `MessageId` = outbox Id; dashboard projection skips duplicate `LastProcessedOutboxMessageId`. WI checkpoint by event type is weak (not per message).

---

## 6. Documented vs runtime

| Path | DOCUMENTED | IMPLEMENTED | CONFIGURED | RUNNING | VERIFIED |
| --- | --- | --- | --- | --- | --- |
| Outbox → Relay → `compliance-events` | Yes ([12-Event-Architecture](./12-Event-Architecture.md), README) | Yes | **No** (empty SB) | **No** | **No** |
| Outbox → Relay → `workforce-events` | Partial (README over-generalizes) | Yes | **No** | **No** | **No** |
| Identity/Person/Org/AG → bus | README implies platform-wide | **No** | — | — | — |
| Compliance in-process projection/task | Event doc lists domain events | Yes | N/A | Yes (hosted Compliance) | **Partial** (DeficiencyIdentified + task count; TaskOverdue/enroll gaps) |
| WI snapshot scheduler | Phase 1 | Yes | Interval default 60 min | Unknown this session | Not runtime-exercised |
| Directory rebuild scheduler | Phase 1 | Yes | Default 360 min | Unknown | Not runtime-exercised |

---

## 7. Event contract audit

Typical Compliance payload (System.Text.Json of the domain record) includes hospital/framework when those properties exist. AggregateId for Compliance outbox is a **hash of HospitalId+FrameworkId**, not the obligation PK (Phase 3D identity is **not** on the outbox envelope).

Workforce/Person/Org events are **string Type** + ad-hoc JSON. No version field. Correlation on bus = `AggregateId`. Causation IDs generally absent.

**Incomplete for consumers that parse `organizationId`:** EmploymentCreated, EmploymentTransferred, Org membership transfer.

---

## 8. Missing events

| Name | Definition | Writer | Consumer | Verdict |
| --- | --- | --- | --- | --- |
| `EmploymentTerminated` | String expected by Directory + WI | **Never** (`DeleteAsync` SoftDelete + audit only) | Handlers registered | **DEFINED BUT NEVER PUBLISHED** |
| `PersonUpdated` | Directory `PersonUpdatedProjection` | **Never** (`UpdateAsync` save + audit) | Registered | **DEFINED BUT NEVER PUBLISHED** |
| `PersonDeleted` | Not found as writer or Directory case | SoftDelete only | No | **NOT DEFINED** (no event type in Directory switch) |
| `FrameworkEnrolledDomainEvent` | Domain record + dispatcher | Enrollment / hospital aggregate | Dashboard candidate list looks for **`ComplianceEnrolled`** | **PUBLISHED TO OUTBOX, NOT A DASHBOARD CANDIDATE** (naming/adapter gap). Journey can also raise `ComplianceEnrolledDomainEvent` (different type). |
| `EmploymentPromoted` | Written | WI list omits it | **PUBLISHED (if relay), NO WI HANDLER** |

`PersonCreated` is written and mark-processed; Directory does not consume it (rebuild instead).

---

## 9. Consumer traces

Hop key: PASS / MISSING / BROKEN / NOT REQUIRED / UNKNOWN

### Person → Directory

Create → `PersonCreated` outbox **PASS** → mark-only **BROKEN** as bus → Directory event **NOT REQUIRED** if rebuild is contract → rebuild HTTP **PASS** (with known `organizationId` query gap from Phase 2) → index **UNKNOWN** this session.

Update/delete → event **MISSING**.

### Employment → WI

Create HTTP Person/Hospital **PASS** → outbox **PASS** (empty DB here) → mark **PASS if hosted** → relay **MISSING (SB)** → WI `ExtractOrganizationId` **BROKEN** even if delivered → snapshot scheduler **NOT REQUIRED** as fallback (MINUTES).

Transfer: Employment row **PASS**; ProfessionalProfile **MISSING**; payload **BROKEN** (`{}`).

Delete: outbox **MISSING**; consumers **BROKEN** (never invoked).

### Training → WI

Live Professional matrix **PASS** (E). WI dashboard **D** until refresh. Phase-4 attendance outbox **MISSING**. **NOT REQUIRED** if SLA is snapshot.

### Compliance enroll → Dashboard

Obligations **PASS** (sync). Outbox `FrameworkEnrolledDomainEvent` **PASS**. Candidate filter **BROKEN**. Hangfire/full rebuild **UNKNOWN** as compensating path. GET `/dashboard` may still compute live depending on endpoint — **not** the same as `DashboardProjections` table.

### Deficiency → Task

Outbox **PASS** → in-process EnsureRemediationTask **PASS** → tasks table **PASS**. Bus **NOT REQUIRED**.

### Evidence → Dashboard

Incremental if `EvidenceUploaded` in type **PASS** (code). Full rebuild on other events. Bus **NOT REQUIRED** for same-service dashboard.

### Membership → JWT

HTTP at login **PASS**. Event **NOT REQUIRED**. Session cache **UNKNOWN**.

### OE ownership → notifications

Persist **PASS**. Communication mapping **PASS**. SB consumer **MISSING** locally.

---

## 10. Snapshot / read-model analysis

| Read model | Mode | Freshness contract (from code defaults) |
| --- | --- | --- |
| WI snapshots / matrices | PERIODIC SNAPSHOT + optional event refresh | ~**60 min** + `POST /intelligence/refresh` |
| DirectoryIndex | HYBRID (rebuild scheduler + unused events) | ~**360 min** + `POST /directory/rebuild` |
| DashboardProjections | HYBRID (outbox incremental + Hangfire nightly) | IMMEDIATE for candidates; enroll/TaskOverdue stale |
| AnalyticsDailySnapshot | SCHEDULED | Hangfire |
| JWT claims | PULL-ON-READ (issue time) | Next login / token |
| Professional training matrix API | LIVE | IMMEDIATE |

Stale **unexpectedly:** dashboard rows waiting on `FrameworkEnrolled` and `TaskOverdue` (event-driven path claimed but filter/handler mismatch). WI/Directory delay is **intentional** if product accepts minutes–hours.

---

## 11. Reliability analysis

| Concern | Compliance in-process | Bus relay | Mark-only |
| --- | --- | --- | --- |
| Atomic with source TX | Yes (same SaveChanges as domain) | After commit poll | After commit poll |
| Retry | `RetryCount` / MaxRetries | Yes on send failure | Identity/Person yes; AG no retry field usage |
| Poison / DLQ | Stuck with Error | Documented SB DLQ; not verified | Rows sit processed with no effect |
| Idempotent consumers | Dashboard message id | MessageId = outbox Id | N/A |
| Ordering | OccurredOn poll | Not guaranteed | N/A |
| Correlation | AggregateId hash / GUID | CorrelationId = AggregateId | Logged |
| Replay | Admin outbox replay (Compliance) | Re-send if RelayProcessedOn cleared | No |
| Observability | Logs + AdminOutbox | Logs | Logs only |

---

## 12. Propagation gaps

1. Architecture README overstates bus as the platform event path.  
2. Mark-only processors **block** workforce relay until ProcessedOn, then relay still needs SB.  
3. Identity backlog 3498 unprocessed — processor not applied to this database.  
4. Empty / employmentId-only payloads vs WI `organizationId`.  
5. Missing writers vs registered consumers (terminated, person updated).  
6. `FrameworkEnrolledDomainEvent` vs `ComplianceEnrolled` candidate string.  
7. `TaskOverdue` not `IsTaskEvent` (only Created/Completed) but **is** a dashboard candidate (`ComplianceEvent.` prefix) — 607 still unprojected: process failures or scope resolution, Phase 2 class B.  
8. ProfessionalProfile hospital/org not updated on transfer.  
9. Worker connection strings / empty Service Bus.

---

## 13. Priority classification

**P0:** 0 — no new tenant-bypass via events; Phase 3A HTTP hospital bind unchanged.

**P1 (incorrect operational/cross-module state if event-or-profile assumed live):**

- Employment delete without `EmploymentTerminated` while Directory/WI register it.  
- Transfer `{}` + no ProfessionalProfile update if UI shows profile hospital as source of truth.  
- FrameworkEnrolled not driving dashboard projection if product expects enroll→projection IMMEDIATE.  
- EmploymentCreated payload missing `organizationId` if WI event-refresh is in-scope.

**P2 (stale read model / trapped outbox):**

- TaskOverdue 607 unprojected.  
- Identity SecurityEventRaised backlog if SIEM/bus was intended.  
- Cert-expiry / Professional outbox trapped (Phase 2 D33) if Communication should notify.  
- Directory PersonUpdated unused if rebuild SLA is too slow for ops.

**P3:** Org empty outbox + no processor; README vs relay list; dual AuditService; AG/Governance mark-only with no consumers.

---

## 14. Recommended remediation (Phase 3F — do not implement here)

Only after BDRs below.

| Problem | Source | Target | Impact | Current | Required | Mechanism | Event / payload | Consumer | Retry / idempotency | Freshness | Risk |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Enroll not a dashboard candidate | Compliance enroll | DashboardProjections | Stale projection | Type `FrameworkEnrolledDomainEvent` skipped | Treat enroll as rebuild candidate **or** emit `ComplianceEnrolled` | C in-process (not bus) | Keep hospital+framework in JSON | `InProcessOutboxProcessor` | Existing retry | IMMEDIATE | Low if only type alias |
| Transfer payload | Workforce | WI | Event refresh no-ops | `{}` | If bus SLA: `{ organizationId, hospitalId, employmentId }` | B optional | `EmploymentTransferred` | WI SnapshotRefresh | MessageId | MINUTES still OK via snapshot | Medium |
| Terminated never written | Workforce delete | WI/Directory | Stale index | SoftDelete | BDR: event **or** document rebuild-only | B or D | `EmploymentTerminated` + org/hospital | Existing handlers | Idempotent deactivate | MINUTES | Medium — dual consumers |
| PersonUpdated never written | Person | Directory | Stale names | No outbox | BDR: event **or** rebuild SLA | B or D | org + person fields | PersonUpdatedProjection | Idempotent upsert | MINUTES–HOURS | Medium |
| WI cannot parse org | Workforce create | WI | Event refresh no-ops | employmentId only | Include `organizationId` | B optional | `EmploymentCreated` | SnapshotRefresh | | MINUTES | Low |
| Docs vs relay | Platform | Operators | Wrong ops model | README all-outbox | Document which DBs relay | Docs | — | — | — | Low |
| TaskOverdue projection | Compliance | Dashboard | Stale task KPIs | 607 unprojected | Fix ApplyIncremental/scope | C | existing type | Dashboard engine | | MINUTES | Medium |
| Identity drain | Identity | none | Table growth | 3498 pending | Host processor **or** stop writing unused types | F or local | SecurityEventRaised | none unless SIEM BDR | | Low |

**Do not** add Service Bus for Deficiency→Task. **Do not** change Phase 3D obligation identity.

---

## 15. Test strategy

Distinguish IMMEDIATE / EVENTUAL / SNAPSHOT.

| Chain | Mutation | Expect event | Expect consumer | Downstream | API | UI | Max delay |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Deficiency→task | Identify deficiency | `DeficiencyIdentifiedDomainEvent` | InProcess EnsureRemediationTask | Task row | GET tasks | Task list | IMMEDIATE |
| Enroll→obligations | Enroll | optional FrameworkEnrolled | SyncForOrganization | 1 row per OE | GET obligations?hospitalId&frameworkId | Journey | IMMEDIATE |
| Enroll→dashboard table | Enroll | candidate after 3F | ProcessOutboxMessage | DashboardProjections | GET dashboard | Dashboard | IMMEDIATE after 3F |
| Employment create | POST employment | optional | HTTP validation | Employment | GET employment | Workforce | IMMEDIATE |
| WI snapshot | refresh or clock | none required | SnapshotService | WorkforceSnapshots | GET intelligence | WI screens | 60 min or manual |
| Directory | rebuild | none required if D | Projector | DirectoryIndex | GET directory | Directory | 360 min or manual |
| JWT | membership change | none | next login | Token claims | login | Shell | next token |
| Transfer profile | BDR | — | — | ProfessionalProfile | GET profile | Professional | IMMEDIATE **only if** sync HTTP chosen |

Do **not** assert WI row change immediately after employment create unless an explicit refresh is in the test.

---

## 16. Business decisions required

1. **Directory consistency:** event-driven (`PersonUpdated` / `EmploymentTerminated` + bus) vs rebuild/scheduler as source of truth (document SLA).  
2. **WI freshness:** event-triggered snapshot vs 60-minute job only.  
3. **Employment transfer → ProfessionalProfile:** required sync update, event, or profile is employment-independent (Phase 1 UNKNOWN).  
4. **JWT:** membership change requires token refresh API vs next login.  
5. **Identity `SecurityEventRaised`:** local telemetry (keep mark-only / stop writing) vs SIEM/bus.  
6. **Dashboard enroll:** alias FrameworkEnrolled to ComplianceEnrolled vs emit both vs Hangfire-only.  
7. **Organization outbox:** implement processor+relay vs delete unused writers.

---

## 17. Phase 3F implementation plan (order)

1. Close BDRs 1–3, 6 (Directory/WI/profile/enroll naming).  
2. In-process dashboard: add `FrameworkEnrolled` to candidate list **or** dual-write `ComplianceEnrolledDomainEvent` (same service — no bus).  
3. Diagnose/fix TaskOverdue projection failures (614 unprojected).  
4. If WI/Directory are event-first: payload schema (`organizationId`, `hospitalId`), `EmploymentTerminated` writer, `PersonUpdated` writer, then **one** relay path (do not add a second publisher inside each processor).  
5. Document README/12-Event-Architecture: Compliance in-process vs OutboxRelayWorker topics vs mark-only drains.  
6. Local/dev: Service Bus emulator **or** explicit “bus optional” so workers do not crash.  
7. Tests per §15; do not regress Phase 3A hospital bind or Phase 3D unique key.

---

## Appendix — hop template (authoritative)

```text
SOURCE MUTATION
    → EVENT CREATED
    → OUTBOX COMMITTED
    → RELAYED          (often MISSING)
    → TRANSPORT        (not configured locally)
    → CONSUMER         (registered, often never invoked)
    → TARGET DATABASE
    → TARGET API
    → UI / READ MODEL
```

A type name or outbox row is **not** VERIFIED. Phase 3E verified the deficiency→task in-process path and SQL/code for the rest.
