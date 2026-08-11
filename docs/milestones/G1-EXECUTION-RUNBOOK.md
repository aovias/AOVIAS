# Sprint G1 — Engineering Integration & First Runtime Validation — Execution Runbook

**Status of this document:** this is the *preparation* deliverable for Sprint G1, not the G1 report itself. The sandbox this engineering program has run in has no .NET SDK, no path to acquire one (its network egress proxy allowlists only a small set of hosts and returns `403 blocked-by-allowlist` for nuget.org/dot.net/GitHub/Microsoft package sources), no outbound SSH, and no VPS target — confirmed by direct probing, not assumed. Sprint G1's objective (first real runtime execution) cannot be produced from inside that sandbox. Per your decision, this runbook is the substitute: an exact, copy-pasteable command sequence with expected output at each stage, so you (or whoever has SDK + VPS access) can run it and report results back — at which point the actual G1 report (Executive Summary / Integration Timeline / Build Results / Runtime Results / Defects Found / Defects Fixed / Remaining Blockers / Readiness for G2) gets written from real evidence instead of inference.

**One defect was found and fixed during this runbook's preparation** (a genuine pre-flight catch, not a hypothetical): `BootstrapOrchestrator`'s Runtime Agent Installation phase previously uploaded the Runtime Agent's `.aovdeploy` package and passed its raw file path directly as `docker run`'s image argument — which cannot work (`docker run` needs an image already in the local Docker image store, not an arbitrary file). Fixed to extract the package, `docker load` its embedded image tar, and `docker compose up -d` it — the same sequence the platform's pre-existing `deploy` verb already used correctly. Without this fix, Stage 6 below would have failed on the very first real run.

---

## 0. Prerequisites

| Requirement | Detail |
|---|---|
| **Machine A — build machine** | Any OS with .NET 10 SDK installed (`dotnet --version` shows `10.x`), network access to `nuget.org`, and this repository checked out. Can be the same machine as Machine B. |
| **Machine B — target VPS** | A fresh Ubuntu 22.04 (or later) VPS, reachable via SSH from Machine A, with a non-root sudo-capable user. Needs outbound internet access itself (to install Docker via `get.docker.com`). |
| **Credentials** | SSH username + (password or private key) for Machine B. A registration token already known to the platform (see step 5 — `EnvironmentRegistrationStore.SeedDefaultTokens`, check `Aovias.Runtime.Agent`'s configuration for what's seeded, or mint one per that store's own API if it exposes one). |
| **Command Center** | A running instance of `Aovias.CommandCenter.Api` reachable from Machine B over HTTPS/HTTP, OR run Command Center itself on Machine A/B for this first test (`dotnet run --project Aovias.CommandCenter/src/Aovias.CommandCenter.Api`). |

Run every command in this runbook from the repository root (`Aovias.Platform.slnx`'s directory) on Machine A unless a step explicitly says otherwise.

---

## Stage 1 — Restore

```bash
dotnet restore Aovias.Platform.slnx
```

**Pass criteria:** exits 0, no `NU1xxx` errors. **If it fails:** capture the full error — this is a Build-category defect per G1's classification, likely a package version issue M3.0's static analysis couldn't see (it only checked for *version drift across packages already referenced*, never resolved anything against a real feed).

## Stage 2 — Build

```bash
dotnet build Aovias.Platform.slnx --no-restore -c Release
```

**Pass criteria:** `Build succeeded.`, 0 Error(s). Warnings are acceptable but should be captured — M3.1's source audit found zero compile-blocking issues, so a clean build here would be the first real confirmation of that finding; any error here is the highest-priority thing to report back, with the exact `CSxxxx` code and file/line.

If your build machine is Windows and you also want the WPF leg:

```bash
dotnet build ComplianceServiceManager/ComplianceServiceManager.slnx --no-restore -c Release
```

(Requires the .NET Desktop workload; not required for the rest of this runbook, which only needs the server-side platform.)

## Stage 3 — Test

```bash
dotnet test Aovias.Platform.slnx --no-build -c Release --logger "console;verbosity=normal"
```

**Pass criteria:** all discovered tests pass. Per M3.2's source-level audit, 394 test methods (`[Fact]`/`[Theory]`) are expected across 5 projects (`Aovias.Bootstrap.Core.Tests`, `Aovias.Configuration.Tests`, `Aovias.DeploymentValidation.Tests`, `Aovias.Publishing.Core.Tests`, `Aovias.Publishing.Providers.DotNet.Tests`) — if the actual discovered count differs meaningfully from ~394, or any test fails, capture the exact test name(s) and failure message. A failure here is a Runtime-category defect (something only a real CLR catches) even though M3.2 found the test *logic* sound against current production code.

## Stage 4 — Publish (platform services + Runtime Agent)

```bash
dotnet run --project tools/PublishRuntimeAgent -c Release
```

**Pass criteria:** ends with `SUCCESS: <path-to-package>` and `Deployment Ready: True`. Note the printed package path — you need it in Stage 6. If it prints `FAILED:` with `ReadinessReport.Errors`, that's a Deployment-category defect in the DRV (Deployment Readiness Validation) pipeline itself — capture every listed error verbatim.

```bash
dotnet run --project tools/PublishAllServices -c Release
```

**Pass criteria:** every platform service publishes successfully. This tool's own console output enumerates which services it covers — capture the full list and any per-service failure.

## Stage 5 — Generate Deployment Package (Gateway / Identity / remaining workload services)

This platform needs a real workload to deploy against a fresh environment to prove anything beyond its own self-hosting. This repository contains a separate, independently-versioned application — `ComplaincePlatform/` (its own git repository, own `.slnx`) — with services literally named `Gateway`, `IdentityService`, and 12 others, which appears to be the intended "Gateway"/"Identity"/"remaining services" workload this sprint's pipeline diagram refers to. **Before using it, confirm it actually has the deployment metadata Aovias's publishing pipeline expects** (a `deployment.metadata.json` or equivalent per-service manifest — check `Aovias.Publishing.Providers.DotNet`'s discovery convention against a service under `ComplaincePlatform/Gateway/`). If it's not yet instrumented for Aovias publishing, that is itself a Documentation/Integration-category finding worth recording (not a platform defect — `ComplaincePlatform` was never confirmed to be Aovias-deployable, only found to exist).

```bash
# Adjust the project path once you've confirmed the correct target and manifest are in place.
dotnet run --project tools/PublishAllServices -c Release -- --project ComplaincePlatform/Gateway/Gateway.csproj
dotnet run --project tools/PublishAllServices -c Release -- --project ComplaincePlatform/IdentityService/IdentityService.csproj
```

(The exact CLI argument shape depends on `tools/PublishAllServices/Program.cs`'s actual argument parsing — read it before running; adjust this command to match. If `ComplaincePlatform` turns out not to be deployable as-is, publish just the Runtime Agent from Stage 4 and treat this as a self-hosting-only first run — still a valid, meaningful G1 result, just narrower than the full pipeline diagram.)

**Pass criteria:** a `.aovdeploy` package per service, each reporting `Deployment Ready: True`.

## Stage 6 — Bootstrap VPS (installs Docker + Runtime Agent + registers + verifies, phases 1–8 in one command)

```bash
dotnet run --project Aovias.Bootstrap/src/Aovias.Bootstrap.Host -- bootstrap \
  --host <vps-ip-or-hostname> \
  --username <ssh-user> \
  --password <ssh-password>            `# or --private-key-path <path>` \
  --profile-name demo \
  --environment-name "G1 First Real Environment" \
  --environment-type Demo \
  --registration-token <pre-shared-token> \
  --public-url https://<vps-ip-or-hostname>:<agent-port> \
  --command-center-base-url https://<command-center-host> \
  --runtime-agent-artifact-path <path-to-runtime-agent-.aovdeploy-from-Stage-4> \
  --remote-config-root /opt/aovias/config \
  --docker-network-name aovias-net
```

(`Aovias.Bootstrap.Host`'s actual argument names may differ slightly — run `dotnet run --project Aovias.Bootstrap/src/Aovias.Bootstrap.Host -- bootstrap --help` first if it supports `--help`, or read `Program.cs`'s argument parser, and adjust flag names to match before running for real.)

**This single command exercises stages 6 through 9 of the pipeline diagram** (Bootstrap VPS → Install Runtime Agent → Register Environment → Synchronize Capabilities) as the 8 fixed `BootstrapOrchestrator` phases: Preflight, Docker installation, Docker network creation, runtime directory creation, runtime configuration generation, Runtime Agent installation, Runtime Agent registration, bootstrap verification.

**Pass criteria (record per phase, all 8 should print `Completed`):**
1. Preflight — connects and confirms Ubuntu.
2. Docker installation — installs Docker via `get.docker.com` if absent.
3. Docker network creation — creates the named network.
4. Runtime directory creation.
5. Runtime configuration generation — **should now produce two files**, `settings.deployment-host.json` and `settings.environment-registration.json` (the second is this sprint's fix — confirm both exist under `{remote-config-root}/profiles/demo/` on the VPS).
6. Runtime Agent installation — **watch this one closely**, it's this sprint's other fix. Should extract the package, run `docker load`, then `docker compose up -d`. Confirm with `docker ps` on the VPS that a container is actually running, and `docker logs aovias-runtime-agent` (or the compose project's container name) shows a normal ASP.NET Core startup, not a crash loop.
7. Runtime Agent registration — self-registers, then registers with Command Center's Fleet Registry.
8. Bootstrap verification — polls the agent's heartbeat and expects `Healthy`.

At the end, note the printed `ReportPath` — it's a machine-readable `BootstrapReport` JSON you should save and share back.

## Stage 7 — Confirm Capability Synchronization & Admission (no separate command — verify via API)

```bash
curl https://<vps-host>:<agent-port>/api/deployment/heartbeat
curl https://<vps-host>:<agent-port>/api/deployment/admission
```

**Pass criteria:** the heartbeat response includes a non-empty `Capabilities` array and `DeploymentReady`/`AdmissionBlockerCount` fields; the admission endpoint returns a full `EnvironmentAdmissionReport` (may show `IsReady: true` with zero diagnostics if nothing has been deployed yet — that is the expected, documented vacuous-ready state for a fresh environment, not a defect). Confirm via the Command Center API too:

```bash
curl https://<command-center-host>/api/environments
```

Should list the newly-bootstrapped environment with matching capabilities.

## Stage 8 — Deploy Gateway / Deploy Identity / Deploy Remaining Services

Deployment is driven through the Runtime Agent's deployment API (uploaded release → validate → ensure folders → deploy), consistent with `Aovias.Release.SDK`'s `IDeploymentClient`. The exact CLI entry point for triggering a deployment from outside the process is `Aovias.ReleaseManager` / `Aovias.Release.SDK` — check whether a console entry point exists there, or drive it via Command Center's API (`Aovias.CommandCenter.Api`'s `EnvironmentsController`) once the package from Stage 5 is uploaded to the Release Library. Record the exact command/API call actually used here once you find the right entry point — this runbook intentionally does not guess at one that source review couldn't confirm exists as a ready-made CLI.

**Pass criteria per service:** admission approved (watch for this sprint's new environment-level gate rejecting the deployment if `IsReady: false` — if it does, that's the fix from Sprint M3.3 working as intended, not a bug, but it does mean you must resolve whatever capability is Missing first), plan generated, provisioning succeeds, container starts, deployment reaches a `Completed`/certified state.

## Stage 9 — Health Verification

```bash
curl https://<vps-host>:<agent-port>/api/deployment/health
```

**Pass criteria:** reports `Healthy` for the Runtime Agent itself and each deployed service's certification result from Stage 8 (`PlatformCertificationOrchestrator`'s smoke-test-based certification, not a separate manual health check — see the M3.3 report §3 stage 13 for why there's no separate synchronous health step here by design).

## Stage 10 — Initialize Reconciliation (no command — verify it started on its own)

```bash
curl https://<vps-host>:<agent-port>/api/deployment/reconciliation/history?limit=1
```

**Pass criteria:** at least one reconciliation cycle recorded within 5 minutes of Stage 8's deployment completing (`ReconciliationSchedulerHostedService`'s periodic cycle) — confirms `LastDeploymentPlanStore` was actually updated and the always-running background loop picked it up, closing the loop this whole runbook exists to prove.

---

## What to report back

For each of the 10 stages above: PASS / FAIL / BLOCKED, the exact command run, and the exact output (or error) — paste console output directly, don't summarize it. That raw evidence is what turns this runbook into the real G1 report.
