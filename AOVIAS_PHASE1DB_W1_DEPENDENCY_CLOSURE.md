# AOVIAS Phase 1D-B — W1 Dependency Closure (`aovias-config`)

**Date:** 2026-10-25  
**Baseline HEAD:** `5a1526d99a68241d40ccba759d8fc62586f4af86`  
**Proposed move:** `aovias-config/` → `AOVIAS.Infrastructure/aovias-config/`  
**Scope:** Audit only — **no moves or code changes in Phase 1D-B**

---

## 1. Move scope (verified on disk)

| Item | Current path | Notes |
|------|--------------|--------|
| Config templates (tracked) | `aovias-config/templates/publishing/Dockerfile.*` (4 files) | Glob + repo index |
| Nginx template | `aovias-config/templates/nginx/runtime-agent.site.conf.template` | Tracked |
| Gitignored runtime data | `aovias-config/secrets/`, `storage/`, `logs/`, `certificates/` | `.gitignore` L79–82 — **must not be deleted** on move; migrate or leave local |
| `AOVIAS.Infrastructure/` | **Does not exist** today | Create parent as part of W1 |
| AccredaOne scaffold | `AOVIAS.AccredaOne/` | **Out of W1 scope** — do not move |

**Verified:** Root discovery treats **`aovias-config` as a directory marker** at repository root (not only file contents).

---

## 2. Dependency closure table (blocking and non-blocking)

Legend: **Blocker** = W1 fails or mis-resolves without same-change fix. **High** = must update in W1 bundle. **Med/Low** = same bundle or follow-up.

### 2.1 Root discovery and layout contract

| File | Symbol / key | Current assumption | Proposed behavior | Change | Blocker |
|------|--------------|-------------------|-------------------|--------|---------|
| `Aovias.Configuration/Layout/LegacyPublishingPlatformRootDiscovery.cs` | `Resolve` | `aovias-config` at repo root | Dual-marker: root `aovias-config` **or** `AOVIAS.Infrastructure/aovias-config` during transition; then target only | Edit legacy + tests | **Yes** |
| `Aovias.Configuration/Layout/LegacyCsmPlatformRootDiscovery.cs` | `Resolve` | `aovias-config` + `Aovias.Runtime.Agent` at root | Same dual-marker for config path | Edit | **Yes** |
| `Aovias.Configuration/Layout/RepositoryLayoutProductionAdapter.cs` | `TryResolve*` | Manifest `discovery.current.*.requiredAll` includes `aovias-config` | Update manifest **current** markers to new relative path; production uses Current only | Manifest + loader tests | **Yes** |
| `repository-layout.json` | `discovery.current/target` | `requiredAll`: `aovias-config` | `AOVIAS.Infrastructure/aovias-config` for target; current updated post-move | JSON | **Yes** |
| `Aovias.Publishing/.../PlatformRootResolver.cs` | `Resolve` | Adapter → legacy publishing walk | No API change; inherits legacy/adapter fix | Indirect | **Yes** |
| `ComplianceServiceManager/.../RootPathResolver.cs` | `ResolvePlatformRoot` | Adapter → legacy CSM walk | Indirect | **Yes** |
| `Aovias.Publishing/.../SolutionPublishingPreflightService.cs` | Walk ~L835 | `aovias-config` folder name at ancestor | Update walk or use layout helper | Edit | **High** |
| `SourceDiscoveryBoundary` | — | No direct `aovias-config` | **No W1 change** | — | No |
| `SolutionDeployableProjectDiscoverer.GetScanRoots` | — | `ComplaincePlatform`, `Aovias.Runtime.Agent` | **No W1 change** | — | No |
| `RuntimeAgentDockerContextStager` | COPY list | No `aovias-config` in Dockerfile COPY | **No W1 change** to stager for config tree | — | No |

### 2.2 ConfigRoot path construction (runtime / CLI)

| File | Symbol / key | Current assumption | Proposed | Change | Blocker |
|------|--------------|-------------------|----------|--------|---------|
| `tools/PublishAllServices/Program.cs` | `Aovias:ConfigRoot`, `outputDirectory`, `ResolvePlatformRoot` | `Path.Combine(platformRoot, "aovias-config")` | `{platformRoot}/AOVIAS.Infrastructure/aovias-config` or shared helper | Edit | **Yes** |
| `tools/PublishRuntimeAgent/Program.cs` | Same pattern | Same | Same | Edit | **Yes** |
| `Aovias.Bootstrap/.../Program.cs` | `ConfigRoot` default | `V:/DevelopmentRepo/V1.0/aovias-config` (legacy dev path) | Layout-aware repo-relative default + `AOVIAS_CONFIG_ROOT` | Edit | **High** (wrong even pre-W1 on many machines) |
| `ComplianceServiceManager/appsettings.json` | `ConfigRoot` | `V:/DevelopmentRepo/V1.0/aovias-config` | Example path under new layout or env-only | Edit | **Med** (dev config) |
| `Aovias.Configuration/Loading/AoviasPlatformBootstrapResolver.cs` | `DefaultConfigRoot` | `/opt/aovias/runtime/config` | Unchanged for deployed hosts; repo dev uses env | **No path move** | No |
| `Aovias.Runtime.Agent/.../appsettings.Development.json` | Config paths | Contains repo-specific segments | Review/update dev example | Edit | **Med** |

### 2.3 Scripts and packaging

| File | Current assumption | Proposed | Blocker |
|------|-------------------|----------|---------|
| `ComplaincePlatform/scripts/build-praman-aovpkg.ps1` | `Join-Path $repoRoot "aovias-config\storage\local\packages\praman"` | New base under Infrastructure | **High** |
| `aovias-config/scripts/*` (if tracked) | Relative to config root | Move with tree | **High** |

### 2.4 Tests and fixtures

| File | Assumption | Change | Blocker |
|------|------------|--------|---------|
| `Aovias.Configuration/tests/.../Layout/*Tests.cs` | Fixtures create `aovias-config` at root | Add post-W1 fixture paths; keep legacy transition tests | **Yes** |
| `Aovias.Publishing/tests/.../ConfigTemplateGeneratorTests.cs` | `V:/DevelopmentRepo/V1.0/aovias-config` | Update golden paths | **High** |
| `Aovias.DeploymentValidation/tests/.../DeploymentValidationTests.cs` | `DeveloperConfigRoot` constant | Update | **High** |
| `Aovias.DeploymentValidation/tests/.../BootstrapValidationTests.cs` | JSON paths under V1.0 | Update | **High** |
| `Aovias.Publishing/tests/.../ContractDrivenPublishingTests.cs` | Excludes `aovias-config/` from scans | Path segment still valid under Infrastructure | **Med** |
| `Aovias.Publishing/tests/.../EnvironmentNeutralPortabilityRegressionTests.cs` | May reference layout | Review | **Med** |

### 2.5 Docker, Compose, CI

| Artifact | `aovias-config` dependency | W1 impact |
|----------|---------------------------|-----------|
| `Aovias.Runtime.Agent/Dockerfile` | Does **not** COPY `aovias-config` | **No** direct W1 Dockerfile change |
| Command/Website Compose | No bind to `aovias-config` at repo root | **No** |
| `ComplaincePlatform/.github/workflows/platform-integrity.yml` | No reference | **No** |
| Website CI | No reference | **No** |

### 2.6 Documentation (non-blocking for build)

| Area | Examples | Change |
|------|----------|--------|
| Phase 0/1 reports | Many references | Update over time |
| `docs/milestones/*` | Storage under `aovias-config/storage` | Doc-only |
| `ComplianceServiceManager/docs/sprint5/VPS-Bootstrap-Guide.md` | Example paths | Med |

---

## 3. Git-tracked reference search (summary)

**Verified:** `git grep -l "aovias-config"` on tracked `*.cs`, `*.ps1`, `*.json`, `*.yml` → **16 code/config files** (excluding Phase report markdown).

**Additional hits** in untracked Phase markdown and assessment docs — not execution blockers.

**Templates tracked:** at least **5** files under `aovias-config/templates/` (publishing + nginx).

---

## 4. Gaps / residual closure risk

| Gap | Severity |
|-----|----------|
| Full scan of **untracked** local `aovias-config/storage/**` dependencies | Med — operator must preserve gitignored data across move |
| Every `deployment.metadata.json` referencing config output paths | Med — deferred INV-2 |
| Generated packages referencing old paths on disk | Med — republish after move |
| Human/VPS deployments using `AOVIAS_CONFIG_ROOT` pointing at old path | **High** — runbook update required |

---

## 5. W1 atomic bundle (must ship together)

1. Create `AOVIAS.Infrastructure/` and `git mv aovias-config` → `AOVIAS.Infrastructure/aovias-config`.
2. Update **legacy discovery** + **manifest current markers** + **Publish CLI** path combines.
3. Update **tests** listed as Blocker/High.
4. **Commit** `repository-layout.json` (and layout code) so all worktrees receive manifest on pull/ff.
5. Optional transitional dual-marker in legacy (old + new path) for one release — recommended.

**Do not** move without resolver/path updates in the **same integration merge**.

---

*Dependency closure audit complete. No physical migration performed in Phase 1D-B.*
