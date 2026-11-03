# AOVIAS Phase 0B — Baseline Validation Gate

**Date:** 2026-10-25  
**Repository:** `V:\DevelopmentRepo\AOVIAS`  
**Scope:** Assessment and validation only — **no restructuring, no Git writes, no source fixes**  
**Prior context:** `AOVIAS_GIT_PATH_ANOMALY_REPORT.md` (path `...` anomaly **resolved** — do not recreate)

---

## 1. Executive summary and gate decision

### Gate decision: **`READY WITH CONDITIONS`**

The integration worktree is on **`main`** with **no staged or unstaged tracked changes**. Untracked artifacts (website release zips, Lighthouse output dirs, Phase 0/0B reports) are **accounted for** and were **not modified**. Agent worktrees hold **preservation-critical local state** and are **behind `main`** at older checked-out SHAs despite having **zero commits ahead of `main`**.

Baseline builds show a **split picture**:

| Area | Outcome |
|------|---------|
| Command backend (`CompliancePlatform.slnx`) | **PASS** |
| Studio publishing leg (`Aovias.PublishingEngine.slnx`) | **PASS** |
| CommandCenter solution | **PASS** |
| Command WebApp (Angular) | **PASS** (warnings) |
| Studio platform CI leg (`Aovias.Platform.slnx`) | **FAIL** (NU1903 vulnerability-as-error) |
| Studio WPF leg (`ComplianceServiceManager.slnx`) | **FAIL** (same NU1903 via dependencies) |
| Corporate website npm CI scripts | **FAIL** (workspace package resolution / TS config) |
| Platform `dotnet test` | **NOT RUN** (build prerequisite failed) |
| Runtime Agent Docker | **NOT RUN** (not required for gate; documented command below) |

**Conditions before Phase 1 (path layout / resolver work):**

1. **Human preservation pass** on Cursor and Claude worktrees (see §3) before any `checkout` / fast-forward.
2. **Treat NU1903 platform restore/build failure** as a known Studio baseline blocker (separate remediation track; not fixed in this phase).
3. **Treat website monorepo npm failures** as environmental/workspace integrity evidence (investigate `@aovias/*` workspace linking; no `npm ci` was run on Main during this phase).
4. **Normalize agent worktrees** to `agent/*/active` @ `main` after preserving local edits (per `AGENTS.md`).

**Not granted:** Unconditional Phase 1 go-ahead without addressing worktree preservation and documenting acceptance of known build failures.

---

## 2. Current branch, HEAD, and status (each worktree)

Commands executed on integrator machine via read-only Git + build invocations (2026-10-25). **Observed facts.**

### 2.1 Main — `V:\DevelopmentRepo\AOVIAS`

| Field | Value |
|-------|--------|
| Root | `V:/DevelopmentRepo/AOVIAS` |
| Branch | `main` |
| HEAD | `5a1526d99a68241d40ccba759d8fc62586f4af86` |
| Latest commit | `5a1526d Merge branch 'docs/website-and-restructuring-assessment' into main` |
| Staged changes | **None** (`git diff --cached --stat` empty) |
| Unstaged tracked changes | **None** (`git diff --stat` empty) |
| `git diff --check` | **PASS** (empty) |

**Untracked (`git status --short`):**

| Path | Notes |
|------|--------|
| `AOVIAS Website/AOVIAS Web/AOVIAS.Website.v1.0.0.zip` | Release artifact |
| `AOVIAS Website/AOVIAS Web/AOVIAS.Website.v1.1.0.zip` | Release artifact |
| `AOVIAS Website/AOVIAS Web/apps/website/.lighthouseci-r1-4/` | CI output (not matched by `.lighthouseci/` ignore) |
| `AOVIAS Website/AOVIAS Web/apps/website/.lighthouseci-r1-art/` | Same |
| `AOVIAS Website/AOVIAS Web/apps/website/.lighthouseci-r1-ui/` | Same |
| `AOVIAS_PHASE0_MIGRATION_READINESS.md` | Assessment doc |
| `AOVIAS_GIT_PATH_ANOMALY_REPORT.md` | Anomaly resolution doc |
| `AOVIAS_PHASE0_BASELINE_VALIDATION.md` | This report (created during 0B) |

**Historical reference SHA `5a1526d…` matches current `main` HEAD** (verified).

### 2.2 Cursor — `V:\DevelopmentRepo\AOVIAS.Cursor`

| Field | Value |
|-------|--------|
| Root | `V:/DevelopmentRepo/AOVIAS.Cursor` |
| Branch | `agent/cursor/phase3h-propagation-contract-closure` |
| HEAD | `44e83120a74c2de8736206dcec789f930003740a` |
| vs `main` | **0 ahead**, **13 behind** (`git log HEAD..main`) |
| `git diff --check` | **PASS** |

**Working tree:**

| Class | Path | Detail |
|-------|------|--------|
| Unstaged tracked | `ComplaincePlatform/IdentityService/IdentityService.csproj` | Local diff vs Cursor HEAD (`5 ++++--` per `git diff --stat`). **Note:** `main` at `5a1526d` already includes `Condition="Exists(...)"` on `identity-signing.pfx`; Cursor copy may differ by comments or minor lines — **preserve until diff reviewed against `main`**. |
| Untracked | `scripts/dev/_phase3h_runtime_probe.py` | Phase 3H runtime probe; not in index |

No staged changes.

### 2.3 Claude — `V:\DevelopmentRepo\AOVIAS.Claude`

| Field | Value |
|-------|--------|
| Root | `V:/DevelopmentRepo/AOVIAS.Claude` |
| Branch | `agent/claude/def-env-006-angular-lifecycle` |
| HEAD | `ddcb59ee537748649ff079749fda0163feec9f15` |
| vs `main` | **0 ahead**, **52 behind** (`git log HEAD..main`) |
| `git diff --check` | **PASS** |

**Working tree:**

| Class | Path | Detail |
|-------|------|--------|
| Untracked directory | `Aovias.CommandCenter/src/Aovias.CommandCenter.Api/Properties/` | Includes `launchSettings.json`; never committed in this worktree |
| Staged / unstaged tracked | **None** | `git diff --stat` empty |

**Historical reference:** branch names match prior Phase 0 docs; SHAs match prior observation for agent tips (Cursor `44e83120`, Claude `ddcb59ee`).

---

## 3. Uncommitted and untracked preservation inventory

| Worktree | Committed vs `main` | Staged | Unstaged tracked | Untracked | Action before sync |
|----------|---------------------|--------|------------------|-----------|-------------------|
| Main | At `main` | None | None | Zips, lighthouseci, reports | Do not delete; commit reports when approved |
| Cursor | Fully merged (0 ahead) | None | `IdentityService.csproj` | `_phase3h_runtime_probe.py` | Commit or copy probe + reconcile csproj vs `main` before ff |
| Claude | Fully merged (0 ahead) | None | None | `CommandCenter` `Properties/` | Commit to task branch or copy locally before ff |

**Inference:** Reflog/history suggests phase3h and def-env-006 are **integrated into `main`**, but **reflog alone is not proof** that Cursor’s **current unstaged csproj** or untracked files are on `main`. **Human diff required.**

**Forbidden during 0B (observed compliance):** No reset, clean, checkout, merge, commit, stage, or deletes performed.

---

## 4. Repository and solution inventory (actual paths)

### 4.1 AOVIAS Command (PRAMAN runtime)

| Component | Path | Entry point |
|-----------|------|-------------|
| Backend services | `ComplaincePlatform/` | `ComplaincePlatform/CompliancePlatform.slnx` |
| API gateway | `ComplaincePlatform/Gateway/` | In slnx |
| KnowledgeBase | `ComplaincePlatform/KnowledgeBase/KnowledgeBase.csproj` | In slnx |
| Angular portal | `WebApp/` | `WebApp/package.json`, `WebApp/angular.json` |
| Command CI | `ComplaincePlatform/.github/workflows/platform-integrity.yml` | Relative to `ComplaincePlatform/` |
| Local compose | `ComplaincePlatform/docker-compose.dev.yml`, `docker-compose.commercial.yml` | Context `.` |

### 4.2 AOVIAS Studio (platform + operator shell)

| Component | Path | Entry point |
|-----------|------|-------------|
| Cross-platform CI solution | `Aovias.Platform.slnx` (repo root) | `scripts/verify-build.sh`, `scripts/verify-tests.sh` |
| Publishing-focused solution | `Aovias.PublishingEngine.slnx` (repo root) | — |
| WPF operator shell | `ComplianceServiceManager/` | `ComplianceServiceManager/ComplianceServiceManager.slnx` |
| Design system (WPF) | `Aovias.DesignSystem/` | Via CSM slnx |
| Bootstrap, Release, Publishing, Orchestration, Runtime Agent | `Aovias.Bootstrap/`, `Aovias.Release/`, `Aovias.Publishing/`, `Aovias.Orchestration/`, `Aovias.Runtime.Agent/` | Per-area `*.slnx` + Platform slnx |
| Publish CLIs | `tools/PublishAllServices/`, `tools/PublishRuntimeAgent/` | In Platform slnx |
| Operations engine | `ComplianceServiceManager.Operations/` | Platform slnx |

### 4.3 AOVIAS Website (corporate / marketing)

| Component | Path | Entry point |
|-----------|------|-------------|
| Monorepo root | `AOVIAS Website/AOVIAS Web/` | `package.json` (workspaces `apps/*`, `packages/*`) |
| Next app | `AOVIAS Website/AOVIAS Web/apps/website/` | `npm run build` via root scripts |
| CI | `AOVIAS Website/AOVIAS Web/.github/workflows/ci.yml` | `npm ci`, lint, typecheck, build |
| Docker | `AOVIAS Website/AOVIAS Web/deployment/docker/Dockerfile`, `deployment/compose/` | `npm run docker:build` |

### 4.4 Other areas

| Area | Path |
|------|------|
| CommandCenter API | `Aovias.CommandCenter/` → `Aovias.CommandCenter.slnx` |
| Shared contracts (future `AOVIAS.Shared`) | `Aovias.Configuration/`, `Aovias.Deployment.Contracts/` (repo root) |
| Infrastructure templates | `aovias-config/` |
| Release validation harness | `Aovias.ReleaseValidation/` (Node; partial) |
| Docs / governance | `docs/`, `AGENTS.md`, `ai-governance/` |
| Dev scripts | `scripts/dev/*.ps1`, `scripts/verify-*.sh` |

**AccredaOne:** No dedicated product directory; KnowledgeBase remains under Command.

---

## 5. Build and test matrix

**Environment observed:** `dotnet 10.0.103`, `node v24.13.0`, `npm 11.6.2`, `docker 29.1.3`.  
**Working directory for .NET:** `V:\DevelopmentRepo\AOVIAS` unless noted.  
**No project files were modified to force passes.**

| ID | Command | Area | Result | Notes |
|----|---------|------|--------|-------|
| S1 | `git diff --check` (×3 worktrees) | Static | **PASS** | All empty |
| S2 | Solution file existence | Static | **PASS** | 9 `*.slnx` at documented paths |
| B1 | `dotnet restore Aovias.Platform.slnx` | Studio | **FAIL** | NU1903: `SSH.NET` 2024.2.0; `Microsoft.AspNetCore.SignalR.Common` 9.0.9 (vulnerability as error) |
| B2 | `dotnet build Aovias.Platform.slnx -c Release` | Studio | **FAIL** | Same NU1903; restore/build blocked |
| B3 | `dotnet test Aovias.Platform.slnx -c Release --no-build` | Studio | **NOT RUN** | B2 failed |
| B4 | `dotnet build Aovias.PublishingEngine.slnx -c Release` | Studio | **PASS** | 0 errors |
| B5 | `dotnet build ComplianceServiceManager/ComplianceServiceManager.slnx -c Release` | Studio | **FAIL** | NU1903 via Bootstrap.Execution / Runtime.SDK dependencies |
| B6 | `dotnet build ComplaincePlatform/CompliancePlatform.slnx -c Release` | Command | **PASS** | 463 warnings, 0 errors |
| B7 | `dotnet build Aovias.CommandCenter/Aovias.CommandCenter.slnx -c Release` | Auxiliary | **PASS** | 0 errors |
| B8 | `npm run typecheck` (website root) | Website | **FAIL** | TS6053/1259; `@aovias/typescript-config` resolution |
| B9 | `npm run lint` (website root) | Website | **FAIL** | `@aovias/eslint-config` not found |
| B10 | `npm run build` (website root) | Website | **FAIL** | Missing `@aovias/typescript-config/nextjs.json` |
| B11 | `npm run build` (`WebApp/`) | Command UI | **PASS** | Angular build OK; NG8113/NG8107/budget warnings |
| B12 | `docker build -f Aovias.Runtime.Agent/Dockerfile .` | Infra | **NOT RUN** | Documented for post-gate; requires root context |
| B13 | `bash scripts/verify-build.sh Release` | Studio | **NOT RUN** | Would invoke B1/B2 path |
| B14 | Command integration tests (guardrails) | Command | **NOT RUN** | Documented in `platform-integrity.yml`; not executed in 0B |

**NuGet lock:** No repo-wide `packages.lock.json` found → **`dotnet restore --locked-mode` NOT APPLICABLE** for Platform slnx in current tree.

---

## 6. SDK, runtime, and environmental prerequisites

| Prerequisite | Status |
|--------------|--------|
| .NET SDK 10.x | **Present** (10.0.103) |
| .NET Desktop / WPF workload | **Required** for CSM slnx (build attempted; failed on NU1903 before isolating WPF) |
| Node.js | **Present** (v24.13.0); website CI pins **22** in workflow — version skew **risk** |
| npm workspaces | Root `node_modules` **exists** on Main website tree; workspace packages **FAIL** to resolve |
| Docker | **Present** (optional for Agent image validation) |
| SQL Server / RabbitMQ | **NOT RUN** — required for full Command integration tests |

---

## 7. Security and configuration findings (no secret values)

| Finding | Category | Detail |
|---------|----------|--------|
| Tracked `apps/website/.env.development`, `.env.production` | **Reviewed** | Pattern scan: no non-empty password/secret lines matched in 0B scan |
| Tracked `apps/website/.env.example` | **PLACEHOLDER** | Keys such as `RESEND_API_KEY`, `SMTP_PASSWORD` present with empty/placeholder semantics |
| Tracked `deployment/environments/.env.*` | **Reviewed** | No matched secret patterns in scan |
| `ComplaincePlatform/.env.example` | Tracked | Example only |
| `appsettings.Local.json` | **Not tracked** | Listed in `ComplaincePlatform/.gitignore` |
| `identity-signing.pfx` | **Gitignored / conditional** | PFX referenced with `Exists()` condition in csproj on Main |

**Security blockers for migration:** None identified from env **file names and pattern scan**. Tracked `.env.development`/`.env.production` on website warrant **periodic human audit** (values not inspected in depth here).

**No credentials, tokens, or env var dumps** appear in this report.

---

## 8. Migration-sensitive path dependencies (inventory only)

Verified coupling points for future directory moves (see also `AOVIAS_REPOSITORY_RESTRUCTURING_ASSESSMENT.md`):

| Mechanism | Path | Dependency |
|-----------|------|------------|
| Studio platform root | `ComplianceServiceManager/Services/RootPathResolver.cs` | `Aovias.Runtime.Agent` + `aovias-config`; `ComplaincePlatform`; `WebApp` |
| Discovery defaults | `ComplianceServiceManager/Services/SourceContext/SourceDiscoveryBoundary.cs` | Folder names `ComplaincePlatform`, `WebApp` |
| Publishing root | `Aovias.Publishing/src/Aovias.Publishing.Core/Discovery/PlatformRootResolver.cs` | `aovias-config`, `ComplaincePlatform` or `Aovias.Runtime.Agent` |
| Deployable discovery | `Aovias.Publishing/.../SolutionDeployableProjectDiscoverer.cs` | `ComplaincePlatform`, `WebApp` metadata paths |
| Worktree defaults | `ComplianceServiceManager/Services/Workspaces/DevelopmentWorkspaceRegistry.cs` | `V:\DevelopmentRepo\AOVIAS`, `.Cursor`, `.Claude` |
| Dev scripts | `scripts/dev/agent-preflight.ps1`, `worktree-status.ps1`, `agent-completion-check.ps1` | Hard-coded `V:\DevelopmentRepo\AOVIAS*` |
| Verify scripts | `scripts/verify-build.sh`, `verify-tests.sh` | `Aovias.Platform.slnx` at repo root |
| Runtime Agent Docker | `Aovias.Runtime.Agent/Dockerfile` | COPY top-level `Aovias.*` folder names |
| Command CI | `ComplaincePlatform/.github/workflows/platform-integrity.yml` | Relative `Gateway/`, `tests/...` |
| Website CI | `AOVIAS Website/AOVIAS Web/.github/workflows/ci.yml` | Repo-relative npm paths |
| Tests | `ComplianceServiceManager.Tests/Workspaces/*.cs` | Hard-coded `V:\DevelopmentRepo\AOVIAS` paths |
| Publishing tests | `Aovias.Publishing/tests/**` | `ComplaincePlatform/...`, legacy `V:\DevelopmentRepo\V1.0\...` |
| AGENTS.md / docs | `docs/development/WORKTREE-OPERATIONS.md` | Integration path conventions |

**No resolver or path code was modified in Phase 0B.**

---

## 9. Failures, blockers, and validations not run

| Item | Classification |
|------|----------------|
| NU1903 on Platform + CSM restore/build | **FAIL** — Studio baseline blocker; needs package upgrade or policy decision |
| Website npm typecheck/lint/build | **FAIL** — workspace package integrity; may need `npm ci` from clean state or package build order ( **no install run in 0B** ) |
| Platform `dotnet test` | **NOT RUN** |
| Runtime Agent Docker build | **NOT RUN** |
| Command `TenantIsolationTests` / full integration job | **NOT RUN** (workflow_dispatch / secrets) |
| Cursor/Claude ff-only to `main` | **BLOCKED** until §3 preservation resolved |

---

## 10. Phase 1 readiness decision

| Decision | Value |
|----------|--------|
| **Phase 1 readiness** | **`READY WITH CONDITIONS`** |
| Path anomaly (`components/...`) | **RESOLVED** per `AOVIAS_GIT_PATH_ANOMALY_REPORT.md` |
| Integration Git cleanliness (tracked) | **PASS** |
| Worktree preservation | **BLOCKED** until Cursor/Claude local state handled |
| Studio Platform build | **FAIL** (NU1903) |
| Command backend build | **PASS** |
| Website CI parity on local machine | **FAIL** |
| Migration inventory | **PASS** (documented) |

**Phase 1** in restructuring plan = layout manifest + resolver updates **without mass `git mv`**. That can be **planned** now but should not start until:

1. Preservation items in §3 are reconciled.  
2. Human accepts or schedules remediation for NU1903 and website npm baseline failures.  
3. Agent worktrees normalized per `AGENTS.md`.

---

## 11. Recommended next task

**Single safest next step:** On the integrator machine, run the **preservation checklist** in §3:

1. `git -C V:\DevelopmentRepo\AOVIAS.Cursor diff ComplaincePlatform/IdentityService/IdentityService.csproj` vs `main` — commit, discard, or confirm identical to `main`.  
2. Preserve `scripts/dev/_phase3h_runtime_probe.py` (commit to appropriate branch or copy out).  
3. Preserve Claude `Aovias.CommandCenter/.../Properties/launchSettings.json` (commit or copy).  
4. Then `checkout agent/cursor/active` / `agent/claude/active` and `merge --ff-only main` on each agent worktree.  
5. Optionally commit Phase 0/0B/ anomaly reports on `main` when ready.

**Follow-on (separate task):** NU1903 vulnerability package remediation for `Aovias.Platform.slnx` restore/build green.

---

## Appendix — Context documents

| Document | Present | Notes |
|----------|---------|-------|
| `AOVIAS_GIT_PATH_ANOMALY_REPORT.md` | Yes | Read first; anomaly resolved |
| `AOVIAS_PHASE0_MIGRATION_READINESS.md` | Yes | Historical; superseded for Git baseline by §2 here |
| `AOVIAS_REPOSITORY_RESTRUCTURING_ASSESSMENT.md` | Yes | Architecture inventory; verify against §4 |
| `ACCREDAONE_KNOWLEDGEBASE_READINESS_ASSESSMENT.md` | Yes (on `main`) | AccredaOne context |

---

*Report created as untracked documentation. Not staged or committed per task constraints.*
