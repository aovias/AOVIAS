# AOVIAS Phase 1G-A — Post-W1 Runtime and Deployment Contract Audit

**Date:** 2026-10-25  
**Scope:** Read-only evidence gathering after W1 (`aovias-config` → `AOVIAS.Infrastructure/aovias-config/`)  
**Integration HEAD audited:** `202dd525e8661ca0c3db68b40ab3a888140b79cf`  
**W1 implementation commit:** `a3188c91975b7d0ca444c9478c0b1590eb747837`

---

## 1. Worktree baseline and preservation

| Worktree | Path | Branch | HEAD | Index | W1 in ancestry |
|----------|------|--------|------|-------|----------------|
| Main | `V:\DevelopmentRepo\AOVIAS` | `main` | `202dd525…` | Clean | Yes (`a3188c9` ancestor) |
| Cursor | `V:\DevelopmentRepo\AOVIAS.Cursor` | `agent/cursor/active` | `202dd525…` | Clean | Yes |
| Claude | `V:\DevelopmentRepo\AOVIAS.Claude` | `agent/claude/active` | `202dd525…` | Clean | Yes |

**Untracked / local-only (preserved, not staged):**

| Artifact | Location | Present |
|----------|----------|---------|
| Runtime probe | Cursor `scripts/dev/_phase3h_runtime_probe.py` | Yes (untracked) |
| Phase 1F-B/C/D reports | Cursor worktree | Yes (untracked) |
| Claude launch settings | `Aovias.CommandCenter/.../Properties/` | Yes (untracked) |
| Main Phase reports, ZIPs, Lighthouse | Main worktree | Yes (untracked) |

**Runtime probe:** Not executed during this audit.

Baseline matches expectations. No preservation risk blocking investigation.

---

## 2. Historical reports (evidence map)

| Report | Location | Used for |
|--------|----------|----------|
| `AOVIAS_PHASE1FA_W1_MIGRATION_REVALIDATION.md` | Main | Pre-W1 dependency inventory (partially superseded by W1 merge) |
| `AOVIAS_PHASE1FB_W1_IMPLEMENTATION_REPORT.md` | **Cursor only** | Implementation scope, test commands |
| `AOVIAS_PHASE1FC_W1_DIFF_REVIEW.md` | **Cursor only** | 26-path review |
| `AOVIAS_PHASE1FD_W1_COMMIT_REPORT.md` | **Cursor only** | Post-commit validation on task branch |
| `AOVIAS_PHASE1FE_W1_MERGE_*` | Main | Merge to `main` |
| `AOVIAS_PHASE1FF_SYNC_PREFLIGHT.md` | Main | Agent ff-sync plan |
| `AOVIAS_PHASE1FG_SYNC_EXECUTION_REPORT.md` | Main | Agent alignment to `202dd525` |
| `AOVIAS_PHASE1DB_W1_EXECUTION_AND_ROLLBACK_PLAN.md` | Main | Rollback / operational-data steps |

Missing on Main: 1F-B/C/D (available on Cursor only). Conclusions below prefer **current source + tests run in this audit** over stale pre-W1 narrative in 1F-A.

---

## 3. Manifest and root-resolution contract

### 3.1 Current manifest (`repository-layout.json`)

- **`discovery.current.platformRoot`:** `requiredAll`: [`AOVIAS.Infrastructure/aovias-config`]; `requiredAny`: [`ComplaincePlatform`, `Aovias.Runtime.Agent`] (L188–191).
- **`discovery.current.csmPlatformRoot`:** `requiredAll`: [`AOVIAS.Infrastructure/aovias-config`, `Aovias.Runtime.Agent`] (L193–195).
- **`products[infrastructure].paths.aoviasConfig.current`:** `AOVIAS.Infrastructure/aovias-config` (L129–131).
- **`compatibilityConstraints[0]`:** Documents Publishing/CSM adapter + Current profile + dual-marker legacy fallback (L241–242).

**Verified (code):** `RepositoryLayoutProductionAdapter` uses **Current only** (`IsPermittedProductionProfile`, L12–13; manifest load via `TryLoadForProduction`, L28–36). Target profile is rejected for production (test: `Production_adapter_never_permits_target_profile`).

**Verified (code):** `PlatformRootResolver.Resolve` — `configuredRoot` wins when directory exists (L12–15); else `ResolvePublishingPlatformRoot(AppContext.BaseDirectory)`.

**Verified (code):** `RootPathResolver.ResolvePlatformRoot` → `ResolveCsmPlatformRoot` only; **CompliancePlatform/WebApp** resolvers remain separate directory walks (not manifest-driven).

**Verified (code):** Missing/invalid manifest → `TryResolve*` false → legacy discovery (`RepositoryLayoutProductionAdapter` L54–59, L100–105). Invalid JSON tested (`ResolvePublishingPlatformRoot_falls_back_when_manifest_invalid`).

**Verified (code):** Nearest matching ancestor wins (`RepositoryLayoutPathResolver.TryResolveRepositoryRoot` L19–20, L33–40; test `TryResolveRepositoryRoot_prefers_nearest_ancestor_when_multiple_match`).

**Verified (code):** Path escape rejected (`CombineUnderRepositoryRoot_rejects_escape_attempts`).

**Verified (integration test):** `Integration_manifest_current_markers_match_post_w1_repository_layout` asserts live repo matches Current publishing + CSM markers and **no** root-level `aovias-config` directory (W1TransitionTests L70–87).

**Static inference:** When legacy walk finds no match, Publishing legacy returns `Path.GetFullPath(Combine(base, "..", x5))` (LegacyPublishingPlatformRootDiscovery L24); CSM returns four `..` segments (L23). These are **not proven safe** end-to-end — **NOT PROVEN** as correct platform roots.

**Config root vs platform root:** `RepositoryLayoutAoviasConfigPaths.ResolveConfigRootDirectory` prefers infrastructure, then legacy, else returns infrastructure path even if missing (L40–51) — used by tools/Bootstrap for **config tree**, not platform root discovery.

---

## 4. Publishing vs CSM — intentional differences

| Aspect | Publishing | CSM |
|--------|------------|-----|
| Production entry | `PlatformRootResolver` → `ResolvePublishingPlatformRoot` | `RootPathResolver.ResolvePlatformRoot` → `ResolveCsmPlatformRoot` |
| Manifest markers | Config + (**ComplaincePlatform** OR **Runtime.Agent**) | Config + **Runtime.Agent** (both required) |
| Legacy walk | Dual-marker config + (ComplaincePlatform OR Runtime.Agent) | Runtime.Agent + dual-marker config |
| `configuredRoot` | Supported (Publishing only) | **Not** on `ResolvePlatformRoot` |

Documented in `repository-layout.json` `compatibilityConstraints[0]` and manifest `discovery.current` blocks.

---

## 5. Scenario matrices

Legend: **V-C** = verified from code; **V-T** = verified by automated test; **N-T** = not covered by tests; **N-P** = not proven in this audit.

### 5.1 Publishing

| Scenario | Outcome | Evidence |
|----------|---------|----------|
| Valid infrastructure root only | Resolves repo root | **V-T** W1 `Legacy_publishing_resolves_when_only_infrastructure_config_exists`; manifest fixture uses infra config (ProductionAdapterTests `CreateCurrentLayoutFixture`) |
| Valid legacy root only | Resolves repo root | **V-T** W1 `Legacy_publishing_resolves_when_only_legacy_root_config_exists` |
| Both config layouts valid | Same repo root; config helper prefers infra | **V-T** `ResolveConfigRootDirectory_prefers_infrastructure_when_both_exist`; manifest matches if infra dir exists (**V-C** `MatchesPlatformRoot` on `requiredAll` infra path) |
| Infrastructure config but missing ComplaincePlatform & Runtime.Agent | Manifest fails; legacy walk fails → fallback `..` path | **V-C** marker logic; **N-T** for final fallback path quality |
| Legacy config only (no infra dir) | Manifest fails (requires infra in `requiredAll`); legacy succeeds if app marker present | **V-C** + **V-T** legacy-only test |
| No valid root | Non-empty string from legacy `..` fallback | **V-C** L24; **N-P** safe behavior |
| Manifest missing / invalid | Legacy publishing walk | **V-T** `TryResolvePlatformRepositoryRoot_returns_false_when_manifest_missing`; invalid JSON fallback test |
| Multiple nested matching roots | Nearest ancestor wins | **V-T** `TryResolveRepositoryRoot_prefers_nearest_ancestor_when_multiple_match` |
| `configuredRoot` set and exists | Uses configured path | **V-C** PlatformRootResolver L12–15; **N-T** dedicated test in this audit |

### 5.2 CSM

| Scenario | Outcome | Evidence |
|----------|---------|----------|
| Valid infrastructure root only (+ Runtime.Agent) | Resolves repo root | **V-T** W1 `Legacy_csm_requires_runtime_agent_with_either_config_layout` (infra branch) |
| Valid legacy config only (+ Runtime.Agent) | Resolves repo root | **V-T** same test (legacy branch) |
| Both config layouts | Same platform root; config helper prefers infra | **V-T** config preference test; **N-T** explicit CSM manifest with both config dirs |
| Infra config dir missing, legacy present | Manifest fails; legacy can still resolve | **V-C** + **V-T** legacy CSM with legacy config |
| Runtime.Agent missing | Manifest CSM fails; publishing may still resolve | **V-T** `Csm_does_not_resolve_when_runtime_agent_missing` |
| Config missing, agent present | No manifest CSM root | **V-T** `Csm_does_not_resolve_when_aovias_config_missing` |
| Neither marker | TryResolve false; legacy still returns non-empty | **V-T** `Csm_does_not_resolve_when_neither_marker_present` (legacy non-empty) |
| Manifest missing | Legacy CSM | **V-T** `Csm_falls_back_to_legacy_when_manifest_missing` |
| Multiple nested roots | Nearest wins (shared PathResolver) | **V-T** contract test (publishing resolver; same algorithm for CSM TryResolveCsmRepositoryRoot) |

---

## 6. Publishing scripts, packages, DeploymentValidation

| Workflow | Repo root | Config root | Templates / outputs | Legacy assumption? | Test / evidence |
|----------|-----------|-------------|---------------------|--------------------|-----------------|
| `tools/PublishAllServices/Program.cs` | Walk: dual-marker config + ComplaincePlatform (L232–246) | `RepositoryLayoutAoviasConfigPaths.ResolveConfigRootDirectory` (L21, L54–58) | Packages under `{configRoot}/storage/local/packages` | Dual-marker walk, not root-only | **V-C**; **N-P** CLI execution |
| `tools/PublishRuntimeAgent/Program.cs` | Same pattern as PublishAllServices (grep/read) | `ResolveConfigRootDirectory` (L26) | Publishing client output | Updated | **V-C** |
| `SolutionPublishingPreflightService` | Uses discoverer / platform root | Dockerfile walk uses `ConfigDirectoryExistsAtRepositoryRoot` + ComplaincePlatform (L836 area) | Preflight outputs | Dual-marker | **V-C**; preflight not executed |
| `build-praman-aovpkg.ps1` | `$repoRoot` from script location (L8) | Prefers `$infraConfigRoot`, else legacy (L9–11) | `storage\local\packages\praman` under chosen root | Explicit legacy fallback | **V-C** L9–11; script **not executed** (would publish/delete `.publish`) |
| `ConfigTemplateGeneratorTests` | N/A | Fixture uses `AOVIAS.Infrastructure/aovias-config` (grep) | Generated templates | Updated in W1 | **V-T** (historical 1F-D + repo source) |
| `ComplianceServiceManager/appsettings.json` | N/A | `"ConfigRoot": "AOVIAS.Infrastructure/aovias-config"` (L3) | Profile-relative secrets | **Active new-layout** | **V-C** |
| `Aovias.Bootstrap.Host/Program.cs` | `ResolveLocalPlatformRoot` dual-marker walk (L437–448) | Default `ResolveConfigRootDirectory(ResolveLocalPlatformRoot())` (L431–433) | Remote deploy paths | No hardcoded `V1.0` in default path | **V-C** |
| `Aovias.Runtime.Agent/appsettings.Development.json` | N/A | Updated in W1 commit | Dev settings | **V-C** (in 26-path set) |
| `DeploymentValidationStages.cs` | Package validation | Flags **developer** output containing `V:/DevelopmentRepo/V1.0/aovias-config` (L538–539) | Warns on stale absolute paths in generated output | **Intentional guard**, not layout resolver | **V-C** |

**Not run in this audit:** full publish, package generation, DeploymentValidation pipeline, Bootstrap deploy/bootstrap CLI.

---

## 7. Docker, scan roots, source-discovery boundaries

| Component | Config tree dependency | W1 impact | Evidence |
|-----------|------------------------|-----------|----------|
| `RuntimeAgentDockerContextStager` | **None** — copies `Aovias.*` folders from platform root (L72–79) | Unchanged; still valid | **V-T** `RuntimeAgentDockerContextStagerTests` (stage includes DeploymentValidation) |
| `Aovias.Runtime.Agent/Dockerfile` | **No** `aovias-config` COPY | Unchanged | **V-C** grep COPY lines |
| `SolutionDeployableProjectDiscoverer.GetScanRoots` | Scans `ComplaincePlatform`, `Aovias.Runtime.Agent` only (L138–141) | No config path | **V-C** |
| `SourceDiscoveryBoundary` | Default roots `ComplaincePlatform`, `WebApp` (L50–57) | No config path | **V-C** |
| Compose / `.github` | — | No `aovias-config` matches | **V-C** grep (empty) |

**Static inference:** Docker/runtime config at deploy time comes from **mounted config root** (Bootstrap/env), not from copying `AOVIAS.Infrastructure/aovias-config` into the image — consistent with W1 moving tracked **templates** only.

**Gap:** End-to-end Docker build from clean CI agent **NOT PROVEN** in this audit (no build executed).

---

## 8. Classified legacy-reference inventory (tracked source)

Summary: **`git grep aovias-config`** on tracked `*.cs`, `*.json`, `*.ps1` → **20 files** (excluding pure path segments in infrastructure string). Full classification:

### 8.1 Intentional compatibility / transition

| Path | Lines / keys | Rationale |
|------|----------------|-----------|
| `RepositoryLayoutAoviasConfigPaths.cs` | `LegacyRootRelativeDirectory`, dual-marker APIs | Rollback / coexistence |
| `LegacyPublishingPlatformRootDiscovery.cs` | Uses `ConfigDirectoryExistsAtRepositoryRoot` | Legacy fallback |
| `LegacyCsmPlatformRootDiscovery.cs` | Same | Legacy fallback |
| `build-praman-aovpkg.ps1` | L10–11 legacy path fallback | Transition |
| `.gitignore` | L79–86 both path prefixes | Ignore ops data at old and new roots |

### 8.2 Active new-layout reference

| Path | Notes |
|------|--------|
| `repository-layout.json` | Current markers → `AOVIAS.Infrastructure/aovias-config` |
| `ComplianceServiceManager/appsettings.json` | Repo-relative infra config root |
| `ConfigTemplateGeneratorTests.cs` | Infra path in JSON fixture |
| `RegressionTests.cs`, `EnvironmentNeutralPortabilityRegressionTests.cs` | Relative walk to `AOVIAS.Infrastructure/aovias-config` |
| `Aovias.Runtime.Agent/.../appsettings.Development.json` | W1-updated |

### 8.3 Test fixtures (legacy or synthetic)

| Path | Notes |
|------|--------|
| `RepositoryLayout*Tests.cs` | Temp dirs `aovias-config` and/or infra paths |
| `LoggingSettingsBinderTests.cs` | Relative path to config tree |
| `DeploymentValidationTests.cs`, `BootstrapValidationTests.cs` | **`V:/DevelopmentRepo/V1.0/aovias-config`** — synthetic developer machine |
| `EnvTemplateValidationRulesTests.cs` | Generic `/path/to/aovias-config` |
| `ContractDrivenPublishingTests.cs` | Excludes segment `aovias-config` from scans |
| `AoviasPlatformBootstrapResolverTests.cs` | Temp dir name prefix |

### 8.4 Stale / historical documentation (non-blocking for runtime)

| Path | Disposition |
|------|-------------|
| `docs/milestones/*.md`, `AOVIAS_PHASE*.md` (Phase 0–1E) | **Historical documentation** — many still mention root `aovias-config`; update in docs-only follow-up |
| `ComplianceServiceManager/docs/sprint5/VPS-Bootstrap-Guide.md` | `/opt/aovias-config` — deployment convention |

### 8.5 Guard / validation (not layout resolver)

| Path | Notes |
|------|--------|
| `DeploymentValidationStages.cs` L538–539 | Fails validation if package output embeds obsolete **V1.0** absolute path |

### 8.6 Unclear / follow-up

| Item | Notes |
|------|--------|
| `ResolveConfigRootDirectory` when neither dir exists | Returns infra path non-existent (**V-C** L51) — callers may fail later; **N-T** |
| Milestone references to packages under old `aovias-config/storage` | May exist on other machines; not in these worktrees |

**No automated search-and-replace performed** (per audit scope).

---

## 9. Operational data and rollback

### 9.1 On-disk status (all three worktrees)

Scanned: `{aovias-config|AOVIAS.Infrastructure/aovias-config}/{storage,secrets,logs,certificates}`.

**Result:** **No operational directories present** on Main, Cursor, or Claude (no files to inventory).

Both path prefixes listed in `.gitignore` (L79–86). Gitignored content would **not** move with `git mv`; W1 merge moved **tracked templates only**.

### 9.2 Rollback plan assessment (`AOVIAS_PHASE1DB_W1_EXECUTION_AND_ROLLBACK_PLAN.md`)

| Rollback concern | Covered in plan? | Post-W1 note |
|------------------|------------------|--------------|
| Revert code/manifest | Yes | Merge commit `202dd525` revertible |
| Restore templates to root | Yes | `git revert` restores tracked layout |
| Dev configuration | Partially | CSM appsettings already infra-relative; some tests still use V1.0 paths |
| Operational data / permissions | Yes (manual backup/move) | **N/A on current worktrees**; still required on envs with gitignored data |
| Publishing/CSM discovery after rollback | Dual-marker code supports legacy root | **V-T** legacy-only scenarios |
| Publishing/package/staging validation | Listed | **Not re-run** in this audit |
| Stale root selection | Dual-marker + manifest Current | Revert would restore manifest to pre-W1 in same commit as code |

**Physical-move readiness:** On these worktrees, **nothing blocked** (no ops data). For machines with legacy `aovias-config/storage`, physical move remains **blocked until backup/inventory** per 1D-B plan.

---

## 10. Validation evidence

### 10.1 Executed in this audit (Main worktree)

| Command | Exit | Result |
|---------|------|--------|
| `dotnet test Aovias.Configuration.Tests.csproj --filter FullyQualifiedName~Layout` | 0 | **34/34 passed** |
| `dotnet test Aovias.Publishing.Core.Tests.csproj --filter FullyQualifiedName~PublishingProjectPathsTests` | 0 | **2/2 passed** |

### 10.2 Historical (not re-run here, cited accurately)

| Source | Claims |
|--------|--------|
| `AOVIAS_PHASE1FE_W1_MERGE_EXECUTION_REPORT.md` | Layout 34/34, Publishing paths 2/2, ConfigTemplateGenerator 6/6, Publishing.Core Release build, `git diff --check` clean |
| `AOVIAS_PHASE1FG_SYNC_EXECUTION_REPORT.md` | Same layout + publishing path tests on Cursor after sync |

**Not executed in 1G-A or cited as run:** Docker build, full publish, praman script, DeploymentValidation suite, Bootstrap CLI, runtime probe.

---

## 11. Defects, gaps, and follow-up

| ID | Severity | Finding | Recommendation |
|----|----------|---------|----------------|
| G1 | Low | Phase/milestone docs still describe root `aovias-config` | Docs-only cleanup task |
| G2 | Low | DeploymentValidation tests use fixed `V1.0` paths | Update fixtures to infra-relative or temp dirs |
| G3 | Medium | Legacy fallback returns arbitrary `..` path when no markers | Document operational risk; optional hardening (separate auth) |
| G4 | Info | End-to-end publish/Docker not verified post-merge on CI | Run controlled publish preflight in future phase |
| G5 | Info | Ops data migration | Required only where gitignored dirs exist (not here) |

**No confirmed critical breakage** in manifest-driven Current profile, dual-marker legacy fallback, tools, or unchanged Docker/discovery boundaries for the **current** integrated tree.

---

## 12. Evidence quality summary

| Category | Examples |
|----------|----------|
| **Verified behavior** | Current manifest markers; production Current-only; W1 transition tests; layout 34/34; infra template paths tracked |
| **Static inference** | Docker deploy config via mount; preflight/package CLI success |
| **Not tested / not proven** | Full publish pipeline; Docker CI build; legacy `..` fallback when no roots; nested repo with conflicting partial markers |

---

## 13. Final status

**PASS WITH FOLLOW-UP ITEMS — NO CRITICAL BREAKAGE FOUND**

Post-W1 contracts for manifest Current profile, Publishing/CSM discovery (with documented differences), publishing tools, and unchanged Docker/source-discovery boundaries are **consistent with source and focused tests**. Remaining items are documentation/fixture hygiene, optional resolver hardening, and environment-specific operational-data handling—not blockers on the integrated `202dd525` baseline on Main/Cursor/Claude.

---

## 14. Git writes in Phase 1G-A

**None** (this report is untracked on Main unless separately committed by a human).
