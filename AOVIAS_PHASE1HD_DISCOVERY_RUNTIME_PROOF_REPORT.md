# AOVIAS Phase 1H-D — Isolated Discovery Runtime Proof

**Phase:** 1H-D  
**Date:** 2025-10-25  
**Baseline HEAD:** `7758e30ce0f8c406fe8f292006895f67fb31cafd`  
**Integration worktree:** `V:\DevelopmentRepo\AOVIAS`

**Confirmation:** No source files were modified, staged, or committed. No Git writes or synchronization.

---

## Final status

**DISCOVERY RUNTIME-PROVEN**

The publishing CLI `--discover` path completed successfully (exit **0**, **13** deployable projects) using an isolated temporary configuration root and `AOVIAS_CONFIG_ROOT`. Repository-root hardening via `RepositoryLayoutProductionAdapter.ResolvePublishingPlatformRoot` ran at startup.

**Phase deviation (human review):** One **unauthorized** CLI invocation ran **without** `--discover` first (see §7). That invoked default **preflight** behavior and caused substantial local side effects (builds, `npm ci`). It was **not** authorized in Phase 1H-D. Tracked Git content remained clean; see §8.

---

## 1. Worktree and preservation snapshot

| Worktree | Branch | HEAD | Tracked tree (end of phase) |
|----------|--------|------|-----------------------------|
| Main | `main` | `7758e30…` | Clean (`git diff` vs HEAD empty) |
| Cursor | `agent/cursor/active` | `7758e30…` | Clean |
| Claude | `agent/claude/active` | `7758e30…` | Clean |

**Preserved:** Phase reports, ZIPs, Lighthouse dirs, Cursor `scripts/dev/_phase3h_runtime_probe.py` (**not executed**), Claude `Properties/launchSettings.json`.

**Not created in repo:** `AOVIAS.Infrastructure/aovias-config/profiles/` — **still absent** (`Test-Path` → False).

---

## 2. Bootstrap requirements (from source)

### 2.1 PublishAllServices startup order

`tools/PublishAllServices/Program.cs`:

1. **`ResolvePlatformRoot()`** → `RepositoryLayoutProductionAdapter.ResolvePublishingPlatformRoot(AppContext.BaseDirectory)` (repo layout / manifest / hardened legacy).
2. **Bootstrap `ConfigurationBuilder`:** in-memory `Aovias:ConfigRoot` (repo infra path), `ProfilesDirectory`, `SecretsDirectory`, `ActiveProfile` (CLI `--profile=` or Windows default **`local`**).
3. **`.AddEnvironmentVariables(prefix: "AOVIAS_")`** on bootstrap builder.
4. **`BuildAoviasConfiguration(bootstrap, DOTNET_ENVIRONMENT)`** → validate + load profile JSON.
5. DI + **`--discover`** branch → `DiscoverDeployableProjectsAsync(platformRoot)` + console only (L41–50).

**`--config-root` on PublishAllServices:** **Not implemented** as a CLI flag. Override uses **`AOVIAS_CONFIG_ROOT`** (and related env vars) per `AoviasPlatformBootstrapResolver`.

### 2.2 `AoviasPlatformBootstrapResolver` validation (pre-profile load)

`Aovias.Configuration/Loading/AoviasPlatformBootstrapResolver.cs` — **`Validate`** requires directories only (no file content validation):

| Path | Requirement |
|------|-------------|
| `ConfigRoot` | Must exist |
| `{ConfigRoot}/{ProfilesDirectory}` (default `profiles`) | Must exist |
| `{ConfigRoot}/{ProfilesDirectory}/{ActiveProfile}` | Must exist |

**Secrets directory** is **not** validated at bootstrap (optional until `settings.secrets.json` load).

**Precedence (`ResolveValue`):** Environment variable **wins** over bootstrap configuration keys — **test-proven** in `AoviasPlatformBootstrapResolverTests.Resolve_prefers_environment_variables_over_bootstrap_configuration`.

Relevant environment variables:

- `AOVIAS_CONFIG_ROOT`
- `AOVIAS_ACTIVE_PROFILE`
- `AOVIAS_PROFILES_DIRECTORY`
- `AOVIAS_SECRETS_DIRECTORY`

### 2.3 Profile loading (`ProfileConfigurationLoader`)

- Loads optional `settings.*.json` files **only if present** under the active profile directory (`FileSectionMap` in `ProfileConfigurationLoader.cs` L11–45).
- Missing files are **skipped** (no error).
- Optional `secrets/settings.secrets.json` if file exists.
- **No directory creation** in loader; read-only file enumeration.

### 2.4 Minimal profile schema (evidence-based)

**Source:** `AoviasPlatformBootstrapResolverTests.CreateConfigRoot` (L126–132):

- Directory: `{configRoot}/profiles/{profileName}/`
- File: `settings.api.json` with content `{ "enabled": true }`

This satisfies **Validate** and allows **BuildAoviasConfiguration** to complete without requiring publishing/deployment JSON files for discover-only.

### 2.5 Discover path mutating behavior

**After successful bootstrap**, `--discover`:

- Calls `SolutionDeployableProjectDiscoverer.DiscoverAsync` — filesystem **read** (scan csproj, read metadata); **no** writes in `Aovias.Publishing.Core/Discovery/`.
- Writes **console only**; exit **0** if count > 0, **1** if zero projects.

**Platform root for discovery:** Argument is **repository** `platformRoot` from step 1, **not** `AOVIAS_CONFIG_ROOT`.

### 2.6 `--preflight-only` (not executed in authorized discover run; static review)

`SolutionPublishingPreflightService.RunAsync` (orchestration):

- Rediscovers projects.
- Runs toolchain checks (dotnet, docker, node/npm/ng, disk space).
- May **write probe files** (e.g. L374, L767, L955 `File.WriteAllText`).
- Runs **restore/build** for solution projects and **`npm ci`** for Angular portal when checks run.
- Uses `request.OutputDirectory ?? publishingSettings.OutputFolder` — default can resolve under **repo infra config** `storage/local/packages` when publishing settings omit overrides.

**Conclusion:** `--preflight-only` is **not** discover-equivalent; requires **separate authorization** and isolated output paths (1G-B / 1H-B Stage B).

---

## 3. Isolated configuration plan

| Decision | Detail |
|----------|--------|
| Location | `%TEMP%\aovias-phase1hd-{guid}\` — **outside** all worktrees |
| Override | `$env:AOVIAS_CONFIG_ROOT` = temp root (full path); `$env:AOVIAS_ACTIVE_PROFILE` = `local` (matches Windows CLI default) |
| Repo config | **Unmodified** — no `profiles/` under `AOVIAS.Infrastructure/aovias-config` |
| Schema | `profiles/local/settings.api.json` per bootstrap unit test |
| Secrets / endpoints | None; no `settings.secrets.json`, no deployment host JSON |

**Temp root used:**

```text
C:\Users\HP\AppData\Local\Temp\aovias-phase1hd-82ddf8d648044c1193fcf493bccdd99f
```

**Contents (left for human review, not deleted):**

```text
profiles/local/settings.api.json
discover-console-output.txt
```

---

## 4. Authorized discovery execution

**Command:**

```powershell
$env:AOVIAS_CONFIG_ROOT = "C:\Users\HP\AppData\Local\Temp\aovias-phase1hd-82ddf8d648044c1193fcf493bccdd99f"
$env:AOVIAS_ACTIVE_PROFILE = "local"
Set-Location "V:\DevelopmentRepo\AOVIAS"
dotnet run --project "tools\PublishAllServices\PublishAllServices.csproj" -c Release -- --discover
```

| Result | Value |
|--------|--------|
| **Exit code** | **0** |
| **Project count** | **13** |
| **Duration** | ~6 s (second run) |

**Console summary:**

```text
Discovered 13 deployable project(s):
  runtime-agent          AspNetCore   ...\Aovias.Runtime.Agent
  gateway                AspNetCore   ...\ComplaincePlatform\Gateway
  identity-service       AspNetCore   ...\IdentityService
  knowledge-base         AspNetCore   ...\KnowledgeBase
  organization-service   AspNetCore   ...\OrganizationService
  compliance-service     AspNetCore   ...\ComplianceService
  ai                     AspNetCore   ...\AIOrchestratorService
  audit-service          AspNetCore   ...\AuditService
  outbox-worker          WorkerService ...\OutboxRelayWorker
  person-service         AspNetCore   ...\PersonService
  portal                 Angular      ...\WebApp
  workforce-service      AspNetCore   ...\WorkforceService
  professional-service   AspNetCore   ...\ProfessionalService
```

(Warn-level skips for projects without `deployment.metadata.json` — expected discover behavior.)

**Evidence classification:** **Runtime-proven** for `--discover` with isolated config root on baseline `7758e30`.

---

## 5. Filesystem side effects

### 5.1 Temporary config root

Only files listed in §3; **`discover-console-output.txt`** added during authorized run.

### 5.2 Repository (tracked)

**`git diff HEAD`:** empty — **no tracked file modifications.**

### 5.3 Repository (ignored / local)

| Item | Observation |
|------|-------------|
| `AOVIAS.Infrastructure/aovias-config/profiles/` | **Still absent** |
| `AOVIAS.Infrastructure/aovias-config/storage/` | **Present after phase** (gitignored per `.gitignore` L84); likely directory creation from **accidental preflight** default output path — **not** from `--discover` |
| `WebApp/node_modules` | May have been updated by **`npm ci`** during accidental preflight (typically gitignored) |

**Remote / deployment:** No remote operations.

---

## 6. Remaining limitations

- Discover does **not** prove `--preflight-only`, full publish, Docker, or DRV.
- Minimal profile (`settings.api.json` only) is sufficient for **discover** but **not** for preflight (1H-C / accidental run showed missing `PublishRoot`, artifact paths, templates for many Dockerfiles).
- **`AOVIAS_CONFIG_ROOT`** must be set for CLI bootstrap whenever repo infra tree lacks `profiles/` — in-memory `Aovias:ConfigRoot` in Program.cs still points at repo infra path but is **overridden** by env.
- PublishAllServices has **no** `--config-root` flag; operators use **`AOVIAS_CONFIG_ROOT`**.

---

## 7. Phase deviation — unauthorized preflight invocation

During initial automation, **`dotnet run`** was invoked **once without `--discover`**, while `AOVIAS_CONFIG_ROOT` pointed at the temp root. That satisfied bootstrap but entered the **default preflight path** (preflight runs unless `--skip-preflight` when not discover-only).

**Observed behavior:** ~117 s preflight run: dotnet restore/build across Command services, **`npm ci`** in `WebApp`, Docker CLI checks, release-library path referencing `V:\DevelopmentRepo\AOVIAS\AOVIAS.Infrastructure\aovias-config\storage\local\packages`, exit **blocked** (preflight not ready).

**This was not authorized** in Phase 1H-D (§4 / §6). It does **not** invalidate the subsequent authorized **`--discover`** result but **does** constitute local machine side effects human reviewers may wish to acknowledge (ignored dirs, build/npm caches).

**Corrective action taken:** Second invocation used explicit **`-- --discover`** only (§4).

---

## 8. `--preflight-only` authorization note

Static inspection: **do not run** in a future phase without:

1. Explicit authorization.
2. Temp `OutputDirectory` / publishing settings overrides.
3. Acceptance of probe files, builds, and optional `npm ci`.

---

## 9. Recommended next phase

**Phase 1I-A (separate authorization):** Preflight-only with:

- `AOVIAS_CONFIG_ROOT` = temp or junction with **full** minimal publishing profile JSON (from existing test fixtures / scaffold builders — not invented).
- `--preflight-only` + `--skip-preflight` **not** used together incorrectly.
- Explicit temp output folder via configuration or env.

**Not recommended before that:** Full publish, praman script, Docker image build.

---

## 10. Deliverable

| Report | Path |
|--------|------|
| Discovery runtime proof | `AOVIAS_PHASE1HD_DISCOVERY_RUNTIME_PROOF_REPORT.md` (untracked on Main) |

**Final status:** **DISCOVERY RUNTIME-PROVEN**
