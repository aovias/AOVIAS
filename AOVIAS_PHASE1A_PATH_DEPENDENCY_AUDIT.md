# AOVIAS Phase 1A — Path-Dependency Audit

**Date:** 2026-10-25  
**Baseline HEAD:** `5a1526d99a68241d40ccba759d8fc62586f4af86`  
**Scope:** Read-only inventory — **no code or config changes**

**Method:** Targeted reads of mandated files + repository search for `DevelopmentRepo`, `ComplaincePlatform`, `WebApp`, `aovias-config`, and resolver symbols. Findings classified **Verified** (file read at HEAD) or **Discovered** (search hit; spot-check recommended before 1B).

**Security:** No credentials, tokens, or secret values recorded.

---

## 1. Executive summary

Path sensitivity is **concentrated in Studio** (CSM, Publishing, Bootstrap, Docker, publish CLIs) and **dev automation** (PowerShell scripts, tests with `V:\DevelopmentRepo\...`). **Command backend** projects are largely **relative** within `ComplaincePlatform/` but CI and logging assume folder name **`ComplaincePlatform`**. **Website** uses repo-relative npm paths plus **absolute paths in QA artifact JSON** (low runtime impact).

**Highest impact:** `PlatformRootResolver`, `RootPathResolver`, `SolutionDeployableProjectDiscoverer`, `RuntimeAgentDockerContextStager`, `Aovias.Runtime.Agent/Dockerfile`, `DevelopmentWorkspaceRegistry.BuildDefaults`.

---

## 2. Mandated file findings

### 2.1 `ComplianceServiceManager/Services/RootPathResolver.cs`

| Symbol | Current assumption | Layout impact | Remediation |
|--------|-------------------|---------------|-------------|
| `ResolveCompliancePlatformRoot()` | Walk up until directory named **`ComplaincePlatform`** exists | **Breaks** if only `AOVIAS.Command/CompliancePlatform/` exists at repo root | Add search path `AOVIAS.Command/CompliancePlatform` or layout config |
| `ResolveWebAppRoot()` | Walk for folder **`WebApp`** | **Breaks** if WebApp only under `AOVIAS.Command/WebApp/` | Same |
| `ResolvePlatformRoot()` | **`Aovias.Runtime.Agent`** + **`aovias-config`** siblings | **Breaks** when Studio subtree nested | Walk from repo root with nested markers |
| `ResolveRuntimeAgentProjectPath()` | `Aovias.Runtime.Agent/src/.../Api.csproj` under platform root | **Breaks** after Studio move | Prefix with `AOVIAS.Studio/` or configurable |
| `ResolveBootstrapHostProjectPath()` | `Aovias.Bootstrap/src/...` | Same | Same |
| Fallback `..\..\..\..\` paths | Assumes CSM output depth to repo | **Fragile** after any depth change | Prefer walk-up over fixed `..` count |

### 2.2 `ComplianceServiceManager/Services/SourceContext/SourceDiscoveryBoundary.cs`

| Symbol | Current assumption | Layout impact | Remediation |
|--------|-------------------|---------------|-------------|
| `GetDefaultApplicationRoots()` | Subdirs **`ComplaincePlatform`**, **`WebApp`** under `context.RootPath` | **Breaks** without those names at repo root | Map product folder or configurable list |

### 2.3 `Aovias.Publishing/.../PlatformRootResolver.cs`

| Symbol | Current assumption | Layout impact | Remediation |
|--------|-------------------|---------------|-------------|
| `Resolve()` | **`aovias-config`** AND (**`ComplaincePlatform`** OR **`Aovias.Runtime.Agent`**) at same root | **Breaks** if Command nested or `aovias-config` under Infrastructure only | Multi-root markers: repo root vs product roots |
| `ResolveWebAppRoot()` | `{platformRoot}/WebApp` | **Breaks** if WebApp nested | Configurable relative segment |
| `ResolveRuntimeAgentProjectPath()` | Fixed relative path under platform root | Studio move | Update relative path |

### 2.4 `Aovias.Publishing/.../SolutionDeployableProjectDiscoverer.cs`

| Symbol | Current assumption | Layout impact | Remediation |
|--------|-------------------|---------------|-------------|
| `GetScanRoots()` | **`ComplaincePlatform`**, **`Aovias.Runtime.Agent`** only | Misses services if path changes; ignores CommandCenter | Update scan roots after Command/Studio waves |
| `IsIgnoredPath()` | Excludes **`ComplianceServiceManager`**, **`tests`**, **`D0RuntimeVerifier`** | May need new ignore segments if paths change | Review after move |
| Angular discovery | `WebApp/angular.json` via `ResolveWebAppRoot` | Coupled to WebApp location | Same as resolver |

### 2.5 `ComplianceServiceManager/Services/Workspaces/DevelopmentWorkspaceRegistry.cs`

| Symbol | Current assumption | Layout impact | Remediation |
|--------|-------------------|---------------|-------------|
| `BuildDefaults()` | Parent of platform root → **`DevelopmentRepo`**; paths **`AOVIAS`**, **`AOVIAS.Cursor`**, **`AOVIAS.Claude`** | **Low** for repo layout migration (worktree paths unchanged) | Document that product moves do not change worktree paths |
| `%LocalAppData%\AoviasStudio\development-workspaces.json` | User machine persistence | **Medium** — stale paths after human renames worktrees | Migration note for operators |

### 2.6 Dev scripts

| File | Assumption | Impact | Remediation |
|------|------------|--------|-------------|
| `scripts/dev/agent-preflight.ps1` | Defaults **`V:\DevelopmentRepo\AOVIAS{,.Cursor,.Claude}`** | Machine-specific; not layout | Parameterize; document |
| `scripts/dev/worktree-status.ps1` | Same | Same | Same |
| `scripts/dev/agent-completion-check.ps1` | Same | Same | Same |

**Note:** Preflight requires **clean** worktree — conflicts with intentional untracked probe/Properties unless scripts exempt `??` paths (future change).

### 2.7 `scripts/verify-build.sh` / `scripts/verify-tests.sh`

| Assumption | Impact | Remediation |
|------------|--------|-------------|
| `REPO_ROOT/ Aovias.Platform.slnx` | **Breaks** when slnx moves under Studio | Update path constant |
| `dotnet restore --locked-mode` | **N/A** today — no repo-wide lock file (Phase 0B) | Separate baseline decision |

### 2.8 `Aovias.Runtime.Agent/Dockerfile`

| Assumption | Impact | Remediation |
|------------|--------|-------------|
| Comment: context = platform root | **Critical** | Set CI `docker build` context to new root |
| `COPY Aovias.Configuration/` … `Aovias.Runtime.Agent/` | Top-level folder names | After Studio nesting, COPY must be `AOVIAS.Studio/Aovias.Configuration/` or build from staged context via `RuntimeAgentDockerContextStager` |

### 2.9 `ComplaincePlatform/.github/workflows/platform-integrity.yml`

| Assumption | Impact | Remediation |
|------------|--------|-------------|
| Steps use paths like `Shared.Infrastructure/...`, `tests/Aovias.Platform.IntegrationTests/...` | Relative to **`ComplaincePlatform/`** checkout default | If workflow stays with Command tree, move workflow file or set `working-directory` |

### 2.10 Website CI — `AOVIAS Website/AOVIAS Web/.github/workflows/ci.yml`

| Assumption | Impact | Remediation |
|------------|--------|-------------|
| Checkout root = monorepo root | **Medium** if folder renamed to `AOVIAS.Website` | Update workflow paths and docs |
| Workspace packages `@aovias/*` | **Baseline FAIL** (Phase 0B) — not path layout | Fix package build order separately |

### 2.11 `docs/development/WORKTREE-OPERATIONS.md`

| Assumption | Impact | Remediation |
|------------|--------|-------------|
| Documents `V:\DevelopmentRepo\AOVIAS` integration path | Governance — correct | Add note that product subfolders do not replace worktree paths |

### 2.12 Tests — `ComplianceServiceManager.Tests/Workspaces/` and related

| File pattern | Assumption | Impact | Remediation |
|--------------|------------|--------|-------------|
| `SourceContextTests.cs` | Creates temp trees with **`ComplaincePlatform`** subfolder | Tests encode folder name | Update fixtures when layout changes |
| `RuntimeProfileDiscoveryValidationTests.cs` | **`V:\DevelopmentRepo\AOVIAS`**, `BuildDefaults(@"V:\DevelopmentRepo")` | CI on non-V: drives | Use temp dirs (already partially) |
| `LocalResourceMonitoringTests.cs` | **`DiskProbePath = V:\DevelopmentRepo`** | Environment-specific | Inject path |

### 2.13 Publishing tests (legacy paths)

| File | Assumption | Impact | Remediation |
|------|------------|--------|-------------|
| `ComposeGeneratorTests.cs` | **`V:\DevelopmentRepo\V1.0\ComplaincePlatform\...`** | Legacy layout | Update golden paths or use resolver |
| `ConfigTemplateGeneratorTests.cs` | **`V:/DevelopmentRepo/V1.0/aovias-config`** | Legacy | Same |
| `DeploymentLifecycleReadinessVerifierTests.cs` | Relative **`ComplaincePlatform/...`** | Breaks on Command move | Prefix with Command root |
| `PublishingProjectPathsTests.cs` | Live **`PlatformRootResolver`** + **`WebApp`** | Integration-style | Update when layout changes |

---

## 3. Additional discovered dependencies

| File | Symbol / key | Current assumption | Layout impact | Remediation |
|------|--------------|-------------------|---------------|-------------|
| `Aovias.Publishing/.../RuntimeAgentDockerContextStager.cs` | `FallbackMonorepoFolders`, `CopyRootPattern` | Top-level **`Aovias.*`** directories | **Critical** with Studio nesting | Parse Dockerfile + config manifest |
| `Aovias.Publishing/.../Generators.cs` | `TryResolvePlatformRoot` | Same as stager | **Critical** | Same |
| `tools/PublishAllServices/Program.cs` | `ResolvePlatformRoot()` | **`aovias-config`** + **`ComplaincePlatform`** | **High** | Unified resolver |
| `tools/PublishRuntimeAgent/Program.cs` | Same + `Aovias:ConfigRoot` | Combines platform root + **`aovias-config`** | **High** | Same |
| `Aovias.Bootstrap/.../Program.cs` | `ConfigRoot` default | **`V:/DevelopmentRepo/V1.0/aovias-config`** | Wrong even pre-migration on some machines | Environment variable + layout manifest |
| `Aovias.Configuration/Deployment/DeploymentPathResolver.cs` | `ForbiddenIdentityFragments` | Rejects **`DevelopmentRepo`**, user profile paths | **Policy** — deployment paths must not point at dev repo | Keep; validate generated packages after moves |
| `Aovias.DeploymentValidation/Validators/DeploymentValidationStages.cs` | Output scan | Fails on **`V:/DevelopmentRepo/V1.0/aovias-config`** in output | Leak detection | Good guardrail; fix templates |
| `ComplaincePlatform/Shared.Logging/LogPathResolver.cs` | Walk to **`ComplaincePlatform`** | Folder name | **Medium** | Keep name or symlink policy |
| `Aovias.ReleaseValidation/src/environment/service-registry.ts` | `PLATFORM_ROOT = 'ComplaincePlatform'` | Relative to CWD | **High** for harness | Configurable root |
| `WebApp/src/**/*.ts` | `@see ComplaincePlatform/docs/...` | Documentation links only | **Low** | Update doc paths |
| `docs/Architecture/*.md`, milestones | Path literals in examples | Docs | **Low** | Batch doc update wave |
| `docs/Architecture/studio-orchestration-s0/*.json` | **`V:\\DevelopmentRepo\\AOVIAS.Cursor`** | Assessment artifact | **Low** | Regenerate or archive |
| `AOVIAS Website/.../.qa-responsive/summary.json` | Absolute screenshot paths | QA artifact | **None** runtime | Ignore or regenerate |
| `ComplianceServiceManager.Tests/Development/LocalDevTabPresentationTests.cs` | Relative paths into **`ComplianceServiceManager/Views/...`** | Breaks if CSM moved | **Medium** | Update relative segments |
| `Aovias.Platform.slnx` | All **`Project Path="..."`** entries | **High** | Any move | Mass path update in slnx |
| `ComplaincePlatform/CompliancePlatform.slnx` | Service project paths | **High** | Command move | Update slnx + CI |
| Docker Compose under `ComplaincePlatform/docker/` | Build contexts | **Medium** | Per-service Dockerfile paths | Review compose build section |

---

## 4. Impact matrix (proposed layout vs today)

| Area | Command under `AOVIAS.Command/` | Studio under `AOVIAS.Studio/` | Infrastructure split |
|------|-----------------------------------|-------------------------------|----------------------|
| CSM local dev | **Broken** until resolvers updated | **Broken** until Agent/Bootstrap paths fixed | **`aovias-config`** move breaks root detection |
| Publishing discover/deploy | **Broken** scan roots | **Broken** Agent Docker staging | Config root separation |
| Command CI | **Broken** unless cwd adjusted | N/A | N/A |
| Command runtime (containers) | Compose/Dockerfile contexts | N/A | N/A |
| WebApp npm | Path-agnostic if API URLs env-based | N/A | N/A |
| Website monorepo | Independent | Independent | Independent |
| Git worktrees | **Unchanged** paths | **Unchanged** | **Unchanged** |

---

## 5. Recommended remediation order (proposal)

1. **Layout manifest JSON** (repo root, command root, studio root, infrastructure root, legacy aliases).
2. **Centralize** root resolution in Publishing + CSM (single module referenced by tools).
3. **Update** `SolutionDeployableProjectDiscoverer.GetScanRoots` + tests.
4. **Update** Dockerfile + stager + CI docker build context in one wave.
5. **Update** slnx files and verify scripts.
6. **Sweep** tests for `V:\DevelopmentRepo` and `V1.0` literals.
7. **Docs** last or in parallel (non-blocking).

---

## 6. Gaps and limits

| Gap | Severity |
|-----|----------|
| Not every file in repo grep’d exhaustively | Medium — spot-check before 1B |
| `docker-compose*.yml` service list not line-audited | Medium |
| Root `.github/` workflows (if any beyond website/Command) | Low — only two workflow files found at HEAD |
| IDE configs (`.vscode`, `.cursor`) | Low |
| AccredaOne | No runtime paths — future boundary only |

---

*Audit only. No remediation performed in Phase 1A.*
