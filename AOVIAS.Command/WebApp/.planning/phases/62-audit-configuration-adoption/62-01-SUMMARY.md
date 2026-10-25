# Phase 62 — Stage 5B.3 — Enterprise Configuration Adoption

**Status:** Implementation complete. Build green, 187/188 unit tests pass (the one failure is
pre-existing and unrelated — see §7).
**human_needed:** rendered-UI signoff for the eleven migrated screens (§6 explains why I could not
reach them), and a decision on the four behaviour changes listed in §5.
**Repositories touched:** `WebApp` (all consumer migration), plus one **data-only** SQL seed script
in `ComplaincePlatform/OrganizationService`. No backend code, no schema changes, no migrations.

---

## 1. What this wave did

Every configurable business definition in the Audit workspace now resolves through
`PlatformConfigurationService`. No Angular component owns a business rule.

Before this wave the Configuration Engine served exactly one category (`AuditType`) to exactly one
consumer (Planning). Everything else — finding statuses, severities, sources, CAPA statuses, risk
ratings, root causes, session statuses, observation types — lived as `export const` label maps,
hardcoded `<option>` blocks and `if (status === …)` chains spread across eighteen files.

- **26 categories** registered, **130 configurable values**.
- **18 consumers** migrated across Planning, Findings, CAPA, Execution, Surveillance, Reports and
  the Auditor workspace.
- **21 duplicate definitions deleted** — label maps, option arrays, lifecycle-step lists, class
  lookup maps, and three copies of the same `formatStatus` chain.
- **24 unit tests** added for the configuration layer.

---

## 2. The generalisation, and why it was necessary

`AuditType` had a bespoke descriptor interface, a bespoke signal, a bespoke API mapper and bespoke
accessors. Reproducing that twenty-five more times would have created twenty-five copies of the same
machinery, each free to drift — the exact failure this wave exists to end.

So the *mechanism* was generalised while the *shape* Planning established was kept intact:

| Concern | Before | Now |
| --- | --- | --- |
| Descriptor | `AuditTypeDescriptor` with named booleans | `ConfigurationValueDescriptor` with an attribute bag |
| Store | one signal | one signal holding a category map |
| API | `getAuditTypes()` with a bespoke mapper | `getCatalog(code)` — same endpoint, same DTOs |
| Seed | `DEFAULT_AUDIT_TYPES` | `AUDIT_CONFIGURATION_CATALOGS` |
| Accessors | `schedulableAuditTypes` etc. | still present, now reading the shared store |

This mirrors the server, where `PlatformConfigurationAppService` already serves every category from
one table with a `Metadata` JSON column. That symmetry was deliberate on the backend
("a new domain needs no schema change and no new code here"); it is now true on the client too.

**No new configuration pattern was introduced and the engine was not redesigned.** The same
`/configuration/categories` endpoints, the same DTOs, the same seed-not-fallback rule, the same
version-guarded writes.

One deliberate improvement to loading: `load()` now calls `getCategoryCodes()` first and requests
only the categories the platform reports. Twenty-six registered catalogs cost **two** requests today,
not twenty-six. Categories the server does not know stay on their seed and cost nothing.

### Files added

- `core/configuration/configuration-catalog.ts` — descriptor, catalog and option types; id
  normalisation and humanisation.
- `core/configuration/audit-configuration.catalogs.ts` — all 26 seed catalogs.
- `core/configuration/platform-configuration.service.spec.ts` — 24 tests.

### Files removed

- `core/configuration/audit-type.catalog.ts` — superseded; its content is the `AuditType` catalog.

---

## 3. Attributes: branching on capability, not identity

The rule that makes the success metric achievable is that consumers must branch on what a value
*does*, never on which value it *is*.

| Attribute | Meaning | Replaces |
| --- | --- | --- |
| `selectableWhenScheduling` | a planner may schedule this audit type | hardcoded Planning `<option>` list |
| `selectableWhenStartingSession` | an auditor may start a session of this type | a *second*, different hardcoded list |
| `selectableWhenFiltering` | appears in a filter dropdown | hardcoded `<option>` blocks |
| `selectableWhenTransitioning` | a reviewer may set this status | `FINDING_STATUS_TRANSITIONS` |
| `selectableWhenCreating` | a finding may be created in this status | hardcoded Draft/Open options |
| `lifecycleStep` / `detailStep` | appears on a lifecycle rail, and in what order | `*_LIFECYCLE_STEPS` arrays |
| `terminal` | the work is finished in this state | two private `readonly` tuples |
| `badgeToken` / `badgeClass` | visual identity | `'sev--' + x.toLowerCase()`, `RISK_LEVEL_CLASS` |
| `fallback` | the catch-all for an unrecognised value | the final `else` of an `if` ladder |
| `ordinal` | backend enum ordinal | `FINDING_SOURCE_ORDINALS` |
| `days` / `occurrences` / `intervalMonths` | numbers a form submits | numbers encoded in option *names* |
| `field` / `labelFromCategory` | what a grouping reads and how it labels | a `groupBy === …` chain |
| `shortLabel` | compact form for a grid column | a regex plus a string replace |

Two of these deserve calling out because they were genuinely invisible before:

- **`selectableWhenScheduling` vs `selectableWhenStartingSession`.** Planning offered Department and
  Follow-up audits but not the external assessments; the Auditor sessions screen did the exact
  opposite. Two hardcoded lists, two different intents, and nothing anywhere recording that the
  difference was deliberate. They are now two attributes with two names.
- **`fallback`.** The SLA and routing-confidence helpers ended in an unconditional `else`. A
  genuinely new SLA state was therefore displayed as "closed" — indistinguishable from a real
  closure. The catch-all is now a named, configurable value.

---

## 4. Consumer migration

Each consumer was migrated and typechecked before the next was started. Duplicate definitions were
deleted only after every consumer of them had been migrated and the build was green.

| # | Consumer | What moved to configuration |
| --- | --- | --- |
| 1 | `audit-planning` | recurrence options **and** the occurrence/interval maths behind them |
| 2 | `audit-findings-page` | status, classification, source, severity, department, promotion readiness, quick filters, group headings |
| 3 | `findings-grid` | classification, source (by ordinal), status, severity + badge class, promotion readiness |
| 4 | `findings-create-panel` | classification, source, severity, department, attachment types, initial status |
| 5 | `findings-detail-panel` | status transitions, attachment types, classification, source, status labels |
| 6 | `findings-dashboard` | classification and source breakdown labels |
| 7 | `audit-finding-detail` | workflow status label in the context panel |
| 8 | `corrective-actions` | status, severity, stage, grouping — including which field each grouping reads |
| 9 | `dcc-grid` | status, severity + badge class, stage, risk class, SLA class and label |
| 10 | `dcc-detail-panel` | lifecycle rail, root cause, CAPA action status, effectiveness review status and schedule, risk, SLA, confidence, severity, stage |
| 11 | `dcc-dashboard` | lifecycle, risk, root cause and stage breakdown labels |
| 12 | `audit-reports` | audit type, session status, reporting periods |
| 13 | `audit-surveillance` | surveillance membership, terminal session/schedule states, session status |
| 14 | `auditor-sessions-page` | selectable audit types, audit type + session status labels |
| 15 | `auditor-dashboard-page` | audit type and session status labels |
| 16 | `auditor-history-page` | audit type and session status labels |
| 17 | `auditor-reports-page` | session status labels |
| 18 | `auditor-session-workspace-page` | session status and observation type labels |
| 19 | `auditor-observations-page` | observation type labels |

---

## 5. Behaviour changes — please review

These are consequences of unifying definitions that had silently diverged. Each is a deliberate
decision, not an accident, and each is now a configuration value an administrator can change back.

1. **CAPA status filter wording.** The lifecycle bar read "Awaiting Evidence" / "Verification
   Pending" while the filter beside it read "Evidence Submitted" / "Pending Verification". The
   lifecycle wording wins, because it matches `DeficiencyLifecycleStatus` — the shape the API emits.
2. **Classification wording in grids and dashboards.** A regex that spaced capitals produced
   "Non Conformity" where the filter offered "Non-Conformity". Everything now reads
   "Non-Conformity". The findings grid keeps its compact "OFI" via `shortLabel`.
3. **Breakdown labels.** Dashboard breakdowns rendered raw API codes (`PolicyMissing`); they now
   render configured labels ("Policy Missing").
4. **Two dropdown orderings.** The department picker was ordered differently on two screens, and the
   Auditor session-type picker used its own order. Both now follow the configured order.

Two deliberate **non**-changes, so they are not mistaken for oversights:

- **`Cancelled` is still not a terminal scheduled-audit state**, so a cancelled audit past its date
  still counts as overdue — exactly as before. This looks wrong, but changing it silently would have
  been worse. It is now a one-field configuration decision.
- **`InProgress` and `Verified` are still absent from the finding status filter.** The previous code
  carried a comment acknowledging this as an unfixed defect. Preserving it keeps this wave a pure
  migration; enabling them is now a configuration toggle, not a code change.

---

## 6. Verification

| Check | Result |
| --- | --- |
| `tsc --noEmit` | clean, after every individual consumer migration and at the end |
| `ng build` (AOT, production) | succeeds; 10 warnings, all pre-existing and in files this wave did not touch |
| `ng test` | 187 passed, 1 failed (pre-existing — see §7) |
| `eslint` on all touched directories | 7 errors, all pre-existing `no-output-native` on `close`/`select`/`sort` outputs this wave did not introduce |
| `prettier` | applied to every touched file |
| Dev server boot | clean; no console errors, no server errors |

**What I could not verify, and why.** The Audit routes sit behind the auth guard, and I have no
credentials — entering them is outside what I will do. The dev server boots cleanly and the
application shell renders, which does confirm the configuration store constructs and seeds at
startup (it is built during app-context initialisation, so a failure there would prevent boot).

To compensate, the 24 new unit tests assert the *behaviours* the screens depend on rather than the
catalog contents: casing-insensitive label resolution, ordinal resolution and the out-of-range case,
the schedulable/startable distinction, the filter/transition/create separation, `Promoted` staying
out of every picker, fallback resolution, lifecycle indexing, load-once semantics, seed retention on
an unreachable API, and signal identity for `OnPush` correctness.

**Still needed:** a signed-off pass over the eleven screens with a real session — loading, CRUD,
filtering, searching, sorting, reports, dashboard, context drawer, navigation, role behaviour,
responsive and dark theme.

---

## 7. The pre-existing test failure

`src/app/app.spec.ts > should render title` expects an `<h1>` containing "Hello, praman-web". This is
the untouched Angular CLI scaffold test; `app.ts` has no `<h1>` and was already modified in the
working tree before this wave began. Unrelated to this work, and left alone.

---

## 8. Platform seed data

`ComplaincePlatform/OrganizationService/Infrastructure/Persistence/Scripts/2026-08-03_SeedAuditConfigurationCatalogs.sql`

Data only — no schema changes, generated from the catalogs file so the two cannot drift. Idempotent:
categories and values are inserted only when absent, so re-running never disturbs a tenant's
overrides.

**This script must be applied.** It contains one repair that matters: `AuditType` is the only
category seeded by the earlier `2026-08-02` script, and those rows predate
`selectableWhenStartingSession`. A database that ran that script would serve `AuditType` rows
lacking the attribute, and the Auditor "start a session" picker — which selects on it — would render
**empty**. The script refreshes those platform-default rows (`OrganizationId IS NULL` only; tenant
overrides are never rewritten).

Without this script the client still works — it runs on its seeds — but no category is editable.

---

## 9. Remaining hardcoded rules

Reported honestly rather than quietly widened.

**Deliberately left in code — not business configuration:**

- `AUDIT_WORKFLOW_STAGES` — navigation topology. Its own docstring states business rules are not
  encoded there, and that is accurate: it describes the order of work, not what may be selected.
- `toFindingWireEnum` — the camelCase/PascalCase transport rule. A property of
  `JsonStringEnumConverter`, not a decision an administrator makes.
- `AuditSessionStatus`, `ObservationType` and similar unions in `auditor-workspace.model.ts` — each
  declared once, typing the wire shape. Their labels and behaviour now come from configuration; the
  union no longer carries display or behaviour truth. (`AuditType` was different: the *same* union
  was declared in two files and claimed a closed set that tenant configuration had already made
  false. It is now `AuditTypeCode`.)

**Genuinely still hardcoded, out of scope for this wave:**

- `shared/models/evidence-status.model.ts` — `EVIDENCE_STATUS_LABEL` plus an alias normaliser, used
  by Evidence, Journey, Tasks and Reporting through *pure functions*. The labels are configurable
  business rules; the alias map is a wire concern. Migrating the labels means making those pure
  functions injectable, which ripples across four modules well outside the Audit workspace. Doing it
  halfway would have created exactly the split source of truth this wave removes, so it was left
  whole and is recorded here as the obvious next consumer.
- Dashboard KPI captions in `dcc-dashboard` ("SLA On Track", "Overdue CAPAs", …) — these label
  *computed metrics*, not values from any category. Making them configurable is a different piece of
  work (a metrics catalog) and would have meant inventing a pattern, which the brief forbids.

**Categories from the brief that do not exist in this codebase:** Priority, Approval Status,
Notification Categories, Task Types (as an Audit concept), Escalation Levels as a catalog
(escalation is a numeric level plus a free-text state, not an enumerated set). Nothing was invented
to fill these in.
