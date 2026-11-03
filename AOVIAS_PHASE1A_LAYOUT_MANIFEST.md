# AOVIAS Phase 1A — Migration-Safe Layout Manifest

**Date:** 2026-10-25  
**Repository root:** `V:\DevelopmentRepo\AOVIAS`  
**Baseline HEAD (verified):** `5a1526d99a68241d40ccba759d8fc62586f4af86`  
**Scope:** Design and inventory only — **no moves, no source edits, no commits**

**Evidence sources:** Live tree inspection, `AOVIAS_REPOSITORY_RESTRUCTURING_ASSESSMENT.md`, `AOVIAS_PHASE0_BASELINE_VALIDATION.md`, `AOVIAS_PHASE0D_NORMALIZATION_EXECUTION.md`, resolver/source reads on `main`.

**Legend:** **Verified** = observed in repo at baseline HEAD. **Proposal** = target layout not authorized for execution. **Assumption** = requires human confirmation.

---

## 1. Baseline revalidation (Phase 1A, fresh)

| Worktree | Path | Branch | HEAD | Working tree |
|----------|------|--------|------|--------------|
| Main | `V:\DevelopmentRepo\AOVIAS` | `main` | `5a1526d…` | Tracked clean; untracked artifacts only (see §10) |
| Cursor | `V:\DevelopmentRepo\AOVIAS.Cursor` | `agent/cursor/active` | `5a1526d…` | `?? scripts/dev/_phase3h_runtime_probe.py` |
| Claude | `V:\DevelopmentRepo\AOVIAS.Claude` | `agent/claude/active` | `5a1526d…` | `?? Aovias.CommandCenter/src/Aovias.CommandCenter.Api/Properties/` |

**Verified:** All three HEADs equal `5a1526d99a68241d40ccba759d8fc62586f4af86`.  
**Verified:** Phase 0C preservation directory exists: `V:\DevelopmentRepo\AOVIAS-Phase0C-Preservation-20261025-131104`.

---

## 2. Target layout (proposal — not authorization to move)

```text
AOVIAS/
├── AOVIAS.Command/
│   ├── CompliancePlatform/          # keep ComplaincePlatform spelling until separate change
│   └── WebApp/
├── AOVIAS.Studio/
│   ├── Aovias.Platform.slnx
│   ├── Aovias.PublishingEngine.slnx
│   ├── ComplianceServiceManager/
│   └── Studio-owned projects and tools
├── AOVIAS.AccredaOne/               # scaffold only today
├── AOVIAS.Website/
├── AOVIAS.Shared/                   # only if cross-product refs justify (see §4)
├── AOVIAS.Infrastructure/
├── docs/
├── scripts/
├── ai-governance/
├── tools/
├── AGENTS.md
└── .gitignore
```

---

## 3. Current-to-target path mapping (primary moves)

| Current path | Proposed path | Product owner | Type | Migration risk | Notes |
|--------------|---------------|---------------|------|----------------|-------|
| `ComplaincePlatform/` | `AOVIAS.Command/CompliancePlatform/` | **Command** | Source + CI + compose | **High** | Spelling preserved in folder name until reviewed rename; 40+ services; CI relative paths |
| `WebApp/` | `AOVIAS.Command/WebApp/` | **Command** | Frontend app | **High** | Publishing discovers `WebApp/angular.json`; env API URLs |
| `ComplianceServiceManager/` | `AOVIAS.Studio/ComplianceServiceManager/` | **Studio** | WPF executable | **Medium** | Path resolvers walk to `ComplaincePlatform`/`WebApp` |
| `ComplianceServiceManager.Tests/` | `AOVIAS.Studio/ComplianceServiceManager.Tests/` | **Studio** | Tests | **Medium** | Hard-coded `V:\DevelopmentRepo\AOVIAS` |
| `ComplianceServiceManager.Operations/` | `AOVIAS.Studio/ComplianceServiceManager.Operations/` | **Studio** | Library | **Low** | In `Aovias.Platform.slnx` |
| `ComplianceServiceManager.Operations.Tests/` | `AOVIAS.Studio/...` | **Studio** | Tests | **Low** | Platform slnx |
| `Aovias.DesignSystem/` | `AOVIAS.Studio/Aovias.DesignSystem/` | **Studio** | WPF UI | **Medium** | CSM slnx only |
| `Aovias.Bootstrap/` | `AOVIAS.Studio/Aovias.Bootstrap/` | **Studio** | CLI/host | **High** | Default `ConfigRoot` uses legacy `V1.0` path |
| `Aovias.Runtime.Agent/` | `AOVIAS.Studio/Aovias.Runtime.Agent/` | **Studio** | Deployable API | **Critical** | Dockerfile COPY paths; stager folder list |
| `Aovias.Publishing/` | `AOVIAS.Studio/Aovias.Publishing/` | **Studio** | Library | **Critical** | Root resolver + discoverer scan roots |
| `Aovias.Release/` | `AOVIAS.Studio/Aovias.Release/` | **Studio** | Library | **Medium** | Platform slnx |
| `Aovias.Orchestration/` | `AOVIAS.Studio/Aovias.Orchestration/` | **Studio** | Library | **Medium** | Docker COPY in Agent build |
| `Aovias.CommandCenter/` | `AOVIAS.Studio/Aovias.CommandCenter/` **or** `AOVIAS.Infrastructure/...` | **UNRESOLVED** | Deployable API | **Medium** | Naming vs PRAMAN “Deficiency Command Center” UX |
| `Aovias.Configuration/` | `AOVIAS.Studio/Aovias.Configuration/` **(initial)** | **Studio** (initial) | Shared library | **High** | Consumed by Publishing, Agent, Bootstrap, Validation — hoist to Shared only after consumer audit |
| `Aovias.Deployment.Contracts/` | `AOVIAS.Studio/Aovias.Deployment.Contracts/` **(initial)** | **Studio** (initial) | Contracts | **High** | Same as Configuration |
| `Aovias.DeploymentValidation/` | `AOVIAS.Studio/` or `AOVIAS.Infrastructure/` | **UNRESOLVED** | Validation service | **Medium** | Forbidden path fragments include `DevelopmentRepo` |
| `Aovias.Runtime.Contracts/` | `AOVIAS.Studio/Aovias.Runtime.Agent/` or `AOVIAS.Shared/` | **UNRESOLVED** | Contracts | **Low** | CSM + Agent refs |
| `Aovias.ReleaseValidation/` | `AOVIAS.Infrastructure/Aovias.ReleaseValidation/` | **Infrastructure** | Node harness | **Medium** | Hard-coded `ComplaincePlatform` in TS registry |
| `aovias-config/` | `AOVIAS.Infrastructure/aovias-config/` | **Infrastructure** | Templates/config | **Critical** | Platform root marker for resolvers |
| `tools/PublishAllServices/` | `AOVIAS.Infrastructure/tools/` or stay under Studio | **UNRESOLVED** | CLI | **Medium** | Duplicated `ResolvePlatformRoot` |
| `tools/PublishRuntimeAgent/` | Same | **UNRESOLVED** | CLI | **Medium** | Same |
| `AOVIAS Website/AOVIAS Web/` | `AOVIAS.Website/` | **Website** | Next monorepo | **Medium** | Space in path; separate CI root |
| `docs/`, `scripts/`, `ai-governance/`, `AGENTS.md` | **Root** (unchanged role) | **Governance** | Docs/scripts | **Low** | Update path literals in scripts |
| `Aovias.Platform.slnx` | `AOVIAS.Studio/Aovias.Platform.slnx` | **Studio** | Solution | **High** | All project paths relative |
| `Aovias.PublishingEngine.slnx` | `AOVIAS.Studio/Aovias.PublishingEngine.slnx` | **Studio** | Solution | **High** | Same |
| `ComplaincePlatform/CompliancePlatform.slnx` | `AOVIAS.Command/CompliancePlatform/CompliancePlatform.slnx` | **Command** | Solution | **High** | CI workflow cwd |
| `ComplianceServiceManager/ComplianceServiceManager.slnx` | `AOVIAS.Studio/ComplianceServiceManager/...` | **Studio** | Solution | **Medium** | WPF leg |
| `AOVIAS.AccredaOne/` | *(new empty scaffold)* | **AccredaOne** | Placeholder | **Low** | No product code today |

**Verified:** No `ProjectReference` from `ComplaincePlatform/**/*.csproj` to root `Aovias.*` — Command and Studio are **filesystem-coupled**, not MSBuild-coupled.

**Policy:** `ComplaincePlatform/KnowledgeBase` stays under **Command** for this program; AccredaOne integration only via future **public publishing/API boundary** (assumption per mission brief).

---

## 4. Unresolved ownership decisions (human required before Phase 1B)

| ID | Item | Options | Evidence |
|----|------|---------|----------|
| O-1 | `Aovias.CommandCenter` | **Studio** (fleet registry API per `Program.cs`) vs **Infrastructure** (ops plane) | Standalone host; in `Aovias.Platform.slnx`; name collides with PRAMAN DCC routes in `WebApp` |
| O-2 | `Aovias.Configuration` / `Aovias.Deployment.Contracts` | **Studio-owned** (initial proposal) vs **AOVIAS.Shared** | No Command csproj refs; many Studio projects ref both |
| O-3 | `Aovias.DeploymentValidation` | Studio vs Infrastructure | Used by publishing pipeline |
| O-4 | `Aovias.Runtime.Contracts` | Under Runtime.Agent vs Shared | CSM + Agent consumers |
| O-5 | `tools/Publish*` | Infrastructure vs Studio/tools | Duplicate root resolution logic |
| O-6 | `AOVIAS.Shared` population | Minimal contracts only vs empty until wave 2 | Avoid premature “shared” dump |
| O-7 | Website path flatten | `AOVIAS.Website/` vs keep nested `AOVIAS Website/` | CI and docs use current path |
| O-8 | `preflight-docker-probe/` (repo root) | Infrastructure temp vs delete after use | **Verified** top-level dir exists |

---

## 5. Solution files (verified — keep distinct)

| Solution | Current path | Owner | Phase 0 build (historical) |
|----------|--------------|-------|----------------------------|
| `CompliancePlatform.slnx` | `ComplaincePlatform/` | Command | **PASS** |
| `Aovias.Platform.slnx` | repo root | Studio | **FAIL** (NU1903) |
| `Aovias.PublishingEngine.slnx` | repo root | Studio | **PASS** |
| `ComplianceServiceManager.slnx` | `ComplianceServiceManager/` | Studio | **FAIL** (NU1903) |
| `Aovias.CommandCenter.slnx` | `Aovias.CommandCenter/` | UNRESOLVED | **PASS** |
| `Aovias.Runtime.Agent.slnx` | `Aovias.Runtime.Agent/` | Studio | Not separately run in 0B |
| `Aovias.Publishing.slnx` | `Aovias.Publishing/` | Studio | Subset of PublishingEngine |
| `Aovias.Release.slnx` | `Aovias.Release/` | Studio | Subset of Platform |
| `Aovias.Orchestration.slnx` | `Aovias.Orchestration/` | Studio | Subset of Platform |

**Proposal:** Do not merge these solutions during layout migration; only update relative paths inside each file.

---

## 6. Required path-resolver changes (by migration wave)

| Component | File | Current assumption | Change after Command move | Change after Studio move |
|-----------|------|-------------------|---------------------------|--------------------------|
| CSM platform root | `RootPathResolver.cs` | Sibling folders `ComplaincePlatform`, `WebApp`, `Aovias.Runtime.Agent`, `aovias-config` | Walk includes `AOVIAS.Command/CompliancePlatform` **or** configurable roots | Agent/Bootstrap paths under `AOVIAS.Studio/` |
| CSM discovery | `SourceDiscoveryBoundary.cs` | Folder names `ComplaincePlatform`, `WebApp` | Add mapped names or single `AOVIAS.Command` scan | N/A |
| Publishing root | `PlatformRootResolver.cs` | `aovias-config` + (`ComplaincePlatform` OR `Aovias.Runtime.Agent`) | Extend markers for nested Command path | Agent under Studio |
| Deployable scan | `SolutionDeployableProjectDiscoverer.cs` | `platformRoot/ComplaincePlatform` | Update `GetScanRoots` | N/A |
| Docker stager | `RuntimeAgentDockerContextStager.cs` | Top-level `Aovias.*` folder names | N/A until Studio wave | COPY paths + stager list |
| Workspace defaults | `DevelopmentWorkspaceRegistry.cs` | `DevelopmentRepo/AOVIAS{,.Cursor,.Claude}` | Repo root unchanged; discovery roots may differ | N/A |
| Publish CLIs | `tools/PublishAllServices/Program.cs`, `PublishRuntimeAgent/Program.cs` | `aovias-config` + `ComplaincePlatform` | Same as resolvers | Studio-relative Agent |
| Command logging | `ComplaincePlatform/Shared.Logging/LogPathResolver.cs` | Walk to `ComplaincePlatform` folder name | Keep name or alias | N/A |
| Release validation | `Aovias.ReleaseValidation/.../service-registry.ts` | `PLATFORM_ROOT = 'ComplaincePlatform'` | Update constant or config | N/A |

**Proposal:** Introduce a single **repository layout manifest** (JSON) consumed by resolvers in Phase 1B+ instead of scattering string literals.

---

## 7. Tests required before and after each wave

| Wave | Pre-move tests | Post-move tests | Exit criteria |
|------|----------------|-----------------|---------------|
| **W0 — Baseline** | Phase 0 matrix (historical); optional re-run Platform/Command builds | N/A | Human sign-off on manifest |
| **W1 — Infrastructure** | `dotnet build` tools referencing `aovias-config` | Publish CLI smoke; DeploymentValidation unit tests | Resolvers find new `aovias-config` path |
| **W2 — Shared/Studio libs** (if any) | `Aovias.PublishingEngine.slnx` build | Publishing.Core.Tests; path tests | No hard-coded repo root in new Shared paths |
| **W3 — Studio tree** | `Aovias.PublishingEngine.slnx` **PASS**; Agent Docker build (not run in 0B) | `RuntimeAgentDockerContextStagerTests`; CSM slnx on Windows | Dockerfile context from new root |
| **W4 — Command tree** | `CompliancePlatform.slnx` build; `WebApp` npm build | `platform-integrity.yml` guardrails job paths; Publishing discoverer | CI cwd updated |
| **W5 — Website** | Website CI scripts (currently **FAIL** historically) | Lighthouse/npm after path fix | Independent CI green |
| **W6 — AccredaOne scaffold** | N/A | N/A | Empty folder + docs only |

**Verified:** Platform `dotnet test` was **NOT RUN** in Phase 0B (Platform build failed).

---

## 8. Docker, CI/CD, Compose, scripts (per wave)

| Artifact | Current | Impact | Remediation |
|----------|---------|--------|-------------|
| `Aovias.Runtime.Agent/Dockerfile` | Build context = repo root; COPY top-level `Aovias.*` | **Critical** | Context root + COPY prefixes after Studio move |
| `ComplaincePlatform/.github/workflows/platform-integrity.yml` | Runs from `ComplaincePlatform/` relative paths | **High** | `defaults.run.working-directory` or path prefix |
| `AOVIAS Website/AOVIAS Web/.github/workflows/ci.yml` | Monorepo root | **Medium** | Path if folder renamed |
| `scripts/verify-build.sh` | `${REPO_ROOT}/Aovias.Platform.slnx` | **High** | Point to `AOVIAS.Studio/Aovias.Platform.slnx` |
| `scripts/verify-tests.sh` | Same pattern | **High** | Same |
| `scripts/dev/*.ps1` | Default `V:\DevelopmentRepo\AOVIAS*` | **Medium** | Parameter defaults + docs |
| Docker Compose (Command) | Under `ComplaincePlatform/` | **Medium** | Service build contexts |
| Publishing generated compose | Uses discovered project paths | **High** | Re-publish after move |

---

## 9. Rollback considerations

- **Verified:** Git history preserves renames if `git mv` used; three worktrees must stay aligned (`merge --ff-only main` after each integration).
- **Assumption:** Rollback wave = revert merge commit on `main` + agent ff-only; no `git reset --hard` on agent trees without human approval (`AGENTS.md`).
- **Risk:** `%LocalAppData%\AoviasStudio\development-workspaces.json` may cache old workspace paths — document manual reset or migration script (Phase 1B).
- **Risk:** Generated `bin/obj`, `node_modules`, local package outputs not in Git — rollback source only; clean rebuild required.

---

## 10. Must remain in place until boundary exists

| Item | Reason |
|------|--------|
| `ComplaincePlatform/KnowledgeBase` | Command SSOT; no direct AccredaOne internal API access without public boundary |
| Cross-product coupling | Publishing metadata paths to Command services — move Command only after discoverer update |
| Untracked local artifacts | See §11 — never delete in migration waves |

---

## 11. Generated and untracked artifacts — do not move/delete accidentally

| Location | Artifact | Worktree |
|----------|----------|----------|
| `scripts/dev/_phase3h_runtime_probe.py` | Phase 3H probe | Cursor (untracked) |
| `Aovias.CommandCenter/.../Properties/launchSettings.json` | Local launch profile | Claude (untracked) |
| `AOVIAS Website/.../*.zip` | Release bundles | Main |
| `AOVIAS Website/.../.lighthouseci-r1-*` | Lighthouse output | Main |
| `AOVIAS_PHASE*.md`, anomaly reports | Phase 0/1 docs | Main (untracked until committed) |
| `ComplaincePlatform/logs/`, `**/bin/`, `**/obj/` | Build/runtime | All |
| `ComplianceServiceManager/packages/` | NuGet cache | gitignored |
| `Aovias.Bootstrap/bootstrap-reports/*.json` | May contain machine paths | Tracked — not secrets; path leakage |
| `preflight-docker-probe/`, `_tmp_phase_probe/`, `_perf_build_out/` | Local/temp | Verify gitignore before moves |

---

## 12. Migration wave sequence (proposal)

```text
W0  Human review this manifest + path audit + ownership decisions (O-1..O-8)
W1  AOVIAS.Infrastructure/  (aovias-config, ReleaseValidation, tools policy)
W2  Resolver manifest + script defaults (no product moves yet) — optional spike
W3  AOVIAS.Studio/           (Platform slnx, Agent, Publishing, CSM, …)
W4  AOVIAS.Command/          (ComplaincePlatform + WebApp)
W5  AOVIAS.Website/          (after npm baseline strategy)
W6  AOVIAS.AccredaOne/       (scaffold + integration design doc only)
```

**Exit criterion for entire Phase 1 program:** Cursor and Claude `agent/*/active` @ `main`; all waves integrated via human merge; build matrix re-run with recorded results.

---

*This document is a proposal. It does not authorize Phase 1B execution.*
