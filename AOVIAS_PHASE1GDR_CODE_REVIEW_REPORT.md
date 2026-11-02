# AOVIAS Phase 1G-D-R — Independent Discovery-Hardening Code Review

**Date:** 2026-10-25  
**Reviewer scope:** Read-only; no file edits, Git writes, or workflow execution.  
**Worktree:** `V:\DevelopmentRepo\AOVIAS.Cursor`

---

## 1. Safety gate

| Item | Expected | Actual |
|------|----------|--------|
| Worktree | `V:\DevelopmentRepo\AOVIAS.Cursor` | **Match** |
| Branch | `agent/cursor/phase1gc-documentation-correction` | **Match** |
| HEAD | `202dd525e8661ca0c3db68b40ab3a888140b79cf` | **Match** (no commits on branch) |

**`git status --short` (summary):**

- **Modified (tracked):** 11 files (3× Phase 1G-C docs + 8× Phase 1G-D code/tests).
- **Untracked:** Phase reports (1F-B/C/D, 1G-C, 1G-D), probe, **2 new 1G-D source/test files**.

**Phase 1G-C preservation:** `DEPLOYMENT.md`, `VPS-Bootstrap-Guide.md`, `.dockerignore` diffs match 1G-C report; `AOVIAS_PHASE1GC_DOCUMENTATION_CORRECTION_REPORT.md` present untracked.

**Runtime probe:** Not executed.

---

## 2. Files and diffs reviewed

| Category | Paths |
|----------|--------|
| **New (untracked)** | `Aovias.Configuration/Layout/RepositoryLayoutPlatformRootDiscovery.cs`, `.../RepositoryLayoutDiscoveryHardeningTests.cs` |
| **1G-D modified** | `LegacyPublishingPlatformRootDiscovery.cs`, `LegacyCsmPlatformRootDiscovery.cs`, `PublishAllServices/Program.cs`, `PublishRuntimeAgent/Program.cs`, `Aovias.Bootstrap.Host/Program.cs`, `build-praman-aovpkg.ps1`, `RepositoryLayoutProductionAdapterTests.cs`, `RepositoryLayoutCsmResolverContractTests.cs` |
| **Unchanged (inspected)** | `RepositoryLayoutProductionAdapter.cs`, `SolutionPublishingPreflightService.FindDockerfile` (L821–846) |
| **1G-C (in same diff)** | `Aovias.Runtime.Agent/DEPLOYMENT.md`, `.dockerignore`, `VPS-Bootstrap-Guide.md` |

**`git diff --stat`:** 11 files, +36 / −74 lines. **`git diff --check`:** pass (no conflict markers / whitespace errors).

---

## 3. Findings

### BLOCKER

*None.*

---

### MAJOR

*None requiring fix before commit.* (See MINOR notes for coverage and tool-alignment nuance.)

---

### MINOR

| ID | Finding | Reference | Recommendation |
|----|---------|-----------|----------------|
| M1 | **`PublishRuntimeAgent` discovery contract widened** — Previously local walk required `Aovias.Runtime.Agent` **and** config. Now uses `ResolvePublishingPlatformRoot`, which accepts `ComplaincePlatform` **or** `Aovias.Runtime.Agent` (+ config). In the full monorepo both app markers exist at the same root, so behavior matches today; a stripped tree with only `ComplaincePlatform` could resolve platform root where the old tool walk would not, then fail later on missing `Aovias.Runtime.Agent` project path. | `tools/PublishRuntimeAgent/Program.cs` diff; `RepositoryLayoutPlatformRootDiscovery.MatchesPublishingLegacyMarkers` | Accept as alignment with `PlatformRootResolver`; optional follow-up test documenting tool entry from `bin/Release`. |
| M2 | **Bootstrap default platform root stricter** — Replaced config-only upward walk + `AppContext.BaseDirectory` fallback with full publishing marker resolution. Invalid for incomplete trees (config-only ancestor), **intended** fail-fast; may surprise operators who relied on silent `BaseDirectory` (which was not a valid repo root). | `Aovias.Bootstrap.Host/Program.cs` `ResolveLocalPlatformRoot` | Document in commit message; `--config-root` / `AOVIAS_CONFIG_ROOT` still bypass platform walk for config path. |
| M3 | **Partial-marker scenarios not exhaustively tested** — e.g. `ComplaincePlatform` without config directory, or config without either app marker: implementation walks past (no match), but no dedicated test. | `RepositoryLayoutPlatformRootDiscovery.TryResolve*` | Optional tests in follow-up; not blocking. |
| M4 | **`build-praman-aovpkg.ps1` dead branch** — After throws, `else { $infraConfigRoot }` on `$configRoot` assignment is unreachable. | `build-praman-aovpkg.ps1` L11–17 | Cosmetic cleanup in a later commit. |
| M5 | **Exception assertions are partial** — Rejection tests assert message **contains** prefix, not full marker list or distinct Publishing vs CSM text. | `RepositoryLayoutDiscoveryHardeningTests.cs` L58, L77 | Adequate for regression; strengthen if messages become localized. |

---

### PASS (evidence-backed)

| ID | Topic | Evidence |
|----|-------|----------|
| P1 | **Marker contract aligned with existing legacy + manifest** | Publishing: config (infra **or** legacy) + (`ComplaincePlatform` **or** `Aovias.Runtime.Agent`) — matches pre-1G-D `LegacyPublishingPlatformRootDiscovery` loop and `repository-layout.json` `discovery.current.platformRoot`. CSM: config + `Aovias.Runtime.Agent` — matches `csmPlatformRoot` and `LegacyCsmPlatformRootDiscovery`. |
| P2 | **Publishing vs CSM difference preserved** | Separate `MatchesPublishingLegacyMarkers` / `MatchesCsmPlatformRoot` and adapter `ResolvePublishingPlatformRoot` vs `ResolveCsmPlatformRoot`. |
| P3 | **W1 infra vs legacy config transition** | `RepositoryLayoutAoviasConfigPaths.ConfigDirectoryExistsAtRepositoryRoot`; tests `Legacy_publishing_supports_infrastructure_and_legacy_config_layouts`, existing `RepositoryLayoutW1TransitionTests`. |
| P4 | **Nested start directory** | `Publishing_resolves_from_nested_start_directory`, `Csm_resolves_from_nested_start_directory`; parent walk returns **nearest** matching ancestor (same semantics as before). |
| P5 | **Temp / no markers fail explicitly** | `..` fallbacks removed from legacy classes; `TryResolve` returns false; `Resolve` throws `InvalidOperationException` with path + marker hints. Tests on empty temp dirs. |
| P6 | **No silent arbitrary parent as root** | Removed `Path.Combine(..., "..", ...)` in legacy and tools. |
| P7 | **Shared helper placement** | `RepositoryLayoutPlatformRootDiscovery` in `Aovias.Configuration.Layout` next to legacy types; no new package references; tools already reference `RepositoryLayoutProductionAdapter`. |
| P8 | **Try/Resolve semantics** | `TryResolve*` non-throwing; legacy `Resolve` throws via shared factory methods; adapter unchanged (manifest-first, then legacy `Resolve`). |
| P9 | **Production adapter Current-only** | `RepositoryLayoutProductionAdapter.cs` not modified; still uses `RepositoryLayoutProfile.Current` only. |
| P10 | **Tools unified on adapter** | `PublishAllServices` / `PublishRuntimeAgent` delegate to `ResolvePublishingPlatformRoot(AppContext.BaseDirectory)` — manifest-first + hardened legacy. |
| P11 | **`FindDockerfile` correctly unchanged** | Still walks ancestors for candidate paths; stops walk on config + `ComplaincePlatform`; no platform-root fallback; not in diff. |
| P12 | **Praman script fail-fast** | Validates derived `$repoRoot` has config tree and `ComplaincePlatform` before publish loop. |
| P13 | **Regression tests meaningful** | Updated contract tests expect throw when neither marker; new tests cover nested success, temp failure, dual config layout; **39/39** layout tests pass (re-run this review, exit 0). |
| P14 | **Phase 1G-C intact** | Doc/.dockerignore hunks unchanged in substance from 1G-C scope. |

---

## 4. Marker contract and layout-transition review

**Derived from:** `repository-layout.json` (`discovery.current`), `RepositoryLayoutAoviasConfigPaths`, existing legacy walks (pre-diff), Phase 1G-A matrices.

| Scenario | Publishing | CSM |
|----------|------------|-----|
| Infra config + app markers | Manifest and/or legacy succeed | Same |
| Legacy root config only | Manifest fails `requiredAll` infra path; legacy succeeds if app markers present | Same |
| No markers | Throw (new) | Throw (new) |
| Nested `bin` under repo | Unchanged intent; tests added | Tests added |

**Partial / conflicting roots:** Walk is deterministic (parent chain only, no directory enumeration order). Nearest ancestor wins. **Not proven:** nested monorepo-in-monorepo with two full marker sets — same limitation as pre-1G-D.

---

## 5. Architecture and dependencies

- **Appropriate responsibility:** Consolidates duplicated marker checks and walk logic; legacy facades retain public API for adapter and tests.
- **Dependency direction:** Configuration layout only; tools already depended on `Aovias.Configuration.Layout`.
- **Exceptions:** `InvalidOperationException` with fixed English messages — consistent with layout contract tests elsewhere; no secrets in messages.

---

## 6. Validation evidence and limitations

| Activity | Executed in this review? | Result |
|----------|-------------------------|--------|
| Layout unit tests (`FullyQualifiedName~Layout`) | **Yes** (Cursor worktree) | **39 passed**, 0 failed |
| Publishing path tests | No (not re-run; 1G-D report cited 2/2; no publishing test files changed) |
| Publish / praman / Docker / probe | **No** |
| Bootstrap Release build | No | **NU1903** on `SSH.NET` 2024.2.0 with `TreatWarningsAsErrors` — present on integrated tree at `202dd525` (`Aovias.Bootstrap.Host.csproj`); **pre-existing**, not introduced by 1G-D diff |
| `git diff --check` | **Yes** | Pass |

**Artifacts:** Review re-run may refresh `bin/`/`obj/` under Configuration tests; no deployment outputs.

---

## 7. Phase 1G-C confirmation

| Artifact | Status |
|----------|--------|
| `DEPLOYMENT.md` | Modified per 1G-C; present in diff |
| `VPS-Bootstrap-Guide.md` | Modified per 1G-C; present in diff |
| `.dockerignore` | Infra storage line present |
| `AOVIAS_PHASE1GC_DOCUMENTATION_CORRECTION_REPORT.md` | Untracked, preserved |

---

## 8. Smallest corrections (if pursued later — not applied in this review)

1. **M4:** Remove unreachable `else` branch in `build-praman-aovpkg.ps1` when editing that file next.
2. **M3:** Add tests for partial markers (optional).
3. **Commit strategy:** Human may split 1G-C docs vs 1G-D code or single commit; include Bootstrap/tool behavior notes (M1, M2) in commit message.

---

## 9. Final status

**READY FOR HUMAN COMMIT REVIEW**

No BLOCKER or MAJOR defect identified in the Phase 1G-D discovery hardening diff relative to stated objectives. Residual items are MINOR (coverage, dead PS branch, tool contract alignment documentation).

---

## 10. Git writes in Phase 1G-D-R

**None** (untracked review report only).
