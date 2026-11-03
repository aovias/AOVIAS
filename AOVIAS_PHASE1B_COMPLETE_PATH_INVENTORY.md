# AOVIAS Phase 1B — Complete Path Inventory

**Date:** 2026-10-25  
**Baseline HEAD:** `5a1526d99a68241d40ccba759d8fc62586f4af86`  
**Enumeration method:** `git ls-files` (Git-tracked only)

---

## 1. Solution file reconciliation

| File | Tracked | Projects in slnx (Path= count) | Notes |
|------|---------|----------------------------------|-------|
| `Aovias.Platform.slnx` | Yes | 38 | Cross-platform Studio CI |
| `Aovias.PublishingEngine.slnx` | Yes | 14 | Publishing-focused subset |
| `ComplaincePlatform/CompliancePlatform.slnx` | Yes | 36 | Command backend |
| `ComplianceServiceManager/ComplianceServiceManager.slnx` | Yes | 2 | WPF + DesignSystem |
| `Aovias.CommandCenter/Aovias.CommandCenter.slnx` | Yes | 6 entries | 2 CC projects + 4 referenced libs in slnx |
| `Aovias.Runtime.Agent/Aovias.Runtime.Agent.slnx` | Yes | 14 | Agent area |
| `Aovias.Publishing/Aovias.Publishing.slnx` | Yes | 10 | Publishing area |
| `Aovias.Release/Aovias.Release.slnx` | Yes | 10 | Release area |
| `Aovias.Orchestration/Aovias.Orchestration.slnx` | Yes | 3 | Orchestration area |
| `Aovias.Bootstrap/Aovias.Bootstrap.sln` | Yes | — | Legacy `.sln` (Bootstrap); not `.slnx` |

**Verified:** Nine `.slnx` files match Phase 1A. **Additional:** one tracked **`Aovias.Bootstrap.sln`** (not previously counted as slnx).

**Not in Git as .csproj:** `WebApp/` (Angular — `package.json` / `angular.json` only). **Node:** `Aovias.ReleaseValidation/`, `AOVIAS Website/AOVIAS Web/` (npm workspaces).

---

## 2. Tracked .csproj inventory summary

**Total tracked `.csproj` files:** **87**

| Top-level prefix | Count | Product owner (planning) |
|------------------|-------|--------------------------|
| `ComplaincePlatform/` | 46 | **Command** |
| `Aovias.Publishing/` | 8 | **Studio** |
| `Aovias.Runtime.Agent/` | 7 | **Studio** |
| `Aovias.Bootstrap/` | 5 | **Studio** |
| `Aovias.Release/` | 4 | **Studio** |
| `Aovias.Orchestration/` | 3 | **Studio** |
| `tools/` | 2 | **UNRESOLVED** (Infrastructure vs Studio tools) |
| `Aovias.Configuration/` | 2 | **Studio** (O-2) |
| `Aovias.CommandCenter/` | 2 | **Studio** (O-1) |
| `Aovias.DeploymentValidation/` | 2 | **UNRESOLVED** (Studio-leaning) |
| `Aovias.Deployment.Contracts/` | 1 | **Studio** (O-2) |
| `Aovias.DesignSystem/` | 1 | **Studio** |
| `ComplianceServiceManager/` | 1 | **Studio** |
| `ComplianceServiceManager.Tests/` | 1 | **Studio** |
| `ComplianceServiceManager.Operations/` | 1 | **Studio** |
| `ComplianceServiceManager.Operations.Tests/` | 1 | **Studio** |

### Command platform projects (46) — proposed target prefix

`AOVIAS.Command/CompliancePlatform/<same-relative-path>`

Includes microservices, `Gateway`, `KnowledgeBase`, `Shared.*`, `Platform.Dashboard`, tests, **`ComplaincePlatform/_tmp_phase_probe/Probe.csproj`** (tracked — migration-sensitive cleanup decision), **`tests/D0RuntimeVerifier`**, **`AIOrchestratorService/D0RuntimeVerifier`**.

### Studio stack projects (35 + 2 tools)

Proposed under `AOVIAS.Studio/` preserving inner folder names (`Aovias.Publishing/...`, etc.).

### Executable / host roles (verified highlights)

| Project | Role |
|---------|------|
| `ComplaincePlatform/Gateway/Gateway.csproj` | Command API gateway |
| `ComplaincePlatform/*/ *Service.csproj` | Command microservices |
| `ComplaincePlatform/OutboxRelayWorker/OutboxRelayWorker.csproj` | Worker |
| `Aovias.CommandCenter.Api` | Studio fleet API |
| `Aovias.Runtime.Agent.Api` | Studio runtime control plane |
| `Aovias.Bootstrap.Host` | Bootstrap CLI host |
| `ComplianceServiceManager` | WPF Studio shell |
| `tools/PublishAllServices`, `PublishRuntimeAgent` | CLI publish helpers |

Full path list (87 lines): identical to `git ls-files "*.csproj"` at HEAD — available via read-only Git on integrator machine.

---

## 3. Docker Compose and build-context audit (exhaustive at HEAD)

### 3.1 `ComplaincePlatform/docker-compose.dev.yml`

| Service | build.context | dockerfile | volumes (host paths) | working_dir | Layout impact |
|---------|---------------|------------|----------------------|-------------|---------------|
| `sqlserver-dev` | — (image only) | — | named volume | — | Low |
| `identity-service-dev` | **`.`** (ComplaincePlatform root) | `docker/Dockerfile.dev.dotnet` | **`.:/app`**; `${USERPROFILE}/.aspnet/https:/https:ro` | `/app/IdentityService` | **MUST UPDATE** if Command tree moves — context must remain service root |
| `compliance-service-dev` | **`.`** | same | **`.:/app`** | `/app/ComplianceService` | Same |
| `audit-service-dev` | **`.`** | same | **`.:/app`** | `/app/AuditService` | Same |
| `organization-service-dev` | **`.`** | same | **`.:/app`** | `/app/OrganizationService` | Same |
| `ai-orchestrator-service-dev` | **`.`** | same | **`.:/app`** | `/app/AIOrchestratorService` | Same |
| (additional services in file) | **`.`** pattern | same | **`.:/app`** | `/app/<ServiceName>` | Same pattern through EOF |

**Mechanism:** Bind-mount entire **`ComplaincePlatform`** tree to `/app`; **`working_dir`** selects service subdirectory. **Not** repo-root context.

**Required change:** Run compose from new `AOVIAS.Command/CompliancePlatform/` directory or set `context:` to that path; volume mount target unchanged in principle.

### 3.2 `ComplaincePlatform/docker-compose.commercial.yml`

| Service | build.context | dockerfile | volumes | working_dir |
|---------|---------------|------------|---------|-------------|
| `gateway-dev` | **`.`** | `docker/Dockerfile.dev.dotnet` | **`.:/app`**; `./Gateway/App_Data:/app/Gateway/App_Data` | `/app/Gateway` |

**Layout impact:** **MUST UPDATE** with Command folder move.

### 3.3 Website — `AOVIAS Website/AOVIAS Web/deployment/compose/docker-compose.yml`

| Service | build.context | dockerfile | volumes | working_dir |
|---------|---------------|------------|---------|-------------|
| `website` | **`../..`** (monorepo root `AOVIAS Web`) | `deployment/docker/Dockerfile` | **`../..:/app`**; named node_modules volumes | `/app` |

**Layout impact:** **MUST UPDATE** if monorepo moves to `AOVIAS.Website/` — context is **relative to compose file location**.

### 3.4 Website — `deployment/compose/docker-compose.prod.yml`

| Service | build.context | dockerfile |
|---------|---------------|------------|
| (prod website) | **`../..`** | `deployment/docker/Dockerfile` |

### 3.5 Website — `docker/docker-compose.yml`

| Service | build.context | dockerfile |
|---------|---------------|------------|
| `website` | **`..`** (one level up from `docker/`) | `deployment/docker/Dockerfile` |

**Note:** Three compose entry points with **different relative context depth** — easy to break during rename.

### 3.6 Standalone Dockerfiles (no Compose in repo)

| File | Context assumption | COPY / path sensitivity |
|------|-------------------|-------------------------|
| `Aovias.Runtime.Agent/Dockerfile` | **Repo platform root** | COPY **`Aovias.Configuration/`**, **`Aovias.Deployment.Contracts/`**, **`Aovias.DeploymentValidation/`**, **`Aovias.Orchestration/`**, **`Aovias.Release/`**, **`Aovias.Publishing/`**, **`Aovias.Runtime.Agent/`** |
| `ComplaincePlatform/docker/Dockerfile.dev.dotnet` | SDK base only; no COPY | Used with compose mount |
| `AOVIAS Website/.../deployment/docker/Dockerfile` | **Monorepo root** | COPY `package.json`, `apps/website/...`, `packages/*` |
| `AOVIAS Website/.../docker/Dockerfile` | (duplicate path variant) | Same pattern |
| `aovias-config/templates/publishing/Dockerfile.*` | Template artifacts | Used in generated deploy packages — **TEST REQUIRED** after template path changes |

**No Compose** references CommandCenter or Studio Agent — Agent image build is **CLI/CI-only** at HEAD.

---

## 4. Additional path assumptions (beyond Phase 1A)

| Location | Mechanism | Class | Notes |
|----------|-----------|-------|-------|
| `RootPathResolver` / `PlatformRootResolver` | Directory walk markers | **MIGRATION BLOCKER** | Phase 1A |
| `SolutionDeployableProjectDiscoverer.GetScanRoots` | Hard-coded subdirs | **MIGRATION BLOCKER** | |
| `RuntimeAgentDockerContextStager` | Top-level folder names | **MIGRATION BLOCKER** | |
| `DevelopmentWorkspaceRegistry.BuildDefaults` | `DevelopmentRepo/AOVIAS*` | **MUST UPDATE** (docs/scripts) | Worktree paths stable |
| `scripts/dev/*.ps1` | Default `V:\DevelopmentRepo\...` | **MUST UPDATE** | Machine-specific |
| `Aovias.Bootstrap.Host/Program.cs` | Default `V:/DevelopmentRepo/V1.0/aovias-config` | **MUST UPDATE** | Legacy layout |
| `Aovias.DeploymentValidation` rules/tests | Forbidden `DevelopmentRepo` paths | **TEST REQUIRED** | Policy guardrails |
| `Aovias.ReleaseValidation/.../service-registry.ts` | `PLATFORM_ROOT = 'ComplaincePlatform'` | **MUST UPDATE** | Relative CWD |
| `ComplianceServiceManager.Tests/*` | `V:\DevelopmentRepo\AOVIAS` fixtures | **TEST REQUIRED** | |
| `Aovias.Publishing/tests/*` | Legacy `V1.0` paths | **TEST REQUIRED** | |
| `ComplaincePlatform/Shared.Logging/LogPathResolver.cs` | Folder name `ComplaincePlatform` | **MUST UPDATE** | Keep spelling |
| `tools/PublishAllServices/Program.cs` | Walk `aovias-config` + `ComplaincePlatform` | **MIGRATION BLOCKER** | |
| `WebApp/src/**/*.ts` | `@see ComplaincePlatform/docs/...` | **DOCUMENTATION ONLY** | |
| `AOVIAS Website/.../.qa-responsive/summary.json` | Absolute screenshot paths | **UNRELATED** | Generated QA artifact |
| `Aovias.Bootstrap/bootstrap-reports/*.json` | Stack traces with `V:\DevelopmentRepo\V1.0\...` | **UNRELATED** | Historical logs; path leakage |
| `preflight-docker-probe/` (root dir exists) | **Untracked?** | **UNRESOLVED** | Not in `git ls-files` at HEAD — local/temp |
| `%LocalAppData%\AoviasStudio\development-workspaces.json` | User registry | **MUST UPDATE** (operator) | Not in repo |

**Grep note:** `V:\DevelopmentRepo` / `V:/DevelopmentRepo` occurrences in **source/tests** are limited (~15 files); many website hits are **QA JSON only**.

---

## 5. AccredaOne boundary (inventory closure)

| Item | Status |
|------|--------|
| `AOVIAS.AccredaOne/` on disk | **Not present** (proposal only) |
| `ComplaincePlatform/KnowledgeBase` | **Command-owned**; in `CompliancePlatform.slnx` |
| Direct AccredaOne → KnowledgeBase API | **Does not exist** — internal authenticated API via Gateway |
| Public publishing API | **Not implemented** — per `ACCREDACONE_KNOWLEDGEBASE_READINESS_ASSESSMENT.md` |

### Questions before AccredaOne consumes published content

1. **Public API contract** — OpenAPI surface, anonymous vs authenticated reads, rate limits.
2. **Projection layer** — Public-safe DTOs vs internal KnowledgeBase entities (GUIDs, tenant fields).
3. **Publication lifecycle** — Editorial workflow, versioning, rollback, CDN cache invalidation.
4. **Search** — Public index vs internal SQL `LIKE` / AI Orchestrator tenant search.
5. **Provenance & compliance** — Citations, source metadata, regulatory disclaimers.
6. **Deployment boundary** — Separate host/BFF in `AOVIAS.AccredaOne/` vs Gateway proxy (recommended: **intentional BFF**, not raw internal API).
7. **Data residency** — Which Command DB tables may be replicated/projections vs live queries.

**Phase 1B:** Do **not** move KnowledgeBase; do **not** assume migration creates AccredaOne runtime.

---

## 6. Inventory gaps (residual)

| Gap | Severity | Mitigation |
|-----|----------|------------|
| Per-project solution membership matrix (87×9) | Low | Derive from slnx Path= + csproj refs in Phase 1C automation |
| Every `deployment.metadata.json` path | Medium | Publishing wave scan before Command move |
| Root `.github/` (if added later) | Low | Only Command + Website workflows at HEAD |
| `WebApp` frontend | Documented as non-csproj | Included in Command ownership |

---

*Complete inventory for Phase 1B. No repository mutations.*
