# AOVIAS Phase 1I-J — INV-1 Full Repository Dependency Inventory & Pre-W3 Gate

**Phase:** 1I-J (read-only inventory)  
**Date:** 2025-10-25  
**Baseline HEAD:** `b9e2de634b86d04f54b3a1d97ad6bfc44fa646f1`  
**Deliverables (untracked):** this report; `AOVIAS_PHASE1IJ_inv1_inventory.json`; `AOVIAS_PHASE1IJ_inv1_matrix.csv`

---

## Final status

**INV-1 COMPLETE — W3 READY FOR HUMAN SCOPE APPROVAL**

The **87 × 9** project/solution inventory target is **verified** (87 `.csproj`, 9 `.slnx`). Cross-wave boundaries align with Phase 1I-B. W3 remains **unauthorized** until a separate human decision approves exact scope and execution. Several **high-risk** coupling points (Docker COPY, deployable scan roots, `discovery.current`, Infrastructure→Studio project references) must be updated in the **same atomic batch** as Studio `git mv`.

---

## 1. Safety gate

| Worktree | Branch | HEAD | Match |
|----------|--------|------|-------|
| Main | `main` | `b9e2de634b86d04f54b3a1d97ad6bfc44fa646f1` | Yes |
| Cursor | `agent/cursor/active` | `b9e2de6…` | Yes |
| Claude | `agent/claude/active` | `b9e2de6…` | Yes |

- **W2 merge present:** Infrastructure paths at `AOVIAS.Infrastructure/…` (ReleaseValidation, publish tools).
- **W3 not started:** No `AOVIAS.Studio/` product tree; Studio roots still at repository root.
- **Tracked tree:** Clean (no staged/tracked edits from this phase).
- **Untracked artifacts:** Preserved (phase reports, ZIPs, Lighthouse, Cursor probe, Claude launch settings).

---

## 2. Enumeration method and counts

| Artifact | Expected (contract) | **Actual** | Method |
|----------|---------------------|------------|--------|
| `.csproj` | 87 | **87** | Recursive file scan excluding `bin/` / `obj/` |
| `.slnx` | 9 | **9** | Recursive scan |
| Legacy `.sln` | — | **1** | `Aovias.Bootstrap/Aovias.Bootstrap.sln` (not parsed into matrix; Bootstrap projects covered by `Aovias.Platform.slnx`) |
| `.fsproj` / `.vbproj` | — | **0** | None found |

**WebApp:** Angular (`package.json` / `angular.json`) — **not** a `.csproj`; W5 scope.

**Node (non-csproj):** `AOVIAS.Infrastructure/Aovias.ReleaseValidation/` (W2 complete).

---

## 3. Solution membership summary

| Solution (repo-relative) | Projects listed in `.slnx` | Notes |
|--------------------------|----------------------------|--------|
| `Aovias.Platform.slnx` | **38** | Superset for Platform/Studio CI |
| `ComplaincePlatform/CompliancePlatform.slnx` | **36** | Command microservices (W4) |
| `Aovias.PublishingEngine.slnx` | **14** | Publishing + DRV + layout tests |
| `Aovias.Runtime.Agent/Aovias.Runtime.Agent.slnx` | **14** | Agent + cross-tree refs |
| `Aovias.Release/Aovias.Release.slnx` | **10** | |
| `Aovias.Publishing/Aovias.Publishing.slnx` | **10** | |
| `Aovias.CommandCenter/Aovias.CommandCenter.slnx` | **6** | |
| `Aovias.Orchestration/Aovias.Orchestration.slnx` | **3** | |
| `ComplianceServiceManager/ComplianceServiceManager.slnx` | **2** | CSM + DesignSystem only |

**Duplicate membership:** Expected — e.g. `Aovias.Configuration` appears in Platform, PublishingEngine, CommandCenter, Release, Publishing, Runtime.Agent slnx files. All resolved paths **exist on disk** (no `MISSING:` entries in generator run).

**Full matrix:** see `AOVIAS_PHASE1IJ_inv1_matrix.csv` (87 rows) and JSON `membership` object.

---

## 4. Projects with **no** solution membership (11)

| Project | Wave / owner | Severity | Notes |
|---------|--------------|----------|--------|
| `ComplaincePlatform/_tmp_phase_probe/Probe.csproj` | Command | **Medium** | Tracked probe; migration-sensitive cleanup (1B) |
| `ComplaincePlatform/AIOrchestratorService.Tests/…` | Command | Low | Test project |
| `ComplaincePlatform/AIOrchestratorService/D0RuntimeVerifier/…` | Command | Low | Verifier tool |
| `ComplaincePlatform/DemoDataSeeder/…` | Command | Low | Seeder |
| `ComplaincePlatform/Shared.Authorization/…` | Command | **Medium** | Referenced by services; not in CompliancePlatform.slnx |
| `ComplaincePlatform/tests/Aovias.Platform.IntegrationTests/…` | Command | Low | Integration tests |
| `ComplaincePlatform/tests/ComplianceStub/…` | Command | Low | Test stub |
| `ComplaincePlatform/tests/D0RuntimeVerifier/…` | Command | Low | Duplicate verifier path |
| `ComplaincePlatform/tests/Gateway.UnitTests/…` | Command | Low | Unit tests |
| `ComplaincePlatform/tests/Shared.Infrastructure.Tests/…` | Command | Low | Unit tests |
| `ComplianceServiceManager.Tests/…` | Studio (W3) | **Medium** | **Not** in CSM slnx; only Operations.Tests in Platform slnx |

**Not blockers for W3 inventory completion** — document for W3/W4 test planning.

---

## 5. Project-reference graph (summary)

**Edge extraction:** All `ProjectReference Include="…"` in 87 csproj files (read-only parse).

**Cross-product boundary (baseline HEAD):**

| From bucket | To bucket | Evidence |
|-------------|-----------|----------|
| **Command** (`ComplaincePlatform/*`) | **Command** only | No `ProjectReference` to `Aovias.*` under `ComplaincePlatform/` (manifest constraint confirmed) |
| **Infrastructure** (`AOVIAS.Infrastructure/tools/*`) | **Studio** | `PublishAllServices` / `PublishRuntimeAgent` → `Aovias.Configuration`, `Aovias.Publishing.SDK` via `..\..\..\` |
| **Studio** (`Aovias.*`, CSM*) | **Studio** (+ internal CommandCenter/Release/Orchestration edges) | Dense relative refs within and across Studio trees via slnx `../` paths |

**W3 impact on Infrastructure tools:** After Studio move to `AOVIAS.Studio/…`, publish tool ProjectReferences must change from `..\..\..\` to **`..\..\..\AOVIAS.Studio\…`** (or shared MSBuild property) — **same W3 batch** as Studio tree move.

**Cycles:** No proven solution-breaking cycle identified without build graph tooling; Studio graphs are layered (Contracts → Core → SDK → hosts). **Suspected** mutual test→product edges only within same service folders.

**Missing ProjectReference targets:** **0** broken includes at HEAD (all relative refs resolve to existing csproj).

**Central build files (W3-sensitive):**

- `ComplaincePlatform/Directory.Packages.props` — **W4** (stays with Command)
- `Aovias.Orchestration/Directory.Build.props`, `Aovias.Release/Directory.Build.props`, `Aovias.Publishing/Directory.Build.props`, `Aovias.Runtime.Agent/Directory.Build.props`, `Aovias.Bootstrap/Directory.Build.props` — **W3** (move with product folders)
- `ComplianceServiceManager/NuGet.Config` — **W3**

---

## 6. W3 scope (manifest M-S-01 … M-S-17)

Approved **Phase 1I-B** disposition: **O-3 DeploymentValidation → Studio** (with W3). **O-4:** Runtime.Contracts co-moves with Agent. **O-6:** Shared deferred.

| ID | Source (current) | Target | Atomic group |
|----|------------------|--------|--------------|
| M-S-01 | `Aovias.Platform.slnx` | `AOVIAS.Studio/Aovias.Platform.slnx` | **G1** — with all Platform slnx project path updates |
| M-S-02 | `Aovias.PublishingEngine.slnx` | `AOVIAS.Studio/…` | **G1** |
| M-S-03–06 | `ComplianceServiceManager*` (4 trees) | `AOVIAS.Studio/…` | **G2** — CSM cluster |
| M-S-07 | `Aovias.Configuration/` | `AOVIAS.Studio/…` | **G3** — configuration hub |
| M-S-08 | `Aovias.Deployment.Contracts/` | `AOVIAS.Studio/…` | **G3** |
| M-S-09 | `Aovias.CommandCenter/` | `AOVIAS.Studio/…` | **G3** |
| M-S-10 | `Aovias.Runtime.Agent/` (+ Contracts) | `AOVIAS.Studio/…` | **G4** — Dockerfile + stager |
| M-S-11 | `Aovias.Publishing/` | `AOVIAS.Studio/…` | **G4** |
| M-S-12 | `Aovias.Bootstrap/` | `AOVIAS.Studio/…` | **G3** |
| M-S-13 | `Aovias.Release/` | `AOVIAS.Studio/…` | **G3** |
| M-S-14 | `Aovias.Orchestration/` | `AOVIAS.Studio/…` | **G3** |
| M-S-15 | `Aovias.DesignSystem/` | `AOVIAS.Studio/…` | **G2** |
| M-S-16 | `Aovias.DeploymentValidation/` | `AOVIAS.Studio/Aovias.DeploymentValidation/` | **G4** (per O-3) |
| M-S-17 | Nested area `*.slnx` (8 under product dirs + roots) | Under `AOVIAS.Studio/…` | **G1** |

**Recommended W3 internal order (single authorized wave, multiple commits only if human splits — default one atomic merge):**

1. Create `AOVIAS.Studio/` and `git mv` **G3+G2+G4** product trees together (all `Aovias.*`, CSM*, DesignSystem).
2. Move root **G1** slnx files and rewrite **all** `<Project Path="…">` entries (Platform slnx alone: 38 paths).
3. Update **`repository-layout.json` `discovery.current`** markers to post-W3 paths **or** keep editing current-only per 1I-B (Target stays non-production).
4. Update **`Aovias.Runtime.Agent/Dockerfile`** COPY paths and `RuntimeAgentDockerContextStager` / `repository-layout.json` `runtimeAgentDockerCopyRoots`.
5. Update **`SolutionDeployableProjectDiscoverer.GetScanRoots`** only if W4 not combined (still `ComplaincePlatform` at root until W4).
6. Fix **Infrastructure publish tools** ProjectReferences to new Studio paths.
7. Update **CSM** `RootPathResolver` / appsettings path segments referencing `Aovias.Runtime.Agent`, config roots (read-only evidence: CSM + Configuration layout tests).

**Validation gates (post-W3, separate authorization for runtime):** `Aovias.Platform.slnx` + `Aovias.PublishingEngine.slnx` Release build; 39 layout tests; Publishing stager tests; optional discover-only.

---

## 7. Cross-wave boundary verification

| Bucket | W3 absorbs? | Evidence |
|--------|-------------|----------|
| `ComplaincePlatform/`, `WebApp/` | **No — W4** | M-C-01–03; 36+ Command csproj |
| `AOVIAS Website/…` | **No — W5** | No csproj at HEAD |
| `AOVIAS.Infrastructure/aovias-config`, ReleaseValidation, tools | **No — W2 done** | At target paths |
| `AOVIAS.AccredaOne/` | **No — W6** | Scaffold |
| `AOVIAS.Shared/` | **No — deferred O-6** | Not instantiated |
| `preflight-docker-probe/` | **No — O-8 housekeeping** | Not in W3 manifest |
| Root `tools/` empty placeholder | **N/A** | Publish tools moved W2 |

**Conflict check:** Phase 1I-B **O-3 Studio** for DeploymentValidation matches manifest target `AOVIAS.Studio/Aovias.DeploymentValidation` in `repository-layout.json` — **no silent override**.

---

## 8. INV-2 — `deployment.metadata.json` reconciliation

| Category | Count |
|----------|-------|
| Glob total | **21** |
| Exclude `_perf_build_out` | **2** |
| **Canonical production** | **19** |

**Excluded (generated / perf):**

- `ComplaincePlatform/_perf_build_out/Gateway/deployment.metadata.json`
- `ComplaincePlatform/_perf_build_out/ComplianceService/deployment.metadata.json`

**Canonical 19 (all W4 or Agent — none change path in W3):**

1. `ComplaincePlatform/AccessGovernanceService/deployment.metadata.json`
2. `ComplaincePlatform/AIOrchestratorService/deployment.metadata.json`
3. `ComplaincePlatform/AuditService/deployment.metadata.json`
4. `ComplaincePlatform/CommunicationService/deployment.metadata.json`
5. `ComplaincePlatform/ComplianceService/deployment.metadata.json`
6. `ComplaincePlatform/EnterpriseDirectoryService/deployment.metadata.json`
7. `ComplaincePlatform/Gateway/deployment.metadata.json`
8. `ComplaincePlatform/GovernanceService/deployment.metadata.json`
9. `ComplaincePlatform/IdentityService/deployment.metadata.json`
10. `ComplaincePlatform/IntegrationService/deployment.metadata.json`
11. `ComplaincePlatform/KnowledgeBase/deployment.metadata.json`
12. `ComplaincePlatform/OrganizationService/deployment.metadata.json`
13. `ComplaincePlatform/OutboxRelayWorker/deployment.metadata.json`
14. `ComplaincePlatform/PersonService/deployment.metadata.json`
15. `ComplaincePlatform/ProfessionalService/deployment.metadata.json`
16. `ComplaincePlatform/WorkforceIntelligenceService/deployment.metadata.json`
17. `ComplaincePlatform/WorkforceService/deployment.metadata.json`
18. `WebApp/deployment.metadata.json`
19. `Aovias.Runtime.Agent/deployment.metadata.json`

**W3 path change for metadata:** **None** (Agent metadata file moves with M-S-10 tree — consumer paths must switch from `Aovias.Runtime.Agent/…` to `AOVIAS.Studio/Aovias.Runtime.Agent/…` in code/tests, not metadata content).

**Consumers:** `SolutionDeployableProjectDiscoverer` (scan roots), Publishing contract tests, publish/discover pipeline — hard-coded `ComplaincePlatform/` and `Aovias.Runtime.Agent` segments in tests and `SolutionPublishingPreflightService` (grep evidence).

---

## 9. Path impacts (W3 — evidence-based)

| Area | Representative paths / files | Risk |
|------|------------------------------|------|
| **Docker** | `Aovias.Runtime.Agent/Dockerfile` COPY `Aovias.Configuration/`, … | **Blocker if omitted** |
| **Stager** | `RuntimeAgentDockerContextStager`, `RuntimeAgentDockerContextStagerTests` | **High** |
| **Discovery** | `SolutionDeployableProjectDiscoverer.GetScanRoots` | **High** (W4 coupling for Command) |
| **Layout** | `repository-layout.json` `discovery.current`, `RepositoryLayoutW1TransitionTests` | **High** |
| **Publish CLI** | `AOVIAS.Infrastructure/tools/Publish*` csproj refs | **High** |
| **CSM** | `ComplianceServiceManager/*` NuGet, launch, `RootPathResolver` | **High** |
| **CI** | `ComplaincePlatform/.github/workflows/platform-integrity.yml` | **W4** (not W3) |
| **Tests** | `ContractDrivenPublishingTests`, layout contract tests | **Medium** |
| **Docs** | G1 runbook (infra tool paths already W2) | Low |

---

## 10. Findings by severity

### Blocker (W3 execution must address in same batch)

1. **Dockerfile + stager COPY roots** must match post-move `AOVIAS.Studio/Aovias.*` layout.
2. **`repository-layout.json` `discovery.current`** platform/CSM/Docker markers must match disk (Target profile remains non-production).
3. **All slnx `<Project Path>`** and cross-slnx `../` references across **9** files after directory depth change.

### High risk

4. **Infrastructure publish tools** ProjectReference depth after Studio nest.
5. **CSM** platform root discovery (`Aovias.Runtime.Agent` path segments).
6. **309+** DeploymentValidation tests path assumptions (move with Studio per O-3).

### Medium risk

7. **11 orphan csproj** — especially `Shared.Authorization`, `ComplianceServiceManager.Tests`, `_tmp_phase_probe`.
8. **Publishing tests** with embedded `ComplaincePlatform/…` metadata paths (unchanged until W4).

### Low risk

9. Historical docs / phase reports with old paths.
10. `AOVIAS_REPOSITORY_RESTRUCTURING_ASSESSMENT.md` narrative.

---

## 11. Unresolved decisions (human)

| ID | Question |
|----|----------|
| W3-EXEC | Authorize single atomic W3 merge vs. sub-batches (not recommended per 1B) |
| Orphans | Add `ComplianceServiceManager.Tests` / `Shared.Authorization` to slnx vs. leave orphan |
| Probe | Disposition of `ComplaincePlatform/_tmp_phase_probe/Probe.csproj` |
| Post-W3 runtime | Authorize `--discover` / stager integration proof on `main` |
| W4 timing | Serialize W4 after W3 validation (required by 1B) |

---

## 12. Recommended next step

1. Human **scope approval** for W3 manifest rows M-S-01–M-S-17 with O-3/O-4 as in 1I-B.
2. Separate **Phase 1I-K (or equivalent)** authorization for W3 implementation in Cursor worktree + task branch.
3. Do **not** start W3 until INV-1 sign-off recorded.

---

## 13. Machine-readable annex

- **`AOVIAS_PHASE1IJ_inv1_inventory.json`** — membership map, orphan list, solution counts.
- **`AOVIAS_PHASE1IJ_inv1_matrix.csv`** — 87-row project-to-solution matrix.

**Note:** A temporary local scanner was used for JSON generation only; **no** tracked scripts were added to the repository.

---

**Report path:** `AOVIAS_PHASE1IJ_INV1_FULL_DEPENDENCY_INVENTORY_REPORT.md` (untracked, Main)
