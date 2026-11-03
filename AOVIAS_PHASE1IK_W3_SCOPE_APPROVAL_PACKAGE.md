# AOVIAS Phase 1I-K — W3 Scope Approval Package and Atomic Migration Plan

**Phase:** 1I-K (read-only scope contract — **no W3 execution authorized**)  
**Date:** 2026-10-25  
**Baseline HEAD:** `b9e2de634b86d04f54b3a1d97ad6bfc44fa646f1`  
**Authoritative inventory:** Phase 1I-J (`AOVIAS_PHASE1IJ_inv1_inventory.json`, `AOVIAS_PHASE1IJ_inv1_matrix.csv`)

---

## Final status

**W3 SCOPE PACKAGE READY FOR HUMAN APPROVAL**

Row-level scope is aligned across Phase 1I-A manifest (M-S-01–M-S-17), `repository-layout.json` **target** paths, and Phase 1I-B decisions (O-3 Studio, O-4 co-move). Residual JSON labels (`ownershipNote: UNRESOLVED O-3`) are **documentation lag** — implementation boundary is **Studio** per human-approved 1I-B. Execution still requires **explicit W3 authorization** after this checklist.

---

## 1. Safety gate

| Worktree | Branch | HEAD | Tracked changes |
|----------|--------|------|-----------------|
| Main | `main` | `b9e2de6…` | None |
| Cursor | `agent/cursor/active` | `b9e2de6…` | None |
| Claude | `agent/claude/active` | `b9e2de6…` | None |

W2 complete (`AOVIAS.Infrastructure/Aovias.ReleaseValidation`, publish tools). **No** `AOVIAS.Studio/` product tree at HEAD. Untracked phase reports and operational artifacts preserved.

**Source reconciliation:** No path conflict between 1I-A M-S rows and `repository-layout.json` `products.studio.paths.*.target` for W3 trees. M-S-16 manifest text allowed Infrastructure alternate; **1I-B binding choice is Studio** → destination **`AOVIAS.Studio/Aovias.DeploymentValidation`**.

---

## 2. Row-by-row W3 scope contract (M-S-01 … M-S-17)

**Legend — atomic groups:** **G1** root/nested slnx; **G2** CSM cluster; **G3** shared Studio libraries & apps; **G4** Agent + Publishing + DRV + Docker/stager coupling.

| ID | Action | Current source | Proposed destination (`repository-layout.json`) | Atomic group | Projects (INV-1) | Key impacts / required updates |
|----|--------|----------------|---------------------------------------------------|--------------|------------------|--------------------------------|
| **M-S-01** | **MOVE** | `Aovias.Platform.slnx` | `AOVIAS.Studio/Aovias.Platform.slnx` | **G1** | **38** `<Project Path>` entries | Rewrite all paths (prefix `AOVIAS.Studio/` or adjust relative `../`); Platform CI entry if any; human/CI `dotnet build` path |
| **M-S-02** | **MOVE** | `Aovias.PublishingEngine.slnx` | `AOVIAS.Studio/Aovias.PublishingEngine.slnx` | **G1** | **14** projects | Same path rewrite; golden PublishingEngine validation |
| **M-S-03** | **MOVE** | `ComplianceServiceManager/` | `AOVIAS.Studio/ComplianceServiceManager/` | **G2** | `ComplianceServiceManager.csproj` (+ slnx 2) | `NuGet.Config`, WPF launch paths, `RootPathResolver` / appsettings `ConfigRoot`, **ProjectReference** to `Aovias.Runtime.Contracts` → becomes in-tree under Studio |
| **M-S-04** | **MOVE** | `ComplianceServiceManager.Tests/` | `AOVIAS.Studio/ComplianceServiceManager.Tests/` | **G2** | 1 csproj (**orphan** — no slnx) | Update test csproj refs; **preserve orphan status** unless separate human decision |
| **M-S-05** | **MOVE** | `ComplianceServiceManager.Operations/` | `AOVIAS.Studio/ComplianceServiceManager.Operations/` | **G2** | 1 csproj | In Platform slnx (38); ref updates |
| **M-S-06** | **MOVE** | `ComplianceServiceManager.Operations.Tests/` | `AOVIAS.Studio/ComplianceServiceManager.Operations.Tests/` | **G2** | 1 csproj | In Platform slnx; ref updates |
| **M-S-07** | **MOVE** | `Aovias.Configuration/` | `AOVIAS.Studio/Aovias.Configuration/` | **G3** | 2 csproj (lib + tests) | **Critical hub** — all slnx `../Aovias.Configuration`; layout tests; `RepositoryLayoutManifestProvider`; production adapter walk markers |
| **M-S-08** | **MOVE** | `Aovias.Deployment.Contracts/` | `AOVIAS.Studio/Aovias.Deployment.Contracts/` | **G3** | 1 csproj | DRV, Publishing, Release, CommandCenter refs |
| **M-S-09** | **MOVE** | `Aovias.CommandCenter/` | `AOVIAS.Studio/Aovias.CommandCenter/` | **G3** | 2 + slnx **6** | Area slnx + Platform membership |
| **M-S-10** | **MOVE** | `Aovias.Runtime.Agent/` (includes **O-4** Contracts) | `AOVIAS.Studio/Aovias.Runtime.Agent/` | **G4** | 7 csproj + slnx **14** | **Dockerfile** COPY paths; **stager** folder list; `deployment.metadata.json`; CSM ref to Contracts; `TryResolvePlatformRoot` marker walk |
| **M-S-11** | **MOVE** | `Aovias.Publishing/` | `AOVIAS.Studio/Aovias.Publishing/` | **G4** | 8 csproj + slnx **10** | Publish CLI graph; preflight; stager tests; `GetScanRoots` unchanged until W4 |
| **M-S-12** | **MOVE** | `Aovias.Bootstrap/` | `AOVIAS.Studio/Aovias.Bootstrap/` | **G3** | 5 csproj + **`Aovias.Bootstrap.sln`** | Legacy `.sln` uses `..\Aovias.Configuration` — rewrite to `..\Aovias.Configuration` **within Studio** (sibling) or `..\..\Aovias.Configuration` depending on depth |
| **M-S-13** | **MOVE** | `Aovias.Release/` | `AOVIAS.Studio/Aovias.Release/` | **G3** | 4 csproj + slnx **10** | ReleaseManager; Bootstrap sln Release.Contracts path |
| **M-S-14** | **MOVE** | `Aovias.Orchestration/` | `AOVIAS.Studio/Aovias.Orchestration/` | **G3** | 3 csproj + slnx **3** | Dockerfile partial COPY; Platform.Core hub |
| **M-S-15** | **MOVE** | `Aovias.DesignSystem/` | `AOVIAS.Studio/Aovias.DesignSystem/` | **G2** | 1 csproj | CSM slnx only (2 projects); WPF resources |
| **M-S-16** | **MOVE** | `Aovias.DeploymentValidation/` | **`AOVIAS.Studio/Aovias.DeploymentValidation/`** (O-3) | **G4** | 2 csproj | Dockerfile COPY; stager fallback + regex COPY roots; **309+** DRV tests; PublishingEngine slnx |
| **M-S-17** | **MOVE** | Nested area `*.slnx` (8 under product dirs) | Under `AOVIAS.Studio/<area>/…` per `repository-layout.json` `solutions[]` | **G1** | See §4 | Each file moves **with** its parent folder; internal `<Project Path>` relative paths mostly unchanged |

**Explicitly OUT of W3 (KEEP at current location until W4/W5):**

- `ComplaincePlatform/`, `WebApp/` (W4)
- `AOVIAS Website/AOVIAS Web/` (W5)
- `AOVIAS.Infrastructure/*` (W2 done)
- `AOVIAS.AccredaOne/` (W6)
- Root governance: `AGENTS.md`, `repository-layout.json`, `scripts/`, `docs/`
- **`preflight-docker-probe/`** — O-8 deferred housekeeping

**Untracked / ignored under Studio trees:** Do not assume relocation of `bin/`, `obj/`, `node_modules/`, local packages, or IDE folders — Git moves **tracked** content only; ignored data stays or is recreated by builds.

---

## 3. O-3 and O-4 validation (implementation boundaries)

### O-3 — `Aovias.DeploymentValidation` → **Studio**

| Evidence | Detail |
|----------|--------|
| **1I-B recommendation** | **Studio** (with W3), aligns with Dockerfile/stager grouping |
| **W3 destination** | `AOVIAS.Studio/Aovias.DeploymentValidation/` |
| **Dockerfile** (`Aovias.Runtime.Agent/Dockerfile`) | `COPY Aovias.DeploymentValidation/` → **`COPY AOVIAS.Studio/Aovias.DeploymentValidation/`** (or build context = repo root with prefixed paths) |
| **Stager** | `RuntimeAgentDockerContextStager.FallbackMonorepoFolders` includes `Aovias.DeploymentValidation`; `CopyRootPattern` parses Dockerfile `COPY Aovias.*/`; `Stage()` copies `Path.Combine(platformRoot, folder)` — **folder names must match disk after move** |
| **Tests** | `RuntimeAgentDockerContextStagerTests` asserts staged `Aovias.DeploymentValidation` |
| **`discovery.current`** | `runtimeAgentDockerCopyRoots` entry must become `AOVIAS.Studio/Aovias.DeploymentValidation` (same batch) |

**Do not** move DRV to Infrastructure in W3 without new human decision (contradicts 1I-B).

### O-4 — `Aovias.Runtime.Contracts` co-move with Agent

| Evidence | Detail |
|----------|--------|
| **Location** | `Aovias.Runtime.Agent/src/Aovias.Runtime.Contracts/` (csproj in Agent slnx) |
| **1I-B** | **Co-move with Agent** — no separate product root |
| **Direct csproj consumer outside Agent tree** | `ComplianceServiceManager/ComplianceServiceManager.csproj` → `..\Aovias.Runtime.Agent\src\Aovias.Runtime.Contracts\...` becomes **`..\Aovias.Runtime.Agent\src\...`** under `AOVIAS.Studio/` (both move — update relative segment) |
| **Indirect consumers** | CSM view models/services (`DeploymentHttpErrorPresenter`, Observability VMs), DRV tests referencing contract types |
| **W3 action** | **MOVE** only as part of M-S-10; **no** promotion to `AOVIAS.Shared` (O-6 deferred) |

---

## 4. Nine-solution migration plan (INV-1)

| # | Current `.slnx` path | Post-W3 path | Project count (HEAD) | Post-W3 validation |
|---|----------------------|--------------|----------------------|---------------------|
| 1 | `Aovias.Platform.slnx` | `AOVIAS.Studio/Aovias.Platform.slnx` | **38** | `dotnet build` Release |
| 2 | `Aovias.PublishingEngine.slnx` | `AOVIAS.Studio/Aovias.PublishingEngine.slnx` | **14** | `dotnet build` + layout tests subset |
| 3 | `ComplianceServiceManager/ComplianceServiceManager.slnx` | `AOVIAS.Studio/ComplianceServiceManager/ComplianceServiceManager.slnx` | **2** | CSM build (desktop workload) |
| 4 | `Aovias.CommandCenter/Aovias.CommandCenter.slnx` | `AOVIAS.Studio/Aovias.CommandCenter/Aovias.CommandCenter.slnx` | **6** | Area build |
| 5 | `Aovias.Runtime.Agent/Aovias.Runtime.Agent.slnx` | `AOVIAS.Studio/Aovias.Runtime.Agent/Aovias.Runtime.Agent.slnx` | **14** | Agent + stager tests |
| 6 | `Aovias.Publishing/Aovias.Publishing.slnx` | `AOVIAS.Studio/Aovias.Publishing/Aovias.Publishing.slnx` | **10** | Publishing.Core.Tests |
| 7 | `Aovias.Release/Aovias.Release.slnx` | `AOVIAS.Studio/Aovias.Release/Aovias.Release.slnx` | **10** | Release build |
| 8 | `Aovias.Orchestration/Aovias.Orchestration.slnx` | `AOVIAS.Studio/Aovias.Orchestration/Aovias.Orchestration.slnx` | **3** | Orchestration build |
| 9 | `ComplaincePlatform/CompliancePlatform.slnx` | *(unchanged — W4)* | **36** | **Not** W3 |

**Not counted in 9:** `Aovias.Bootstrap/Aovias.Bootstrap.sln` — legacy Visual Studio solution; **6** projects (4 Bootstrap + Configuration + Release.Contracts). After W3, `.sln` paths `..\Aovias.Configuration` must resolve to **`AOVIAS.Studio/Aovias.Configuration`**. Bootstrap projects remain fully listed in **Platform slnx (38)** — **coherent** if `.sln` updated in same batch.

**Platform slnx (38 projects):** Full list in `AOVIAS_PHASE1IJ_inv1_matrix.csv` rows where `Solutions` contains `Aovias.Platform.slnx`. Every `<Project Path="…">` must be rewritten; cross-folder refs like `AOVIAS.Infrastructure/tools/PublishAllServices` **stay outside Studio** (one extra `../` level from Studio slnx location).

**Solution orphans (11):** **Preserved** — no W3 action to add/remove from slnx (per Phase 1I-J). Includes `ComplianceServiceManager.Tests` (Studio tree moves but stays orphan).

---

## 5. Cross-wave dependencies (W3 batch)

### 5.1 W2 publishing tools (Infrastructure → Studio refs)

| File | Current refs | Post-W3 required |
|------|--------------|------------------|
| `AOVIAS.Infrastructure/tools/PublishAllServices/PublishAllServices.csproj` | `..\..\..\Aovias.Configuration`, `..\..\..\Aovias.Publishing\...` | **`..\..\..\AOVIAS.Studio/Aovias.Configuration`**, **`..\..\..\AOVIAS.Studio/Aovias.Publishing\...`** |
| `AOVIAS.Infrastructure/tools/PublishRuntimeAgent/PublishRuntimeAgent.csproj` | Same pattern | Same |
| `Program.cs` (both) | `ResolvePublishingPlatformRoot` — manifest-driven | Ensure `repository-layout.json` **current** markers match post-W3 disk |

### 5.2 `repository-layout.json` — **`discovery.current`** (production)

Update **current** (not production Target profile activation) to post-W3 paths, aligned with existing **`discovery.target`** literals where applicable:

| Field | Current (HEAD) | Post-W3 **current** (proposed) |
|-------|----------------|--------------------------------|
| `platformRoot.requiredAny` | `ComplaincePlatform`, `Aovias.Runtime.Agent` | Keep **`ComplaincePlatform`** until W4; change Agent → **`AOVIAS.Studio/Aovias.Runtime.Agent`** |
| `csmPlatformRoot.requiredAll` | `…/aovias-config`, `Aovias.Runtime.Agent` | Agent → **`AOVIAS.Studio/Aovias.Runtime.Agent`** |
| `deployableProjectScanRoots` | `ComplaincePlatform`, `Aovias.Runtime.Agent` | Agent path prefixed (**Command root unchanged until W4**) |
| `runtimeAgentProjectRelative` | `Aovias.Runtime.Agent/src/.../Api.csproj` | **`AOVIAS.Studio/Aovias.Runtime.Agent/src/...`** |
| `runtimeAgentDockerCopyRoots` | seven `Aovias.*` top-level names | seven **`AOVIAS.Studio/Aovias.*`** paths (match `discovery.target` list) |
| `products.studio.paths.*.current` | repo-root Studio trees | **`AOVIAS.Studio/…`** for each moved product |

**Also update:** `products.studio.migrationStatus` when human approves execution.

**Do not** enable `RepositoryLayoutProfile.Target` in production adapter without separate contract phase.

### 5.3 Code / tests (same batch)

| Component | Path / symbol |
|-----------|----------------|
| Layout tests | `RepositoryLayoutW1TransitionTests`, `RepositoryLayoutContractTests`, CSM resolver contract tests |
| Stager | `RuntimeAgentDockerContextStager.cs` — `FallbackMonorepoFolders`, `TryResolvePlatformRoot` (Configuration + Agent folder names) |
| Dockerfile | `AOVIAS.Studio/Aovias.Runtime.Agent/Dockerfile` COPY lines |
| Discoverer | `SolutionDeployableProjectDiscoverer.GetScanRoots` — **Agent path only** in W3 if Command stays at root |
| CSM | `RootPathResolver`, launch settings (untracked Claude copy preserved separately) |
| Docs | G1 runbook already W2 paths; add Studio prefixes where referencing `Aovias.*` trees |

### 5.4 Explicitly **not** W3

- `ComplaincePlatform/.github/workflows/platform-integrity.yml` (W4)
- Website CI (W5)
- `--discover`, preflight, Docker build, npm (separate authorization)

---

## 6. Execution contract (for future authorized phase)

### 6.1 Atomic move groups and order (single merge strongly preferred)

```text
Pre: Cursor task branch from b9e2de6; Main untouched; INV-1 signed off.

Batch W3 (one authorized commit set + human merge):
  1. git mv all G2+G3+G4 product directories → AOVIAS.Studio/…  (M-S-03…16 trees)
  2. git mv G1 slnx files → AOVIAS.Studio/…  (M-S-01, M-S-02; M-S-17 satisfied by tree moves)
  3. Rewrite all 9 Studio slnx + Platform/PublishingEngine project paths
  4. Update Aovias.Bootstrap.sln relative paths
  5. Update Infrastructure publish *.csproj ProjectReferences
  6. Update Dockerfile, stager, discoverer (scoped), layout manifest discovery.current + studio paths.current
  7. Update any broken relative ProjectReference in moved csproj (depth changes for cross-tree refs)

Post (same authorization as W2 pattern):
  Scoped Release builds: Platform.slnx, PublishingEngine.slnx
  Tests: Layout (39), PublishingProjectPaths (2), RuntimeAgentDockerContextStagerTests
  NOT: discover, preflight, Docker daemon, npm
```

### 6.2 Preconditions

- W2 merged and worktrees synced (**done** @ `b9e2de6`)
- Human sign-off on this package (§8)
- Separate **W3 implementation** authorization
- Cursor worktree + `agent/cursor/w3-*` task branch per AGENTS.md

### 6.3 Rollback

- Revert merge commit on `main`; restore `discovery.current` and paths; **no** `git clean` on ignored storage/node_modules
- Task branch preserved until human confirms

### 6.4 Stop conditions

- Unexpected tracked file outside M-S scope
- Missing ProjectReference target after move
- Layout or stager tests fail
- Ambiguity on DRV destination (must be Studio)
- Attempt to include Command or Website trees in W3 batch

---

## 7. Findings severity (summary)

| Severity | Count | Examples |
|----------|-------|----------|
| **Blocker** (must fix in W3 batch) | 3 | Docker+stager; discovery.current; all slnx paths |
| **High** | 4 | Publish tool refs; CSM Contracts ref; Platform 38-path rewrite; DRV tests |
| **Medium** | 2 | Bootstrap .sln; orphan CSM.Tests |
| **Low** | 2 | Historical docs; JSON O-3 label text |

---

## 8. Human approval checklist

*Architecture approval (1I-B) does **not** authorize execution. Check each box to authorize a **future** W3 implementation phase.*

| # | Item | Approve? |
|---|------|----------|
| 1 | **M-S-01** MOVE Platform slnx → `AOVIAS.Studio/` | ☐ |
| 2 | **M-S-02** MOVE PublishingEngine slnx | ☐ |
| 3 | **M-S-03–06** MOVE CSM cluster (incl. orphan Tests tree move) | ☐ |
| 4 | **M-S-07–09** MOVE Configuration, Deployment.Contracts, CommandCenter | ☐ |
| 5 | **M-S-10** MOVE Runtime.Agent (+ O-4 Contracts in-tree) | ☐ |
| 6 | **M-S-11–14** MOVE Publishing, Bootstrap, Release, Orchestration | ☐ |
| 7 | **M-S-15** MOVE DesignSystem | ☐ |
| 8 | **M-S-16** MOVE DeploymentValidation to **`AOVIAS.Studio/…`** (O-3) | ☐ |
| 9 | **M-S-17** Nested slnx co-move with parent folders | ☐ |
| 10 | **O-3** Dockerfile + stager + dockerCopyRoots use **Studio** DRV path | ☐ |
| 11 | **O-4** Contracts stay under Agent; update CSM ProjectReference only | ☐ |
| 12 | All **9** Studio `.slnx` path rewrites (38 + 14 + … project entries) | ☐ |
| 13 | **W2 publish tools** `.csproj` → `AOVIAS.Studio/…` references | ☐ |
| 14 | **`discovery.current`** updated; **Target** remains non-production in code | ☐ |
| 15 | **11 solution orphans** unchanged (no slnx scope creep) | ☐ |
| 16 | **Deferred:** `--discover`, preflight, Docker run, npm ReleaseValidation | ☐ |
| 17 | Authorize **separate** W3 implementation task (Cursor branch + merge) | ☐ |

---

## 9. Recommended next step

After checklist completion: authorize **Phase 1I-L (or equivalent) W3 controlled migration** in `V:\DevelopmentRepo\AOVIAS.Cursor` only — mirroring W2 discipline (inventory gate already satisfied by 1I-J).

---

**Report path:** `AOVIAS_PHASE1IK_W3_SCOPE_APPROVAL_PACKAGE.md` (untracked, Main root)
