# AOVIAS — Master Engineering Roadmap
### The single engineering roadmap from current state to Version 1.0

| | |
|---|---|
| **Status** | Governing document — all future milestones and sprints derive from this file |
| **Owner** | Chief Software Architect |
| **Date** | 2026-07-06 |
| **Companions** | `IMPLEMENTATION_GUIDELINES.md` (how to build), `ARCHITECTURE_DECISIONS.md` (why it is built this way) |
| **Supersedes** | Nothing — incorporates `MILESTONE-1-DETERMINISTIC-PUBLISHING.md`, `MILESTONE-2-INFRASTRUCTURE-CONTRACT-ARCHITECTURE.md`, sprint reports M1.1–M1.5 and M2.0–M2.6, and `CONTRACT-SCHEMA-EVOLUTION-POLICY.md` as completed/reference work |

---

## 1. Vision

**Platform vision.** AOVIAS is a domain-independent enterprise application platform. An operator provisions a clean Ubuntu VPS, runs one bootstrap command, registers the environment, and deploys an entire application platform — validated before execution, certified after it, repeatable indefinitely, recoverable deterministically — without ever editing compose files, creating directories, fixing permissions, installing certificates, or reading Docker logs. PRAMAN is the first workload; the platform contains zero PRAMAN knowledge.

**Version 1.0 objective.** A customer-operable platform: deterministic publishing → sealed infrastructure contracts → planned & admitted deployments → provisioned, executed, health-verified, certified releases → drift-detected, self-healing operations → rollback and history → managed across a small fleet of Docker/Linux environments from the Operations Command Center, including air-gapped installation. Kubernetes, cloud-managed targets, and AI operations are **explicitly not** V1.0.

**Long-term objective.** The same sealed contracts and deployment plans project onto Kubernetes, Azure Container Apps, AWS ECS, and Nomad through target bindings; fleets span clouds and air-gaps; AI assists diagnosis and operations. The platform should feel like Terraform's declarativeness + ArgoCD's reconciliation + Octopus's release management + Docker Desktop's approachability — and stay simpler than all of them for the enterprise-application case.

---

## 2. Architectural Principles (non-negotiable)

1. **Single source of truth.** Every concept has exactly one owner and one implementation. No duplicated runtime logic, infrastructure definitions, planning, or validation. Anything found duplicated is a defect, not a style issue.
2. **Layered architecture.** Publishing → Contracts → Planning → Provisioning → Execution → Operations → Fleet → Cloud → AI. Each layer consumes only the layer above it; no layer bypasses another. A layer may be *incomplete*; it may never be *circumvented*.
3. **Platform first.** No healthcare or PRAMAN-specific logic in any platform assembly. Workload knowledge lives in per-service specification files, never in platform code (enforced since M2.1/M2.3).
4. **Deterministic.** Publishing, planning, execution, rollback, and reconciliation produce identical outputs for identical inputs. Timestamps and identifiers are quarantined into declared-volatile artifacts. Nondeterminism is a regression.
5. **Provider-based.** All host/orchestrator/OS/cloud interaction goes through provider interfaces (`IInfrastructureExecutionContext`, repair executors, secret/certificate providers, target bindings). Core logic never shells out or touches the OS directly.
6. **Enterprise.** Everything is audited, versioned, backward compatible per the schema evolution policy, and evolvable for a decade. Additive-only package layout changes; reader N accepts N and N−1.
7. **Declared, sealed, consumed.** Infrastructure truth flows one way: authored specification → compiled & sealed by Publishing → consumed read-only by everything downstream. Stages that read never write (ADR-0002).
8. **Decisions precede execution.** Planning/admission decides; provisioning and execution only carry out approved plans. Provisioners and executors never make planning decisions (ADR-0007).

---

## 3. Platform Layers

| # | Layer | Responsibility | Primary components | Consumes | Status |
|---|---|---|---|---|---|
| L1 | **Publishing** | Turn source into deterministic, environment-neutral, verified `.aovdeploy` packages; own the Release Library | `Aovias.Publishing.*`, `PublishingPipelineOrchestrator`, `ReleaseLibraryService` | source + specifications | ✅ M1.1–M1.5 |
| L2 | **Contracts** | The language of infrastructure: specification language, schema v2, requirement/capability model, sealed `infrastructure.contract.json`, migration policy | `Aovias.Deployment.Contracts`, `ContractSchemaVersions`, migrators | L1 packages | ✅ M2.0–M2.3 |
| L3 | **Planning** | Deterministic deployment plans; admission engine matching requirements against environment capabilities; blockers before any mutation | `DeploymentPlanner`, Admission Engine (M2.4) | L2 sealed contracts | ✅ M2.4 |
| L4 | **Provisioning** | Idempotent host mutation to satisfy contracts; provider-backed; recovery actions on failure | `InfrastructureProvisioningPipeline`, provisioners, `IInfrastructureExecutionContext` | L3 approved plans | ✅ M2.5 |
| L5 | **Execution** | Deterministic deployment execution by the Runtime Agent; health verification; certification | Runtime Agent execution runtime (M2.5), certification | L4 provisioned state | ✅ core / 🔶 certification & health E2E |
| L6 | **Operations** | Desired vs observed state, drift detection, policy-governed self-healing, rollback, history, audit, Command Center | Reconciliation namespace (M2.6), rollback, CSM | L5 deployed state | 🔶 reconciliation done; rollback/history/UX open |
| L7 | **Fleet** | Multi-environment, environment groups, release promotion (demo→uat→prod), fleet dashboard | environment registry, promotion workflow | L6 per-environment operations | ⬜ |
| L8 | **Cloud** | Target bindings beyond docker-compose: Kubernetes, ACA, ECS, Nomad; cloud provider adapters | `IDeploymentTargetBinding` projections | L2 contracts + L3 plans | ⬜ (V2; binding seam exists) |
| L9 | **AI** | AI-assisted diagnosis, recommendations, operational copilot — advisory first, never bypassing L3 admission | AI operations services | L6/L7 telemetry & history | ⬜ (V2+) |

Dependency rule: L(n) consumes L(n−1)'s artifacts through its published interfaces only. Example of a violation (historical, fixed in M2.0): the Runtime Agent rebuilding contracts at deploy time instead of reading the sealed artifact.

---

## 4. Current State (evidence-based)

Completed and reported (see per-sprint implementation reports in this folder):

- **M1 — Deterministic Publishing**: M1.1 debt cleanup (legacy pipeline deleted), M1.2 deterministic engine, M1.3 contract-driven publishing, M1.4 environment-neutral packages, M1.5 Release Library with lifecycle/promotion states, persisted index, single registration path.
- **M2 — Infrastructure Contract Architecture**: M2.0 contract authority (sealed contract is read, never rebuilt), M2.1 infrastructure specification language + compiler, M2.2 schema v2 + versioned migration (+ living evolution policy), M2.3 requirement/capability model, M2.4 deployment planning & admission engine, M2.5 provisioning engine & execution runtime, M2.6 reconciliation & self-healing with policy and safety gating.

**Known caveats carried into this roadmap (must be burned down, not forgotten):**

- **C1 — Verification debt.** Several sprints (M1.4, M1.5, and others by their own disclosure) were *traced correct, not build-verified* — no .NET SDK was available in the implementation sandbox. Nothing after M1.3 has demonstrably passed `dotnet build && dotnet test`.
- **C2 — Dual drift concepts.** `PlatformDeploymentService.DetectDriftAsync` (platform-wide deployment status) was deliberately left beside the M2.6 reconciliation loop; unification was deferred with rationale (M2.6 §9).
- **C3 — No end-to-end proof.** The full chain (publish → plan → admit → provision → execute → verify → certify → reconcile) has never run against a real clean VPS.
- **C4 — Open feature gaps to the vision**: VPS bootstrap, environment registration UX + capability declaration flow, contract-fed certification, rollback integration with the plan/recovery domain, deployment history surfaces, promotion *workflow* (states exist, workflow doesn't), Command Center, fleet, air-gapped install.

---

## 5. Milestones to Version 1.0

Numbering continues from completed M1/M2. Complexity: S/M/L/XL. Order below **is** the suggested implementation order; each milestone's exit criteria gate the next.

### M3 — Verified Platform Baseline
**Objective.** Convert "traced correct" into "verified correct" and freeze a trustworthy baseline. **Problem solved:** C1, C2, and the debt registers accumulated inside M1.x/M2.x reports — the platform currently rests on untested code paths. **Scope:** restore full build/test capability; run and fix the complete test suite across all solutions; execute the M1 double-publish fingerprint gate for 10/10 services; consolidate the dual drift implementations (C2) under the M2.6 engine; close or formally re-register every "Technical Debt" item listed in sprint reports; establish CI that blocks on build+tests+grep gates. **Out:** new features. **Dependencies:** none (first). **Exit criteria:** all solutions build; all tests green in CI; fingerprint reproducibility proven; single drift/reconciliation implementation; debt register empty or re-adjudicated. **DoD:** CI badge green on main; baseline tag `v0.9-baseline` cut. **Risk (M):** traced-only code may hide compile/runtime errors — timebox fixes, don't redesign. **Complexity: M.**

### M4 — Bootstrap & Environment Registration
**Objective.** A clean Ubuntu VPS becomes a registered, capability-declared AOVIAS environment with one command. **Problem solved:** today host preparation is a hand-run bash script with duplicated path knowledge; environments exist without declared capabilities, so admission can't be authoritative. **Scope:** bootstrap installer (installs Docker + Runtime Agent + config root + TLS; consumes a contract-derived host-preparation manifest — no hardcoded paths); registration handshake issuing environment identity + token; capability declaration persisted with the registration (`EnvironmentRegistrationStore` extended); CSM environment onboarding flow; admission (M2.4) wired to registered capabilities as the Deployment-Ready gate; re-bootstrap idempotency. **Out:** fleet groups, cloud images. **Dependencies:** M3. **Exit criteria:** clean VPS → operational registered environment in ≤ 10 minutes, zero manual steps beyond running the installer and pasting a token; deploying against an unregistered/insufficient environment is blocked at admission with recovery guidance. **DoD:** bootstrap tested on ≥ 2 fresh VPS images; `provision-deployment-storage.sh` deleted or reduced to generated-manifest executor. **Risk (M):** OS variance (Ubuntu LTS versions) — pin supported matrix. **Complexity: M/L.**

### M5 — Certified Deployment End-to-End
**Objective.** The target workflow Publish → Deployment Ready → Deploy → Health Verification → Platform Certified runs as one verified chain on real environments. **Problem solved:** the pieces exist (M2.4/M2.5, certification checklist models) but certification is not contract-fed and the chain is unproven (C3). **Scope:** health verification consuming the contract's health domain (probe kinds incl. non-HTTP); `PlatformCertificationChecklist` generated from requirements (every requirement demonstrably satisfied → certified); deterministic operator-error taxonomy on every failure path (`DeploymentOperatorError` completeness audit); deployment reports; SignalR progress to CSM hardened. **Out:** rollback, promotion. **Dependencies:** M4. **Exit criteria:** full platform (10 services) deploys to a fresh M4 environment; certification report shows requirement-by-requirement evidence; every induced failure class yields a typed error with recovery action, never a raw Docker log. **DoD:** E2E suite in CI against a disposable VPS (or high-fidelity container sim); 3 consecutive clean-VPS deployments with zero manual ops. **Risk (M/L):** flaky health timing on small VPSes — contract-declared stabilization windows already exist; tune, don't hardcode. **Complexity: L.**

### M6 — Rollback, History & Promotion
**Objective.** Deployments become reversible and auditable; releases move through environments deliberately. **Problem solved:** rollback exists in `Aovias.Release` but predates plans/recovery domains; history is fragmentary; promotion has states (M1.5) but no workflow. **Scope:** rollback executed as a *plan* (same planner/admission/execution path as deploy — no parallel machinery), gated by the contract's recovery domain (rollback-safety class; data-compatible vs manual); post-rollback health verification + certification; durable deployment history (per environment: what, when, by whom, plan, certificate, outcome) with replay; promotion workflow over the M1.5 ladder (Draft→Verified→Certified→Production-Approved) with per-environment channels; audit trail for every state transition. **Out:** multi-environment orchestration UI (M8). **Dependencies:** M5. **Exit criteria:** deploy vN → deploy vN+1 → rollback → redeploy cycle passes repeatedly with certified health at every step; history answers "what is running where and why" without filesystem inspection; promotion denial blocks deployment to protected environments. **DoD:** rollback E2E in CI; `Aovias.Release` rollback path either delegates to the plan-based path or is deleted (no parallel implementation). **Risk (L):** data-migration rollbacks — V1 policy: automatic rollback only for `stateless`/`data-compatible`; `manual` class requires operator confirmation. **Complexity: L.**

### M7 — Operations Command Center
**Objective.** CSM becomes the Enterprise Operations Command Center: one pane for publish, plan, deploy, certify, drift, heal, rollback, history, audit. **Problem solved:** capabilities exist API-first but operator experience is fragmented across dashboards built per-sprint. **Scope:** environment overview (registered environments, capabilities, drift status, running versions); deployment console (plan preview + admission blockers before deploy; live execution; certification results); reconciliation console (drift reports, policy decisions, approve-repair for operator-gated actions, cycle history); release console (library, lifecycle, promotion); audit views; every screen consumes existing APIs — **no business logic in the UI layer**. **Out:** fleet-wide aggregation (M8), web UI rewrite. **Dependencies:** M5 (consoles need real data), M6 (rollback/history/promotion views). **Exit criteria:** an operator completes the entire golden path and one failure-recovery path using only the Command Center — no CLI, no SSH, no file explorer. **DoD:** scripted operator walkthrough recorded as the acceptance artifact; UX debt register triaged. **Risk (M):** UI scope creep — screens are bounded by the API surface; new capabilities are API-first. **Complexity: M/L.**

### M8 — Fleet & Multi-Environment
**Objective.** Manage many environments as one fleet. **Problem solved:** V1 customers run demo/uat/prod (and multiple customer sites); today each environment is an island. **Scope:** environment groups and fleet registry; fleet dashboard (health/version/drift matrix across environments); coordinated promotion (promote release through channel → environments in order, admission per environment); per-environment secrets/profile management with contract-driven required-secret checklists; fleet-wide reconciliation posture (policy per environment class: prod conservative, demo permissive — M2.6 policy framework already environment-aware); agent update mechanism (fleet-managed Runtime Agent upgrades — the platform must deploy itself). **Out:** cross-cloud fleets, federation. **Dependencies:** M6, M7. **Exit criteria:** three-environment fleet (demo/uat/prod) driven from one Command Center; a release promoted demo→uat→prod with per-stage certification; agent self-update proven. **DoD:** fleet E2E scenario in CI; concurrent-deploy safety (two environments deploying simultaneously) tested. **Risk (M/L):** agent self-update is the riskiest mutation in the platform — staged rollout + automatic rollback of the agent itself required. **Complexity: L.**

### M9 — Enterprise Hardening & Air-Gapped (V1.0 gate)
**Objective.** Make it shippable to enterprises. **Problem solved:** security, audit completeness, offline installation, and platform-upgrade guarantees are implied everywhere and finished nowhere. **Scope:** security pass (agent API authn/authz review, secret handling audit, TLS everywhere, least-privilege container users — contract security domain enforced); complete audit trail (every mutating operation attributable: who/what/when/plan/outcome); air-gapped installation (offline bundle: bootstrap + agent + packages + base images; `offline-artifact-store` capability; no network egress required end-to-end); backup/restore of platform state (release index, registrations, history); platform version-upgrade path (V1.0 → V1.1 upgrade rehearsal incl. schema migrations per evolution policy); documentation set (operator guide, installation guide, troubleshooting taxonomy — generated from contracts where possible); performance/soak baseline (deploy time, reconcile cycle cost, package size budgets). **Dependencies:** M8. **Exit criteria:** §10 Version 1.0 exit criteria all green. **DoD:** external-style acceptance: a non-developer operator installs air-gapped from documentation alone. **Risk (M):** security findings late in the cycle — start the security review in parallel with M7. **Complexity: L.**

**Post-1.0 (V2 horizon, defined now so V1 decisions don't foreclose them):**
**M10 — Kubernetes & Cloud Bindings (XL):** production K8s target binding from unmodified sealed contracts (seam proven in M2.x), ACA/ECS follow; capability adapters per orchestrator; hybrid fleets. **M11 — AI-Assisted Operations (L):** diagnosis over reconciliation history and operator-error taxonomy, recommendation engine, natural-language ops queries — advisory only, every action still through admission. **Future:** multi-cloud federation, marketplace of service specifications, policy-as-code.

---

## 6. Sprint Breakdown

Sprints are sized for one implementation-model session each, with `IMPLEMENTATION_GUIDELINES.md` as the standing contract. Acceptance criteria below are the sprint-report headline; each sprint also inherits the standing DoD (guidelines §8).

**Standing process rule (effective M3.1 forward, codified in `IMPLEMENTATION_GUIDELINES.md` §12):** every integration/verification phase — M3 below, and every later phase that re-earns trust in the platform before new capability work resumes — follows the same four-stage progression in order: (1) **Build** — resolve compiler errors, establish a trusted build baseline; (2) **Test** — restore and execute automated tests against that baseline; (3) **Runtime** — validate on a real VPS; (4) **Certification** — repeat the successful cycle to prove it, not just demonstrate it once. This is a permanent addition to how the roadmap is executed, not a one-time sequence specific to M3 — it re-triggers at every future point where the roadmap pauses to prove the platform rather than extend it.

**M3 — Verified Platform Baseline**
- **M3.0 Toolchain Restoration & Canonical Build Baseline** — Objective: a trusted build baseline (no new features). Scope: canonical solution entry points decided and recorded; solution-coverage gaps fixed; project/package/target-framework consistency validated; SDK/toolchain prerequisites documented; undocumented modules (Bootstrap, CommandCenter) retro-documented. Acceptance: canonical solutions defined; coverage gaps resolved; dependency graph internally consistent; CI prerequisites identified. Depends: —.
- **M3.1 Build Restoration** — Objective: everything compiles. Scope: restore SDK/CI, fix compile errors from traced-only sprints, no behavior changes beyond what compilation forces. Acceptance: `dotnet build` green for all `.slnx`. Depends: M3.0.
- **M3.2 Test Restoration & Fingerprint Gate** — Objective: all suites green + M1 reproducibility proven. Scope: fix failing tests, run double-publish gate 10/10, wire CI blocking. Acceptance: CI green; fingerprint equality report committed. Depends: M3.1.
- **M3.4′ Debt Register Burn-down + Drift Consolidation** — Objective: empty the M1.x/M2.x debt lists; one reconciliation truth (C2). Scope: fix or formally re-adjudicate each debt item; add grep gates; fold `DetectDriftAsync` semantics into the M2.6 engine as a platform-scope view or delete it — per `ROADMAP_STATUS_REVIEW.md` §9.2, M3.3's own scope was already almost entirely delivered by M2.6, so M3.3 merges into this sprint rather than running standalone. Acceptance: debt table with disposition per item; gates in CI; one drift implementation; endpoint behavior preserved. Depends: M3.2.

  **M3.0 delivered (this session), scope note:** see `ROADMAP_STATUS_REVIEW.md` (2026-07-06) for the
  audit that identified this sprint's need — the review found solution-coverage gaps
  (`Aovias.Bootstrap.sln` covered only 1 of its 4 projects; `ComplianceServiceManager`/
  `Aovias.DesignSystem` were reachable from no canonical Windows-leg solution) and undocumented
  modules (Bootstrap, CommandCenter — now retro-documented in
  `BOOTSTRAP-AND-COMMANDCENTER-ENGINEERING-STATE.md`). Fixed: `Aovias.Bootstrap.sln` now lists all
  4 projects + its external reference closure; `ComplianceServiceManager.slnx` now explicitly lists
  `Aovias.DesignSystem` alongside CSM and is documented as the platform's Windows canonical build
  entry point, paired with `Aovias.Platform.slnx` as the cross-platform entry point; two
  previously-unlisted cross-platform tools (`tools/PublishAllServices`, `tools/PublishRuntimeAgent`)
  added to `Aovias.Platform.slnx`. Static Verification (scripts actually run, not traced): all 90
  `ProjectReference` paths across all 36 production `.csproj` resolve; zero circular project
  references (max depth 8); zero package-version drift across every `PackageReference` in the
  tree; target frameworks consistent (`net10.0` everywhere except the two intentionally
  `net10.0-windows`/WPF projects). Found and disclosed, not fixed (register with M3.1): a handful
  of projects (`Aovias.DeploymentValidation`, its test project, `Aovias.DesignSystem`,
  `ComplianceServiceManager`) omit the `LangVersion=preview` every other strict-build project sets
  — likely harmless (net10.0's default `LangVersion` is already modern) but inconsistent, left
  for M3.1 rather than edited blind with no compiler to confirm the change is safe. No `dotnet` SDK
  and no network reachability to `nuget.org`/`packages.microsoft.com` in this sandbox — every
  build/test claim above is Static Verification or Source Analysis, never Runtime Verification. See
  the M3.0 sprint report for the full disposition.

  **M3.1 delivered (this session), scope note:** this sandbox has no `.NET` SDK, no cached
  toolchain, no network reachability of any kind (verified: `dotnet` absent; `curl` against
  `nuget.org`/`dot.net`/`packages.microsoft.com`/`github.com`/`google.com` all return connection
  failure), and no `sudo`/privilege escalation available to attempt installing one — strictly more
  restrictive than the M3.0 session (which at least found a network path, albeit a 403). `dotnet
  build`/`dotnet restore` are therefore **Blocked (no SDK, no network, no sudo in this sandbox)**
  for both `Aovias.Platform.slnx` and `ComplianceServiceManager.slnx` — this cannot be claimed as
  Verified/Passed/Green per `IMPLEMENTATION_GUIDELINES.md` §11, and is not claimed as such below.
  In place of an actual compile, this sprint ran the highest-value substitute achievable without a
  compiler: a source-level "would this compile" audit across every production project (interface/
  implementation member-matching for every `class X : IY`, DI-registration completeness for every
  constructor-injected type, `using`/namespace resolution, cross-project call-signature matching,
  `required`-member initialization, and duplicate/ambiguous short-type-name collisions). Four
  parallel passes covered Deployment.Contracts/Publishing/Orchestration, Release.Contracts/SDK/
  ReleaseManager/PackageBuilder, Runtime.Agent/CommandCenter/Bootstrap/Configuration/
  DeploymentValidation, and ComplianceServiceManager/DesignSystem. Result: zero confirmed
  compile-blocking defects across all four areas. One candidate finding (an apparently-truncated
  `Aovias.Runtime.Agent.Infrastructure/DependencyInjection.cs` missing four DI registrations) was
  investigated and disproven by direct `Read`-tool inspection of the full 202-line file — every
  registration the finding claimed was missing is present (`RuntimeCapabilitiesProvider`,
  `EnvironmentRegistrationRecordStore`, `EnvironmentAdmissionStore`,
  `ICommandCenterRegistrationClient`, both M4.3/M4.4 `AddHostedService<>` calls); this is the same
  bash-mounted-stale-file-view phenomenon disclosed in the M3.0 report, now confirmed recurring —
  **files edited via this program's own tools earlier in a session cannot be trusted via bash reads
  in that same session; the file-editing tool's own `Read` is authoritative.** The one carried-over
  item from M3.0 (`LangVersion=preview` omitted on 4 projects) was investigated and left
  intentionally unfixed: none of the 4 projects use any preview-only C# syntax (confirmed by source
  search), so the omission is a config inconsistency with no compile risk, not a build blocker —
  per this sprint's own rule ("fix only if required to restore the build, otherwise document"), it
  remains registered as debt rather than edited blind. No architecture was changed, no functionality
  was added or removed. See the M3.1 sprint report for the full disposition and evidence tiers.

  **M3.2 delivered (this session), scope note:** "Test Restoration & Integration," re-run after the
  corrected M3.0→M3.1→M3.2 sequencing (the original M3.2 report above predates that correction and
  was scoped to solution/build-config hardening, not test execution). Same sandbox blocker as M3.1
  (no SDK, no network, no sudo) means no `dotnet test` was executed; three parallel source-level
  audits instead reviewed all 5 real Aovias.* test projects (394 declared `[Fact]`/`[Theory]`
  methods, 66 files) against current production code for Legacy/Regression/Architecture-Drift/
  Environment-SDK-Dependent staleness — the four categories this sprint's brief requires. **Zero
  issues found in any category**; no test modified, disabled, or removed. Specifically cleared:
  `Aovias.Publishing.Core.Tests` (M1-era, never previously reviewed for test-logic staleness, only
  for compile-safety); the dual admission concepts `EnvironmentAdmissionTests.cs` (M4.4) vs.
  `DeploymentAdmissionAndPlanningTests.cs` (M2.4), confirmed genuinely distinct, not superseding;
  `FleetManagementTests.cs` (M2.7) vs. `EnvironmentRegistrationTests.cs` (M4.3) BaseUrl-idempotency
  assertions, confirmed non-contradictory; `InfrastructureContractMigrationTests.cs` confirmed
  exercising the current v1→v2 migration chain in full. **New scoping finding, disclosed not
  fixed:** a nested, independently-git-tracked product (`ComplaincePlatform/`, its own `.slnx` and
  15 test projects) was found in the repository — never referenced by any Aovias.* solution or
  sprint report, reads as a customer compliance workload rather than a platform component per
  `IMPLEMENTATION_GUIDELINES.md` §1.11 (platform-first). Excluded from this sprint as out of scope;
  disclosed rather than silently ignored. `dotnet test` remains **Blocked (no SDK, no network, no
  sudo in this sandbox)** for all 5 in-scope projects — not claimed as Verified/Passed. See the
  M3.2 report addendum for the full per-suite breakdown and evidence tiers.

  **M3.3 delivered (this session), scope note:** "End-to-End Platform Integration" — traced the full
  14-stage deployment pipeline (Build→Test→Publish→Package→Bootstrap→Runtime Agent→Environment
  Registration→Capability Declaration→Admission→Deployment Plan→Provisioning→Deployment→Health
  Verification→Certification→Reconciliation) for actual cross-stage wiring, not single-file compile
  correctness (M3.1) or single-project test logic (M3.2). Same SDK/network/sudo blocker as before,
  refined this session: the egress proxy is reachable and returns `403 blocked-by-allowlist` for
  nuget.org/dot.net/GitHub/Microsoft CDNs — a deliberate policy block, not total connectivity
  failure as M3.1/M3.2 framed it (practical effect unchanged: SDK acquisition still impossible).
  **Two genuine integration defects found and fixed:** (1) `EnvironmentRegistrationSettings`
  (M4.3) had no `settings.*.json` file mapping in `ProfileConfigurationLoader`, so a bootstrapped
  environment's automatic re-registration-on-restart could never activate — fixed by adding the
  mapping and having Bootstrap generate the file from values it already holds; (2) M4.4's
  environment-level Deployment Ready report was computed and persisted but never actually consulted
  before a deployment executed — fixed by wiring `EnvironmentAdmissionStore` into
  `ReleaseDeploymentProvider`'s existing admission checkpoint, reusing the store with no new
  evaluation logic. One recurring bash-mount-staleness false positive (on the same
  `DependencyInjection.cs` file flagged in M3.0/M3.1) was disproven via direct `Read`-tool
  inspection. Two lower-confidence findings (a possible Bootstrap container-startup race; an
  apparent Bootstrap/Release-Library path mismatch that turned out to be two intentionally separate
  subsystems) are documented, not fixed. No stage was Runtime- or End-to-End-verified — every claim
  is Source Analysis/Static Verification per `IMPLEMENTATION_GUIDELINES.md` §11. See the M3.3
  sprint report for the full per-stage tagging.

**M4 — Bootstrap & Environment Registration**
- **M4.1 Host-Preparation Manifest** — generation from contracts (runtime-agent's own spec included); bash reduced to executor. Acceptance: manifest-driven prep on fresh VPS. Depends: M3.
- **M4.2 Bootstrap Installer** — one-command install (Docker, agent, config root, TLS, token). Acceptance: ≤10-min clean-VPS bring-up, idempotent re-run. Depends: M4.1.
- **M4.3 Registration & Capability Declaration** — handshake, identity, persisted capabilities. Acceptance: registered environment visible in CSM with capability list. Depends: M4.2.
- **M4.4 Admission Wiring** — Deployment-Ready gate consumes declared capabilities; unmet requirement blocks with recovery text. Acceptance: admission matrix E2E (satisfied/missing/external/deferrable). Depends: M4.3.

  **M4.1 delivered (this session), scope note:** the sprint actually executed against a "Zero-Touch
  Environment Bootstrap" brief compressing this section's M4.1 (host-preparation) and part of M4.2
  (one-command bootstrap install) into a single sprint; M4.3's CSM-visible capability declaration
  and M4.4's admission wiring were not attempted and remain open as described above. Delivered:
  `Aovias.Bootstrap.Contracts`/`Aovias.Bootstrap.Core`/`Aovias.Bootstrap.Host` — a new `bootstrap`
  verb (additive; the pre-existing `deploy` verb is unchanged) that runs eight fixed, idempotent,
  resumable phases against a bare Ubuntu VPS: Preflight, Docker installation, Docker network
  creation, runtime directory scaffold, runtime configuration generation
  (`settings.deployment-host.json`, the same shape `DeploymentPathResolver` already reads — no new
  schema), Runtime Agent installation, Runtime Agent registration (reuses the existing
  `POST /api/deployment/register` self-registration and Command Center `POST /api/environments/register`
  Fleet Registry endpoints verbatim — no new registration logic), and end-to-end heartbeat
  verification, producing one `BootstrapReport`. Real SSH/VPS execution requires runtime
  verification (this sandbox has no .NET SDK); phase-sequencing, idempotency, and resume logic have
  regression test coverage. See the M4.1 sprint report for the full disposition.

  **M4.3 delivered (this session), scope note:** "Environment Registration & Capability
  Declaration." A Runtime Agent now registers itself with the Operations Command Center's Fleet
  Registry automatically on every startup, with no external tool or operator step required — this
  is the automatic counterpart to M4.1's bootstrap tool, which called the same two endpoints from
  outside the process during first-time VPS setup only. New `EnvironmentRegistrationHostedService`
  (Runtime Agent) performs the same two-step handshake M4.1 established (in-process
  `EnvironmentRegistrationStore` self-registration for a middleware-valid API key, then
  `POST /api/environments/register` against Command Center) and persists the returned identity
  locally so restarts don't create duplicates; idempotency is reinforced server-side by making
  `FleetRegistry.Register` upsert by `BaseUrl` instead of always minting a new id. Capability and
  platform-version synchronization ride the existing M2.7 `RuntimeAgentPollingService` heartbeat
  loop (two new fields copied each cycle) rather than a new polling mechanism. Note: the
  `ComplianceServiceManager` WPF app has its own, separate, legacy-looking `IEnvironmentManager`
  concept that this sprint did not touch — the Fleet Registry is the only registration target an
  autonomous agent can push to. Real Command Center round-trips require runtime verification (no
  .NET SDK in this sandbox); registration decision logic (first-run, restart-survival,
  Command-Center-forgot-me recovery), the Fleet Registry upsert, and heartbeat capability/version
  sync all have regression test coverage. See the M4.3 sprint report for the full disposition.

  **M4.4 delivered (this session), scope note:** "Environment Admission & Deployment Readiness."
  Every Runtime Agent now continuously evaluates its own environment-level Deployment Ready state
  and synchronizes it to Command Center, distinct from both the existing per-package DRV report
  (`Aovias.Publishing.Contracts.DeploymentReadinessReport`, environment-independent) and the
  existing per-deployment `DeploymentAdmissionEngine` (Sprint M2.4, evaluated once per deployment
  attempt against one package's contract). New `EnvironmentAdmissionEvaluator` classifies every
  requirement this environment has been asked to satisfy so far (sourced from
  `LastDeploymentPlanStore`, Sprint M2.6) into the four states the roadmap's M4.4 line names —
  Satisfied/Missing/External/Deferrable — by relabeling `RequirementCapabilityMatcher`'s existing
  Satisfied/Missing/IncompatibleVersion output; it introduces no second matching algorithm.
  "Authoritative capabilities" for this evaluator means the environment's declared
  `EnvironmentCapabilityProfile` (Sprint M2.3 D6, new `EnvironmentRegistration:CapabilityProfileId`
  setting, defaults to `docker-host`) — deliberately not the Sprint M4.3 heartbeat-synced
  `RuntimeCapability.*` flat strings, which is a disjoint vocabulary describing what the Runtime
  Agent process itself supports, not what infrastructure the host provides (disclosed explicitly so
  this distinction isn't lost). New `EnvironmentAdmissionHostedService` recomputes on startup and
  every 2 minutes thereafter (picking up both new deployments and capability-profile config
  changes via the existing `IOptionsMonitor` live-reload mechanism) and persists the result
  (`EnvironmentAdmissionStore`, same atomic-write convention as Sprint M4.3's registration record) —
  surviving a Runtime Agent restart without recomputation. The heartbeat DTO and `RemoteEnvironment`
  gained a compact `DeploymentReady`/`AdmissionBlockerCount` summary (rides the existing M2.7
  heartbeat poll loop, no new sync mechanism); a new `GET /api/deployment/admission` Runtime Agent
  endpoint returns the full diagnostic detail with remediation text for CSM/operators. Disclosed
  limitation: a completed deployment's readiness effect is picked up on the hosted service's next
  2-minute tick, not synchronously at the instant `ReleaseDeploymentProvider` finishes — a
  deliberate choice to avoid touching that already-large execution path this sprint. Real Command
  Center/heartbeat round-trips require runtime verification (no .NET SDK in this sandbox);
  classification logic (all four states, multi-service aggregation, profile-driven transitions),
  persistence/restart, and the hosted service's recalculation triggers all have regression test
  coverage. See the M4.4 sprint report for the full disposition.

**M5 — Certified Deployment End-to-End**
- **M5.1 Health Verification Engine** — contract health domain incl. probe kinds; stabilization windows. Acceptance: per-service verified health from contract only. Depends: M4.
- **M5.2 Contract-Fed Certification** — checklist generated from requirements; evidence per item. Acceptance: certification report for full platform. Depends: M5.1.
- **M5.3 Failure Taxonomy Completion** — every failure path typed + recovery action; no raw-log surfacing. Acceptance: fault-injection matrix passes. Depends: M5.1.
- **M5.4 E2E Chain in CI** — publish→…→certify against disposable environment; 3× repeatability. Acceptance: pipeline green thrice consecutively. Depends: M5.2, M5.3.

**M6 — Rollback, History & Promotion**
- **M6.1 Rollback as a Plan** — plan-based rollback through admission/execution; recovery-domain gating. Acceptance: certified rollback cycle E2E. Depends: M5.
- **M6.2 Deployment History** — durable per-environment history + replay + query API. Acceptance: "what runs where" answerable via API only. Depends: M6.1.
- **M6.3 Promotion Workflow** — ladder transitions with rules, channels, protected environments; audit. Acceptance: promotion denial blocks prod deploy. Depends: M6.2.
- **M6.4 Legacy Rollback Retirement** — `Aovias.Release` path delegates or is deleted. Acceptance: zero parallel rollback implementations (grep gate). Depends: M6.1.

**M7 — Operations Command Center**
- **M7.1 Environment & Deployment Consoles** — overview, plan preview with blockers, live execution, certification view. Acceptance: golden path via UI only. Depends: M5.
- **M7.2 Reconciliation & Repair Console** — drift, policy decisions, operator-approval queue, cycle history. Acceptance: operator-gated repair approved via UI. Depends: M7.1.
- **M7.3 Release & History Consoles + Audit** — library, promotion, history, audit views. Acceptance: scripted operator walkthrough recorded. Depends: M6, M7.1.

**M8 — Fleet & Multi-Environment**
- **M8.1 Fleet Registry & Dashboard** — groups, health/version/drift matrix. Acceptance: 3-environment fleet visible. Depends: M7.
- **M8.2 Coordinated Promotion** — channel-ordered promotion with per-stage admission/certification. Acceptance: demo→uat→prod promotion E2E. Depends: M8.1, M6.3.
- **M8.3 Per-Environment Secrets & Profiles** — contract-driven required-secret checklists per environment. Acceptance: missing prod secret blocks at admission, not at runtime. Depends: M8.1.
- **M8.4 Agent Self-Update** — fleet-managed agent upgrade with staged rollout + self-rollback. Acceptance: fleet agent upgrade E2E incl. induced failure. Depends: M8.1.

**M9 — Enterprise Hardening & Air-Gapped**
- **M9.1 Security Pass** — authn/authz, secrets audit, TLS, least privilege; findings fixed. Acceptance: security checklist signed off. Depends: M7 (can start parallel to M8).
- **M9.2 Audit Completeness** — every mutation attributable; audit query surface. Acceptance: audit E2E for all mutating operations. Depends: M8.
- **M9.3 Air-Gapped Install** — offline bundle build + offline bootstrap + offline deploy. Acceptance: full install with egress disabled. Depends: M4, M8.
- **M9.4 Backup/Restore & Platform Upgrade** — state backup/restore; V1.0→V1.1 rehearsal with schema migration. Acceptance: restore drill + upgrade drill pass. Depends: M9.2.
- **M9.5 Documentation & Release Readiness** — operator/install/troubleshooting docs (contract-generated where possible); soak/perf baselines; V1.0 release checklist. Acceptance: non-developer installs from docs alone. Depends: all.

---

## 7. Architecture Evolution

**Version 1 (M3–M9).** Docker-compose on Linux VPS fleets, single target binding, CSM (WPF) as Command Center, file-based secret provider (vault seam present), air-gapped capable. The contract schema, plan format, and provider seams are already V2-shaped — V1 deliberately ships with one implementation behind each seam.

**Version 2 (M10–M11).** Kubernetes and cloud bindings as additional Layer-3 projections of *unchanged* sealed contracts; vault/KeyVault secret providers; hybrid fleets; AI-assisted operations reading history/telemetry, acting only through admission. No schema redesign is expected: the requirement/capability model was built for this (ADR-0004); new targets mean new capability adapters + bindings.

**Future.** Multi-cloud federation, web-based Command Center, specification marketplace, policy-as-code admission extensions. Evolution mechanism is always the same: new provider/binding/capability-kind — never a new parallel subsystem.

---

## 8. Technical Debt Strategy

**Cannot be deferred (fix in the sprint that finds it):** duplicated implementations or models; layer bypasses; contract writes outside Publishing; nondeterminism in publishing/planning/execution/rollback/reconciliation; unverified "traced-only" code merged to main (post-M3 this is banned outright); missing audit on mutating operations; placeholder/TODO implementations.

**May be deferred (with registration):** UX polish; performance beyond budgets; additional providers/bindings; broader OS matrices; nice-to-have query surfaces. Deferral requires an entry in the milestone report's debt table with owner, trigger condition, and target milestone — undated debt is forbidden.

**Cleanup philosophy.** Debt is retired by *deletion or replacement, never accretion*: the M1.1 pattern (delete the legacy pipeline before building on it) is the template. Every milestone's first sprint may reserve capacity for burning predecessor debt.

**Refactoring philosophy.** Refactor when a rule in `IMPLEMENTATION_GUIDELINES.md` is violated or a second consumer is about to duplicate logic — not for aesthetics mid-sprint. Behavior-preserving refactors ship with the regression evidence that proves preservation (golden fingerprints, snapshot tests).

---

## 9. Verification Strategy

- **Architecture verification.** Every milestone opens with a spec-vs-code assessment (the M1/M2 review pattern: file/line evidence, no unverified claims) and closes with an ADR update. Grep gates in CI enforce structural rules (no service-id switches, no parallel implementations, no contract writes outside Publishing).
- **Implementation verification.** `dotnet build` + full test suite green before any sprint is reported complete (post-M3, non-negotiable). Signature-tracing is a review aid, never an acceptance substitute.
- **Regression strategy.** Every bug fixed and every determinism property earns a permanent regression test (the M1 §10 suite is the model: reproducibility, stale-artifact, contract-consistency, failure-before-packaging). Suites accumulate; they are never trimmed to go green.
- **Integration testing.** Cross-layer seams (publish→plan, plan→provision, provision→execute, execute→reconcile) each carry integration suites with induced-failure matrices.
- **End-to-end testing.** The golden path and the failure/recovery paths run in CI against disposable environments (fresh VPS or high-fidelity container simulation) — 3× repeatability is the standing bar; fleet and air-gapped scenarios join at M8/M9.
- **Production readiness.** V1.0 requires: soak test (fleet under continuous reconcile + periodic deploys for ≥ 72h), restore drill, upgrade drill, security sign-off, and the documented non-developer installation.

---

## 10. Versioning Strategy

| Artifact | Scheme | Rules |
|---|---|---|
| **Contract schema** | integer versions (v1, v2, …) | Governed by `CONTRACT-SCHEMA-EVOLUTION-POLICY.md`: one Current writer version; readers accept Current + Supported via registered migrators; Deprecated→Removed with republish requirement. This policy is the template for all other schemas below. |
| **Packages (`.aovdeploy`)** | layout version in metadata + additive-only changes within a major | Reader N accepts N and N−1; breaking layout change = major, requires republish path. |
| **Publishing engine** | semver; engine version stamped in metadata | Determinism-affecting changes bump minor and re-baseline golden fingerprints (one controlled re-baseline event). |
| **Runtime Agent** | semver; capability-advertised (build manifest already carries `deploymentApi`, `pathPolicyVersion`) | Agent advertises API + schema support; CSM/planner check compatibility before use; fleet-managed upgrades (M8.4). |
| **Providers / bindings** | interface-versioned; new capability kinds via governed registry | Additive kinds are minor; interface breaks are major and require side-by-side support for one release. |
| **Deployment plans** | plan format version + content hash | Plans are immutable, hash-addressed, and replayable; executor rejects unknown plan versions (no best-effort execution). |
| **APIs (Agent/SDK)** | versioned routes; N−1 supported | CSM and Agent may skew by one minor version in a fleet during upgrades. |

---

## 11. Risks & Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Traced-only code fails once actually built (C1) | Baseline slips | M3 is first and fix-only; timebox; nothing new lands before CI is green |
| Agent self-update bricks environments | Fleet outage | Staged rollout, health-gated, agent self-rollback (M8.4); bootstrap can always reinstall |
| Determinism erodes under feature pressure | Golden gates rubber-stamped | Fingerprint gates in CI, approval workflow with recorded diffs, ADR-0012 enforced in review |
| Reconciliation repairs cause harm in prod | Trust loss | M2.6 policy + safety gate already environment-aware; prod defaults conservative; operator-approval queue (M7.2) |
| Layer bypass creep (UI or scripts reaching into lower layers) | Architecture decay | API-first rule for CSM; grep gates; ADR log as review checklist |
| Schema/plan version skew across fleets | Deploy failures | Versioning strategy §10; compatibility checks at admission; upgrade drills (M9.4) |
| Data-migration rollbacks | Data loss | Recovery-class gating: automatic only for stateless/data-compatible; manual class needs operator confirmation |
| WPF Command Center limits future web needs | Rework at V2 | All logic API-first; UI is a thin consumer — replaceable without platform change |
| Single-maintainer bus factor on architecture intent | Drift across AI implementation sessions | This governance trio + ADRs + sprint-report discipline exist precisely for this |

---

## 12. Version 1.0 Exit Criteria

V1.0 ships when **all** of the following are demonstrably true:

1. CI green: all solutions build, all suites pass, grep gates pass (M3, standing).
2. Clean Ubuntu VPS → registered environment in ≤ 10 minutes via bootstrap; idempotent; zero manual steps (M4).
3. Full platform deploy on such an environment: admitted, provisioned, executed, health-verified, and certified with requirement-level evidence — 3× consecutively, zero manual operations (M5).
4. Deploy→rollback→redeploy cycle certified at every step; complete deployment history; promotion workflow protecting production (M6).
5. Golden path and one failure-recovery path completable entirely from the Operations Command Center (M7).
6. Three-environment fleet with coordinated promotion and proven agent self-update (M8).
7. Security sign-off, complete audit trail, air-gapped install with egress disabled, backup/restore and platform-upgrade drills passed (M9).
8. 72-hour soak with continuous reconciliation and periodic deploys: no unexplained drift, no unapproved repairs, no manual interventions.
9. A non-developer operator completes installation and first deployment from documentation alone.
10. Zero known critical/high debt items; every deferred item registered with owner and target.

Absent any one of these, V1.0 does not ship.
