# Phase 61 — Enterprise Administration Platform (Stage 5) — Research

> Scope: Stage 5A (Administration Foundation) + 5B (Administration Expansion) + 5C
> (Configuration-Driven Platform Integration).
> Repositories in scope: `WebApp` (Angular 21) and `ComplaincePlatform` (.NET services + Gateway).
> Date: 2026-08-01.

---

## 1. Prerequisite verification — INF-01

Stage 5 may not begin until INF-01 (EF Core migration system recovery) is complete.
**Verified complete.**

Evidence — `ComplaincePlatform` commit `a73c9be`, *"fix(INF-01): reconcile EF Core migration
system and add CI drift gate"*:

| INF-01 requirement | Evidence |
| --- | --- |
| EF Core migration system recovered | `[Migration]` / `[DbContext]` attributes restored on `Phase36_ComplianceStandardRename` and `H1_4_DashboardProjectionFoundation`, which had been invisible to EF Core — never applied, never reported pending |
| Model Snapshot verified and committed | `ComplianceDbContextModelSnapshot.cs` regenerated, 1,883 → 7,071 lines (the committed snapshot previously described roughly a quarter of the model) |
| Fresh database validated | Fresh DB reaches parity with dev: 91 vs 102 tables; the 11-table delta is Hangfire's, created at app startup by design |
| Existing database validated | Dev database upgrades with data intact |
| CI migration validation operational | `scripts/ci/validate-migrations.ps1` (119 lines) gates missing `[Migration]` attributes, snapshot drift, and failure to build a database from scratch; negative-tested by removing an attribute and confirming failure |
| Runtime login available | Pre-existing; auth stack unchanged by INF-01 |
| Audit Workspace runtime verified | Pre-existing through Phase 60 |

**Carry-forward constraint.** `ENGINEERING_STANDARDS.md` §1.1–1.2 still applies to every
Stage 5B migration:

- Never hand-write a migration. Always `dotnet ef migrations add <Name>`, then edit the
  generated file if needed. A hand-written migration has no `.Designer.cs` and does not
  regenerate the snapshot.
- `dotnet ef migrations remove` remains unsafe where a hand-written migration precedes the
  target. Delete migration files manually and restore the snapshot from git.
- ComplianceService currently carries 83 migration files with only 33 `.Designer.cs` files.
  The snapshot is now correct, but the historical hand-written migrations remain. Every new
  Stage 5B migration must be generated, not authored.

**Open risk.** `ComplaincePlatform` has a large uncommitted working tree (modifications
across AIOrchestratorService, AuditService, ComplianceService, Gateway, and the solution
file). Per the user's direction this phase proceeds without git versioning, so Stage 5B
changes will intermingle with that existing work. Migration validation must therefore be
run explicitly rather than inferred from a clean tree.

---

## 2. Current state — WebApp administration surface

### 2.1 An Administration Workspace already exists

Stage 5A is **consolidation and re-tabbing**, not greenfield. The workspace exists today:

- `src/app/features/settings/pages/administration-shell.component.ts` (790 lines) —
  `AdministrationShellComponent`. Renders a context bar ("Administration Center", environment
  badge, "Back to AOVIAS Command"), a left navigation aside driven by config, a mobile nav
  toggle, and a `<router-outlet>`.
- `src/app/features/settings/models/settings-nav.model.ts` — config-driven nav.
  `SettingsNavSection[]` with `getSettingsNavSections()`, plus `getSettingsHomeCards()` for
  the workspace home page.
- `src/app/features/settings/routes.ts` (123 lines) — `SETTINGS_ROUTES`, mounted at
  `/app/settings` **outside** `ShellComponent` (it renders its own header).

The single-workspace principle is already partly enforced. `app.routes.ts` defines
`legacyAdminRedirect(legacyPrefix, targetPrefix)` and uses it to fold old top-level admin
routes into the workspace: `/app/ai-identity` → `/app/settings/ai`, `/app/directory` →
`/app/settings/workforce/directory`, `/app/integration` →
`/app/settings/platform/integrations`, `/app/notifications` → `/app/settings/notifications`,
`/app/professional` → `/app/settings/workforce/professionals`,
`/app/workforce-intelligence` → `/app/settings/workforce/intelligence`. `settings/routes.ts`
adds a second redirect layer for legacy in-workspace placements
(`identity/ai-identity/*` → `ai/*`, `platform/notifications/*` → `notifications/*`).

**This redirect pattern is the model for Stage 5A.** Folding a feature into the workspace
means: mount its routes under `/app/settings`, then leave a `legacyAdminRedirect` at the old
top-level path. No route deletions, no broken deep links.

### 2.2 Existing navigation sections (7 + About)

| Section | Items |
| --- | --- |
| General | Organization, Branding |
| Identity & Access | Users, Password Management |
| AI | Dashboard, AI Identity, Models, Providers, Prompt Library, Policies, Executions, Usage & Analytics, Approvals (9) |
| Notifications | Overview, Templates, Channels, Delivery Providers, Queues, Delivery History, Preferences, Subscriptions (8) |
| Workforce | Dashboard, Workforce Intelligence, Enterprise Directory, Departments, Designations, Professional Registry, Membership (7) |
| Platform | Integrations Overview + Connectors, Mappings, Synchronizations, Webhooks, Imports, Exports, Monitoring, Audit; Compliance Defaults *(comingSoon)*, Licensing, Commercial Ops |
| System | Audit & Activity |
| About | About AOVIAS Command |

`SettingsNavItem` already supports `icon`, `disabled`, `comingSoon`, and `exactMatch` — the
`comingSoon` flag is the mechanism for Stage 5B tabs that are navigable before their schema
lands.

### 2.3 Shared workspace primitives that already exist

Stage 5A's "Shared Workspace Experience" requirement is partly met. Reuse these; do not
re-implement.

`src/app/shared/ui/administration/` — exported via `index.ts`:
- `AdminPageStateComponent` + `AdminPageStateKind` (loading / empty / error states)
- `AdminStatCardComponent`
- `AdminStatusChipComponent` + `AdminStatusTone`
- `AdminSectionCardComponent`
- `admin-layout.css`

`src/app/shared/ui/context-drawer/` — `ContextDrawerComponent`, `context-drawer.model.ts`,
`ContextDrawerService` (with spec). **This is the Context Drawer the Stage 5A and 5C
requirements call for.** It exists and is tested; Stage 5A wires it into the administration
shell, Stage 5C feeds it configuration-derived content.

`src/app/shared/ui/workspace/` — `WorkspaceSearchComponent`, `workspace-layout.ts`,
`workspace-states.ts`, and SCSS for header / section / page / empty / error / skeleton.

`src/app/shared/ui/table/table.component.ts` — shared table.

**Gap:** there is no shared *toolbar* and no shared *filter* primitive. `WorkspaceSearchComponent`
covers search only. Stage 5A must add `AdminToolbarComponent` and `AdminFilterBarComponent`
alongside the existing administration primitives.

### 2.4 Tab-by-tab gap analysis (target IA vs. today)

| Target tab | Status | Detail |
| --- | --- | --- |
| **Organization** | Partial | `general/organization`, `general/branding` exist. Missing: Facilities, Buildings, Departments*, Units, Locations, Financial Year, Business Calendar, Localization, Regional Settings, Time Zone. *Departments exists but under Workforce. |
| **Programs** | **Absent from UI** | Backend partially exists — see §3.2. No admin UI at all. |
| **Templates** | **Absent** | No template library UI. `DocumentTemplate` exists backend-side for documentation only. |
| **Frameworks** | Misplaced | Full feature at `/app/frameworks` (`FRAMEWORKS_ROUTES`) as an operational workspace, not administration. Stage 5A folds the *administrative* surface (frameworks, chapters, standards, objective elements, versioning, activation, cross-mapping) into the workspace. |
| **Risk** | **Absent as configuration** | `/app/risks` (`RISKS_ROUTES`) is operational risk register. No matrix/formula/threshold configuration. |
| **Workflow** | **Absent** | No UI. Backend engine exists but rules are compiled classes — see §3.3. |
| **Notifications** | Good | 8 pages under `notifications/`. Mostly satisfies 5B's Notification Center; needs Reminder Rules, Escalation Rules, Digest. |
| **Approvals** | **Absent** | No approval configuration UI. `AI → Approvals` exists but is AI-scoped only. |
| **Teams** | Good | `workforce-admin`, 7 pages. Maps to Stage 5A Team Administration. Missing: Skills, Certifications, Availability, Capacity, Assignment Overview as first-class admin surfaces. |
| **Security** | Partial / scattered | `identity/users`, `identity/password` in-workspace; but `/app/access-governance` (`ACCESS_GOVERNANCE_ROUTES`), `/app/security` (`SECURITY_ROUTES`), and `/app/governance` (`GOVERNANCE_ROUTES`) sit **outside** the workspace. Violates "no duplicate administration pages". |
| **AI** | Good | 9 pages. Substantially satisfies 5B AI Administration. Missing: Knowledge Sources, Confidence Thresholds as explicit surfaces. |
| **System** | Scattered | `system/audit`, `platform/licensing`, `platform/commercial`, `platform/compliance` *(comingSoon)*, `theme`, `about`. Missing: Number Sequences, Retention, Feature Flags, Backup Policies, Storage Policies, Default Values. |

**Consolidation debt (blocks the "no duplicate pages" checklist item):** `access-governance`,
`security`, `governance`, `frameworks`, and `risks` are top-level routes with administrative
content. Each needs a decision — fold wholly, split operational vs. administrative, or leave
operational and surface only configuration in the workspace.

---

## 3. Current state — ComplaincePlatform backend

### 3.1 Service and controller inventory

| Service | Controllers |
| --- | --- |
| ComplianceService | 50+ including `FrameworkController`, `FrameworkReadController`, `V1/ProgramsController`, `RisksController`, `FindingsController`, `EvidenceController`, `JourneysController`, `ComplianceTasksController`, `AuditScheduleController`, `DocumentationController`, `MigrationValidationController` |
| IdentityService | `AuthController`, `UsersAdminController`, `SecurityController`, `TokenController`, `InternalUserController`, `InternalTokenController` |
| AccessGovernanceService | `InternalAuthorizationController`, `PrivilegeAuditController` |
| EnterpriseDirectoryService | `DirectoryController` |
| AIOrchestratorService | `AiController`, `DraftsController` |
| AuditService | `AuditController` |
| GovernanceService | `InternalGovernanceController` |
| CommunicationService | *(no controllers — consumed internally)* |

Stage 5A's "prefer integration over new implementation" is well supported: Organization,
Framework, Workforce/Directory, and Identity all have live APIs.

### 3.2 Programs — partial backend, no configuration depth

- `ComplianceService/Domain/ComplianceProgram.cs` — thin: `HospitalId`,
  `ComplianceFrameworkId`, `ComplianceFramework`, `Name`, `Explainability`. Carries the
  comment `// Add other fields as needed`.
- `ComplianceService/Controllers/V1/ProgramsController.cs` — route
  `api/v{version:apiVersion}/compliance/programs`; only `POST`, `GET {programId}`, `GET`.

Stage 5B's Annual Audit Programs (annual/quarterly/monthly/risk-based/department cadence,
approval, coverage tracking, progress monitoring, automatic audit generation) requires
substantial extension of this aggregate — **not** a new one. Extending the existing entity
avoids a second competing "program" concept.

### 3.3 Workflow — engine exists, rules are hardcoded

`ComplianceService/Application/Workflow/`:
- `IWorkflowRule.cs` — a rule declares an `EventType` and an `ExecuteAsync`.
- `WorkflowRuleEngine.cs` — resolves `IEnumerable<IWorkflowRule>` from DI, filters
  `r.EventType == envelope.EventType`, executes each, and deliberately swallows exceptions
  so "workflow automation must not fail the originating domain transaction".
- `Rules/AuditSessionClosedWorkflowRule.cs`, `Rules/EvidenceApprovedWorkflowRule.cs` — the
  entire rule set, as compiled C# classes.
- `ComplianceService.Tests/WorkflowRuleEngineTests.cs` — engine has test coverage.

This is the single most important Stage 5C finding: **the dispatch mechanism is already
correct.** The engine resolves rules from a collection and matches on event type. Making
workflow configurable does not require replacing the engine — it requires a
`ConfiguredWorkflowRule` that is constructed from persisted configuration and registered into
the same `IEnumerable<IWorkflowRule>`. The compiled rules can then be migrated one at a time,
with the engine and its tests untouched.

A second, unrelated workflow engine exists in `AIOrchestratorService/Application/WorkflowEngine/`
(`IWorkflow`, `IWorkflowExecutor`, `IWorkflowRegistry`, `WorkflowExecutor`, `WorkflowRegistry`,
`StartComplianceWorkflow`) plus `Application/AI/Autonomy/WorkflowTemplates.cs`. Stage 5B must
decide whether the configurable engine unifies these or leaves the AI orchestration engine
alone. **Recommendation: leave it alone this phase** — it serves AI autonomy, not audit
lifecycle, and unifying them is not required by any Stage 5 deliverable.

### 3.4 Risk — formula hardcoded

`ComplianceService/Application/Services/RiskRegisterAppService.cs` builds the risk matrix with
a fixed 5×5 loop (`for likelihood 1..5`, `for impact 1..5`) and a fixed formula
`var score = likelihood * impact;` (line ~226), banded by a `ScoreBand(score)` helper.

Other risk scoring is scattered:
- `ComplianceJourneyService.cs:1191` and `ComplianceEscalationJob.cs:145` both call
  `journey.CalculateRiskScore(majorNcCount, minorNcCount)` — a domain method.
- `DeterministicNabhScoringService.cs:192` —
  `Math.Round(Math.Min(100m, (riskAccumulator / (maxRisk * 4m)) * 100m), 2)`.
- `ControlCenterAggregationService.cs:456` — `RiskScore = Round2(risk)`.

Stage 5C's "Risk scoring uses configured Risk Matrix" therefore touches at least four call
sites with three different notions of risk score. **These are not the same quantity** — the
matrix score (1–25), the journey NC-derived score, and the NABH 0–100 accreditation score
serve different purposes. Collapsing them would be a behavioural regression. The plan must
scope which one the configured matrix governs (the register matrix) and explicitly document
the others as out of scope.

### 3.5 Severity — compiled enums, five of them

- `ComplianceService/Domain/Enums/ComplianceSeverity.cs`
- `ComplianceService/Domain/Enums/DeficiencySeverity.cs`
- `ComplianceService/Domain/Enums/IncidentSeverity.cs`
- `ComplianceService/Domain/GapSeverity.cs`
- `Shared.Infrastructure/Domain/Enums/FindingSeverity.cs`
- `ComplianceService/Domain/Copilot/Enums/RecommendationSeverity.cs`

Stage 5B's Severity Matrix (Critical / Major / Moderate / Minor / Observation / Opportunity
for Improvement, with business, reporting, workflow, and notification rules) cannot simply
replace these — enums are persisted as ints across many tables and changing them is a data
migration, not a configuration change. **Recommended approach:** keep the enums as the
storage primitive and introduce a configurable *severity definition* table that supplies
label, colour, ordering, and the four rule sets, keyed by the existing enum value. This gives
configurability without a destructive migration.

### 3.6 Approvals and signatures — fragments only

- `AccessGovernanceService/Domain/Aggregates/ApprovalAuthority.cs` — exists.
- `ComplianceService/Domain/Documentation/DocumentApproval.cs` +
  `Domain/Enums/DocumentApprovalStatus.cs` — document-scoped approval only.
- No generic approval chain, no delegation, no escalation, no digital signature entity.

Stage 5B's Approval Engine and Digital Signatures are genuinely new schema.

---

## 4. Consequences for planning

1. **Stage 5A is mostly restructuring.** The shell, nav model, redirect pattern, admin
   primitives, and context drawer all exist. The real work is the 12-tab IA, two new shared
   primitives (toolbar, filter bar), folding in the four scattered top-level admin features,
   and scaffolding the six absent tabs.

2. **Stage 5B is the bulk of the effort and is backend-dominated.** Ten domains, each
   needing entities, a generated migration, repository/service, controller, Gateway route,
   typed Angular client, and an admin UI. This is the multi-wave portion.

3. **Stage 5C is riskier than it reads.** "Nothing should remain hardcoded" collides with
   three findings above: severity enums are persisted ints, risk score means three different
   things at different call sites, and the workflow rules that exist are few but load-bearing.
   Each replacement needs a behaviour-preserving default — configuration seeded to reproduce
   today's behaviour exactly — so that "no regression" is verifiable rather than hoped for.

4. **Sequencing implication.** Because the workflow engine already dispatches from a
   collection, and because severity/risk can be made configurable *behind their current
   storage*, Stage 5C can be delivered incrementally per domain rather than as a big-bang
   cutover. Each domain's cutover is: seed configuration reproducing current behaviour →
   switch the read path to configuration → verify identical output → allow the configuration
   to diverge.

---

## 5. Verification approach

Every Stage 5B wave must, per `ENGINEERING_STANDARDS.md` and the Stage 5 engineering rules:

```
dotnet ef migrations add <Name>            # generated, never hand-written
pwsh scripts/ci/validate-migrations.ps1    # attributes, snapshot drift, fresh-DB build
```

plus fresh-database validation, existing-database upgrade validation, and runtime validation
of the affected lifecycle stage. Stage 5C waves additionally require a
seeded-configuration-reproduces-current-behaviour check before the read path is switched.
