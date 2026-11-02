# AOVIAS Phase 1G-D-F — Minor Findings Triage Report

**Date:** 2026-10-25  
**Worktree:** `V:\DevelopmentRepo\AOVIAS.Cursor`  
**Branch:** `agent/cursor/phase1gc-documentation-correction`  
**HEAD:** `202dd525e8661ca0c3db68b40ab3a888140b79cf` (unchanged)

**Source review:** `AOVIAS_PHASE1GDR_CODE_REVIEW_REPORT.md` (M1–M5)

---

## 1. Safety gate

| Check | Result |
|-------|--------|
| Worktree / branch / HEAD | **Match** expectations |
| Phase 1G-C docs + report | **Preserved** (3 modified doc files; `AOVIAS_PHASE1GC_DOCUMENTATION_CORRECTION_REPORT.md` untracked) |
| Phase 1G-D code/tests/reports | **Preserved** (8 modified + 2 untracked source/test files; 1G-D / 1G-D-R reports untracked) |
| Main / Claude | Not modified (not re-verified in this step; prior gates at `202dd525`) |
| Runtime probe / publish / deploy | **Not executed** |

**Code changes in this phase:** **None** (triage-only).

**Read-only validation:** `git diff --check` — **pass** (exit 0).

---

## 2. Finding-by-finding triage

### M1 — `PublishRuntimeAgent` uses Publishing OR-markers

| Field | Detail |
|-------|--------|
| **Location** | `tools/PublishRuntimeAgent/Program.cs` — `ResolvePlatformRoot()` → `RepositoryLayoutProductionAdapter.ResolvePublishingPlatformRoot` |
| **Contract** | Same as `PlatformRootResolver.Resolve` (L17): manifest-first, then legacy publishing markers (config + `ComplaincePlatform` **or** `Aovias.Runtime.Agent`). See `RepositoryLayoutPlatformRootDiscovery.MatchesPublishingLegacyMarkers`. |
| **Prior behavior** | Local walk required **both** `Aovias.Runtime.Agent` and config at the same ancestor. |
| **Risk** | **Compatibility (edge):** Hypothetical tree with only `ComplaincePlatform` + config could now resolve platform root; tool then fails at `File.Exists(projectPath)` (L17–20). **Not a silent wrong-root publish.** Full monorepo: identical root. |
| **Smallest action** | None required; optional deferred test from `tools/.../bin/Release`. |
| **Decision** | **Acceptable by design** |
| **Rationale** | Phase 1G-D goal was fail-fast alignment with production publishing discovery, not a separate Runtime-Agent-only marker policy. Duplicating a third walk would increase drift risk. CSM remains stricter via `ResolveCsmPlatformRoot` elsewhere. |

---

### M2 — Bootstrap stricter than configuration-only fallback

| Field | Detail |
|-------|--------|
| **Location** | `Aovias.Bootstrap.Host/Program.cs` — `ResolveLocalPlatformRoot()` → `ResolvePublishingPlatformRoot`; used only when `ConfigRoot` is not set via `--config-root` or `AOVIAS_CONFIG_ROOT` (L431–433). |
| **Contract** | Default config root = `ResolveConfigRootDirectory(platformRoot)` where `platformRoot` must satisfy **publishing** markers, not config directory alone. |
| **Prior behavior** | Walk stopped at first config directory; else returned `AppContext.BaseDirectory` (often **not** a valid repo root — masked mis-invocation). |
| **Risk** | **Compatibility:** Operators who ran bootstrap from an output folder without repo markers previously got a meaningless default; they now get an explicit exception. **Supported layouts** (full AOVIAS checkout) unchanged. Explicit `--config-root` / env unchanged. |
| **Smallest action** | Document in commit message (human); no code change. |
| **Decision** | **Acceptable by design** |
| **Rationale** | Stricter behavior implements Phase 1G-D “fail explicitly”; weakening would restore silent mis-resolution without evidence that config-only ancestors are valid supported layouts. |

---

### M3 — Partial-marker test coverage gaps

| Field | Detail |
|-------|--------|
| **Location** | `RepositoryLayoutPlatformRootDiscovery.TryResolvePublishingPlatformRoot` / `TryResolveCsmPlatformRoot`; tests in `RepositoryLayoutDiscoveryHardeningTests.cs`, `RepositoryLayoutCsmResolverContractTests.cs` (e.g. agent missing, config missing). |
| **Contract** | A directory matches only when **all** required markers for that profile are present on the same ancestor; partial sets do not match and walk continues. |
| **Gap** | No dedicated test for e.g. `ComplaincePlatform` without config, or config without app markers (distinct from “neither marker” empty fixture). |
| **Risk** | **Maintainability / coverage** only; logic is conjunctive in source (L8–15, L13–15); existing “missing one required marker” tests cover manifest/CSM paths. |
| **Smallest action** | Add 1–2 focused tests in a follow-up if desired. |
| **Decision** | **Deferred follow-up** |
| **Rationale** | Does not exercise a new contract boundary beyond existing CSM tests (`Csm_does_not_resolve_when_aovias_config_missing`, etc.) and temp-dir rejection tests. Not required before commit. |

---

### M4 — Dead `else` branch in `build-praman-aovpkg.ps1`

| Field | Detail |
|-------|--------|
| **Location** | `ComplaincePlatform/scripts/build-praman-aovpkg.ps1` L11–17 — after L11–15 `throw` when both config paths absent, L17 `else { $infraConfigRoot }` is unreachable. |
| **Contract** | `$configRoot` prefers infra, else legacy; throws if neither exists. |
| **Risk** | **Maintainability only**; no runtime behavior change if removed. |
| **Smallest action** | Replace ternary with two-branch `if (Test-Path $infraConfigRoot) { ... } else { $legacyConfigRoot }` after guards. |
| **Decision** | **Deferred follow-up** |
| **Rationale** | Unreachability is established, but edit is cosmetic and touches a script excluded from 1G-D “no publish” validation. Prefer bundling with a future script hygiene or praman validation phase rather than expanding this commit mix. **Not fix-required before commit.** |

---

### M5 — Lightweight exception assertions

| Field | Detail |
|-------|--------|
| **Location** | `RepositoryLayoutDiscoveryHardeningTests.cs` — `Publishing_rejects_temp_directory_without_markers` (L58), `Csm_rejects_temp_directory_without_markers` (L77): `Assert.Contains("Could not locate...", ex.Message)`. |
| **Contract** | Fail-fast throws `InvalidOperationException` with human-readable marker summary from `CreatePublishingNotFoundException` / `CreateCsmNotFoundException`. |
| **Risk** | **Maintainability:** Message tweaks could pass/fail tests without behavior regression; current asserts catch gross failure-to-throw. Adapter/legacy message equality tested in `RepositoryLayoutProductionAdapterTests` (empty temp dir). |
| **Smallest action** | Assert full message or marker substrings in follow-up. |
| **Decision** | **Deferred follow-up** |
| **Rationale** | Strengthening does not materially improve protection beyond existing throw + adapter/legacy parity tests; avoids brittle full-string tests on copy edits. |

---

## 3. Summary matrix

| ID | Decision | Fix before commit? |
|----|----------|------------------|
| M1 | Acceptable by design | **No** |
| M2 | Acceptable by design | **No** |
| M3 | Deferred follow-up | **No** |
| M4 | Deferred follow-up | **No** |
| M5 | Deferred follow-up | **No** |

**`repository-layout.json`:** Not modified; no contract defect identified.

---

## 4. Validation (this phase)

| Command | Result |
|---------|--------|
| `git diff --check` | **Pass** |
| Unit tests re-run | **Not re-run** (no code changes; 1G-D-R recorded 39/39 layout tests) |
| Release builds | **Not re-run** |

---

## 5. Preservation confirmation

| Artifact set | Status |
|--------------|--------|
| Phase 1G-C documentation + report | **Intact** |
| Phase 1G-D implementation + tests + reports | **Intact** |
| Untracked probe | **Present**, not executed |

---

## 6. Final status

**READY FOR HUMAN COMMIT REVIEW**

No MINOR finding requires a code change before commit. Five findings resolve to two **acceptable by design** (M1, M2) and three **deferred follow-up** (M3–M5). Human commit may note M1/M2 rationale in the commit message; optional post-merge cleanup for M4/M3/M5.

---

## 7. Git writes

**None** (untracked triage report only).
