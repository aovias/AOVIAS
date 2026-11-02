# AOVIAS Phase 1G-D — Legacy Repository-Root Discovery Hardening Report

**Date:** 2026-10-25  
**Worktree:** `V:\DevelopmentRepo\AOVIAS.Cursor`  
**Branch:** `agent/cursor/phase1gc-documentation-correction`  
**Starting HEAD:** `202dd525e8661ca0c3db68b40ab3a888140b79cf` (unchanged — no commits)

---

## 1. Safety gate

| Check | Result |
|-------|--------|
| Worktree | `V:\DevelopmentRepo\AOVIAS.Cursor` |
| Branch | `agent/cursor/phase1gc-documentation-correction` |
| Main / Claude HEAD | `202dd525…` (not modified) |
| Phase 1G-C doc diff | **Preserved** (3 files still modified) |
| Phase 1G-C report | **Preserved** (`AOVIAS_PHASE1GC_DOCUMENTATION_CORRECTION_REPORT.md` untracked) |
| Runtime probe | Not executed |

**Initial uncommitted 1G-C diff:** 3 documentation files (+6/−3). **Final:** same 3 files plus 1G-D code/test changes (see §4).

---

## 2. Discovery paths investigated

| Location | Prior behavior | Evidence | 1G-D action |
|----------|----------------|----------|-------------|
| `LegacyPublishingPlatformRootDiscovery` | Parent walk; **5× `..` fallback** | `LegacyPublishingPlatformRootDiscovery.cs` | Fail-fast via shared helper |
| `LegacyCsmPlatformRootDiscovery` | Parent walk; **4× `..` fallback** | `LegacyCsmPlatformRootDiscovery.cs` | Fail-fast via shared helper |
| `RepositoryLayoutProductionAdapter` | Manifest then legacy `Resolve` | Unchanged flow | Inherits fail-fast legacy |
| `PlatformRootResolver` / CSM `RootPathResolver` | Adapter entry points | Call adapter | No direct edit |
| `tools/PublishAllServices/Program.cs` | Duplicate walk + **4× `..`** | L232–246 (before) | **`ResolvePublishingPlatformRoot`** |
| `tools/PublishRuntimeAgent/Program.cs` | Duplicate walk + **4× `..`** | L81–95 (before) | **`ResolvePublishingPlatformRoot`** |
| `Aovias.Bootstrap.Host/Program.cs` `ResolveLocalPlatformRoot` | Config-only walk; **`AppContext.BaseDirectory` fallback** | L437–450 (before) | **`ResolvePublishingPlatformRoot`** |
| `SolutionPublishingPreflightService.FindDockerfile` | Upward walk to add Dockerfile candidates; **breaks on markers; no `..` root** | L821–846 | **No change** |
| `build-praman-aovpkg.ps1` | Repo root from `$PSScriptRoot`; defaulted missing config to nonexistent infra path | L8–11 (before) | **Throw** if config + ComplaincePlatform missing |

---

## 3. Marker contract (derived from existing code + manifest)

**Publishing (legacy + tools via adapter):**

- Configuration directory at repository root: `AOVIAS.Infrastructure/aovias-config` **or** `aovias-config` (`RepositoryLayoutAoviasConfigPaths.ConfigDirectoryExistsAtRepositoryRoot`).
- Plus **either** `ComplaincePlatform` **or** `Aovias.Runtime.Agent`.

**CSM (legacy + adapter):**

- Same configuration directory rule.
- Plus **`Aovias.Runtime.Agent`** (required).

Manifest **Current** profile in `repository-layout.json` uses the infrastructure config path in `requiredAll`; legacy walk preserves **dual config layout** (W1 transition).

**Failure mode:** `InvalidOperationException` with message listing required markers (no secrets).

---

## 4. Files changed

| File | Rationale |
|------|-----------|
| `Aovias.Configuration/Layout/RepositoryLayoutPlatformRootDiscovery.cs` | **New** — shared TryResolve + marker checks + exception messages |
| `LegacyPublishingPlatformRootDiscovery.cs` | Delegate to shared TryResolve; remove `..` fallback |
| `LegacyCsmPlatformRootDiscovery.cs` | Same |
| `tools/PublishAllServices/Program.cs` | Use production adapter (manifest-first + legacy) |
| `tools/PublishRuntimeAgent/Program.cs` | Same |
| `Aovias.Bootstrap.Host/Program.cs` | Fail-fast platform root for default `ConfigRoot` |
| `ComplaincePlatform/scripts/build-praman-aovpkg.ps1` | Fail-fast when config tree or ComplaincePlatform missing |
| `RepositoryLayoutDiscoveryHardeningTests.cs` | **New** regression tests |
| `RepositoryLayoutProductionAdapterTests.cs` | Expect throw when no markers (empty temp dir) |
| `RepositoryLayoutCsmResolverContractTests.cs` | Expect throw when neither marker |
| *(unchanged from 1G-C)* `DEPLOYMENT.md`, VPS guide, `.dockerignore` | Preserved |

**Not changed:** `repository-layout.json`, DeploymentValidation fixtures, Docker/CI, `SolutionPublishingPreflightService` logic.

---

## 5. Compatibility preserved

- Infrastructure-only and legacy-only config roots still resolve (covered by W1 transition tests + new `Legacy_publishing_supports_infrastructure_and_legacy_config_layouts`).
- Manifest-first resolution unchanged; legacy only when manifest missing/invalid/nonmatching.
- Publishing OR-marker vs CSM AND-marker distinction unchanged.

---

## 6. Regression tests

**Added:** `RepositoryLayoutDiscoveryHardeningTests` (5 tests)

| Test | Intent |
|------|--------|
| `Publishing_resolves_from_nested_start_directory` | Valid nested path → same root |
| `Csm_resolves_from_nested_start_directory` | Same for CSM |
| `Publishing_rejects_temp_directory_without_markers` | Fail-fast, clear message |
| `Csm_rejects_temp_directory_without_markers` | Fail-fast, clear message |
| `Legacy_publishing_supports_infrastructure_and_legacy_config_layouts` | W1 transition |

**Updated:** `RepositoryLayoutProductionAdapterTests`, `RepositoryLayoutCsmResolverContractTests` (no-marker cases → throw).

**Results:**

| Command | Exit | Result |
|---------|------|--------|
| `dotnet test Aovias.Configuration.Tests.csproj --filter FullyQualifiedName~Layout` | 0 | **39/39 passed** (was 34 + 5 new) |
| `dotnet test Aovias.Publishing.Core.Tests.csproj --filter PublishingProjectPathsTests` | 0 | **2/2 passed** |

**PowerShell (`build-praman-aovpkg.ps1`):** No automated harness in repo. **Manual procedure (when authorized):** From repo root, run script — expect success; copy script to a temp folder outside repo and run — expect throw with config/ComplaincePlatform message.

---

## 7. Release builds and diff check

| Project | Release build |
|---------|---------------|
| `Aovias.Configuration` | **Succeeded** |
| `Aovias.Publishing.Core` | **Succeeded** |
| `tools/PublishAllServices` | **Succeeded** |
| `tools/PublishRuntimeAgent` | **Succeeded** |
| `Aovias.Bootstrap.Host` | **Failed** — pre-existing `NU1903` on `SSH.NET` 2024.2.0 (not introduced by 1G-D) |

| Check | Result |
|-------|--------|
| `git diff --check` | **Pass** |

**Generated artifacts:** Standard `bin/`/`obj/` under built projects (not cleaned per instructions).

---

## 8. Untested boundaries

- End-to-end CLI `--discover` / publish (not run).
- `build-praman-aovpkg.ps1` execution (not run).
- Bootstrap Host compile blocked by NU1903 on this machine.
- ComplianceServiceManager UI tests (no `RootPathResolver`-specific tests found).
- Symlink/junction edge cases (theoretical; not in scope).

---

## 9. Git / phase boundaries

| Action | Performed? |
|--------|------------|
| `git add` / `git commit` / merge / sync | **No** |
| Phase 1G-C edits + report | **Yes — preserved** |
| Runtime probe | **Not executed** |

---

## 10. Final status

**READY FOR HUMAN CODE REVIEW**

Recommend separate commits or a single commit on task branch after review: (1) 1G-C documentation, (2) 1G-D discovery hardening — per human integration preference.
