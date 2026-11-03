# AOVIAS Phase 1C-A — Layout Contract

**Date:** 2026-10-25  
**Baseline HEAD:** `5a1526d99a68241d40ccba759d8fc62586f4af86`  
**Authority:** Non-destructive contract + tests — **no physical migration, no production resolver wiring**

---

## 1. Safety gate (verified)

| Worktree | Branch | HEAD | Notes |
|----------|--------|------|-------|
| Main | `main` | `5a1526d…` | Tracked clean; prior Phase reports + new 1C-A artifacts unstaged |
| Cursor | `agent/cursor/active` | `5a1526d…` | Probe preserved |
| Claude | `agent/claude/active` | `5a1526d…` | CommandCenter Properties preserved |

---

## 2. Machine-readable contract

**File:** `repository-layout.json` (repository root)

| Field group | Purpose |
|-------------|---------|
| `schemaVersion` | Contract format version (`1.0.0`) |
| `interpretation.current` / `interpretation.target` | How to read path pairs |
| `ownershipDecisions` | O-1, O-2a, O-2b approved **planning** decisions |
| `policies` | KnowledgeBase Command ownership, AccredaOne boundary, `ComplaincePlatform` spelling |
| `products[]` | Product IDs, owners, `paths.{key}.current` vs `.target`, migration status |
| `solutions[]` | Nine `.slnx` paths (current + target) |
| `discovery.current` / `discovery.target` | Platform root markers, application roots, deployable scan roots, Agent/WebApp relative paths, Docker COPY roots |
| `compatibilityConstraints` | Explicit note that legacy resolvers are **not** wired in 1C-A |
| `unresolvedDecisions` | O-3..O-8, INV-1, INV-2 |
| `inventoryGapsPreservedFromPhase1B` | Matrix + metadata inventory deferred |

**Validation:** `RepositoryLayoutLoader.Validate` — required schema version, non-empty discovery markers, no `..` or absolute paths in segments.

**No developer-specific absolute paths** in the JSON.

---

## 3. Code abstraction (contract-only)

New types under `Aovias.Configuration/Layout/` (additive; **does not alter** `RootPathResolver`, `PlatformRootResolver`, `SourceDiscoveryBoundary`, `SolutionDeployableProjectDiscoverer`, or `RuntimeAgentDockerContextStager`):

| Type | Role |
|------|------|
| `RepositoryLayoutManifest` (+ models) | JSON contract shape |
| `RepositoryLayoutLoader` | Load + validate manifest |
| `RepositoryLayoutPathResolver` | Resolve roots/paths from manifest + profile (`Current` / `Target`) |

**Profile semantics:**

- **Current** — matches on-disk layout today (`ComplaincePlatform`, `WebApp`, root `aovias-config`, etc.).
- **Target** — proposed layout; exercised in tests via **temporary fixtures**, not assumed to exist on disk.

---

## 4. Approved planning decisions (recorded in JSON)

| ID | Decision |
|----|----------|
| O-1 | `Aovias.CommandCenter` → **AOVIAS.Studio** (initial) |
| O-2a | `Aovias.Configuration` → **AOVIAS.Studio** (initial); Shared promotion requires cross-product consumers + versioning |
| O-2b | `Aovias.Deployment.Contracts` → **AOVIAS.Studio** (initial); same Shared rule |
| — | KnowledgeBase remains **Command**-owned at `ComplaincePlatform/KnowledgeBase` |
| — | AccredaOne = scaffold + future **public** API boundary |

**Does not authorize moves.**

---

## 5. Inventory gaps (unchanged)

Phase 1B gaps **preserved** in manifest and this doc:

- Full project-to-solution membership matrix (87×9)
- Complete `deployment.metadata.json` inventory
- Residual Compose edge cases

Separate automation task — **not** expanded in 1C-A.

---

## 6. Production behavior

**Verified unchanged:** `ComplianceServiceManager/Services/RootPathResolver.cs`, `SourceDiscoveryBoundary.cs`, `PlatformRootResolver.cs`, publishing discoverer/stager — **no edits** in Phase 1C-A.

**Wiring task (future Phase 1C-B+):** Inject or read `repository-layout.json` from legacy resolvers behind compatibility fallbacks.

---

## 7. Files added or modified (Phase 1C-A)

| Path | Change |
|------|--------|
| `repository-layout.json` | **Added** |
| `Aovias.Configuration/Layout/RepositoryLayoutModels.cs` | **Added** |
| `Aovias.Configuration/Layout/RepositoryLayoutLoader.cs` | **Added** |
| `Aovias.Configuration/Layout/RepositoryLayoutPathResolver.cs` | **Added** |
| `Aovias.Configuration/tests/.../Layout/RepositoryLayoutContractTests.cs` | **Added** |
| `AOVIAS_PHASE1CA_LAYOUT_CONTRACT.md` | **Added** (this file) |
| `AOVIAS_PHASE1CA_TEST_REPORT.md` | **Added** |

No Docker, Compose, CI, slnx, or directory layout changes.

---

*Phase 1A/1B reports preserved.*
