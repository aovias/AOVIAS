# AOVIAS — Roadmap Status Review
### Authoritative engineering status. Supersedes maturity assumptions in all prior implementation reports.

| | |
|---|---|
| **Type** | Engineering program audit — no files modified, no code written |
| **Date** | 2026-07-06 |
| **Method** | Direct source-tree inspection, artifact timestamp analysis, sprint-report cross-examination, toolchain probe. Every claim below is backed by repository evidence or explicitly marked as unverifiable. |
| **Maturity model** | ✅ Certified · 🟢 Runtime Verified · 🟡 Build Verified · 🔵 Source Implemented · ⚪ Planned |
| **Confidence** | **High** = execution or strong artifact evidence · **Medium** = source verified, never executed · **Low** = inferred, limited verification |

---

## 1. Executive Summary

**The platform is architecturally strong, source-rich, and execution-poor.** The audit verified that the M1.1–M1.5 and M2.0–M2.6 sprint reports are *honest about source changes*: every claimed artifact spot-checked exists (Release Library service + models, specification language + data file, schema v2 migration, requirement/capability model, admission engine, deployment plans, execution runtime, full reconciliation namespace), and every claimed deletion happened (`ExecuteLegacyAsync` gone, `ReleaseLibraryOrganizer` gone, hardcoded dependency switch gone, `DemoBundleServiceIds` gone, agent no longer rebuilds contracts, `infrastructure.contract.json` now checksummed in `ContractFiles`).

**But nothing built after the rework has ever been compiled, tested, or run.** No sprint report from M1.1 onward contains recorded build output; M2.x reports explicitly disclose "no .NET SDK — no test has been executed." This audit's own toolchain probe confirms the environment cannot build (`dotnet` absent; installer blocked by proxy 403; apt offers only SDK 8 against `net10.0` targets). There is no CI (`.github` absent). The runtime evidence is damning by its dates: the ten `.aovdeploy` packages in `aovias-config/storage/local/packages/` were produced **2026-07-04** — by the *pre-M1 pipeline* (engine 1.0.0, before determinism rework) — while all M1/M2 source landed 07-05/07-06. There is **no `release-index.json`** (the M1.5 library has never executed), the deployment-sessions state directory is **empty** (no deployment has ever run through the new execution runtime), and no reconciliation history exists.

**Honest maturity: the entire M1/M2 program is 🔵 Source Implemented.** Approximately 74 test files exist, including suites for admission, planning, execution, migration, specification, and reconciliation — a genuinely good safety net that has never been fired.

**Biggest risks:** (1) an unknown number of latent compile/test failures across ~2 milestones of unbuilt code — the cheapest possible bugs to find, currently unfound; (2) undocumented work-in-progress discovered in the tree (`Aovias.Bootstrap/`, `Aovias.CommandCenter/` — M4/M7-scope code with tests but **no implementation report**, violating the governance workflow); (3) solution-coverage gaps (`ComplianceServiceManager` is in **no** solution file; `Aovias.Runtime.Agent.Infrastructure/Domain/SDK/Contracts` are in `Aovias.Platform.slnx` only transitively via `Agent.Api`); (4) residual ADR drift (PRAMAN-specific key and service-id switches in `InfrastructureAuthoringProfiles.cs`, `ServiceSmokeTestCatalog.cs`).

**Immediate recommendation (§12): B — enter the integration phase (execute roadmap M3) now.** No feature work should land until the platform compiles, its 74 test files run, and one real publish→deploy executes.

---

## 2. Engineering Scorecard

Legend: A = Architecture complete · S = Source implemented · B = Build verified · R = Runtime verified · C = Certified. Conf = confidence in this row's classification.

| Milestone | A | S | B | R | C | Status | Conf |
|---|---|---|---|---|---|---|---|
| M1.1 Publishing debt cleanup | ✅ | ✅ (deletions verified in tree) | ❌ no recorded build output | ❌ | ❌ | 🔵 | High |
| M1.2 Deterministic publishing engine | ✅ | ✅ (`FingerprintPolicy`, obsoleted random revision verified) | ❌ | ❌ (existing packages predate it) | ❌ | 🔵 | High |
| M1.3 Contract-driven publishing | ✅ | ✅ (hardcoded switches gone) | ❌ | ❌ | ❌ | 🔵 | High |
| M1.4 Environment-neutral packages | ✅ | ✅ | ❌ (report discloses no SDK) | ❌ | ❌ | 🔵 | High |
| M1.5 Release Library & lifecycle | ✅ | ✅ (8 new files verified; both old copy paths deleted; CSM delegates) | ❌ (disclosed) | ❌ (**no `release-index.json` exists anywhere**) | ❌ | 🔵 | High |
| M2.0 Contract authority | ✅ | ✅ (agent/DRV rebuild calls gone; file in `ContractFiles`) | ❌ | ❌ | ❌ | 🔵 | High |
| M2.1 Specification language | ✅ | ✅ (`Specifications/` + `infrastructure-specifications.json`; registry loads from it) | ❌ (report cites pre-existing build-health blockers) | ❌ | ❌ | 🔵 | High |
| M2.2 Schema v2 & migration | ✅ (+ living policy doc) | ✅ (`Migration/` complete, v1→v2 migrator, equality tests present) | ❌ | ❌ | ❌ | 🔵 | High |
| M2.3 Requirement/Capability | ✅ | ✅ (`Capabilities/` complete incl. registry + matcher + version constraints) | ❌ | ❌ | ❌ | 🔵 | High |
| M2.4 Planning & Admission | ✅ | ✅ (`Planning/` + `Admission/` complete, plan integrity/validator) | ❌ (disclosed) | ❌ | ❌ | 🔵 | High |
| M2.5 Provisioning & Execution runtime | ✅ | ✅ (`Execution/` complete: runtime, gates, task provisioners) | ❌ (disclosed: "No test has been executed") | ❌ (deployment-sessions dir empty) | ❌ | 🔵 | High |
| M2.6 Reconciliation & Self-healing | ✅ | ✅ (full `Reconciliation/` namespace + agent discovery engine + scheduler; old worker removed, refs are comments only) | ❌ (disclosed) | ❌ (no cycle history exists) | ❌ | 🔵 | High |
| M3 Verified baseline | ✅ (spec in ROADMAP) | ❌ | ❌ | ❌ | ❌ | ⚪ | High |
| M4 Bootstrap & registration | ✅ | 🔶 **partial, undocumented** (`Aovias.Bootstrap/` — contracts, orchestrator, state store, scaffold builder, tests; no report) | ❌ | ❌ | ❌ | 🔵 partial | Medium |
| M5 Certified deployment E2E | ✅ | 🔶 partial (M2.5 ships health-gate + certification task provisioners; contract-fed certification & E2E absent) | ❌ | ❌ | ❌ | 🔵 partial | Medium |
| M6 Rollback, history, promotion | ✅ | ❌ (promotion *states* exist from M1.5; plan-based rollback absent; legacy `Aovias.Release` rollback untouched) | ❌ | ❌ | ❌ | ⚪ | High |
| M7 Operations Command Center | ✅ | 🔶 **partial, undocumented** (`Aovias.CommandCenter.Api` with Environments/Fleet controllers + Infrastructure — no report) | ❌ | ❌ | ❌ | 🔵 partial | Medium |
| M8 Fleet & multi-environment | ✅ | 🔶 traces only (FleetController, FleetEventStore, FleetDeploymentClientFactory) | ❌ | ❌ | ❌ | ⚪/🔵 | Medium |
| M9 Hardening & air-gapped | ✅ | ❌ | ❌ | ❌ | ❌ | ⚪ | High |

**Nothing in the repository qualifies for 🟡, 🟢, or ✅.** The only runtime-verified behavior on record is the *pre-rework* publishing pipeline (packages of 2026-07-04) — a codebase that no longer exists.

---

## 3. Sprint Audit

| Sprint | Verdict | Why |
|---|---|---|
| M1.1–M1.3 | **Partial** (was: Completed) | Source complete and verified in-tree; reports contain *no build/test output at all*, so acceptance evidence is missing. Downgraded from their implicit "done" to source-complete. |
| M1.4, M1.5 | **Partial** | Honest disclosure: traced, not built. Source verified by this audit (files exist, deletions done). Open until built + tested + first library registration executes. |
| M2.0–M2.3 | **Partial** | Same pattern; source fully present and coherent (incl. tests). M2.1 additionally cites "pre-existing platform build health items" — unresolved and unenumerated anywhere. |
| M2.4–M2.6 | **Partial** | Same; explicitly "No test has been executed." M2.6's consolidation claim verified (worker deleted, endpoint delegation in place) at source level. |
| M3.x | **Not started** — and **blocked in this sandbox** (no SDK, install blocked). Requires an environment with .NET 10 SDK. |
| M4.x / M7.x (partial code) | **Undocumented / out-of-process** | `Aovias.Bootstrap` and `Aovias.CommandCenter` exist with no implementation reports, no roadmap sprint claimed, and inclusion in `Aovias.Platform.slnx`. Violates guidelines §2 (Report) and §8 (DoD). Must be retro-documented or treated as unreviewed WIP. |
| M5.x, M6.x, M8.x, M9.x | **Not started** (M5/M8 have incidental partial source via M2.5 / CommandCenter). |

No sprint is **Superseded**; none needs to be. No sprint is formally **Deferred**.

---

## 4. Architecture Audit (drift against ROADMAP / ADRs / Guidelines)

**Conformant (verified):** ADR-0002/0003 (publishing sole writer; sealed contract consumed — rebuild calls gone from agent and DRV; contract checksummed); ADR-0005 core (registry loads specifications data; hardcoded `GetDependencies`/`DemoBundleServiceIds` deleted); ADR-0011 (single library service; both legacy copy paths deleted; CSM delegates via SDK); ADR-0012 (`FingerprintPolicy`, random build revision obsoleted); ADR-0014 (one reconciliation loop; old worker removed; endpoint delegates).

**Drift found (ranked):**

| # | Drift | Evidence | Severity |
|---|---|---|---|
| D1 | **Undocumented subsystems** — Bootstrap + CommandCenter code with no reports, violating the governance workflow the day after it was adopted | `Aovias.Bootstrap/**`, `Aovias.CommandCenter/**` in tree & `Aovias.Platform.slnx`; no `M*.md` report mentions them | High (process) |
| D2 | **Workload knowledge in platform code** (ADR-0015): `"portal" => … PRAMAN_API_BASE_URL` switch in `InfrastructureAuthoringProfiles.cs:92`; per-service switch in `ServiceSmokeTestCatalog.cs:16-33` | grep hits verified | Medium |
| D3 | **Solution coverage gaps**: `ComplianceServiceManager.csproj` in no `.slnx`; Agent Infrastructure/Domain/SDK/Contracts projects not directly in `Aovias.Platform.slnx` (build only transitively via `Agent.Api`); old per-area solutions deleted without a recorded decision | slnx content inspection | Medium |
| D4 | `LegacyContractCompatibilityBuilder` still present and referenced by `DeploymentPackageContractsResolver` — ADR/T4 deletion target pending | grep verified | Low (scheduled) |
| D5 | Bootstrap bash script still hardcodes `/opt/aovias` paths | `provision-deployment-storage.sh` | Low (expected until M4 completes) |
| D6 | Cosmetic service-name switch in `SolutionPublishingLogWriter.cs:69-75` (display names) | grep verified | Cosmetic |
| D7 | Grep gates, CI enforcement mandated by ROADMAP §9 / Guidelines §6 — **do not exist** (no `.github`, no gate scripts found) | toolchain probe | High |

---

## 5. Technical Debt Audit

**Must fix before continuing (blocks everything downstream):**
1. **Verification debt** — two milestones of unbuilt, untested code (C1). Risk: unknown compile/test failure count. This is the single largest engineering risk in the program.
2. **No CI / no gates** (D7) — the guidelines are unenforceable until this exists.
3. **Undocumented WIP** (D1) — Bootstrap/CommandCenter must be retro-audited, reported, or quarantined before M4/M7 formally start; otherwise the reports-as-truth model collapses.
4. **Solution coverage** (D3) — define the canonical build entry points; a "verified baseline" is meaningless if CSM and agent internals aren't in it.

**Can be deferred (register with owner + target):**
5. D2 workload-knowledge switches → fold into specification data (natural home: M3.4 or a small M2 follow-up).
6. D4 legacy compatibility builder deletion → after first build-verified publish round-trip.
7. D5 bootstrap script → M4.1 replaces it by design.
8. M2.1's unenumerated "pre-existing build health items" → will surface automatically in M3.1; capture them then.

**Cosmetic:** D6 log display names; `ServiceContractRegistry.cs` encoding anomaly (still detected as binary by grep).

---

## 6. Build Readiness

**Verified evidence:** this environment **cannot** build: no `dotnet` on PATH; `dot.net` installer blocked (proxy 403); apt offers SDK 6/8 only against `net10.0` targets. No CI definition exists in the repo. No sprint report contains build output.

**Source assumptions (unverified):** the tree is *plausibly* buildable — namespaces/DI registrations spot-traced consistently across ~15 files this audit read, and the sprint reports performed line-by-line signature tracing. But M2.1 explicitly references pre-existing build-health blockers, and two milestones of cross-assembly changes (29-project platform solution) landed without a compiler pass. **Expectation: the first `dotnet build Aovias.Platform.slnx` will fail with a non-trivial but tractable error list.** Confidence in "builds clean on first try": **Low**.

**Blockers to build verification:** a machine/agent with .NET 10 SDK + NuGet access (or offline feed); decision on canonical solution entry points (D3); CSM (WPF, `net10.0-windows`) needs a Windows build leg.

---

## 7. Runtime Readiness

Target chain, conservatively marked:

| Stage | Mark | Evidence & reasoning |
|---|---|---|
| Publish (new engine) | **Unknown** | Never executed; only pre-rework packages exist (2026-07-04). Tests exist. |
| Bootstrap | **Blocked** | Source skeleton only (undocumented); no installer artifact; bash script is the only working path and it's manual. |
| Registration | **Unknown** | `EnvironmentRegistrationStore` + capability profiles exist in source; no registered-environment state found. |
| Admission | **Unknown** | Engine + tests exist (`EnvironmentAdmissionTests`); never run, not even as a test suite. |
| Deployment (plan-based execution) | **Unknown** | Execution runtime + gates in source; deployment-sessions state directory **empty**. |
| Health verification | **Unknown** | Health-gate provisioner in source; never exercised against a live container by the new runtime. |
| Rollback | **Blocked** | Plan-based rollback not implemented (M6); legacy path exists but is pre-plan-era and slated for delegation/deletion — running it would create evidence for the wrong architecture. |
| Reconciliation | **Unknown** | Full loop in source with tests; no cycle history anywhere. |

**Bottom line:** zero stages are Verified. The realistic path to first runtime evidence is: build → test suites → publish one service with the new engine → deploy to a dev VPS via the new runtime → observe one reconciliation cycle. Nothing in the source suggests this is far away — but nothing proves it is near, either. Confidence: **Medium** that the chain runs after a normal integration-debugging effort; **Low** that it runs unmodified.

---

## 8. Certification Readiness

| Gate | Ready? | Why |
|---|---|---|
| **Engineering Integration** | **Yes — and overdue** | Exactly what the platform needs now; all inputs exist (source, tests, specs, governance docs). |
| **Platform Certification** | No | Requires runtime-verified golden path 3×; zero runtime evidence exists. |
| **Production Pilot** | No | Requires certification + rollback (M6) + operator surface (M7) + security pass (M9.1). |
| **General Availability** | No | Requires V1.0 exit criteria (ROADMAP §12): fleet, air-gapped, audit, soak — all ⚪. |

---

## 9. Roadmap Corrections

The ROADMAP's shape survives this audit largely intact — M3-first was the right call and is now *proven* right. Corrections:

1. **M3 must expand** to absorb what this audit found: add **M3.0 Toolchain & Canonical Solutions** (SDK-equipped build environment, decide/record solution entry points incl. CSM and agent-internals coverage — resolves D3) and extend **M3.4** to include retro-adjudication of the undocumented Bootstrap/CommandCenter code (D1) and the D2 workload-knowledge cleanup.
2. **M3.3 (drift consolidation) is smaller than planned** — M2.6 already deleted the old worker and delegated the endpoint; only the `DetectDriftAsync` overlap remains. Merge M3.3 into M3.4. Net: M3 stays 4 sprints (M3.0, M3.1, M3.2, M3.4′).
3. **M4 is not greenfield** — the roadmap should acknowledge `Aovias.Bootstrap` as unreviewed partial input to M4.1/M4.2, not a blank start. Its quality is unknown; M4.1 begins with an audit of it (per guidelines §2 Analyze).
4. **M7/M8 boundary**: `Aovias.CommandCenter.Api` (Environments/Fleet controllers) is fleet-shaped, not WPF-CSM-shaped. Either the Command Center strategy has silently shifted to a web API + (eventual) web UI — which contradicts ROADMAP §11's "WPF Command Center" risk entry — or this is speculative code. **A one-page ADR must settle the Command Center architecture before M7.** This is the only *architectural* (not process) decision this audit escalates.
5. **No milestones are missing, none should merge/split beyond the above, none are complete, none are obsolete.** M5/M6/M8/M9 ordering stands. Post-1.0 (M10/M11) untouched.

---

## 10. Integration & Certification Plan (Stabilization Phase = corrected M3)

**Feature freeze effective immediately**: no new capabilities, no new subsystems, no new namespaces until exit.

| Step | Content | Exit gate |
|---|---|---|
| 1. Build Restoration (M3.0+M3.1) | SDK-equipped environment; canonical solutions decided (incl. CSM, agent internals); fix all compile errors — fix-only, no redesign; capture M2.1's latent build-health items | `dotnet build` green on canonical solutions, Windows leg incl. CSM |
| 2. Test Restoration (M3.2) | Run all ~74 test files; fix or adjudicate every failure; stand up CI with build+test+grep gates | CI green; gates active |
| 3. Integration Validation | First real execution of the new stack, in order: publish 1 service → verify fingerprint reproducibility (2× publish) → library registration creates `release-index.json` → publish all 10 → plan+admission against a dev environment → deploy via execution runtime → health gate → one reconciliation cycle | Each stage's first successful run recorded with output in an M3 report |
| 4. Runtime Validation | Full-platform deploy on a fresh dev VPS (manual bootstrap allowed — M4 not required); 3× repeatability; induced-failure sampling (one per failure class) | 3 consecutive clean deploys; failures produce typed recovery guidance |
| 5. Certification Prep | Tag `v0.9-baseline`; retro-adjudicate Bootstrap/CommandCenter (keep-with-report / quarantine / delete); Command Center ADR (§9.4); debt register emptied or dated | Baseline tag; ADR merged; empty undated-debt list |
| 6. Bug fixing | Continuous throughout; every fix lands with a permanent regression test (guidelines §5) | — |

**Phase exit = feature development resumes (M4).** Expected effort: dominated by steps 1–2 (unknown error count) and step 3 (first-run integration debugging).

---

## 11. Updated Roadmap

| Item | Status | Depends on |
|---|---|---|
| M1.1–M1.5 Deterministic Publishing | 🔵 **Completed (source) / unverified** — verification transfers to M3 | — |
| M2.0–M2.6 Contract Architecture → Reconciliation | 🔵 **Completed (source) / unverified** — verification transfers to M3 | M1 |
| **M3 Stabilization** (M3.0 Toolchain & Solutions · M3.1 Build · M3.2 Tests+CI · M3.4′ Debt/Drift/Retro-adjudication + integration/runtime validation per §10) | 🔴 **In Progress — BLOCKED in current sandbox** (needs .NET 10 build environment) | M1, M2 source |
| M4 Bootstrap & Registration (absorbing existing `Aovias.Bootstrap` after audit) | ⚪ Future | M3 |
| M5 Certified Deployment E2E | ⚪ Future (partial source via M2.5) | M4 |
| M6 Rollback, History & Promotion | ⚪ Future | M5 |
| M7 Operations Command Center (**pending Command Center ADR**, absorbing `Aovias.CommandCenter` after audit) | ⚪ Future | M5 (+M6 for its consoles) |
| M8 Fleet & Multi-Environment | ⚪ Future | M6, M7 |
| M9 Hardening & Air-Gapped → **V1.0** | ⚪ Future | M8 (M9.1 may start with M7) |
| M10 Kubernetes/Cloud · M11 AI Ops | ⚪ Post-1.0 | V1.0 |

---

## 12. Immediate Recommendation

### **B) Enter the integration phase.**

Not (A): two milestones of unexecuted code is the maximum safe inventory of unverified work; adding features multiplies unknowns against an unbuilt base, and the guidelines (§2.4, §6) already prohibit it post-M3 — M3 simply hasn't been honored yet. Not (C): certification requires runtime evidence that does not exist; entering certification now would certify nothing. Not (D): the architecture audit (§4) found the implemented code *conformant* with the ADRs on every load-bearing decision — the architecture is not the problem; the absence of execution evidence is.

Supporting evidence in one line each: no build toolchain has ever compiled the post-rework tree (§6); the only runtime artifacts predate the rework (packages dated 07-04 vs source dated 07-05/06); the M1.5 library index and M2.5 deployment-session store are empty; 74 test files have never been executed; there is no CI to stop any of this from getting worse.

**First action:** provision a build environment with the .NET 10 SDK and run `dotnet build Aovias.Platform.slnx` — every subsequent decision in this program is cheaper after that one command.
