# AOVIAS Phase 1C-B — Production Compatibility Contract

**Date:** 2026-10-25  
**Baseline HEAD:** `5a1526d99a68241d40ccba759d8fc62586f4af86`  
**Scope:** Current-profile manifest integration for **Publishing platform root only**

---

## 1. Dependency analysis (pre-implementation)

| Component | Root discovery mechanism | Can reference `Aovias.Configuration`? | 1C-B integration |
|-----------|-------------------------|----------------------------------------|------------------|
| `PlatformRootResolver` | `AppContext.BaseDirectory` walk; optional `configuredRoot` | **Yes** (via `Aovias.Publishing.Core`) | **Integrated** |
| `RootPathResolver` (CSM) | Separate walk: **`Aovias.Runtime.Agent` + `aovias-config`** (stricter than Publishing) | **Yes** (CSM csproj) | **Not integrated** — different markers would risk behavior change |
| `SourceDiscoveryBoundary` | Hard-coded `ComplaincePlatform`, `WebApp` under `context.RootPath` | Could, but not required for platform root | **Unchanged** |
| `SolutionDeployableProjectDiscoverer` | `platformRoot` + fixed subdirs | Already via Publishing → Configuration | **Unchanged** (inherits `PlatformRootResolver`) |
| `RuntimeAgentDockerContextStager` | Dockerfile COPY folder names | Publishing.Core | **Unchanged** |

**Cycles:** `Aovias.Configuration` does not reference Publishing or CSM. Integration is one-way.

**CWD:** Resolvers use **`AppContext.BaseDirectory`**, not process CWD, for walks (unchanged).

---

## 2. Compatibility guarantees

| # | Guarantee | Mechanism |
|---|-----------|-----------|
| 1 | Current layout remains default | Production adapter uses **`RepositoryLayoutProfile.Current` only** |
| 2 | Target paths never used in production | `IsPermittedProductionProfile` rejects Target; no env-based profile switch |
| 3 | Missing manifest | `TryLoadForProduction` → false → **`LegacyPublishingPlatformRootDiscovery`** (same algorithm as pre-1C-B `PlatformRootResolver`) |
| 4 | Invalid JSON / unsupported schema / validation errors | `TryLoadFromFile` fails closed → legacy fallback; **no throw** in `ResolvePublishingPlatformRoot` |
| 5 | Paths resolved under verified repo root | `RepositoryLayoutPathResolver.CombineUnderRepositoryRoot` rejects `..` and escape |
| 6 | Target profile | Only in **1C-A contract tests** and temp fixtures via `RepositoryLayoutPathResolver` directly |
| 7 | Public API shape | `PlatformRootResolver.Resolve(configuredRoot)` signature unchanged; `configuredRoot` still wins when valid |
| 8 | Secrets | No logging of manifest contents or environment in new code |

**Explicit non-guarantee:** When manifest is **valid** and markers match, manifest walk may run **before** legacy walk. On the integration repository and parity fixtures, result **equals** legacy (verified by tests). Divergence would indicate manifest markers out of sync with legacy — treat as contract bug, not silent retargeting to **target** layout.

---

## 3. Production integration surface

| Type | Responsibility |
|------|----------------|
| `RepositoryLayoutSchema` | Supported schema versions (`1.0.0`) |
| `RepositoryLayoutLoader.TryLoadFromFile` / `TryLoadFromJson` | Non-throwing load + validation |
| `RepositoryLayoutManifestProvider` | Walk-up locate `repository-layout.json`; in-process cache |
| `RepositoryLayoutProductionAdapter` | `TryResolvePlatformRepositoryRoot`, `ResolvePublishingPlatformRoot` |
| `LegacyPublishingPlatformRootDiscovery` | Byte-for-byte behavioral fallback for Publishing |

**`PlatformRootResolver.Resolve`:** After `configuredRoot`, delegates to `RepositoryLayoutProductionAdapter.ResolvePublishingPlatformRoot`.

---

## 4. Out of scope (Phase 1C-B)

- `RootPathResolver`, `SourceDiscoveryBoundary`, `RuntimeAgentDockerContextStager` direct manifest reads
- CSM platform root (different discovery rule)
- Deployable scan roots / Docker COPY lists from manifest
- Target-profile production activation
- Physical directory moves

---

## 5. Missing manifest behavior (documented)

**Observed behavior:** Identical to pre-1C-B Publishing resolver — upward walk for `aovias-config` and (`ComplaincePlatform` or `Aovias.Runtime.Agent`), else relative `..\..\..\..\..` from base directory.

---

## 6. Inventory gaps

Phase 1B gaps (**unchanged**): project-solution matrix, `deployment.metadata.json` inventory, residual Compose edges.

---

*Does not authorize Phase 1C-C physical migration.*
