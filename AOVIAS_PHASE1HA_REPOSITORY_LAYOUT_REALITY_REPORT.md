# AOVIAS Phase 1H-A — Repository Layout Reality Check

**Phase:** 1H-A (read-only audit)  
**Date:** 2025-10-25  
**Baseline commit (all worktrees):** `7758e30ce0f8c406fe8f292006895f67fb31cafd`  
**W1 reference commit:** `a3188c91975b7d0ca444c9478c0b1590eb747837`

**Confirmation:** No repository files were modified, moved, renamed, deleted, staged, or committed during this audit. Only this untracked report was created on Main.

---

## Final status

**LAYOUT MIGRATION VERIFIED — NO BROADER MOVE AUTHORIZED**

W1 (`aovias-config` templates → `AOVIAS.Infrastructure/aovias-config`) is reflected consistently in Git, on disk, in `repository-layout.json`, and in production resolvers. Root-level **product** trees remain at the repository root by manifest design (`migrationStatus`: `at-repository-root`). A future whole-repository restructuring (Command/Studio nesting) is **not** evidenced as started and must not be inferred from W1.

---

## 1. Worktree state and preservation gate

| Worktree | Path | Branch | HEAD | Tracked tree |
|----------|------|--------|------|--------------|
| Main | `V:\DevelopmentRepo\AOVIAS` | `main` | `7758e30ce0f8c406fe8f292006895f67fb31cafd` | Clean |
| Cursor | `V:\DevelopmentRepo\AOVIAS.Cursor` | `agent/cursor/active` | `7758e30ce0f8c406fe8f292006895f67fb31cafd` | Clean |
| Claude | `V:\DevelopmentRepo\AOVIAS.Claude` | `agent/claude/active` | `7758e30ce0f8c406fe8f292006895f67fb31cafd` | Clean |

**Worktree registration:**

```text
V:/DevelopmentRepo/AOVIAS         7758e30 [main]
V:/DevelopmentRepo/AOVIAS.Claude  7758e30 [agent/claude/active]
V:/DevelopmentRepo/AOVIAS.Cursor  7758e30 [agent/cursor/active]
```

**Untracked inventory (preserved; not exhaustive):**

| Worktree | Notable untracked artifacts |
|----------|----------------------------|
| Main | Phase 0–1G reports (`AOVIAS_PHASE*.md`), `AOVIAS Website/…` ZIPs and `.lighthouseci-*` dirs, restructuring/assessment docs |
| Cursor | Phase 1F/1G reports, `scripts/dev/_phase3h_runtime_probe.py` (**present**) |
| Claude | `Aovias.CommandCenter/src/Aovias.CommandCenter.Api/Properties/` incl. `launchSettings.json` (**present**) |

**Gate result:** Expected branch/HEAD matched on all three worktrees. Artifact inventory is consistent with prior phases (additional Main reports from 1G-G/1G-H sync are expected). No repair or synchronization performed.

**Governance:** `AGENTS.md` two-worktree model respected; audit performed read-only from Main integration context with cross-worktree inspection only.

---

## 2. W1 configuration migration — verified facts

### 2.1 Git history (`a3188c9`)

**Subject:** `feat: migrate aovias config to infrastructure layout`

**Exact five tracked template files migrated (100% renames):**

| # | Pre-W1 path | Post-W1 path |
|---|-------------|--------------|
| 1 | `aovias-config/templates/nginx/runtime-agent.site.conf.template` | `AOVIAS.Infrastructure/aovias-config/templates/nginx/runtime-agent.site.conf.template` |
| 2 | `aovias-config/templates/publishing/Dockerfile.angular` | `AOVIAS.Infrastructure/aovias-config/templates/publishing/Dockerfile.angular` |
| 3 | `aovias-config/templates/publishing/Dockerfile.aspnet` | `AOVIAS.Infrastructure/aovias-config/templates/publishing/Dockerfile.aspnet` |
| 4 | `aovias-config/templates/publishing/Dockerfile.static` | `AOVIAS.Infrastructure/aovias-config/templates/publishing/Dockerfile.static` |
| 5 | `aovias-config/templates/publishing/Dockerfile.worker` | `AOVIAS.Infrastructure/aovias-config/templates/publishing/Dockerfile.worker` |

At parent `cc3df70`, **only** those five paths were tracked under `aovias-config/` (no tracked scripts or ops trees in Git at the old root).

**Same commit also:** updated `repository-layout.json`, resolver wiring, publish CLIs, Bootstrap default config resolution, CSM/Agent dev settings, tests (`RepositoryLayoutW1TransitionTests`, etc.), `.gitignore` dual-path ignores for ops subtrees.

### 2.2 Current Git-tracked config inventory

**Under old root `aovias-config/`:** **zero** tracked files (`git ls-files aovias-config/*` → empty).

**Under `AOVIAS.Infrastructure/aovias-config/`:** **five** tracked files (templates only). No other tracked files under `AOVIAS.Infrastructure/`.

### 2.3 Filesystem (all three worktrees — identical at HEAD)

| Path | Main | Cursor | Claude |
|------|------|--------|--------|
| `<root>/aovias-config` | **Absent** | **Absent** | **Absent** |
| `<root>/AOVIAS.Infrastructure/aovias-config` | **Present** | **Present** | **Present** |
| `…/templates/` | **Present** | **Present** | **Present** |
| `…/storage`, `secrets`, `logs`, `certificates` | **Absent** on disk | Same | Same |

Operational subtrees (`storage/`, `secrets/`, etc.) are **gitignored** at both legacy and infrastructure paths (`.gitignore` L79–86). Their absence on these worktrees is expected for clean clones; **hypothesis only:** other machines may still hold data under a legacy root `aovias-config/` — not observable here.

### 2.4 Old vs new coexistence and resolver behavior

**Manifest (`discovery.current`):**

- Publishing platform root: `requiredAll` → `AOVIAS.Infrastructure/aovias-config`; `requiredAny` → `ComplaincePlatform` or `Aovias.Runtime.Agent`.
- CSM platform root: `requiredAll` → `AOVIAS.Infrastructure/aovias-config`, `Aovias.Runtime.Agent`.

**Production profile:** `RepositoryLayoutProductionAdapter` uses **Current** profile only (`IsPermittedProductionProfile`).

**Resolution order (Publishing / CSM):**

1. Load `repository-layout.json`; walk ancestors with manifest markers (`RepositoryLayoutPathResolver`).
2. On failure → legacy walk (`LegacyPublishingPlatformRootDiscovery` / `LegacyCsmPlatformRootDiscovery` → `RepositoryLayoutPlatformRootDiscovery`).

**Config root directory (templates + runtime config tree on host):**

- `RepositoryLayoutAoviasConfigPaths.ResolveConfigRootDirectory(platformRoot)` **prefers** `AOVIAS.Infrastructure/aovias-config` when it exists; else legacy `aovias-config`; else returns infrastructure path (for error surfacing).
- **Test contract:** `ResolveConfigRootDirectory_prefers_infrastructure_when_both_exist` (`RepositoryLayoutW1TransitionTests.cs` L55–67).
- **Legacy discovery** accepts **either** infrastructure or root `aovias-config` as config marker (`ConfigDirectoryExistsAtRepositoryRoot`).

**Live repo integration assertion (static):** `Integration_manifest_current_markers_match_post_w1_repository_layout` expects **no** root-level `aovias-config` directory (L87). On-disk state satisfies that assertion’s filesystem check.

**If both paths existed on disk (hypothetical rollback machine):**

- Manifest markers require infrastructure path → manifest walk still finds repo root when infrastructure dir exists.
- Config content reads prefer infrastructure over legacy root.
- **Risk (hypothesis):** stale packages under legacy `storage/` could be ignored by tooling that resolves infrastructure only — would need runtime proof on that machine, not confirmed here.

### 2.5 Production configuration pointers

| Consumer | Setting / behavior | Value / mechanism | Classification |
|----------|-------------------|---------------------|----------------|
| `ComplianceServiceManager/appsettings.json` | `ConfigRoot` | `AOVIAS.Infrastructure/aovias-config` | **Current and valid** |
| `Aovias.Runtime.Agent/.../appsettings.Development.json` | `ConfigRoot` | `AOVIAS.Infrastructure/aovias-config` | **Current and valid** |
| `tools/PublishAllServices/Program.cs` | Platform root + `Aovias:ConfigRoot` | `RepositoryLayoutProductionAdapter.ResolvePublishingPlatformRoot` + `ResolveConfigRootDirectory` | **Current and valid** |
| `tools/PublishRuntimeAgent/Program.cs` | Same | Same | **Current and valid** |
| `Aovias.Bootstrap/.../Program.cs` | Default `--config-root` | `ResolveConfigRootDirectory(ResolvePublishingPlatformRoot(...))` | **Current and valid** |
| `Aovias.Publishing.Core/.../PlatformRootResolver.cs` | Platform root | `RepositoryLayoutProductionAdapter.ResolvePublishingPlatformRoot` | **Current and valid** |
| `ComplianceServiceManager/Services/RootPathResolver.cs` | CSM platform root | `RepositoryLayoutProductionAdapter.ResolveCsmPlatformRoot` | **Current and valid** |
| `ComplaincePlatform/scripts/build-praman-aovpkg.ps1` | Package storage root | Prefers `$infraConfigRoot`, fallback `$legacyConfigRoot` | **Intentional compatibility fallback** |

---

## 3. Repository root inventory (immediate children)

Classification uses `repository-layout.json` **current** paths as the intentional baseline. “Move impact” refers to a **future** manifest **target** wave, not W1.

### 3.1 Product / application projects

| Entry | Tracked | Manifest alignment | Move impact (future target) |
|-------|---------|-------------------|----------------------------|
| `ComplaincePlatform/` | Yes | `products.command.paths.compliancePlatform.current` | High — resolvers, solutions, Docker COPY |
| `WebApp/` | Yes | `products.command.paths.webApp.current` | High |
| `ComplianceServiceManager/` | Yes | Studio product, root today | High |
| `ComplianceServiceManager.Operations/` | Yes | Related CSM ops | Medium |
| `ComplianceServiceManager.Operations.Tests/` | Yes | Tests | Medium |
| `ComplianceServiceManager.Tests/` | Yes | Tests | Medium |
| `Aovias.CommandCenter/` | Yes | Studio product, root today | High |
| `Aovias.Runtime.Agent/` | Yes | Studio + discovery marker | High |
| `Aovias.Publishing/` | Yes | Studio product | High |
| `Aovias.Bootstrap/` | Yes | Studio product | High |
| `Aovias.Orchestration/` | Yes | Studio product | Medium |
| `Aovias.Release/` | Yes | Studio product | Medium |
| `Aovias.DesignSystem/` | Yes | Studio product | Medium |
| `AOVIAS.Website` path via `AOVIAS Website/` | Yes (nested) | `products.website.paths.monorepoRoot.current` | Medium (O-7 flatten) |
| `AOVIAS.AccredaOne/` | Yes | AccredaOne scaffold | Low (additive) |

### 3.2 Shared libraries and contracts

| Entry | Tracked | Manifest | Move impact |
|-------|---------|----------|-------------|
| `Aovias.Configuration/` | Yes | Studio `configuration.current` | High — layout contract hub |
| `Aovias.Deployment.Contracts/` | Yes | Studio | Medium |
| `Aovias.Runtime.Contracts/` | Yes | O-4 unresolved | Medium |

### 3.3 Infrastructure and deployment assets

| Entry | Tracked | Manifest | Notes |
|-------|---------|----------|-------|
| `AOVIAS.Infrastructure/aovias-config/templates/` | Yes (5 files) | `infrastructure.paths.aoviasConfig` at **target** layout | **W1 complete** |
| `Aovias.ReleaseValidation/` | Yes | `releaseValidation.current` at root | Future: `AOVIAS.Infrastructure/Aovias.ReleaseValidation` |
| `Aovias.DeploymentValidation/` | Yes | Studio (O-3 provisional) | Resolver/Docker scan roots |
| `preflight-docker-probe/` | Yes | O-8 unresolved | Local probe asset |

### 3.4 Tests and QA

Test projects live under product trees (e.g. `Aovias.Configuration/tests/`, `ComplianceServiceManager.Tests/`). Root-level `*.Tests` folders listed above. **Intentional** at root today.

### 3.5 Documentation and historical phase reports

| Entry | Tracked | Notes |
|-------|---------|-------|
| `docs/` | Yes | Product/milestone documentation |
| `ACCREDACONE_KNOWLEDGEBASE_READINESS_ASSESSMENT.md` | Yes | Assessment |
| `AOVIAS_REPOSITORY_RESTRUCTURING_ASSESSMENT.md` | Yes | Pre-migration planning |
| `AOVIAS_PHASE*.md` (many) | **Untracked** on Main | Phase audit trail; preserve |
| `AOVIAS_GIT_PATH_ANOMALY_REPORT.md` | Untracked | Phase artifact |

Many phase/milestone **tracked** docs still **mention** root `aovias-config` historically — see §5.

### 3.6 Build, development, and automation tooling

| Entry | Tracked | Manifest | Notes |
|-------|---------|----------|-------|
| `tools/` | Yes | `publishTools.current` → future infra | Publish CLIs W1-updated |
| `scripts/` | Yes | Repo automation | No `aovias-config` string hits in `scripts/` |
| `ai-governance/` | Yes | Governance | — |

### 3.7 Repository metadata and configuration

| Entry | Tracked | Notes |
|-------|---------|-------|
| `.git/`, `.gitattributes`, `.gitignore` | Yes / meta | Dual ignore paths for config ops dirs |
| `AGENTS.md` | Yes | Agent contract |
| `repository-layout.json` | Yes | **Source of truth** for current vs target |
| `Aovias.Platform.slnx`, `Aovias.PublishingEngine.slnx` | Yes | Root solutions per manifest |

### 3.8 Local-only or generated artifacts

| Entry | Tracked | Notes |
|-------|---------|-------|
| `AOVIAS Website/.../*.zip` | Untracked | Local packaging |
| `AOVIAS Website/.../.lighthouseci-*` | Untracked | CI artifacts |
| Cursor `scripts/dev/_phase3h_runtime_probe.py` | Untracked | Dev probe |
| Claude `Properties/launchSettings.json` | Untracked | Local dev |
| `bin/`, `obj/` (under projects) | Ignored | Not root entries |

---

## 4. Manifest compliance assessment

| Check | Result | Evidence |
|-------|--------|----------|
| `products[infrastructure].migrationStatus` = `infrastructure-config-at-target` | **Pass** | Config tree under `AOVIAS.Infrastructure/aovias-config` |
| `discovery.current.platformRoot` markers on disk | **Pass** | Infra config + `ComplaincePlatform` + `Aovias.Runtime.Agent` exist |
| `discovery.current.csmPlatformRoot` markers | **Pass** | Infra config + `Aovias.Runtime.Agent` |
| Obsolete root `aovias-config/` directory | **Pass** | Absent all worktrees |
| Command/Studio `migrationStatus: at-repository-root` | **Pass** | Product folders still at root (intentional **current**, not defect) |
| `discovery.target.*` paths | **N/A on disk** | Target entries are proposals; directories not required to exist |
| `worktreePathsUnchangedByProductMoves` | **Pass** | Worktrees unchanged |

**Confirmed defect:** None for W1 scope on these worktrees.

**Hypotheses (not confirmed here):**

- Developer machines with legacy **ignored** `aovias-config/storage` may need physical migration before decommissioning old path (see Phase 1G-A / 1D-B themes).
- Documentation drift (root `aovias-config` prose) — quality issue, not layout failure.

---

## 5. Dependency and reference analysis

Legend: **V** = current and valid · **F** = intentional fallback · **D** = historical documentation · **T** = test/fixture · **S** = potentially stale · **U** = unresolved / needs runtime proof

### 5.1 Production code and scripts (non-test)

| File | Reference | Class |
|------|-----------|-------|
| `repository-layout.json` | Infra paths, compatibility note for legacy root | **V** |
| `Aovias.Configuration/Layout/RepositoryLayoutAoviasConfigPaths.cs` | Both path constants; prefer infra | **V** / **F** |
| `Aovias.Configuration/Layout/RepositoryLayoutPlatformRootDiscovery.cs` | Dual config markers in errors and walks | **V** / **F** |
| `Aovias.Configuration/Layout/LegacyCsmPlatformRootDiscovery.cs` | Comment references `aovias-config` | **V** (delegates to shared discovery) |
| `Aovias.Configuration/Layout/RepositoryLayoutProductionAdapter.cs` | Manifest-first, legacy fallback | **V** |
| `ComplianceServiceManager/appsettings.json` | `AOVIAS.Infrastructure/aovias-config` | **V** |
| `Aovias.Runtime.Agent/.../appsettings.Development.json` | Same | **V** |
| `tools/PublishAllServices/Program.cs`, `tools/PublishRuntimeAgent/Program.cs` | Adapter + `ResolveConfigRootDirectory` | **V** |
| `Aovias.Bootstrap/.../Program.cs` | Same pattern for local platform + config root | **V** |
| `Aovias.Publishing/.../PlatformRootResolver.cs` | Adapter | **V** |
| `ComplianceServiceManager/Services/RootPathResolver.cs` | CSM adapter | **V** |
| `ComplaincePlatform/scripts/build-praman-aovpkg.ps1` | Infra + legacy roots | **F** |
| `Aovias.DeploymentValidation/Validators/DeploymentValidationStages.cs` | Detects `V:/DevelopmentRepo/V1.0/aovias-config` in output | **T** (guardrail on stale absolute paths) |
| `Aovias.Runtime.Agent/.dockerignore` | Both storage ignore paths | **V** |
| `Aovias.Runtime.Agent/.env.example` | Generic `/path/to/aovias-config` | **D** (host convention) |
| `Aovias.Runtime.Agent/DEPLOYMENT.md` | Documents infra templates path | **V** |

### 5.2 Tests (representative)

| Area | Class |
|------|-------|
| `RepositoryLayoutW1TransitionTests.cs`, `RepositoryLayoutDiscoveryHardeningTests.cs`, contract tests | **T** / **V** |
| `DeploymentValidationTests.cs`, `BootstrapValidationTests.cs` | **T** (synthetic `V1.0/aovias-config`) |
| `EnvironmentNeutralPortabilityRegressionTests.cs`, `RegressionTests.cs`, `ConfigTemplateGeneratorTests.cs` | **V** (walk to `AOVIAS.Infrastructure/aovias-config`) |
| `ContractDrivenPublishingTests.cs` | **T** (excludes `aovias-config/` segment from scans) |

### 5.3 CI, Docker, Compose

| Scope | Finding |
|-------|---------|
| `.github/` | **No** `aovias-config` matches (grep) |
| `**/docker-compose*.*` | **No** matches |
| `**/Dockerfile*` (repo-wide grep) | **No** `aovias-config` path strings in Dockerfiles |
| Runtime Agent image | Config supplied via mount/env, not COPY of template tree (consistent with W1) |

### 5.4 Phase reports and assessments (untracked / tracked)

Multiple `AOVIAS_PHASE*.md`, `AOVIAS_REPOSITORY_RESTRUCTURING_ASSESSMENT.md`, `docs/milestones/*.md` still describe **pre-W1** root layout. **D** — not execution defects.

### 5.5 Parent-directory discovery

Post–1G-D, legacy `Resolve()` paths **throw** if markers missing (no silent `..` escape). Manifest walk remains upward from `startDirectory`. **V** for hardened behavior; full runtime matrix not re-executed in this audit (no build/test run per scope).

---

## 6. What moved vs what stayed (summary)

| Category | W1 change | Still at repository root |
|----------|-----------|---------------------------|
| Config **templates** (5 files) | **Moved** to `AOVIAS.Infrastructure/aovias-config/templates/` | — |
| Config **ops data** (`storage`, `secrets`, …) | Never tracked; ignore rules duplicated for both paths | N/A in Git |
| Legacy **`aovias-config/` directory** | Removed as tracked tree (renames only) | **Does not exist** on disk |
| Command platform (`ComplaincePlatform`, `WebApp`) | **Unchanged** | Yes |
| Studio stack (CSM, Publishing, Agent, …) | **Unchanged** | Yes |
| `tools/`, `scripts/`, solutions | **Unchanged** location (tooling **updated** for new config path) | Yes |
| `AOVIAS.Infrastructure/` | **Created** as config holder (templates only in Git) | Yes |

---

## 7. Recommended next phase (no authorization implied)

| Priority | Phase theme | Evidence required before any move |
|----------|-------------|-----------------------------------|
| 1 | **Docs-only cleanup (G1-class)** | Inventory of milestone/phase docs referencing root `aovias-config`; no path moves |
| 2 | **Legacy ops data audit (machine-local)** | List `aovias-config/storage` vs `AOVIAS.Infrastructure/aovias-config/storage` on deploy hosts; backup before delete |
| 3 | **Manifest target waves (W2+)** | Explicit wave authorization; update `discovery.target` wiring; full solution/project matrix (INV-1); CI/Docker proof |
| 4 | **Unresolved O-3–O-8** | Human decisions recorded in manifest before moving `DeploymentValidation`, `tools/`, website flatten, `preflight-docker-probe` |

**Not recommended without separate authorization:** Moving Command or Studio trees to `AOVIAS.Command/` / `AOVIAS.Studio/` — would touch resolvers, slnx paths, Docker COPY lists in manifest **target**, and hundreds of project references.

---

## 8. Verification commands (read-only replay)

```powershell
$ROOT = "V:\DevelopmentRepo\AOVIAS"
$TARGET = "7758e30ce0f8c406fe8f292006895f67fb31cafd"

git -C $ROOT rev-parse main
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" rev-parse HEAD
git -C "V:\DevelopmentRepo\AOVIAS.Claude" rev-parse HEAD

Test-Path (Join-Path $ROOT "aovias-config")                              # expect False
Test-Path (Join-Path $ROOT "AOVIAS.Infrastructure\aovias-config\templates")  # expect True
git -C $ROOT ls-files "aovias-config/*"
git -C $ROOT ls-files "AOVIAS.Infrastructure/aovias-config/*"

git -C $ROOT show --summary a3188c91975b7d0ca444c9478c0b1590eb747837
```

Optional (requires test run authorization elsewhere):

```powershell
dotnet test Aovias.Configuration/tests/Aovias.Configuration.Tests/Aovias.Configuration.Tests.csproj --filter "FullyQualifiedName~RepositoryLayoutW1TransitionTests"
```

---

## 9. Phase 1H-A completion

| Deliverable | Path |
|-------------|------|
| Layout reality report | `AOVIAS_PHASE1HA_REPOSITORY_LAYOUT_REALITY_REPORT.md` (untracked on Main) |

**Final status:** **LAYOUT MIGRATION VERIFIED — NO BROADER MOVE AUTHORIZED**
