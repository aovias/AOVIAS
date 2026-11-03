# AOVIAS Phase 1C-C — CSM Compatibility Contract

**Date:** 2026-10-25  
**Baseline HEAD:** `5a1526d99a68241d40ccba759d8fc62586f4af86`

---

## 1. Verified resolver semantics (independent)

| Consumer | Method | Markers (walk from `AppContext.BaseDirectory`) | Fallback |
|----------|--------|--------------------------------------------------|----------|
| **Publishing** | `PlatformRootResolver.Resolve` | `aovias-config` **AND** (`ComplaincePlatform` **OR** `Aovias.Runtime.Agent`) | 5× `..` from base |
| **CSM** | `RootPathResolver.ResolvePlatformRoot` | **`aovias-config` AND `Aovias.Runtime.Agent`** (both required) | 4× `..` from base |

**Verified distinction:** A repo root with only `aovias-config` + `ComplaincePlatform` satisfies **Publishing** but **not** CSM (contract test `Csm_does_not_resolve_when_runtime_agent_missing`).

Other CSM methods (`ResolveCompliancePlatformRoot`, `ResolveWebAppRoot`, bootstrap paths) **unchanged** — still legacy folder walks.

---

## 2. Manifest representation

Under `discovery.current` / `discovery.target`:

| Key | Consumer | Shape |
|-----|----------|--------|
| `platformRoot` | Publishing | `requiredAll` + `requiredAny` |
| `csmPlatformRoot` | CSM | `requiredAll`: `["aovias-config", "Aovias.Runtime.Agent"]`, `requiredAny`: `[]` |

**Target CSM (planning only):** `requiredAll`: Infrastructure config + Studio Runtime Agent paths.

`MatchesPlatformRoot`: when `requiredAny` is empty, **all** `requiredAll` entries must exist (CSM mode).

---

## 3. Production integration

| Entry | Behavior |
|-------|----------|
| `RepositoryLayoutProductionAdapter.TryResolveCsmPlatformRepositoryRoot` | Manifest + **Current** profile + `csmPlatformRoot` only |
| `RepositoryLayoutProductionAdapter.ResolveCsmPlatformRoot` | Manifest attempt → `LegacyCsmPlatformRootDiscovery` |
| `RootPathResolver.ResolvePlatformRoot` | Delegates to `ResolveCsmPlatformRoot` |

**Publishing** continues to use `platformRoot` via `ResolvePublishingPlatformRoot` (1C-B).

---

## 4. Compatibility guarantees

1. **Current layout default** — production never reads `discovery.target` for resolution.
2. **No silent cross-policy merge** — CSM never uses Publishing `platformRoot` markers.
3. **Missing/invalid manifest** — CSM legacy walk (identical to pre-1C-C when markers match).
4. **Invalid manifest does not throw** through `ResolveCsmPlatformRoot`.
5. **Path boundary** — `CombineUnderRepositoryRoot` rejects `..` and escape.
6. **Public APIs** — `RootPathResolver` method signatures unchanged; only `ResolvePlatformRoot` body delegates.
7. **Target profile** — available only via `RepositoryLayoutPathResolver.TryResolveCsmRepositoryRoot(..., Target)` in tests.

---

## 5. Explicitly unchanged

- `SourceDiscoveryBoundary`
- `SolutionDeployableProjectDiscoverer.GetScanRoots`
- `RuntimeAgentDockerContextStager`
- CSM `ResolveCompliancePlatformRoot` / `ResolveWebAppRoot`
- Docker, Compose, CI, physical directories

---

## 6. Worktree note

Phase 1C-A/B artifacts (`repository-layout.json`, Layout code) are on **Main** at HEAD. **Cursor** worktree at same SHA has **only** the untracked runtime probe (no layout copy) — preserved, not discarded.

---

*Does not authorize physical migration (Phase 1D).*
