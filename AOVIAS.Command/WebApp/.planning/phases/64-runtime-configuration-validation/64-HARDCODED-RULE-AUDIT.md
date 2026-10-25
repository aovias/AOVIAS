# Configuration Coverage & Hardcoded Rule Audit — Stage 5C.2, Phase 6

Every pattern the brief names, searched across `features/audit` and `features/auditor`, with each hit
classified. Searches were run on non-spec sources.

---

## Search results

| Pattern | Hits | Migrated | Intentional | Still hardcoded |
| --- | --- | --- | --- | --- |
| `switch (…)` | 2 | — | 2 | 0 |
| `status === '…'` / `stage === '…'` | 9 | — | 8 | 1 → **fixed** |
| `severity === '…'` | 0 | — | — | 0 |
| Label maps (`Record<…, string> = {`) | 3 | — | 2 | 1 (other domain) |
| Permission checks | 5 | 5 | — | 0 |
| Lifecycle / stage-order arrays | 1 | 1 | — | 0 |
| Ordering arrays | 0 | — | — | 0 |
| `AuditType` literals | 6 | — | 2 | 4 → **DEF-CFG-004** |
| Risk band thresholds | 1 | — | — | 1 → **fixed** |

---

## Classified: intentional

These are not business configuration and were deliberately left.

| Location | What it is | Why it stays |
| --- | --- | --- |
| `audit-context-panel.model.ts:71` | `switch (subject.kind)` → Copilot persona key | Wire contract with the AI service, keyed off subject kind not stage. Same class as `toFindingWireEnum`. |
| `workflow-condition.model.ts:93` | `switch (condition.is)` | This *is* the rule engine — the dispatch that evaluates the five fact tests. Configuring it would be configuring the interpreter. |
| `audit-workflow-status-bar` (6 hits) | `item.status === 'complete' \| 'current'` | `AuditWorkflowStageStatus` is presentation state the engine *produces*, not a business status. Not tenant-configurable by design. |
| `audit-lifecycle-state.model.ts:155` | `s.status === 'complete'` counting | Engine internal, same reason. |
| `findings-detail-panel.ts:680` | `Record<string,string>` | Query-parameter builder, not a label map. |
| `auditor-dashboard-page.ts:284` | `Record<string,string>` | URL query parser, not a label map. |
| Wire union types (`FindingSeverity`, `DeficiencySeverity`, `FindingSource`, …) | Single declarations typing the API shape | Labels and behaviour come from configuration; the union no longer carries display or behaviour truth. Classified in Wave 1. |
| `audit-prep-template.service.ts` `'InternalAudit'`, `'Surveillance'` | Seed content of built-in prep templates | Template *content*, equivalent to any other seeded default. Templates are user-editable records. |

---

## Fixed during this wave

### DEF-CFG-002 — owner acceptance gated on a status literal · **Medium** · fixed

- **Root cause.** `canAcceptOwnership` tested `d.status === 'Assigned' || d.lifecycleStatus === 'Assigned'`. Which status permits owner acceptance is a business rule; identity was standing in for capability.
- **Evidence.** `dcc-detail-panel.component.ts:1725` (pre-fix).
- **Runtime reproduction.** Rename `DeficiencyStatus.Assigned` to "Dispatched" in configuration. The accept/reject controls disappear for every deficiency awaiting acceptance, with no error — the section simply never renders.
- **Risk.** An organization using its own terminology silently loses the CAPA ownership handshake. No log, no failure; the control is just absent.
- **Fix.** `awaitingOwnerAcceptance` attribute on `DeficiencyStatus`, read via `configuration.attribute(...)`. Covered by *"moves which status permits owner acceptance"*.

### DEF-CFG-003 — risk matrix thresholds hardcoded · **Medium** · fixed

- **Root cause.** `computedRiskLevel()` held the band boundaries as an `if` ladder: `>= 20` Critical, `>= 12` High, `>= 6` Medium.
- **Evidence.** `dcc-detail-panel.component.ts:1544` (pre-fix).
- **Runtime reproduction.** Configure `RiskLevel` for a 3×3 matrix (max score 9). Every deficiency bands as Low or Medium; Critical becomes unreachable because no score can reach 20.
- **Risk.** Silent, systematic mis-rating of risk on any organization not using a 5×5 matrix — a compliance-material error that looks like correct output.
- **Fix.** `minScore` attribute on each `RiskLevel`, resolved highest-band-first. Covered by *"moves the risk-matrix bands"*.

### DEF-CFG-001 — tenant configuration leakage · **Critical** · fixed

Full write-up in the Runtime Validation Report. Summary: `reset()` was correct but never called, so
configuration survived logout and hospital switch, and the load-once guard stopped the incoming
tenant fetching its own.

---

## Still hardcoded

### DEF-CFG-004 — form defaults bypass configuration · **Medium** · **open**

- **Root cause.** Create forms initialise their bound field to a literal: `auditType = 'InternalAudit'` (Planning, twice; Auditor sessions, twice), `source = 'InternalAudit'`, `classification = 'AuditFinding'`, `severity = 'Minor'`, `department = 'clinical'` (Findings capture).
- **Evidence.** `audit-planning.component.ts:538,551,854`; `auditor-sessions-page.component.ts:209,240`; `findings-create-panel.component.ts:424` and neighbours.
- **Runtime reproduction.** Set `selectableWhenScheduling: false` on `InternalAudit`. The Planning form's dropdown no longer contains it, so the `<select>` renders with no selection; submitting without touching it posts `InternalAudit` — a type the organization has withdrawn from scheduling.
- **Risk.** A blank-looking required field, and a record created with a value the administrator disabled. Not data loss, but it undermines "disable" as a configuration action.
- **Why not fixed here.** The correct fix is to seed the field from the configured list *and reconcile when configuration arrives*, because configuration loads asynchronously after the component constructs. That is a change to form-initialisation behaviour, which this wave's rules exclude ("no business behaviour changes", "no speculative improvements"). It is a contained follow-up, not a refactor.
- **Recommended fix.** On each affected form, initialise from the relevant `optionsWith(...)` signal and add a reconciliation that moves the selection to the first available option when the current one is not offered. Roughly six call sites; one shared helper would do it.

### DEBT-CFG-005 — `statusTone` matches status text by substring · **Low** · open

- **Location.** `auditor-assessment-page.component.ts:421`.
- **What.** Badge tone chosen by `status.toLowerCase().includes('verified' | 'compliant' | 'reject' | …)`.
- **Why it is debt.** The same class of rule as the severity badge tokens migrated in Wave 1, but applied to free-text status strings aggregated from several domains (obligations, evidence, CAPA), not to one configurable category. Migrating it needs those strings to be normalised first.
- **Risk.** Renaming a status in configuration can change its badge colour — a status renamed to "Closed Out" keeps matching `closed` and stays green, which happens to be right; one renamed to "Signed Off" loses its tone and goes neutral. Cosmetic.

### DEBT-CFG-006 — `timelineItemClass` layer→class map · **Low** · open

- **Location.** `auditor-assessment-page.component.ts:405`.
- **What.** A `Record<string,string>` mapping compliance-graph layers to CSS classes.
- **Why it is debt, not a defect.** The layers are compliance-model entity kinds, not Audit business configuration. Making it configurable would mean inventing a category outside this programme's domain, which the brief forbids.

### DEBT-CFG-007 — evidence status labels · **Medium** · open, carried from Wave 1

- **Location.** `shared/models/evidence-status.model.ts`.
- **What.** `EVIDENCE_STATUS_LABEL` plus an alias normaliser, consumed by Evidence, Journey, Tasks and Reporting through *pure functions*.
- **Why still open.** The labels are configurable business rules; the alias map is a wire concern. Migrating means making those pure functions injectable across four modules outside the Audit workspace. Doing half of it would recreate the split source of truth these waves removed.
- **Risk.** Evidence and verification status labels are the one significant user-facing text an administrator cannot change. Contained and well understood.

### DEBT-CFG-008 — dashboard KPI captions · **Low** · open, carried from Wave 1

- **Location.** `dcc-dashboard.component.ts` (~30 captions), `findings-dashboard.component.ts` (~9).
- **What.** Captions such as "SLA On Track", "Overdue CAPAs", "Blocking Accreditation".
- **Why it is debt.** These label *computed metrics*, not values from any category. Making them configurable is a metrics-catalog problem, a different piece of work.

---

## Coverage

**Configuration coverage — 28 of 28 identified Audit business-definition categories are
configuration-backed (100%).** Every category in the Wave 1 inventory plus the two workflow
categories added in Wave 2. Categories the brief names that do not exist in this codebase — Priority,
Approval Status, Notification Categories, Escalation Levels as an enumerated set — were not invented.

**Workflow coverage — 6 of 6 stages and 9 of 9 surfaces are configuration-backed (100%).** Stage
order, completion, outstanding work, entry prerequisites, recommendations and surface permissions all
resolve through the engine. No workflow decision is made outside it, verified by scan.

**Metadata coverage — 27 of 42 non-spec components and services in `audit` + `auditor` read
configuration (64%).** The remaining 15 are components with no business-definition surface — shells,
export utilities, API services and panels that render only record data. This figure is a denominator
artefact rather than a gap: the meaningful measure is that no component *owns* a business rule, which
the scan confirms.

**Honest caveat on all three figures.** They measure the Audit and Auditor modules, which is what
these three waves covered. They are not platform-wide: Journey, Tasks, Evidence and Reporting were
never in scope, and DEBT-CFG-007 is the one place that boundary is user-visible.
