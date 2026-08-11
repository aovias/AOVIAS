# Bootstrap Architecture — Sprint M7.x / M7.x.2: Enterprise Bootstrap Reliability & Self-Healing Framework

This document describes the bootstrap orchestration pipeline after the M7.x and M7.x.2 refactors:
every stage, its state transitions, recovery paths, verification rules, and fallback strategies.

## M7.x.2 — Bootstrap State Machine & Failure Diagnostics

M7.x's session initialization (SSH connectivity probe, privilege-mode detection) ran as plain code
before the stage loop even started. That had a real defect: an exception thrown there had no
`BootstrapPhaseName` to attach to, so the generic Operation Engine's own safety net could only
report a bare, unattributed failure — observed in practice as *"Bootstrap reports Failed at Connect
while the actual exception originates from DockerNetworkCreation."* M7.x.2 closes this at the root:

- **Connect and ValidatePrivileges are now real stages** (`BootstrapPhaseName.Connect = 1`,
  `.ValidatePrivileges = 2`), first in the pipeline, subject to the exact same lifecycle guarantee
  as every other phase. There is no more orchestrator code that runs outside a stage boundary.
- **Every one of a stage's lifecycle calls is individually guarded**, immediately at the call site
  (`BootstrapOrchestrator.RunStageAsync`/`ExecuteGuardedAsync`/`VerifyGuardedAsync`/`RepairGuardedAsync`).
  Whatever throws, wherever it throws, is caught before it can pass through another call frame and
  lose its origin — this is the actual mechanism that guarantees the reported failure always
  matches the actual failed stage.
- **`BootstrapStageException`** (`Phase`, `LifecycleStep`) is what every such catch constructs
  immediately, replacing ad hoc generic exceptions with one structured type carrying exactly what
  the UI needs to attribute correctly.
- **Genuine cancellation vs. a "declined" signal are distinguished**: `catch (OperationCanceledException)
  when (cancellationToken.IsCancellationRequested)` rethrows only when the *actual* run token was
  cancelled (Cancel button); a bare `OperationCanceledException` used internally to signal something
  else (e.g. "the operator declined the sudo password prompt") falls through to the normal
  exception-to-failure path instead of masquerading as a cancelled run.
- **`DockerNetworkStage` gained an automatic diagnostics engine**
  (`DockerManager.CollectNetworkDiagnosticsAsync`): daemon status (`docker info`), every existing
  network (`docker network ls`), a direct conflict check against the target network name, Docker
  socket permissions, rootless-mode detection, and effective identity (`id`) — never "Unhandled
  exception" again for a network-creation failure.
- **`BootstrapPhaseResult` gained Stage Timeline fields**: `StartedUtc`, `RetryCount`,
  `RepairAttempts`, `VerificationResult` — populated by the guarded lifecycle runner. Repair
  activity also streams into the stage's own live log feed via `IBootstrapExecutionObserver.OnLog`
  as it happens, not only inside a failure card after the fact. These are additive fields on an
  already write-only report artifact (never deserialized back by the app), so no migration is
  needed for older `bootstrap-reports/*.json` files.

## Philosophy

The target VPS is treated as hostile: partial previous installations, interrupted bootstraps,
permission corruption, stale containers and compose files, immutable file attributes, crash loops,
configuration drift, network interruptions, and expired sessions are all *expected* conditions,
not surprises. The pipeline's job is to converge the host to one healthy runtime no matter how
many times it runs and no matter what state it starts from — and, when convergence is impossible,
to fail with a precise, evidence-backed diagnosis and a recommended remediation, never a bare
error.

Three rules govern everything:

1. **Remote state is authoritative.** Local checkpoints (`BootstrapStateStore`) are hints that
   spare redundant work — every stage's *discovery* inspects the actual host before trusting them.
   A checkpoint that lies (reimaged VPS, manually removed Docker) is self-healed, not believed.
2. **Only fail after recovery attempts.** Every stage gets one bounded automatic repair round per
   failure site before it fails, and the failure records what was tried.
3. **An inconclusive reading is never a blocker.** Only a genuine, parsed negative signal fails a
   check; a host that can't answer (`stat`/`sha256sum` missing, unparseable output) is tolerated
   with a note.

## Component map (all in `Aovias.Bootstrap.Core`)

| Component | Responsibility |
| --- | --- |
| `BootstrapOrchestrator` | The state machine. Guarded per-stage lifecycle, checkpoints, report + transcript persistence. |
| `RemoteExecutionEngine` | Every remote command: elevation strategy (root / passwordless sudo / password sudo with prompt-and-cache), per-command timing, session transcript, transient-failure retry with reconnect. |
| `PermissionRepairService` | The Permission Repair Engine: directory specs, `mkdir → chattr -R -i → chown → chmod → stat` verify pipeline, container-UID detection, `ls -la`/`lsattr`/`findmnt` evidence collection. |
| `DockerManager` | Every Docker interaction: install, daemon verify/restart, network, image load, compose up (`--force-recreate` always), container state parsing, health wait, container + network diagnostics. |
| `ConfigurationSynchronizer` | Checksum-based config sync: generate → compare (SHA-256) → upload only drift → verify upload. |
| `Stages/*` | One `BootstrapStage` per `BootstrapPhaseName`, each owning Validate / Discover / Execute / Verify / Repair / Diagnostics / Rollback. Connect and ValidatePrivileges (M7.x.2) lead the pipeline. |
| `BootstrapStageException` (Contracts) | `(Phase, LifecycleStep, inner)` — what every guarded lifecycle catch constructs immediately, guaranteeing correct attribution (M7.x.2). |

`Aovias.Bootstrap.Execution` supplies the SSH.NET executor, the HTTP registration client (which
reads failed responses' structured `DeploymentFailure` bodies rather than discarding them), and
`BootstrapRunner`, the single composition root the CLI and AOVIAS Studio both call.

## The stage lifecycle state machine

```
per stage, in fixed BootstrapPhaseName order (Connect and ValidatePrivileges lead every run,
never checkpoint-skipped — reachability and privilege mode are session properties):
    Discover ──satisfied──▶ AlreadySatisfied ──▶ next stage
    │   (remote inspection; checkpoint is only a hint some stages consult)
    └─not satisfied─▶ Validate ──blocked──▶ Failed (+ diagnostics)
                      └─▶ Execute ──failed──▶ Repair ──repaired──▶ re-Execute (once)
                          │                   └─not repaired──▶ Failed (+ repair record + diagnostics)
                          └─▶ Verify ──failed──▶ Repair ──repaired──▶ re-Verify (once)
                              │                  └─not repaired──▶ Failed (+ repair record + diagnostics)
                              └─▶ Completed (checkpoint recorded, StartedUtc/RetryCount/RepairAttempts/VerificationResult populated)

any stage failure stops the run (later stages depend on earlier ones genuinely completing)
```

**M7.x.2 hardening**: every one of the six calls above (Discover/Validate/Execute/Verify/Repair/
CollectDiagnostics) is wrapped in its own try/catch, immediately at the call site
(`BootstrapOrchestrator.RunStageAsync` and its `*GuardedAsync` helpers) — so an exception thrown
from *any* of them is caught before it can travel through another frame and lose which stage/step
it came from. On catch, a `BootstrapStageException(Phase, LifecycleStep, inner)` is constructed
immediately and logged; the failure it produces always carries the *correct* `stage.Phase`. This
replaced M7.x's original design, where SSH connectivity and privilege-mode detection ran as plain
code *before* the stage loop started — an exception there had no stage to attach to at all, which
is what let a `DockerNetworkCreation` failure surface as "Failed at Connect."

`OperationCanceledException` handling is deliberately narrow: `catch (OperationCanceledException)
when (cancellationToken.IsCancellationRequested)` rethrows only when the run's own token was
actually cancelled (the Cancel button); a bare `OperationCanceledException` used as an internal
signal for something else (`RemoteExecutionEngine` throws one, untied to any token, when the
operator declines a sudo password prompt) falls through to the general catch and becomes a normal,
reportable stage failure instead of masquerading as a cancelled run.

## Stage-by-stage: discovery, verification, repair, fallback

### 1. Connect (`ConnectStage`)
- **Discover:** never satisfied — reachability is a session property, re-probed every run.
- **Execute:** `TestConnectionAsync`. **Verify:** a successful probe is its own verification.
- **Repair:** one bounded retry after a short delay (`BootstrapTunables.ConnectRetryDelayMs`,
  default 2s, 0 in tests) — tolerates a transient network blip without failing the whole run.

### 2. Validate Privileges (`ValidatePrivilegesStage`)
- **Discover:** never satisfied — privilege mode is re-detected every session. Closes a real
  latent defect: the original design detected it inside the old combined Preflight phase, so a
  resumed run whose Preflight was checkpoint-skipped never re-detected it, silently running every
  later "privileged" command unelevated.
- **Execute:** `RemoteExecutionEngine.DetectPrivilegeModeAsync` (root / passwordless sudo /
  password sudo / unavailable). `Unavailable` is this stage's own blocker — no repair; there is no
  path to root Bootstrap can create on its own.

### 3. Preflight (`PreflightStage`)
- **Discover:** checkpointed → satisfied (OS identity, CPU architecture, disk, memory don't drift
  session-to-session — unlike reachability/privilege mode, which are their own preceding stages).
- **Execute:** ALL blockers reported together — registration intent (token/URLs supplied), Ubuntu
  identity + supported release, CPU architecture, disk (≥5 GB), memory (≥512 MB), outbound
  connectivity (raw-IP probe, isolates routing from DNS), DNS resolution
  (`download.docker.com` — deliberately not `get.docker.com`, the install marker).
- **Verify:** execution is its own verification. **Repair:** none — preconditions are the
  operator's to fix, with the full list in one shot.

### 4. Docker Installation (`DockerInstallationStage`)
- **Discover:** `docker --version` on the host. Checkpoint irrelevant — a host that lost Docker
  gets it reinstalled.
- **Execute:** `get.docker.com | sh && systemctl enable --now docker`.
- **Verify:** `docker info` — an installed CLI is not a working engine.
- **Repair (verify failures only):** `systemctl restart docker`, then re-verify. An install-script
  failure is not retried — rerunning a failed installer unchanged is churn, not repair.

### 5. Docker Network (`DockerNetworkStage`)
- **Discover:** `docker network inspect` (privileged — an unprivileged inspect misreads "denied"
  as "absent"). **Execute:** create. **Verify:** re-inspect. **Repair:** re-create once.
- **Diagnostics (M7.x.2):** `DockerManager.CollectNetworkDiagnosticsAsync` — `docker info`,
  `docker network ls`, a direct conflict check against the target network's name, Docker socket
  permissions, rootless-mode detection (`SecurityOptions`), and effective identity (`id`). Never a
  bare "Unhandled exception" for a network-creation failure again.

### 6. Platform Directories (`RuntimeDirectoryStage`)
- **Discover:** requires positive proof — checkpoint AND profile directory present AND `stat`
  confirms the tree's mode. Anything less re-runs the repair pass (cheap, idempotent).
- **Execute:** `PermissionRepairService.EnsureRuntimeTreeAsync`: profile `mkdir`, `chattr -R -i`
  (best-effort — rotated logs/closed releases have been observed immutable-flagged, which makes
  even root's chmod fail "Operation not permitted"), `chown -R` to the SSH user (skipped for
  root — SFTP already writes as root), `chmod -R 0777` (the container runs as a fixed non-root UID
  unrelated to the SSH user; see the service's remarks for why broad-open beats per-UID chown).
- **Verify:** `stat` — parsed mismatch fails; inconclusive passes with a note.
- **Repair:** one more full ensure pass. **Diagnostics:** `ls -la` + `lsattr -R` + `findmnt`
  (read-only remount detection).

### 7. Generate Configuration (`ConfigurationGenerationStage`)
- **Discover:** pure checksum comparison of every generated `settings.*.json` against the host.
  All current → satisfied (nothing uploaded, drift provably absent). An unverifiable checksum
  counts as drift here — prefer a redundant re-upload over trusting an unverifiable file.
- **Execute:** upload only missing/drifted files; verify each upload's checksum.
- **Verify:** files exist and no *parsed* mismatch — hosts without `sha256sum` pass with a note
  (deliberately weaker than discovery; see the stage's remarks).
- **Repair:** one full re-synchronization pass.

### 8. Validate Runtime Configuration (`ConfigurationValidationStage`)
- **Discover:** never satisfied — in-process contract validation re-runs every attempt ("never
  skip validation"; it is free). Validates every generated settings object against its own
  DataAnnotations contract plus the custom-enforced requirements (logging `logRoot`, deployment
  storage `Root`). Failure carries File / Property / Expected / Actual / Suggested Fix per finding
  and stops the run before Docker is ever touched.

### 9. Runtime Startup (`RuntimeStartupStage`)
- **Discover:** container running AND healthy (Docker healthcheck) → satisfied. A merely-existing
  (stopped/crash-looping/unhealthy) container is NOT satisfied — it will be recreated. "Container
  start is not success."
- **Execute:** upload package (SHA-256 verified end-to-end; mismatch = corrupted upload), clear +
  re-extract the extraction directory (unconditionally — stale root-owned files from older
  attempts), write the compose `.env`, `docker load` the embedded image, prepare every bind-mount
  directory through the permission engine (independent of stage 4's history), compose up
  `--force-recreate` (a crash-looping container never picks up host-side fixes without
  recreation), best-effort `ufw allow` for the public port.
- **Verify:** poll container state until running+healthy; a growing restart count short-circuits
  as a diagnosed crash loop ("Container Crash — ..."), a timeout as "Health Check Failed — ...".
- **Repair:** corrupted upload → one re-upload/re-execute; unhealthy/crash-loop → one round of
  bind-mount permission re-repair + force-recreate, then re-verify.
- **Diagnostics (always on failure):** container state summary, `docker compose ps`, last 200 log
  lines, verbatim `ls -la`/`lsattr -R`/`findmnt` of every bind-mount directory, and the
  container's actual configured user (`docker inspect .Config.User` — never assumed).

### 10. Register Environment (`RegistrationStage`)
- **Discover:** never satisfied — registration re-runs on every attempt (real-VPS lesson). It is
  idempotent end-to-end by design (the agent re-registers itself on every restart via
  `EnvironmentRegistrationHostedService`; the Command Center's `FleetRegistry.Register` upserts by
  BaseUrl), and the verification stage needs the API key self-registration issues
  (`BootstrapStageContext.IssuedApiKey`, in-memory only, never persisted) to poll the agent's
  authenticated heartbeat endpoint — a checkpoint-skipped registration on a resumed run would leave
  verification with no key and a guaranteed 401.
- **Validate:** absolute-URI checks for the agent and Command Center URLs.
- **Execute:** bounded retry for transient failures (a just-started container binds its port a few
  seconds late), but **fail-fast on token rejection** — an affirmatively rejected token will not
  change on retry; the failure carries the agent's own reason plus remediation guidance.
- **Fallback:** the HTTP client surfaces the response body's structured failure (category,
  message) — never only a status code.

### 11. Verify Health (`VerificationStage`)
- **Discover:** never satisfied — end-to-end health is re-verified on every run by design.
- **Execute:** heartbeat poll until "Healthy", then Runtime Version presence, then a real SignalR
  negotiate. All four signals (API, heartbeat, version, SignalR) are required.

## Rollback policy

`RollbackAsync` exists on every stage but the default is a deliberate no-op: bootstrap stages
install and configure — they never own user data, and destructive un-installation on failure
(removing Docker, deleting directories that may hold operator content) is strictly worse than a
diagnosable partial state that the next resumable, self-healing run converges from. Stages may
override where a genuinely safe inverse exists; none currently need to.

## Failure anatomy

Every failed phase result carries:

- **Message** — the classified headline (`Configuration Validation Failed — ...`,
  `Container Crash — ...`, `Docker Network Failed — ...`, `Health Check Failed — ...`,
  `Registration Failed — ...`, `Runtime Startup Failed — ...`, or — for a stage whose lifecycle
  itself threw (M7.x.2) — `Phase '{Phase}' threw during {LifecycleStep}.`) that AOVIAS Studio's
  failure card maps to a category.
- **RootCause** — the stage's own detail, then `=== Automatic Repair Attempts ===` (what
  self-healing tried and how each attempt went), then the stage's collected diagnostics sections.
  Diagnostics collection is itself guarded so it can never mask the failure it decorates.
- **StartedUtc / RetryCount / RepairAttempts / VerificationResult** (M7.x.2, Stage Timeline) —
  when the stage started, how many Execute/Verify cycles ran after a successful repair, the
  ordered list of repair attempts made, and the post-execution verification's own evidence text —
  populated on both success and failure.

## Session evidence

`RemoteExecutionEngine` records every command (text, elevation flag, exit code, duration, first
stderr line — never passwords, which travel exclusively via the executor's dedicated parameter).
The orchestrator persists this next to the JSON report:

```
<working dir>/bootstrap-reports/<target-identity>-<timestamp>.json
<working dir>/bootstrap-reports/<target-identity>-<timestamp>-transcript.log
```

## Verification suite

`Aovias.Bootstrap.Core.Tests` (54 tests) covers, against the in-memory host fake: fresh targets,
already-healthy targets (full-run convergence with zero mutating commands), partially bootstrapped
targets, checkpoint drift (state store says complete, host says otherwise → reinstalled),
configuration drift (checksum-detected, corrected byte-for-byte), crash-looping containers
(diagnosed with recorded repair attempts), corrupted uploads, unhealthy-but-running containers
(recreated, not trusted), registration token rejection (fail-fast with guidance), transient
registration failures (retried), all three privilege modes including wrong-password re-prompting
and prompt cancellation, resumed sessions re-detecting privilege mode, cancellation, and preflight
blocker aggregation. Real-VPS behavior must still be verified on a real VPS — the suite proves
orchestration logic, not SSH/Docker/network reality.

**M7.x.2 additions** specifically prove the reported defect is fixed: a genuine .NET exception
thrown deep inside `DockerNetworkCreation`'s own Execute step is attributed to
`DockerNetworkCreation` — never to `Connect` — with Connect/ValidatePrivileges/Preflight correctly
reported as the successes they were, and with Docker network diagnostics attached; the mirror case
(an exception during `ValidatePrivileges`' own detection) is attributed there, never to `Connect`.
