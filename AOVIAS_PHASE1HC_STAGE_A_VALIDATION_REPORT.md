# AOVIAS Phase 1H-C — Stage A Validation and Discovery-Only Safety Gate

**Phase:** 1H-C  
**Date:** 2025-10-25  
**Integration worktree:** `V:\DevelopmentRepo\AOVIAS`  
**Baseline HEAD:** `7758e30ce0f8c406fe8f292006895f67fb31cafd`

**Confirmation:** No source files were modified, staged, or committed. No publish, package, Docker, DRV, bootstrap, or probe execution. Generated `bin/`/`obj/` and test temp dirs were **not** cleaned (per phase rules).

---

## Final status

**STAGE A PARTIALLY PASSED — VALIDATION GAPS REMAIN**

All authorized Stage A **test suites** and **Release builds** passed on Main. **`--discover` was not successfully executed** — the CLI aborts during configuration bootstrap because the on-disk config root has **templates only** (no `profiles/` tree). Static review confirms the **`--discover` code path** is non-mutating once the host reaches it; runtime proof of discover output remains **unverified**.

---

## 1. Worktree and artifact preservation

| Worktree | Branch | HEAD | Tracked tree |
|----------|--------|------|--------------|
| Main | `main` | `7758e30ce0f8c406fe8f292006895f67fb31cafd` | Clean (before/after validation) |
| Cursor | `agent/cursor/active` | `7758e30…` | Clean |
| Claude | `agent/claude/active` | `7758e30…` | Clean |

**Preserved (not executed / not removed):**

- Cursor: `scripts/dev/_phase3h_runtime_probe.py` — **excluded and not run**.
- Claude: `Aovias.CommandCenter/.../Properties/launchSettings.json` — present.
- Main/Cursor: Phase reports, ZIPs, Lighthouse dirs — untracked, unchanged pattern.

**Gate:** Expected baseline matched. No repair, sync, reset, clean, or stash.

---

## 2. Safe validation inventory (inspected before execution)

| # | Target | Project / entry | Side-effect review |
|---|--------|-----------------|-------------------|
| 1 | Layout / discovery tests | `Aovias.Configuration/tests/Aovias.Configuration.Tests/Aovias.Configuration.Tests.csproj` — filter `FullyQualifiedName~Layout` | xUnit; temp dirs in tests only; no publish |
| 2 | Publishing paths | `Aovias.Publishing.Core.Tests` — `PublishingProjectPathsTests` | Unit tests only |
| 3 | ConfigTemplateGenerator | Same — `ConfigTemplateGenerator` filter | Unit tests; may use temp dirs |
| 4 | Docker context stager | Same — `RuntimeAgentDockerContextStager` filter | Staging under `%TEMP%`; test `finally` deletes staging (test-owned cleanup) |
| 5 | DeploymentValidation | `Aovias.DeploymentValidation/tests/Aovias.DeploymentValidation.Tests/Aovias.DeploymentValidation.Tests.csproj` — full suite | xUnit; Reliability tests use mocks/fakes (no live Docker requirement observed in run); 309 tests in ~592 ms |
| 6 | Release builds | `Aovias.Configuration.csproj`, `Aovias.Publishing.Core.csproj`, `Aovias.DeploymentValidation.csproj` | `dotnet build -c Release` only; no `Publish` target invoked |

**Not executed:** Full Platform slnx (historical **NU1903** — out of scope). Bootstrap Release build (NU1903 noted in 1G-F; not required for this Stage A scope).

---

## 3. Stage A test execution (Main)

Working directory for all commands: `V:\DevelopmentRepo\AOVIAS`.

| Step | Command | Failed | Passed | Skipped | Total | Exit |
|------|---------|--------|--------|---------|-------|------|
| 1 | `dotnet test Aovias.Configuration/tests/Aovias.Configuration.Tests/Aovias.Configuration.Tests.csproj --filter "FullyQualifiedName~Layout"` | 0 | **39** | 0 | 39 | 0 |
| 2 | `dotnet test Aovias.Publishing/tests/Aovias.Publishing.Core.Tests/Aovias.Publishing.Core.Tests.csproj --filter "FullyQualifiedName~PublishingProjectPathsTests"` | 0 | **2** | 0 | 2 | 0 |
| 3 | Same project, `--filter "FullyQualifiedName~ConfigTemplateGenerator"` | 0 | **6** | 0 | 6 | 0 |
| 4 | Same project, `--filter "FullyQualifiedName~RuntimeAgentDockerContextStager"` | 0 | **2** | 0 | 2 | 0 |
| 5 | `dotnet test Aovias.DeploymentValidation/tests/Aovias.DeploymentValidation.Tests/Aovias.DeploymentValidation.Tests.csproj` | 0 | **309** | 0 | 309 | 0 |

**Aggregate Stage A tests:** **358 passed**, 0 failed, 0 skipped.

**Output locations:** Standard test/build outputs under respective project `bin/Debug/net10.0/` and `obj/` trees (Configuration, Publishing.Core, DeploymentValidation, transitive references).

**Failure handling:** No failures; dependent validation was not stopped early.

---

## 4. Release builds

| Command | Result | Warnings | Errors |
|---------|--------|----------|--------|
| `dotnet build Aovias.Configuration/Aovias.Configuration.csproj -c Release` | Succeeded | 0 | 0 |
| `dotnet build Aovias.Publishing/src/Aovias.Publishing.Core/Aovias.Publishing.Core.csproj -c Release` | Succeeded | 0 | 0 |
| `dotnet build Aovias.DeploymentValidation/Aovias.DeploymentValidation.csproj -c Release` | Succeeded | 0 | 0 |

**NU1903:** Not observed on these three projects. Historical NU1903 on Platform/CSM slnx remains **unverified** at this HEAD and is **not** treated as a Stage A regression.

**Artifacts:** `bin/Release/net10.0/` under built projects and dependencies.

---

## 5. Read-only layout marker check

| Check | Result |
|-------|--------|
| `<repo>/aovias-config` exists | **False** |
| Infra template `Dockerfile.aspnet` exists | **True** |
| Config `storage/` created by validation | **False** (unchanged before/after `--discover` attempt) |

**Classification:** **Statically reviewed** (filesystem); consistent with 1H-A.

---

## 6. `--discover` capability assessment

### 6.1 Static inspection (call graph)

**Entry:** `tools/PublishAllServices/Program.cs`

| Phase | Behavior | Mutating? |
|-------|----------|-----------|
| L9 | `ResolvePlatformRoot()` → `RepositoryLayoutProductionAdapter.ResolvePublishingPlatformRoot(AppContext.BaseDirectory)` | **Read-only** walk / manifest (post–1G-D hardened) |
| L18–37 | Build DI + `BuildAoviasConfiguration(...)` | **Validates** config root; **throws** if `profiles/` missing |
| L41–50 (`--discover`) | `client.DiscoverDeployableProjectsAsync(platformRoot)` then `Console.WriteLine` | **Intended read-only** enumeration |

**Discover implementation:** `SolutionDeployableProjectDiscoverer.DiscoverAsync` — enumerates `*.csproj`, reads metadata, builds in-memory list; **no** `File.Write` / `Directory.Create` / delete in `Aovias.Publishing.Core/Discovery/` (grep verified).

**Early return:** When `--discover` is set, program **returns before** preflight (L62+), publish (L99+), or output directory use for packages (L53–58 not reached for discover-only path after successful bootstrap).

**Contrast (not run):**

- `--preflight-only` — may write under `OutputDirectory` / run Docker probes (**Stage B**).
- Default publish — writes packages under config `storage/` (**prohibited**).

### 6.2 Execution attempt

**Command:**

```powershell
dotnet run --project "V:\DevelopmentRepo\AOVIAS\tools\PublishAllServices\PublishAllServices.csproj" -c Release -- --discover
```

**Result:** **Failed before discover logic** — `AoviasConfigurationBootstrapException`:

```text
Configuration root: V:\DevelopmentRepo\AOVIAS\AOVIAS.Infrastructure\aovias-config
Profiles directory not found: ...\aovias-config\profiles
Active profile: local
```

**Stack:** `AoviasPlatformBootstrapResolver.Validate` → `ServiceCollectionExtensions.BuildAoviasConfiguration` → `Program.cs` L18 ( **before** L41 `discoverOnly` branch).

**Side effects observed:** None on repo config tree (`storage/` still absent). No packages, no publish dirs.

**Exit code:** Non-zero (process crash).

### 6.3 Conclusions

| Question | Answer | Evidence status |
|----------|--------|-----------------|
| Is `--discover` branch non-mutating if reached? | **Yes** (design) | **Statically reviewed** |
| Does CLI use hardened platform root resolution? | **Yes** (always at L9) | **Statically reviewed** |
| Was discover output produced on this repo? | **No** | **Unverified** (bootstrap blocker) |
| Is failure a layout/W1 defect? | **No** — missing **runtime config layout** (`profiles/`), not template migration | **Runtime-proven** (exception message) |

**Deferral reason:** Executing discover requires either (a) a populated config root with `profiles/` (typically gitignored / machine-local), or (b) a **separate authorized** CLI change to defer bootstrap until after discover (not in scope).

**Not run:** `--preflight-only`, publish, `build-praman-aovpkg.ps1`, Docker, DRV.

---

## 7. Evidence classification summary

| Area | Status after 1H-C |
|------|-------------------|
| Layout / discovery / 1G-D hardening | **Test-proven** (39 layout tests) |
| Publishing paths & config templates | **Test-proven** (2 + 6 tests) |
| Runtime Agent Docker stager | **Test-proven** (2 tests; includes temp `%TEMP%` stage) |
| DeploymentValidation contracts/runtime | **Test-proven** (309 tests) |
| Configuration / Publishing.Core / DeploymentValidation Release build | **Build-proven** (0 warnings on these projects) |
| Publish CLI `--discover` enumeration | **Unverified** (bootstrap prerequisite) |
| Full publish / preflight / Docker / DRV | **Unverified** (not authorized) |
| NU1903 on Platform/CSM slnx | **Unverified** (historical; separate track) |

---

## 8. Generated artifacts (not deleted)

- `Aovias.Configuration/bin/`, `obj/` (Debug + Release)
- `Aovias.Publishing/**/bin/`, `obj/` (Debug + Release, test build graph)
- `Aovias.DeploymentValidation/bin/`, `obj/` (Debug + Release)
- Transitive dependency `bin/obj` from test/build graph (Orchestration, Release SDK, Runtime Agent, CommandCenter infra, etc.)
- `tools/PublishAllServices/bin/`, `obj/` from `dotnet run` build

Test staging dirs under `%TEMP%` from stager tests were removed by test `finally` blocks (test behavior, not agent cleanup).

---

## 9. Recommended next phase

**Proposed: Phase 1I-A — Discovery execution with isolated config root (Stage B-lite)**

1. **Separate authorization** to create a **temporary** config root (e.g. `%TEMP%\aovias-discover-{guid}\AOVIAS.Infrastructure\aovias-config\`) with minimal `profiles/local/` copied or symlinked from an operator-approved source — **without** touching production gitignored storage.
2. Set `AOVIAS_CONFIG_ROOT` / bootstrap env so `PublishAllServices --discover` passes validation, then re-run **only** `--discover`.
3. If discover succeeds, optional **separate authorization** for `--preflight-only` with `Publishing:OutputFolder` under `%TEMP%` (1H-B Stage B2).

**Why not broader refactor:** Stage A closes 1H-B gaps R-14 (DeploymentValidation + stager) and confirms no regression on merged layout work; remaining risk is **CLI bootstrap coupling**, not W1 path resolution.

**Must fix before rollout:** None identified from test failures.

---

## 10. Prohibited operations (honored)

No full publish, praman script, Docker image build, DRV/bootstrap, VPS validation, probe execution, Git writes, or synchronization.

---

## 11. Deliverable

| Report | Path |
|--------|------|
| Stage A validation | `AOVIAS_PHASE1HC_STAGE_A_VALIDATION_REPORT.md` (untracked on Main) |

**Final status:** **STAGE A PARTIALLY PASSED — VALIDATION GAPS REMAIN**
