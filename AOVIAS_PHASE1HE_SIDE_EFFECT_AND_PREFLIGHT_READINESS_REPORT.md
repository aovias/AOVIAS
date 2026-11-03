# AOVIAS Phase 1H-E — Side-Effect Audit and Preflight Readiness

**Phase:** 1H-E (read-only)  
**Date:** 2025-10-25  
**Baseline HEAD:** `7758e30ce0f8c406fe8f292006895f67fb31cafd`

**Confirmation:** No preflight, build, `npm ci`, Docker, publish, cleanup, synchronization, or Git writes. No secrets read or printed.

---

## Final status

**READY FOR SEPARATE PREFLIGHT AUTHORIZATION**

Side effects from the unauthorized Phase 1H-D preflight are **identified and attributed with high confidence** to ignored/generated paths only; **tracked Git remains clean**. A future `--preflight-only` run **must not** reuse the minimal Phase 1H-D profile or default repo output paths without an expanded temp fixture (§6).

---

## 1. Worktree safety snapshot

| Worktree | Branch | HEAD | Tracked tree |
|----------|--------|------|--------------|
| Main | `main` | `7758e30…` | Clean (`git diff HEAD` empty) |
| Cursor | `agent/cursor/active` | `7758e30…` | Clean |
| Claude | `agent/claude/active` | `7758e30…` | Clean |

**Preserved:**

- Temp config (Phase 1H-D): `C:\Users\HP\AppData\Local\Temp\aovias-phase1hd-82ddf8d648044c1193fcf493bccdd99f` — **exists**, untouched this phase.
- Cursor `scripts/dev/_phase3h_runtime_probe.py` — **not executed**.
- Claude `Properties/launchSettings.json` — present.
- Main untracked reports, ZIPs, Lighthouse — unchanged pattern.

---

## 2. Phase 1H-D evidence summary

### 2.1 Authorized discovery run

**Command** (`AOVIAS_PHASE1HD_DISCOVERY_RUNTIME_PROOF_REPORT.md` §4):

```powershell
$env:AOVIAS_CONFIG_ROOT = "C:\Users\HP\AppData\Local\Temp\aovias-phase1hd-82ddf8d648044c1193fcf493bccdd99f"
$env:AOVIAS_ACTIVE_PROFILE = "local"
Set-Location "V:\DevelopmentRepo\AOVIAS"
dotnet run --project "tools\PublishAllServices\PublishAllServices.csproj" -c Release -- --discover
```

| Item | Value |
|------|--------|
| Exit code | **0** |
| Deployable projects | **13** |
| Config override | `AOVIAS_CONFIG_ROOT` overrides in-memory repo `Aovias:ConfigRoot` |
| Active profile | **`local`** (Windows default + env) |
| Platform root | Repository via `RepositoryLayoutProductionAdapter.ResolvePublishingPlatformRoot` |

**Writes attributed to authorized discover:** temp `discover-console-output.txt`; no repo `storage/` (1H-C: storage absent; 1H-D: unchanged after discover).

### 2.2 Unauthorized deviation (plain statement)

Automation once ran **`dotnet run` without `--discover`** while `AOVIAS_CONFIG_ROOT` was set. Bootstrap passed; the CLI ran **default preflight** (~117 s): Release **dotnet restore/build** across services, **`npm ci`** in `WebApp`, Docker CLI probes, **release-library directory creation** under repo infra storage default, console report, exit **blocked**. This was **outside** Phase 1H-D authorization.

---

## 3. Side-effect inventory (read-only audit)

| Path | Exists | Ignored? | Before 1H-D | Attribution | Confidence | Follow-up |
|------|--------|----------|-------------|-------------|------------|-----------|
| `AOVIAS.Infrastructure/aovias-config/profiles/` | No | — | Absent | — | High | None |
| `AOVIAS.Infrastructure/aovias-config/storage/local/packages/` | Yes | Yes (`.gitignore` L84) | **No** (1H-C) | Accidental preflight `CheckReleaseLibrary` (L312–332) | **High** | Optional human removal of empty category dirs |
| Category subdirs (AI, Gateway, …) | Yes, empty | Yes | No | Same; **LastWriteTime 2026-10-25 16:16:17** | High | Optional |
| `WebApp/node_modules/` | Yes | Yes (`WebApp/.gitignore`) | Existed since **2026-08-11** | **`npm ci`**; **LastWriteTime 2026-10-25 16:17:11** | **High** | None unless reinstall desired |
| Tracked repo files | — | Tracked | — | `git diff HEAD` empty | High | None |
| `bin/` / `obj/` | Present | Build output | Mixed (1H-C tests + preflight) | Medium | None |
| Temp `aovias-phase1hd-…` | Yes | Outside repo | 1H-D | Discover + setup | High | **Preserve** |

**Uncertain:** Stray `%TEMP%` probe dirs if `TemporaryFolder` was empty during accidental run (DRV probe failed fast) — not exhaustively searched.

**Do not infer cleanliness from tracked Git alone.**

---

## 4. `--preflight-only` source audit (not executed)

### 4.1 CLI (`tools/PublishAllServices/Program.cs`)

- Same bootstrap as discover until branch.
- **`--preflight-only`:** calls `RunSolutionPreflightAsync`, prints report, returns (no `PublishSolutionAsync`).
- **`outputDirectory`:** `publishingSettings?.OutputFolder ?? Path.Combine(ResolveConfigRootDirectory(**platformRoot**), "storage/local/packages")` — default targets **repo infra**, not `AOVIAS_CONFIG_ROOT`.

### 4.2 `SolutionPublishingPreflightService.RunAsync` — mutating behavior

| Activity | Side effects |
|----------|----------------|
| Discover | Read-only |
| Release library check | **Creates** `outputDirectory` + category folders |
| DRV check | Writable test; **temporary** probe dir under `{TemporaryFolder}/preflight-probe` (create/write/delete) when `TemporaryFolder` set |
| .NET / Docker / Angular checks | **External processes** (`dotnet`, `docker`, `node`, `npm`, **`npm ci`**) |
| Solution build check | **Release restore/build** → `bin/`/`obj/` |
| Optional Docker build test | Docker/build staging (when not skipped) |

**Not side-effect-free.**

### 4.3 `SolutionPublishingPreflightRequest`

Includes **`SkipDockerBuildTest`** (used in `CheckDockerAsync`) — can limit Docker **build** probe when set true on the request; **PublishAllServices does not expose this on the CLI** today (always default request). Angular **`npm ci`** and solution **builds** still run when checks proceed.

### 4.4 Redirecting output outside repo

Requires **`settings.publishing.json`** (or equivalent merged config) with Windows paths for at least:

- `OutputFolder`, `PublishRoot`, `ArtifactRoot`, `TemporaryFolder`, `DeploymentValidationFolder`, `ValidationConfigRoot`
- Template paths → existing repo files under `AOVIAS.Infrastructure/aovias-config/templates/`
- `PlatformProjectsRoot` → repository root

**No tracked `settings.publishing.json`** in repo at HEAD — fixture must be built from `PublishingSettings` required fields + scaffold/tests in a **future authorized** step.

### 4.5 Phase 1H-D temp profile insufficient

Minimal `settings.api.json` only — preflight **blocked** on missing publishing/deployment paths (observed in accidental run).

---

## 5. Proposed execution boundary (future authorization only)

**Do not run until separately authorized.**

### 5.1 Preparation (new temp tree — do not reuse phase1hd)

1. Create `%TEMP%\aovias-preflight-{new-guid}\` with writable subdirs: `output`, `publish`, `artifacts`, `temp`, `drv-workspace`, `config-root\profiles\local\`, optionally `config-root\profiles\demo\`.
2. Populate **`config-root`** with expanded profile JSON (including **`settings.publishing.json`**) — paths under preflight temp only; template paths point at **`V:\DevelopmentRepo\AOVIAS\AOVIAS.Infrastructure\aovias-config\templates\...`** (read-only).
3. Add deployment-host JSON for **`DeploymentActiveProfile`** (`demo` by default in `PublishingSettings`) if profile check requires it — derive schema from `PlatformFeatureActivationTests` / `DeploymentHostPathsResolver` tests (placeholder host paths under temp, not production).

### 5.2 Proposed command (illustrative)

```powershell
$preflightRoot = Join-Path $env:TEMP "aovias-preflight-<NEW-GUID>"
# ... create dirs + settings.publishing.json + other required profile files under $preflightRoot\config-root ...

$env:AOVIAS_CONFIG_ROOT = Join-Path $preflightRoot "config-root"
$env:AOVIAS_ACTIVE_PROFILE = "local"

Set-Location "V:\DevelopmentRepo\AOVIAS"
dotnet run --project "tools\PublishAllServices\PublishAllServices.csproj" -c Release -- --preflight-only --profile=local
```

**Pre-run verification:** Confirm `Publishing:OutputFolder` in merged config equals temp path (not repo `storage/`).

### 5.3 Safeguards

| Risk | Mitigation |
|------|------------|
| Repo storage mutation | **`OutputFolder`** and related paths **must** be temp-only |
| `npm ci` / builds | Explicit operator consent; expect WebApp `node_modules` and `bin/obj` updates |
| Docker daemon absent | Expect Docker check failure; distinguish from layout regressions |
| Secrets | No real credentials in fixture JSON |
| Accidental publish | Use **`--preflight-only`** only; never omit discover/preflight flags incorrectly |

### 5.4 Cleanup policy

Leave temp trees for human review unless explicitly authorized later; **do not** auto-delete `%TEMP%` broadly.

---

## 6. Remaining risks

- Preflight may still **fail** (blocked) even with valid paths — e.g. missing Dockerfiles/templates, Docker down — without indicating W1/1G-D regression.
- CLI lacks **`--config-root`** and **`SkipDockerBuildTest`** surface — configuration file quality is the main control.
- Re-running preflight without temp **`OutputFolder`** may append to repo **`storage/local/packages`** again.

---

## 7. Recommended next phase

**Phase 1I-B (authorized):** Scaffold temp **`settings.publishing.json`** from code/tests, run **`--preflight-only`** with temp output root, record console verdict and filesystem delta **only under temp + ignored build outputs**.

---

## 8. Deliverable

| Report | Path |
|--------|------|
| Side-effect & preflight readiness | `AOVIAS_PHASE1HE_SIDE_EFFECT_AND_PREFLIGHT_READINESS_REPORT.md` (untracked on Main) |

**Final status:** **READY FOR SEPARATE PREFLIGHT AUTHORIZATION**
