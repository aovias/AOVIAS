# Aovias.Bootstrap & Aovias.CommandCenter — Engineering State (retro-documentation)

**Written:** Sprint M3.0 (Toolchain Restoration & Canonical Build Baseline), per `ROADMAP_STATUS_REVIEW.md` §4 Drift D1 ("undocumented subsystems... violating the governance workflow") and this sprint's Engineering Rule: *"If undocumented platform modules are found (Bootstrap, CommandCenter), document their current engineering state but do not redesign them."*

This document is descriptive, not prescriptive. It records what exists, what it does, what depends on it, and what evidence tier backs each claim. It changes nothing about either module's design. Both modules were in fact built and reported on within this same engineering program (Sprints M4.1, M4.3, M4.4 for Bootstrap; M2.7, M4.3, M4.4 for CommandCenter) — the process gap `ROADMAP_STATUS_REVIEW.md` flagged is that neither module had ever been retro-connected to a single, discoverable, module-level status document independent of the individual sprint reports scattered across `docs/milestones/`. This file is that missing index, not new engineering work.

---

## 1. Aovias.Bootstrap

**Purpose:** Automated bootstrap of a bare Ubuntu VPS into a registered, capability-declared AOVIAS Runtime Agent deployment target, plus the pre-existing `deploy` verb (SSH-push a package to an already-prepared host).

**Projects** (now fully covered by both `Aovias.Bootstrap/Aovias.Bootstrap.sln`, fixed this sprint, and `Aovias.Platform.slnx`):

| Project | Role |
|---|---|
| `Aovias.Bootstrap.Contracts` | `BootstrapPhaseName`/`BootstrapPhaseOutcome` enums, `BootstrapTarget`/`BootstrapRequest`/`BootstrapPhaseResult`/`BootstrapReport` models, `IBootstrapTargetExecutor`, `IBootstrapOrchestrator`, `IBootstrapStateStore`, `IBootstrapRegistrationClient`. |
| `Aovias.Bootstrap.Core` | `BootstrapOrchestrator` (the 8-phase state machine: Preflight, Docker installation, Docker network creation, runtime directory creation, runtime configuration generation, Runtime Agent installation, Runtime Agent registration, bootstrap verification), `BootstrapStateStore` (file-backed resume state), `RuntimeConfigurationScaffoldBuilder`. |
| `Aovias.Bootstrap.Host` | The executable (`aovias-bootstrap`). Two verbs: `deploy` (pre-existing, unmodified SSH-push-a-package flow) and `bootstrap` (new, M4.1). Real implementations: `SshBootstrapTargetExecutor` (SSH.NET), `HttpBootstrapRegistrationClient`. |
| `Aovias.Bootstrap.Core.Tests` | 9 facts covering full-success ordering, idempotent re-run, resume-after-interruption, Force re-run, fail-fast-at-first-blocker, registration payload correctness, heartbeat verification. |

**Sprints that built it:** M4.1 (bootstrap orchestrator + all 8 phases + tests), M4.3 (added the separate, always-on `EnvironmentRegistrationHostedService` inside the Runtime Agent itself — not part of this project, but the two-step self-register/fleet-register handshake this project's `RunRuntimeAgentInstallationAsync` phase performs once, at bootstrap time, is the same handshake M4.3's hosted service repeats on every agent restart).

**Evidence tier:** Source Implemented (🔵). Every phase, the resume/idempotency logic, and the registration payload shape have regression tests, but — consistent with `ROADMAP_STATUS_REVIEW.md`'s finding — none of it has ever been compiled or run against a real VPS in this program; no .NET SDK has been available in any sandbox session to date. Tag: **Requires SDK Execution** for build, **Blocked (no VPS/SSH target in this sandbox)** for the real-VPS validation the M4.4 report already flagged as the next required step before further feature work.

**Known, disclosed gaps (not fixed here — out of M3.0 scope):**
- The bootstrap bash script predecessor (`provision-deployment-storage.sh`) still hardcodes `/opt/aovias`-style paths and has not been deleted (ROADMAP_STATUS_REVIEW D5) — expected to be retired once `bootstrap` is validated on a real VPS, per the original M4.1 design intent.
- No installer artifact (`.deb`/binary release) exists; the only working path today is running the `Aovias.Bootstrap.Host` executable directly against SSH credentials.

---

## 2. Aovias.CommandCenter

**Purpose:** The Operations Command Center — server-side fleet aggregation and monitoring for every registered Runtime Agent environment. Deliberately never provisions or deploys anything itself (Final Engineering Rule, carried since M2.7: "Command Center never performs provisioning directly" / "Runtime Agent remains autonomous").

**Projects** (covered by `Aovias.CommandCenter/Aovias.CommandCenter.slnx` and `Aovias.Platform.slnx`):

| Project | Role |
|---|---|
| `Aovias.CommandCenter.Api` | Two controllers: `EnvironmentsController` (register/list/get/search/details/health/drift/reconciliation/history/maintenance) and `FleetController` (fleet-wide metrics/events). Thin — delegates everything to Infrastructure services. |
| `Aovias.CommandCenter.Infrastructure` | `FleetRegistry` (`IFleetRegistry` — the in-memory fleet membership store, idempotent-by-`BaseUrl` upsert since M4.3), `RuntimeAgentPollingService` (the one heartbeat-polling `BackgroundService`, extended in M4.3/M4.4 to also sync capabilities/platform version/Deployment Ready state), `FleetStatusService`, `FleetHeartbeatCache`, `FleetEventStore`, `FleetDeploymentClientFactory`. |

**Sprints that built it:** M2.7 (Fleet Registry, heartbeat ingestion, fleet status/search/events/metrics — the original, largest drop), M4.3 (capability/platform-version sync, BaseUrl-based idempotent re-registration), M4.4 (Deployment Ready/admission-blocker-count sync).

**Evidence tier:** Source Implemented (🔵), same disclosure as Bootstrap — regression-tested at the source level (`FleetManagementTests.cs`, `EnvironmentRegistrationTests.cs`, `EnvironmentAdmissionTests.cs`), never compiled or run. `FleetRegistry` is explicitly, deliberately non-durable (in-memory `ConcurrentDictionary`) — documented since M2.7 as an accepted gap, not an oversight: a Command Center restart loses fleet membership, and every registered agent's own client-side persisted record (M4.3) plus BaseUrl-based re-upsert are the two independent defenses against that gap producing duplicate entries.

**Command Center architecture note (not resolved by this sprint, flagged for visibility):** `ROADMAP_STATUS_REVIEW.md` §9.4 observes that this module is a fleet-shaped web API, not the WPF "Command Center" console `ROADMAP.md` §11 originally described as a risk to plan around. This is a genuine open architectural question — whether the Command Center strategy has shifted to web API (+ eventual web UI) or whether `ComplianceServiceManager` (WPF) remains the intended operator console with this API as its backend — and the status review is explicit that **a one-page ADR must settle this before M7**, which is out of M3.0's scope (M3.0 is toolchain/solution hygiene only). Recorded here so the open question has one findable home instead of being re-discovered.

**Known, disclosed gaps (not fixed here — out of M3.0 scope):**
- No environment-state durability beyond the in-memory registry (M2.7 Technical Debt, still open).
- No web UI consumes `Aovias.CommandCenter.Api` today — it currently has no operator-facing frontend of its own.

---

## 3. What this document does not do

It does not add tests, does not change any file in `Aovias.Bootstrap/` or `Aovias.CommandCenter/`, does not resolve the Command Center architecture question, and does not retire any of the disclosed gaps above. Those remain exactly what they were before this sprint — now with one place that says so.
