# Operation Engine & Autonomous Bootstrap — Implementation Plan

**Status:** Planned (no implementation started).
**Scope:** AOVIAS Studio (ComplianceServiceManager) + additive seams in `Aovias.Bootstrap`.
**Date:** 2026-07-08.
**Prerequisite reading:** `BOOTSTRAP-AND-COMMANDCENTER-ENGINEERING-STATE.md`, ADR-0011/D10 (`ARCHITECTURE_DECISIONS.md`), Sprint M4.1 bootstrap engine.

---

## 1. Mission

Transform Bootstrap into an autonomous provisioning system. The operator never executes
commands manually: AOVIAS Studio becomes the orchestration layer, executing the Bootstrap
Engine **in-process**; the CLI becomes a thin wrapper over the same engine. There must be
exactly **one implementation of provisioning logic**.

Rather than making Bootstrap a special case, this plan introduces a generic **Operation
Engine** so every long-running operation in Studio (Bootstrap now; Publish, Deploy,
Rollback, Repair, Upgrade, Reconcile later) shares one enterprise-grade execution
framework: overall progress, stage progress, live logs, cancellation, retry, resume,
notifications, operation history, and telemetry.

```
AOVIAS Studio (WPF)
   └── Operation Engine  ← new, generic
         └── BootstrapOperationExecutor  ← thin adapter
               └── BootstrapRunner  ← extracted composition root (ONE provisioning entry point)
                     └── Aovias.Bootstrap.Core (orchestrator, unchanged logic)
                           └── SshBootstrapTargetExecutor (moved out of Host)

aovias-bootstrap CLI
   └── BootstrapRunner (same one) + ConsoleObserver  ← Host becomes a thin wrapper
```

---

## 2. Current state (verified against source, this session)

### Backend (`Aovias.Bootstrap`)
- `IBootstrapOrchestrator.BootstrapAsync(BootstrapRequest, CancellationToken)` →
  `Task<BootstrapReport>` — one opaque call, 8 phases (`BootstrapPhaseName` 1–8), stops at
  first failure, reports per-phase outcomes only after the entire run. **No progress seam**:
  no `IProgress<T>`, no events, `RunPhaseAsync` is private.
- `BootstrapStateStore` writes `bootstrap-state/{identity}.json` immediately after each
  phase succeeds — the existing resume/idempotency mechanism. Failed phases are never
  recorded.
- `SshBootstrapTargetExecutor` (SSH.NET) and `HttpBootstrapRegistrationClient` live in
  **`Aovias.Bootstrap.Host`** (the CLI exe project). Core depends only on
  `IBootstrapTargetExecutor`; Core's test suite runs on `FakeBootstrapTargetExecutor`
  without SSH.
- `Program.cs` composes the per-run object graph (Core DI + real executor + real
  registration client). That composition exists nowhere else.

### Studio side (shipped 2026-07-08)
- `BootstrapViewModel`: 4-step wizard (Target Host / Environment / Review / Execute), SSH
  fact discovery via `TargetHostProbeService` (CSM-owned SSH.NET), execution via
  `BootstrapExecutionService`, which **launches the CLI as a child process**
  (`dotnet run --project …`) and **polls the state-store JSON** for coarse progress,
  regex-parsing the CLI's final report for failure messages.

### Weaknesses motivating this plan
1. `dotnet run` recompiles the Host project on every provisioning run — slow, fragile.
2. Password auth passes the secret as a **process argument** (visible in process lists).
   In-proc execution eliminates this entirely.
3. Progress is coarse (completed-phases only); "currently running" is inferred; failure
   detail is regex-parsed from stdout.
4. CSM carries provisioning-adjacent duplication (probe service + CLI arg builder).

---

## 3. Phase A — Backend seams (`Aovias.Bootstrap`, additive only)

### A1. New project: `Aovias.Bootstrap.Execution`
Move (not copy) from `Aovias.Bootstrap.Host`:
- `SshBootstrapTargetExecutor` (+ SFTP upload path)
- `HttpBootstrapRegistrationClient`
- **New:** `BootstrapRunner` — extracted verbatim from `Program.cs`'s `RunBootstrapAsync`
  composition (Core DI + executor + registration client + working directory). The one
  public provisioning entry point: takes target, options (force, working directory), an
  observer, a cancellation token.

Host references Execution. CSM references Execution + Contracts (Contracts already
referenced). SSH.NET moves to Execution's csproj.

*Why not into Core:* Core is deliberately I/O-free — its entire test suite runs against
`FakeBootstrapTargetExecutor`. SSH.NET in Core breaks that boundary for no benefit.

### A2. Progress seam: `IBootstrapExecutionObserver` (Contracts)
A callback interface (not `IProgress<T>` — several distinct signal kinds):
- **Phase lifecycle:** phase starting / phase finished (existing `BootstrapPhaseOutcome` + message).
- **Command lifecycle:** remote command starting / finished (command text, exit code, output) — feeds live logs.
- **Upload progress:** file, bytes sent, total bytes — SSH.NET's `SftpClient.UploadFile`
  already accepts a byte-count callback; this is a genuine signal, not simulated.
- **Free-form log line.**

Wiring (all additive, backward compatible):
- `BootstrapAsync` gains an **optional** observer parameter (default null → null-object
  observer). Existing callers and all Core tests compile and pass unchanged.
- `BootstrapOrchestrator`'s phase loop calls starting/finished around `RunPhaseAsync` —
  two call sites, no logic change.
- `SshBootstrapTargetExecutor` accepts an optional observer, reports command/upload events.
  `FakeBootstrapTargetExecutor` ignores it.
- Host gets `ConsoleBootstrapObserver` reproducing today's console output exactly.

### A3. Cancellation honesty
`BootstrapAsync` already takes a token, but SSH.NET command execution is synchronous —
cancellation granularity is **between remote commands**, not mid-command. Verify the
orchestrator checks the token between phases (add if missing); the executor checks between
commands. UI Cancel messaging must match reality: "finishing current remote command, then
stopping."

### A4. Optional enhancement (separately shippable): streamed remote output
`Execute()` returns output only on completion. SSH.NET supports begin-execute + reading the
output stream, enabling live `docker load` layer lines ("Loading layer 3/7") during agent
installation — the only honest way to produce per-layer sub-task text for the *install*
stage. The *upload* stage already gets real byte-level progress from A2. **Deferred** to a
follow-up; byte progress + per-command events already give a strong live feel.

### A5. Explicitly unchanged
Phase order/content, `BootstrapStateStore` format, `BootstrapTarget`/`BootstrapRequest`
shapes, registration flow, `--force` semantics. Provisioning behavior is bit-for-bit what
it is today.

---

## 4. Phase B — Operation Engine (Studio-side, WPF-free core)

### B1. Placement
New namespace `ComplianceServiceManager.Operations`, two layers:
- **Engine core (no WPF references):** everything except the ViewModel/controls. Designed
  for verbatim extraction into an `Aovias.Operations` library once Publishing becomes the
  second consumer. Staying CSM-internal first avoids freezing a platform-wide API against a
  single consumer.
- **Presentation layer (WPF):** one reusable ViewModel + shared controls.

### B2. Engine core types

| Type | Responsibility |
|---|---|
| `OperationDescriptor` | Identity (kind, display name, target identity), ordered `OperationStageDefinition[]` (id, name, description, weight), flags: `SupportsResume`, `SupportsRetry`, `SupportsCancel` |
| `OperationStageStatus` enum | Pending / Running / Completed / Warning / Failed / Cancelled / Skipped ("AlreadySatisfied" → Completed-with-note) |
| `IOperationExecutor` | Per-operation strategy: executes against an `OperationContext`; the only thing an operation implements |
| `OperationContext` | Executor-facing surface: report stage transitions, sub-progress (current-task text + optional fraction), append log lines (auto-scoped to current stage), `CancellationToken`, `IsResume`/attempt number |
| `OperationInstance` | Live state of one run: per-stage status/logs/durations, overall weighted progress, elapsed, current stage/task, failure record, terminal outcome; raises plain .NET change events (thread-agnostic) |
| `OperationEngine` | `Start(descriptor, executor)` → instance; owns per-operation CTS; enforces one active run per target identity; `Cancel`, `Retry` (re-invoke with `IsResume = true`); appends completed runs to history; publishes completion/failure notifications |
| `OperationHistoryStore` | JSON under `%LocalAppData%\AoviasStudio\operations\` — one record per run: kind, target, stage timings, outcome, log file path. The telemetry/history substrate |
| `OperationFailure` | Problem / Cause / RecommendedAction / raw Details — same categorized shape as `BootstrapFailureInfo` / `OperatorErrorItem`, making the "no raw stack traces in primary UI" rule structural |

**Progress math (honesty rules):** overall % = completed stage weights + running stage's
sub-fraction × its weight. Elapsed = wall clock. **ETA shown only after ≥ 1 weighted stage
completes**, labeled "estimated": elapsed ÷ completed-weight × remaining-weight. No
fabricated ETAs on a cold start.

**Threading model:** engine runs executors via `Task.Run` (SSH stack is sync); all
engine/instance events fire on worker threads; **only the presentation layer marshals to
the Dispatcher** (the `RunOnUiThread` pattern already proven in `BootstrapViewModel`).
Executors never touch WPF.

### B3. Presentation layer (reusable across future operations)
- `OperationViewModel` — wraps an `OperationInstance`: observable stages, overall %,
  elapsed (DispatcherTimer), ETA, current stage/task, failure, Cancel/Retry commands,
  per-stage log expansion.
- Shared controls, generalized from what Bootstrap/Publishing already render by hand:
  - `OperationProgressHeader` — bar + % + current stage + current task + elapsed/ETA
  - `OperationTimeline` — ✓/⟳/pending stage list with per-stage expanders and log lines
  - `OperationFailureCard` — thin wrapper or direct reuse of `OperatorErrorCard`
  - `OperationLogPane` — Pipeline Console styling/behaviors incl. `AutoScrollBehavior`
- Notifications: on terminal state, write through the existing `ILoggingService` (already
  feeds Alerts & Events) and raise an engine event the shell status bar can subscribe to.
  No new notification infrastructure.

---

## 5. Phase C — Bootstrap on the engine (Studio)

### C1. `BootstrapOperationExecutor : IOperationExecutor`
- Builds `BootstrapTarget` from wizard state (same mapping as today's `BuildRequest()`).
- Calls `BootstrapRunner.RunAsync(...)` **in-process** with an adapter observer translating
  `IBootstrapExecutionObserver` → `OperationContext`: phase starting → stage Running;
  phase finished → Completed/Failed (AlreadySatisfied → Completed + note); upload bytes →
  sub-progress fraction + "Uploading runtime agent — 34.2 / 87.5 MB"; command events →
  stage-scoped log lines.
- Stage list = the 8 real `BootstrapPhaseName` phases, **1:1** — no cosmetic stages.
  Display names/descriptions reuse the current wizard's.
- Weights: rough real-cost weighting (agent upload/installation heaviest, preflight
  lightest), tuned later from history-store timings.
- Resume: `SupportsResume = true`; retry re-invokes the runner; the unchanged
  `BootstrapStateStore` makes completed phases no-ops, which the adapter surfaces as
  instantly-Completed stages — "previous stages remain green" falls out of real engine
  behavior, not UI state faking.
- Repair = retry with `Force = true` (unchanged semantics).
- Post-success Studio-side step (not an engine phase): existing `IEnvironmentManager`
  registration sync + health snapshot → Environment Summary, kept exactly as built,
  including the local-only fallback path.

### C2. `BootstrapViewModel` changes (surgical, not a rewrite)
- **Unchanged:** steps 0–2 (Target Host / Environment / Review), fact discovery,
  existing-runtime banner, Environment Summary.
- **Swapped:** step 3's hand-rolled stage list + polling → embedded `OperationViewModel`
  bound to the shared controls.
- **Deleted:** `BootstrapExecutionService` (child process, state-file polling, report-line
  regex), `BootstrapCliResult`, `BootstrapPhaseReportLine`; `BootstrapStageViewState`
  superseded by the engine's stage model.
- **Kept:** `TargetHostProbeService` (pre-flight discovery is a Studio concern, not a
  provisioning phase), `RootPathResolver` additions, PasswordBox code-behind.
- **Advanced/Developer panel:** retains generated-command display (rebuilt from the same
  arg-builder logic, labeled "equivalent CLI command", display-only), generated
  configuration values, execution log + export. Password rendered as `--password ********`
  — never the real secret (fixes the current panel's incidental exposure).

### C3. DI changes (CSM `App.xaml.cs`)
- **Add:** `OperationEngine` (singleton), `OperationHistoryStore` (singleton),
  `BootstrapOperationExecutor` (transient/factory — one per run).
- **Remove:** `BootstrapExecutionService`.
- `BootstrapRunner` needs no CSM container registration — it composes its own per-run graph
  internally (as `Program.cs` does today), sidestepping the per-target-executor lifetime
  problem entirely.

---

## 6. Phase D — CLI thinning + consolidation

- **D1.** `Program.cs` `bootstrap` verb → parse args (unchanged) → `BootstrapRunner` with
  `ConsoleBootstrapObserver` → same final summary, same exit codes. Net deletion of the
  composition code from Host.
- **D2.** Regression check: capture current CLI output for a fake-executor run (Core tests)
  and usage/error paths; assert identical after thinning.
- **D3.** Optional consolidation (do last, low risk): reimplement
  `TargetHostProbeService`'s fact commands on top of `IBootstrapTargetExecutor` so CSM
  drops its private SSH.NET usage — one SSH stack platform-wide.

---

## 7. Phase E — Future migrations (design constraint now, work later)

Publishing has the closest existing pattern (`IProgress<PublishingProgress>`, stage
timings, cancellation CTS) — its adapter is nearly mechanical. Deploy / Rollback /
Reconcile follow. **Nothing in Phases A–D may bake Bootstrap-specific assumptions into the
engine core.** Acceptance test for the engine API, reviewed at design time:
*"could `PublishSolutionAsync` be wrapped without engine changes?"*
Actual migration of those screens is out of scope for this effort.

---

## 8. Cross-cutting concerns

- **Error handling:** executor exceptions → `OperationFailure` via a categorizer
  (generalize the existing `BuildRecommendedAction` + `OperatorErrorInterpreter` heuristics
  into an operation-kind-aware mapper). Raw text only in `Details`/logs. Errors attach to
  the failed stage; subsequent stages stay Pending (never Failed-by-contagion).
- **Logging:** per-stage log lines kept in memory (capped) + streamed to a per-run log file
  referenced by the history record; Export = open/copy that file. Structured line =
  timestamp, stage id, severity, text.
- **State management — three deliberately separate stores:**
  1. Live `OperationInstance` (memory).
  2. `OperationHistoryStore` (Studio history/telemetry).
  3. `bootstrap-state/*.json` (engine's own resume truth — owned by the engine; after this
     plan nothing in Studio reads it, since progress comes from the observer).
- **Asynchronous execution model:** Task-based; executors on `Task.Run`; engine events on
  worker threads; Dispatcher marshaling only at the ViewModel boundary.
- **Testing strategy:**
  - *Core:* existing suite unchanged (observer optional) + new tests asserting observer
    callback ordering with `FakeBootstrapTargetExecutor`.
  - *Engine core:* pure unit tests — state machine transitions, weighted progress math,
    cancellation, retry/resume flags, history persistence — against a scripted fake
    executor (fast, no I/O).
  - *Studio:* UIA smoke passes (wizard steps, timeline rendering, failure card, cancel), as
    performed throughout this session.
  - *Honest gap:* end-to-end against a real Ubuntu VPS remains **Runtime Verification
    pending** (Evidence Tiers, `IMPLEMENTATION_GUIDELINES.md`); the fake-executor path
    exercises everything except SSH itself.
- **Migration/rollback strategy:** Phases A–D are independently shippable, each leaving the
  app fully working. A: engine behaviorally unchanged. B: engine core exists unused.
  C: Bootstrap switches — the child-process service is deleted only here, so reverting C
  restores today's behavior. D: CLI thins last. Feature-parity checklist before deleting
  the old path: live stage status, failure card, retry-resume, Advanced command display,
  environment registration sync.

---

## 9. Risks

| Risk | Mitigation |
|---|---|
| In-proc SSH blocks or leaks on abandoned runs | Engine owns lifetime; executor is `IDisposable`, disposed in engine's finally; cancellation checked between commands; mid-command limitation documented in UI copy |
| Core test suite breakage from observer seam | Optional parameter + null-object default; zero existing call-site changes |
| Engine API over-fitted to Bootstrap | Publishing-wrappability review gate on the API before Phase C starts |
| `dotnet run` removal changes working-directory semantics for `bootstrap-state` | `BootstrapRunner` takes an explicit working directory (already a CLI arg); Studio passes `RootPathResolver.ResolveBootstrapWorkingDirectory()` — same location as today; resume state carries over |
| Long-lived singleton `BootstrapViewModel` vs per-run engine state | ViewModel holds only the current `OperationViewModel`; `StartOver` detaches cleanly; engine enforces one active run per target |

---

## 10. Effort estimate

| Phase | Scope | Estimate |
|---|---|---|
| A | Execution project, observer seam, runner extraction | 1–1.5 days |
| B | Engine core + history + presentation controls | 2–2.5 days |
| C | Bootstrap executor + ViewModel swap + deletions | 1–1.5 days |
| D | CLI thinning + output regression + SSH consolidation | 0.5–1 day |
| **Total** | | **5–6.5 days**, each phase a safe stopping point |

---

## 11. Open decision

**Engine core placement.** Recommendation: CSM-internal with clean boundaries first;
extract to a standalone `Aovias.Operations` project when Publishing migrates (the second
consumer proves the API). Making it standalone from day one is a Phase B line-item change
only.
