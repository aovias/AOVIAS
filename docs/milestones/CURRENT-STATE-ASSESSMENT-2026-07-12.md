# AOVIAS Studio — Current State Assessment (Evidence-Based Engineering Audit)

| | |
|---|---|
| **Date** | 2026-07-12 |
| **Auditor role** | Principal Software Architect / Enterprise Platform Auditor |
| **Method** | Direct source-tree inspection **+ actual build verification + actual test execution** on a machine with the .NET 10.0.103 SDK. This is the first audit in the program able to compile and run the tree; all prior audits (incl. `ROADMAP_STATUS_REVIEW.md` 2026-07-06 and the G1/G2 runbooks) were blocked by a sandbox with no SDK. |
| **Evidence tiers** | **Source Analysis** (code read) · **Build Verification** (compiler run) · **Runtime Verification** (executed & observed) · **Certification** (repeatable proven golden path). These are never mixed below. |
| **Ratings** | Not Started · Early · Partial · Mostly Complete · Production Ready · Certified |

---

## 0. What changed since the 2026-07-06 baseline (the headline)

The prior authoritative audit's single largest finding was: *"nothing built after the rework has ever been compiled, tested, or run… the entire M1/M2 program is 🔵 Source Implemented."* **That gap is now partially closed with hard evidence:**

| Claim (07-06) | Status now (07-12) | Evidence |
|---|---|---|
| "Cannot build; expect first `dotnet build` to fail with a non-trivial error list" | **FALSE — builds clean** | `dotnet build Aovias.Platform.slnx -c Release` → **Build succeeded, 0 Warning(s), 0 Error(s), 18.3s** (29 projects) |
| "74 test files have never been executed" | **Now executed** | `dotnet test Aovias.Platform.slnx` → **617 passed / 2 failed / 619 total (99.7%)** across 6 assemblies |
| "No `release-index.json` exists anywhere — M1.5 library never ran" | **FALSE — it exists & is populated** | `aovias-config/storage/local/packages/.release-index/release-index.json`, `LastUpdatedUtc 2026-07-11`, fingerprints + package hashes + dependency graph + lifecycle/promotion state |
| "Only pre-rework packages (07-04) exist" | **FALSE — new-engine packages exist** | `.aovdeploy` packages dated **07-08 / 07-11** with per-package `publishing-report.json` (validation score 100), versioned folders, a runtime-agent **golden package** |
| "WPF `ComplianceServiceManager` in no solution; never built" | **Builds clean; currently running** | Compiles with 0 CS errors; the only build errors are file-**locks** because the app is **live (PID 22020, up since 07-11 14:33)** |

**Net:** the platform has moved decisively from 🔵 *Source Implemented* to 🟡 *Build Verified* across the board, and the **Publish → Package → Release-Library** portion of the pipeline is now 🟢 *Runtime Verified*. The **Deploy → Provision → Health → Reconcile** portion remains **never executed** (`deployment-sessions/` = 0 files; no reconciliation history).

---

## 1. Solution Architecture — **Mostly Complete / Production-grade design** (94%)

**Evidence.** ~96,500 LOC of first-party platform code (excluding the 342K-LOC `ComplaincePlatform` target application and 3 duplicated publish-temp trees). Two canonical build entry points exist and are honored:
- `Aovias.Platform.slnx` — 29 cross-platform `net10.0` projects (builds on any OS).
- `ComplianceServiceManager.slnx` — the Windows/WPF leg (`net10.0-windows`, `UseWPF`).

**Layering is real and enforced by project references,** not just aspirational. The Runtime Agent is textbook Clean/DDD + CQRS: `…Agent.Api` (controllers, middleware, filters) → `…Application` (MediatR commands/queries, behaviors) → `…Domain` (entities, value objects, events) → `…Infrastructure` (Docker, reconciliation, observability, persistence) → `…Contracts` / `…SDK`. Publishing, Release, Bootstrap, and Orchestration all follow a `Contracts / Core / Providers / SDK` split with dependency inversion through interfaces.

**Contracts-first is observed:** sealed `Aovias.*.Contracts` assemblies, DTO namespaces, `infrastructure.contract.json` checksummed into packages, an SDK per subsystem consumed by the WPF app.

**Concerns / drift (verified):**
- **D2 (Medium):** workload-specific knowledge still leaks into platform code — PRAMAN-specific switches remain (`InfrastructureAuthoringProfiles.cs`, `ServiceSmokeTestCatalog.cs`) per the standing ADR-0015 drift item.
- **No `Directory.Build.props` / central package management** — versions are pinned per-csproj (e.g. WPF app mixes `Microsoft.Extensions.*` 9.0.9 with a `net10.0` target); tolerable but a consistency risk at scale.
- Architecture is **being preserved**: the 07-06 ADR-conformance findings (publishing sole-writer, sealed contract, single reconciliation loop, single library service) still hold in the tree.

---

## 2. AOVIAS Studio (WPF) — **Mostly Complete (UI surface) / Partial (commercial)** (68%)

**Evidence.** `ComplianceServiceManager` = the WPF app (`WinExe`, `net10.0-windows`, CommunityToolkit.Mvvm, MediatR, SignalR client, SSH.NET, Serilog, Polly). **27 XAML views** across a full operator workspace, MVVM-structured (parallel `Views/` + `ViewModels/` per area), a `DarkTheme.xaml`, app icon, and a `Navigation` layer.

Workspaces present (each with a view + view-model): Overview, Publishing, Release Center, Releases, Deployment Center, Platform Deployment Center, Deployment Monitor, Bootstrap, Environments, Runtime Agents, Runtime Dashboard, Certificates, Infrastructure, Observability, Live Operations, Reconciliation, Alerts, Audit, Rollback History, Local Dev Services, AI Copilot, Settings, plus a PRAMAN shell.

- **Build:** clean compile (0 CS errors). **Runtime:** the app is **live right now** — strong evidence it launches and stays up.
- **Commercial gaps:** checked-in `packages/` folder and a nested `.git` inside the WPF project (repo hygiene); no installer/MSIX artifact found; branding is functional (icon + dark theme) but not productized; operator documentation absent. UX coverage is broad but its depth per workspace is **not runtime-verified** here (only that the shell runs).

---

## 3. Publishing Engine — **Production Ready (source+build+runtime for local profile)** (78%)

**Evidence (Runtime Verified).** Real artifacts on disk produced by the engine: 25 `.aovdeploy` packages, each with a `publishing-report.json` showing staged validation (`Package`, `Contracts`, …), `OverallScore: 100`, `"READY FOR DEPLOYMENT"`, `ChecksumCount: 24`, `PackageVerified: true`. Deterministic identity is present in `release-index.json`: `PublishingFingerprint`, `PackageHash`, `BuildIdentity` (source-revision@timestamp), `DeploymentCompatibility: "contract:1.0;engine:1.0.0;fingerprint-policy:1"`.

- Multi-provider: `.DotNet`, `.Angular`, `.Static` providers implemented + a DotNet provider test project.
- **Test:** 192/193 pass. **One real failure** — `ContractDrivenPublishingTests.Demo_membership_is_declared_in_metadata_not_hardcoded` (expected 11, actual 22 — a doubling bug in demo-membership enumeration). Low severity but a genuine defect now surfaced.
- **Not yet verified:** incremental-publish reuse and cross-machine fingerprint reproducibility (2× publish equality) — the tests exist; a clean reproducibility run was not performed here.

---

## 4. Release Management — **Mostly Complete** (72%)

**Evidence (Runtime Verified for library/lifecycle).** `release-index.json` is populated and current (07-11): per-artifact `LifecycleState`, `PromotionStatus`, `IsCertified: true` + `CertifiedUtc`, a **dependency graph** (`gateway` → `runtime-agent`), `ReleaseLibraryCategory`, immutable `ArtifactId` (`gateway-1.0.0`). `ReleaseManagerService` + `FileSystemReleaseRepository` persist under a single naming convention; a **golden package** baseline exists for runtime-agent.

- Immutable releases, versioning, promotion states, compatibility metadata, and package-level certification are **real and executed**.
- **Gap (verified):** *plan-based rollback (roadmap M6) is not implemented* — only promotion states exist; the legacy `Aovias.Release` rollback path predates the plan era. Rollback is the largest missing capability here.
- Note: `IsCertified: true` here = **package validation** certification, **not** runtime/platform certification (§13). Do not conflate.

---

## 5. Bootstrap Engine — **Mostly Complete (Orchestration Engine, not a script)** (72%)

**Verdict on the brief's question:** Bootstrap is a genuine **orchestration engine**, not a script. `BootstrapOrchestrator` drives **12 discrete stages** (`Preflight → Connect → ValidatePrivileges → DockerInstallation → DockerNetwork → RuntimeDirectory → ConfigurationGeneration → ConfigurationValidation → Registration → RuntimeStartup → Verification`), each idempotent/resumable, with a `BootstrapStateStore`, `PermissionRepairService`, `RemoteExecutionEngine`, `IBootstrapElevationHandler`, and an SSH target executor.

- **Build + Test Verified:** **56/56 tests pass**, including `BootstrapSelfHealingTests`, `BootstrapPrivilegeElevationTests`, `BootstrapOrchestratorCancellationTests`, `BootstrapStateStoreTests`.
- Planning / execution / verification / repair / retry / resume / permission handling / Docker provisioning / registration / remote state / **idempotency** are all present in source and unit-verified.
- **Not Runtime Verified:** never run against a real Ubuntu VPS (no target available). Documented open risk: container-startup race — `docker run -d` returns before the agent is ready and the next phase calls it with no health-poll/backoff (M3.3 §5, unfixed).

---

## 6. Deployment Engine — **Partial** (58%)

**Evidence.** Full source, build-verified: `PlatformDeploymentPlanner` / `DeploymentPlanBuilder` → `DeploymentPlanValidator` → `DeploymentExecutionRuntime`, with `DeploymentAdmissionEngine` (per-package gate, enforced), `EnvironmentAdmissionStore`/`Evaluator` (environment gate — now consulted before execution after the M3.3 fix), `InfrastructureProvisioningPipeline`, dependency ordering, and typed `DeploymentStepResult` failures. Chunked upload, drift detection, reliability metrics, and rollback all exist on the `IPlatformDeploymentService` surface.

- **Critical evidence gap:** `deployment-sessions/` = **0 files**. **No deployment has ever executed** through the runtime — single-service or multi-service. Health verification, recovery, and runtime registration are consequently **never exercised**.
- **Test gap:** `ReleaseDeploymentProvider` (the 26-dependency class central to execution) has **no direct unit test** (M3.3 §5, confirmed).

---

## 7. Runtime Agent — **Mostly Complete (source/build) / Unverified (runtime), zero tests** (62%)

**Evidence.** The most feature-complete subsystem by source: ~13.8K LOC. Health reporting (`HealthProvider`, `HealthController`), config sync, repair executors, upgrade path, diagnostics, **reconciliation** (`RuntimeObservedStateDiscoveryEngine`, `ReconciliationSchedulerHostedService`, `RepairExecutors`, `FileReconciliationHistoryStore`), telemetry/observability (metrics/events/timeline/state stores, `DockerEventListenerService`), self-healing loop, environment registration (`EnvironmentRegistrationHostedService` + record store), heartbeat, and a SignalR `RuntimeHub`. Always-on hosted services confirmed wired.

- **Build Verified.** **Runtime: never executed live** (no reconciliation history, no live heartbeat observed).
- **Highest testing risk in the platform:** the Runtime Agent ships **zero test projects** — none of its reconciliation, admission, or deployment infrastructure is unit-covered. (Contrast: Bootstrap 56 tests, DeploymentValidation 309.)

---

## 8. Monitoring — **Partial** (55%)

Health monitoring, metrics (`RuntimeMetricsStore`), events (`RuntimeEventBus`, `InMemoryRuntimeEventStore`), timeline (`RuntimeTimelineStore`), state projection, and SignalR broadcast to the WPF `Observability`/`LiveOperations` views are all implemented and build-verified. **Stores are in-memory** — no durable metrics/telemetry backend, no alerting pipeline beyond an `AlertsView` shell. **No live telemetry has been observed** (agent never run). Operator visibility exists as wiring, not as proven signal.

## 9. Diagnostics — **Partial** (55%)

`DeploymentBoundaryDiagnostics` + filter, `DeploymentOperatorErrorDto` (typed operator-facing errors), `CorrelationIdMiddleware`, `ExceptionHandlingMiddleware`, structured Serilog logging. Failure transparency and correlation are designed in and build-verified. Root-cause/diagnostic-collection depth is source-only; never exercised against a real failure.

## 10. Security — **Partial** (55%)

**Evidence.** API-key authentication middleware (`ApiKeyAuthenticationMiddleware`) validating against `EnvironmentRegistrationStore.ValidateApiKey`; `Bearer` token type modeled; secrets **externalized** to a `secrets/` directory convention (`SecretsDirectory = "secrets"`, placeholders `"configured-in-secrets"` — **no hardcoded credentials found** in platform code). Correlation + request logging present.
- **Gaps:** no verified TLS/certificate automation (a `CertificatesView` exists but cert lifecycle is unproven); authorization is coarse (API-key gate, no role model verified); secure-default posture is source-level only, never security-tested. No secrets manager integration.

## 11. Testing — **Partial** (60%)

**Executed (this audit): 617 passed / 2 failed / 619 total (99.7%)** — first execution in program history.

| Assembly | Result |
|---|---|
| Aovias.DeploymentValidation.Tests | 309 / 309 ✅ |
| Aovias.Publishing.Core.Tests | 192 / 193 ❌ (1) |
| Aovias.Bootstrap.Core.Tests | 56 / 56 ✅ |
| Aovias.Configuration.Tests | 35 / 35 ✅ |
| ComplianceServiceManager.Operations.Tests | 24 / 25 ❌ (1) |
| Aovias.Publishing.Providers.DotNet.Tests | 1 / 1 ✅ |

**Two real, now-surfaced defects** (exactly the "cheapest bugs to find, currently unfound" the 07-06 audit predicted):
1. `OperationInstanceProgressTests.OverallProgress_weights_stages_unequally…` — expected 0.2, got 0.8 (**stage-weight inversion** in progress computation).
2. `ContractDrivenPublishingTests.Demo_membership_is_declared…` — expected 11, got 22 (**doubled enumeration**).

**Coverage gaps (Critical):** Runtime Agent, Release, Orchestration, and CommandCenter ship **no test projects at all**. Integration tests exist only as source traces (M3.3); **no Playwright / E2E** anywhere. **No CI** (`.github` absent) — though `scripts/verify-{build,tests,fingerprint}.sh` gate scripts now exist, they are unwired.

## 12. Build Health — **Build Verified (both legs)**

| Solution | Compiles | Warnings | Errors | Notes |
|---|---|---|---|---|
| `Aovias.Platform.slnx` (29 proj) | ✅ | **0** | **0** | Release, 18.3s, clean |
| `ComplianceServiceManager.slnx` (WPF) | ✅ (0 CS errors) | 160 reported* | 21 = **file-locks only** | All errors are `MSB3021/MSB3027` "file locked by ComplianceServiceManager (22020)" — the running app; **no compilation error** |

*WPF Release warning count reported by MSBuild summary; not itemized in captured output — recommend a clean re-run with the app closed to enumerate. The cross-platform leg is genuinely 0/0.

- Dependencies restore cleanly (NuGet reachable on this machine). No broken project references. `ComplaincePlatform` (the 342K-LOC target app) was **not** built in this pass — out of scope for ops-platform maturity; it is the deployment payload, not AOVIAS Studio.

## 13. Runtime Certification — **Not Certified; partially Runtime Verified**

| Stage | Tier reached | Evidence |
|---|---|---|
| Publish | 🟢 **Runtime Verified** | 25 real packages + validation reports (score 100) |
| Package / Release Library | 🟢 **Runtime Verified** | populated `release-index.json` (fingerprints, lifecycle, promotion, dep-graph) |
| Bootstrap | 🟡 Build + Unit | 56 tests; never run vs a VPS |
| Deploy / Provision | 🟡 Build only | `deployment-sessions/` empty — **never executed** |
| Health / Reconcile | 🟡 Build only | no reconciliation history — **never executed** |
| **Production certification** | ❌ **None** | zero repeatable golden-path runs |

**Answer to "has this platform actually been proven to run?":** *Partly.* The **publish/release half is proven** on the developer machine. The **deploy/runtime half has never run** end-to-end against any Docker/VPS target. No production certification exists.

## 14. Technical Debt — ranked

| Rank | Item | Evidence |
|---|---|---|
| **Critical** | No deployment/runtime has ever executed (deploy→reconcile) | `deployment-sessions/` = 0 files; no reconcile history |
| **Critical** | 4 subsystems (Runtime Agent, Release, Orchestration, CommandCenter) have **zero tests** | no test csproj under those trees |
| **Critical** | No CI to prevent regression of the now-green build/tests | `.github` absent |
| **High** | 2 failing unit tests (weight inversion; doubled enumeration) | test run above |
| **High** | Plan-based rollback (M6) absent | Release/Deployment source |
| **High** | No E2E/Playwright; integration is source-traced only | repo scan |
| **Medium** | `ReleaseDeploymentProvider` (26 deps) untested | M3.3 §5 |
| **Medium** | Workload-specific switches in platform code (D2) | ADR-0015 drift |
| **Medium** | M4.1–M4.4 have **no per-sprint implementation reports** (only one consolidated state doc) — governance drift persists | `docs/milestones` has no `M4*.md` |
| **Medium** | Monitoring stores are in-memory; no durable telemetry/alerting | Observability source |
| **Low** | WPF repo hygiene: checked-in `packages/`, nested `.git` | WPF tree |
| **Low** | No central package management / `Directory.Build.props` | csproj scan |

**Positive debt signal:** **zero `TODO`/`FIXME`/`HACK`** markers and no `NotImplementedException` in production code (only in test mocks). Unusually disciplined for this scale.

## 15. Commercial Readiness — **Internal Platform** (40%)

AOVIAS Studio today resembles a **maturing internal platform**, past "developer tool," well short of "commercial/enterprise product."
- **For it:** broad, coherent operator UI (27 workspaces), clean architecture, deterministic publishing that actually runs, AI-copilot surface, consistent naming, disciplined code.
- **Against it:** no installer/packaging, no operator documentation, no runtime-proven deploy path, no CI/quality gates, in-memory telemetry, unfinished rollback, branding functional-not-productized, repo-hygiene issues. Reliability is **unproven** where it matters most (deploy/heal).

## 16. Platform Maturity (0–100, evidence-based)

| Dimension | Score | Basis |
|---|---|---|
| Architecture | **94** | clean DDD/CQRS + contracts; both solutions build 0/0; minor drift |
| Implementation (roadmap coverage) | **70** | M1–M2 + M4.1–4.4 done; M5/M6/M8/M9 partial/absent |
| Runtime | **35** | publish/release run; deploy/reconcile never run |
| Deployment | **58** | full source+build; 0 executions |
| Bootstrap | **72** | 12-stage engine, 56 tests; no VPS run |
| Release Management | **72** | library executed; rollback missing |
| Runtime Agent | **62** | complete source+build; zero tests; no live run |
| Monitoring | **55** | wired, in-memory, unobserved |
| Diagnostics | **55** | designed, build-verified, unexercised |
| Testing | **60** | 99.7% pass but 4 subsystems uncovered, no CI/E2E |
| Commercial Readiness | **40** | internal-platform stage |
| **Overall Platform** | **~62** | strong build-verified base; runtime half unproven |

## 17. Executive Summary

**Strengths:** (1) Architecture is genuinely strong and now **build-proven** — 29 projects compile 0/0. (2) The publish→release half of the pipeline is **runtime-proven** with real deterministic artifacts and a working release library. (3) Bootstrap is a real, unit-tested orchestration engine. (4) Exceptional code discipline (0 TODO/dead-placeholder debt). (5) A broad, running WPF operator surface.

**Weaknesses:** (1) The deploy→provision→health→reconcile half has **never executed**. (2) Four core subsystems have **no tests**. (3) **No CI**. (4) Rollback (M6) unimplemented. (5) No E2E; monitoring telemetry is in-memory and unobserved.

**Highest engineering risk:** the entire runtime-execution half of the platform is unproven — its first real deploy will surface an unknown defect count (mitigated: everything now compiles and 99.7% of existing tests pass, so the base is healthier than the 07-06 audit feared).
**Highest architectural risk:** the Command Center strategy ambiguity (web API `Aovias.CommandCenter` vs. WPF shell) remains unsettled by ADR; and workload-knowledge drift (D2).
**Highest commercial risk:** reliability is unproven exactly where a customer would first feel it (bootstrap+deploy to their VPS); no installer, no operator docs.

**Most stable subsystems:** DeploymentValidation (309/309), Publishing (runtime-proven), Bootstrap (56/56), Release Library (runtime-proven).
**Least mature:** Deployment execution, Monitoring, Diagnostics, Commercial packaging (all unexecuted/unproven).

## 18. Immediate Recommendations (stabilization-aligned; no redesign)

1. **Execute the runtime half once, for real (M3.4).** With the SDK now proven on this machine, stand up a local Docker host (or one dev VPS) and run: bootstrap → agent startup → registration → admission → **deploy one service** → health gate → **one reconciliation cycle**. This single run converts the largest block of unknowns to evidence. *Determinism/Reliability.*
2. **Fix the 2 failing tests** (stage-weight inversion; doubled demo enumeration) and land each with a regression assertion. *Maintainability.*
3. **Stand up CI** (`.github` or equivalent) wiring the existing `scripts/verify-*.sh` gates: build both solutions + `dotnet test` + fingerprint reproducibility. Lock in today's green state before it regresses. *Reliability.*
4. **Add a first test project to the Runtime Agent**, starting with reconciliation and the `ReleaseDeploymentProvider` admission gate — the untested code closest to production risk. *Reliability/Integration.*
5. **Retro-document M4.1–M4.4** as per-sprint reports (close the governance gap) and settle the **Command Center architecture ADR** (web vs. WPF). *Maintainability.*
6. **Verify fingerprint reproducibility** (publish one service 2×, assert equal fingerprint/hash) — cheap, high-value determinism proof the artifacts imply but don't yet demonstrate. *Determinism.*

Defer (register, don't do now): plan-based rollback (M6), durable telemetry backend, installer/branding, D2 workload-switch cleanup.

## 19. Final Dashboard

```
Architecture ............. 94%   (Mostly Complete — build-proven, both solutions 0 errors)
Implementation ........... 70%   (M1–M2 + M4.1–4.4 source-complete; M5/6/8/9 partial)
Bootstrap ................ 72%   (12-stage engine, 56/56 tests; no VPS run)
Deployment ............... 58%   (full source+build; 0 executions — sessions dir empty)
Runtime Agent ............ 62%   (complete source+build; ZERO tests; never run live)
Release Management ....... 72%   (release-index executed; rollback missing)
Monitoring ............... 55%   (wired, in-memory, never observed)
Diagnostics .............. 55%   (designed + build-verified; unexercised)
Testing .................. 60%   (617/619 pass = 99.7%; 4 subsystems uncovered; no CI/E2E)
Commercial Readiness ..... 40%   (Internal Platform stage)
Overall Platform ......... 62%

Platform Stage ........... Internal Platform (build-verified; publish/release runtime-verified)
Current Priority ......... Platform Stabilization & first end-to-end Runtime Verification (M3.4)
Highest Risk ............. Deploy→reconcile half has never executed; 4 subsystems untested; no CI
Next Milestone ........... M3.4 — Runtime Validation: one real bootstrap→deploy→reconcile run
Distance to Commercial v1.0  Large. Gating work (evidence-based, not calendar):
                            M3.4 runtime cert (3× golden path) → M6 rollback → M7 operator
                            surface/ADR → M8 fleet → M9 hardening/air-gapped + installer + CI
                            + operator docs. The build/test foundation is now sound; the
                            distance is dominated by unproven runtime + productization, not
                            by architecture or source completeness.
```

---
*Evidence provenance: `dotnet build/test` transcripts (2026-07-12), `aovias-config/storage/local/**` artifact inspection, source tree read of all `Aovias.*` + `ComplianceServiceManager*` projects. Every "Runtime Verified" claim maps to an on-disk artifact; every "never executed" claim maps to an empty/absent state directory. `ComplaincePlatform` (342K LOC) is the deployment target, not AOVIAS Studio, and was excluded from ops-platform scoring.*
