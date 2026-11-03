# AOVIAS Phase 1I-AC — W4 Command Migration Readiness

**Phase:** 1I-AC (read-only planning — **no implementation, staging, or Git history changes**)  
**Date:** 2026-10-25  
**Worktree:** `V:\DevelopmentRepo\AOVIAS`  
**Branch:** `main`  
**HEAD:** `bcd63a6ecef7d67090be2104b3fd662635c1fa60` (W3 merge commit)

---

## Final status

**`W4 READINESS PASS — READY FOR IMPLEMENTATION AUTHORIZATION`**

Implementation authorization, scope checklist, and agent worktree rules remain **separate** gates (per W3 pattern). This report does **not** authorize execution.

---

## 1. Baseline verification

| Check | Result |
|-------|--------|
| Worktree | `V:\DevelopmentRepo\AOVIAS` |
| Branch | `main` |
| HEAD | `bcd63a6ecef7d67090be2104b3fd662635c1fa60` (**expected** W3 merge) |
| W3 commit `6e66c37…` in history | **Yes** |
| Tracked working tree | **Clean** (`git status -uno --short` empty) |
| Untracked artifacts | **Preserved** (phase reports, website ZIPs/Lighthouse, INV-1, etc.) |

No fetch, merge, push, or worktree sync performed.

---

## 2. Approved W4 directory moves (contract)

From `repository-layout.json`, `AOVIAS_PHASE1IA_TARGET_PHYSICAL_LAYOUT_REPORT.md` (M-C-01–M-C-03), and `AOVIAS_PHASE1B_MIGRATION_CONTRACT.md`:

| ID | Action | Source (current) | Target | Tracked files (approx.) |
|----|--------|------------------|--------|-------------------------|
| **M-C-01** | **MOVE** tree | `ComplaincePlatform/` | `AOVIAS.Command/CompliancePlatform/` | **3157** |
| **M-C-02** | **MOVE** tree | `WebApp/` | `AOVIAS.Command/WebApp/` | **1368** |
| **M-C-03** | **MOVE** (co-located) | `ComplaincePlatform/CompliancePlatform.slnx` | `AOVIAS.Command/CompliancePlatform/CompliancePlatform.slnx` | (within M-C-01) |

**Naming policy (unchanged):** Folder segment remains **`ComplaincePlatform`** (misspelling preserved) under `AOVIAS.Command/` until a separate reviewed rename (`repository-layout.json` §policies).

**Target parent:** `AOVIAS.Command/` does **not** exist on disk today; first move creates the Command product root.

**Internal layout:** `WebApp/` keeps its internal structure under `AOVIAS.Command/WebApp/` (Angular app, `angular.json`, `deployment.metadata.json`, docs, `.planning/`, etc.).

**Knowledge base:** Policy `knowledgeBaseCurrentPath` is `ComplaincePlatform/KnowledgeBase` today → becomes `AOVIAS.Command/CompliancePlatform/KnowledgeBase` with M-C-01.

---

## 3. Path-dependent updates (concise inventory)

Grouped by **must change for structural validity** after W4 (not a full file list — implementation uses `git mv` + targeted edits mirroring W3).

### 3.1 Repository contract and discovery (`discovery.current`)

**File:** `repository-layout.json`

| Area | Current (post-W3) | Post-W4 `current` (already defined in `target` / planning) |
|------|-------------------|---------------------------------------------------------------|
| `products.command.paths.compliancePlatform` | `ComplaincePlatform` | `AOVIAS.Command/CompliancePlatform` |
| `products.command.paths.webApp` | `WebApp` | `AOVIAS.Command/WebApp` |
| `products.command.migrationStatus` | `at-repository-root` | → command-at-target (implementation label) |
| `solutions[]` Command slnx | `ComplaincePlatform/CompliancePlatform.slnx` | `AOVIAS.Command/CompliancePlatform/CompliancePlatform.slnx` |
| `discovery.current.platformRoot.requiredAny` | `ComplaincePlatform`, Studio Agent | **`AOVIAS.Command/CompliancePlatform`**, Studio Agent |
| `discovery.current.defaultApplicationRoots` | `ComplaincePlatform`, `WebApp` | **`AOVIAS.Command/CompliancePlatform`**, **`AOVIAS.Command/WebApp`** |
| `discovery.current.deployableProjectScanRoots` | `ComplaincePlatform`, Studio Agent | **Command CompliancePlatform path**, Studio Agent |
| `discovery.current.webAppAngularManifestRelative` | `WebApp/angular.json` | **`AOVIAS.Command/WebApp/angular.json`** |
| `policies.knowledgeBaseCurrentPath` | `ComplaincePlatform/KnowledgeBase` | **`AOVIAS.Command/CompliancePlatform/KnowledgeBase`** |
| `compatibilityConstraints` prose | Root `ComplaincePlatform` markers | Update to Command paths + **legacy fallback** policy (mirror W3) |

**Note:** `discovery.target` already describes post-W4 paths; W4 should update **`current`** only (Target remains non-production until explicitly enabled — same rule as W3/K-14).

### 3.2 AOVIAS.Studio — publishing and layout (W3 integration points)

| Component | Path / behavior to update |
|-----------|---------------------------|
| `SolutionDeployableProjectDiscoverer.GetScanRoots` | `ComplaincePlatform` → `AOVIAS.Command/CompliancePlatform` |
| `PlatformRootResolver.ResolveWebAppRoot` | `WebApp` → `AOVIAS.Command/WebApp` |
| `RepositoryLayoutPlatformRootDiscovery` | Publishing legacy marker: accept **Command** CompliancePlatform path; optional transition: old root folder |
| `SolutionPublishingPreflightService` | Platform-root walk using `ComplaincePlatform` directory check |
| `RootPathResolver` (CSM) | `ResolveCompliancePlatformRoot`, `ResolveWebAppRoot`, fallbacks |
| `SourceDiscoveryBoundary` | Folder names `ComplaincePlatform`, `WebApp` |
| Other CSM services/tests | **~15+** files reference sibling `ComplaincePlatform` / `WebApp` (discovery, runtime profiles, workspace tests) |
| `Aovias.Configuration` layout tests | Fixture dirs + embedded JSON expecting root paths; contract tests for Current vs Target |
| Publishing tests | `PublishingProjectPathsTests`, `ContractDrivenPublishingTests`, deployment verifier tests, compose tests — hardcoded `ComplaincePlatform/` and `WebApp` segments |
| `Aovias.Runtime.Agent` | `Program.cs`, `deployment/runtime.json`, OpenApi config — Command path references (verify during implementation) |
| `Aovias.CommandCenter.Api` | `Program.cs` — Command/WebApp references (grep hit) |

**W2 (`AOVIAS.Infrastructure/`):** Publish tool `.csproj` paths point at Studio only; **no** Command path in Infrastructure csproj graph. G1 runbook **docs** still use `ComplaincePlatform/Gateway/…` CLI examples — update in W4 doc pass (same class as NF-01 for Studio slnx).

### 3.3 Inside Command trees (move with tree; edit where rooted)

| Category | Examples |
|----------|----------|
| **Command CI** | `ComplaincePlatform/.github/workflows/platform-integrity.yml` — `dotnet` steps use paths **without** `ComplaincePlatform/` prefix (assume platform subfolder as CWD). After monorepo move to `AOVIAS.Command/CompliancePlatform/`, workflow must set **`working-directory`** or prefix all paths (structural **blocker** for CI). |
| **Compose** | `ComplaincePlatform/docker-compose.dev.yml`, `docker-compose.commercial.yml` — `context: .` relative to compose file location; moves with tree; **review** any `../WebApp` or repo-root references. |
| **Scripts** | `ComplaincePlatform/scripts/build-praman-aovpkg.ps1` (explicit `$repoRoot/ComplaincePlatform`), `scripts/ops/*.ps1`, deploy scripts |
| **Solution** | `CompliancePlatform.slnx` — internal `<Project Path=` entries are tree-relative; likely stable after co-move; validate slnx build entry in implementation |
| **WebApp** | `angular.json`, `deployment.metadata.json`, `.planning/**` cross-refs to `ComplaincePlatform/…` in markdown (documentation-only unless scripts consume them) |

### 3.4 Root governance docs (optional same wave)

| Path | Relevance |
|------|-----------|
| `docs/milestones/G1-EXECUTION-RUNBOOK.md` | Publish CLI `--project ComplaincePlatform/…` |
| `docs/Architecture/**`, `docs/milestones/ROADMAP.md` | Many narrative `ComplaincePlatform/` / `WebApp` paths (doc consistency; not runtime blockers) |

### 3.5 Explicitly out of W4 scope

| Item | Reason |
|------|--------|
| `AOVIAS Website/AOVIAS Web/**` | **W5** (`AOVIAS.Website` target) |
| `AOVIAS.Shared` | Not instantiated |
| `AOVIAS.AccredaOne/` | W6 scaffold |
| Studio orphans → Platform slnx | Policy unchanged |
| `ComplaincePlatform` **spelling rename** | Separate human decision |
| Application logic / NU1903 / npm health | Not restructuring |

---

## 4. W3 / W2 integration points that must stay valid

| Integration | Requirement after W4 |
|-------------|----------------------|
| **W3 Studio paths** | `AOVIAS.Studio/…`, publish-tool ProjectReferences, Runtime Agent Dockerfile COPY — **unchanged** by W4 |
| **W2 Infrastructure** | `AOVIAS.Infrastructure/aovias-config`, tools, ReleaseValidation — **unchanged** |
| **Platform root discovery** | Must still resolve with **`AOVIAS.Infrastructure/aovias-config`** + (**Command CompliancePlatform** or **Studio Runtime Agent**) per updated `discovery.current` |
| **CSM platform root** | Still **`aovias-config` + Studio Runtime Agent** (no Command folder in CSM marker set today) |
| **Publishing scan roots** | Command deployables + Studio Agent (replace root `ComplaincePlatform` segment only) |
| **Runtime Agent Docker** | `runtimeAgentDockerCopyRoots` remain **Studio-only** (no Command COPY in staged W3 Dockerfile) |

---

## 5. W5 exclusion and collision risks

| Risk | Assessment |
|------|------------|
| **W5 website** | Target `AOVIAS.Website` vs `AOVIAS.Command/WebApp` — **distinct** products; no target path overlap |
| **`AOVIAS.Command/` pre-exists** | **No** — safe to create |
| **Duplicate `WebApp` name** | Only one tracked `WebApp/` tree at repo root today |
| **Untracked root `ComplianceServiceManager/`** | Observed on `main` (legacy empty/local); **not** `AOVIAS.Command`; does not block W4 target paths |
| **Nested `.git` in Command trees** | **Not** present at HEAD (`ComplaincePlatform/.git`, `WebApp/.git` absent) — single monorepo move |
| **Overwrite on move** | None unless untracked files exist **inside** target paths — implementation should inspect before `git mv` |

---

## 6. Focused structural checks (W4-appropriate)

| Check | Result |
|-------|--------|
| Tracked trees exist at source paths | **Yes** — `ComplaincePlatform/`, `WebApp/` |
| Command slnx present | **Yes** — `ComplaincePlatform/CompliancePlatform.slnx` |
| Angular manifest | **Yes** — `WebApp/angular.json` |
| Layout JSON target profile matches M-C-01/02 | **Yes** — `discovery.target` / product targets aligned |
| W3 merge on `main` | **Yes** — baseline valid for W4 planning |
| Command csproj → Aovias.* ProjectReference at repo baseline | **Constraint preserved in layout JSON** — Command graph stays separate from Studio projects |

**Not performed (per phase):** builds, tests, discover, preflight, Docker, npm, full INV-1 re-audit, per-service metadata inventory.

---

## 7. Unresolved decisions (material only)

| Topic | Impact | Recommendation |
|-------|--------|----------------|
| **Legacy path transition** | Resolvers/tests currently assume **repo-root** `ComplaincePlatform` and `WebApp` | Mirror **W3 pattern**: update `discovery.current` + add **legacy directory fallbacks** for one transition window (human policy at W4 approval gate) |
| **Command CI `working-directory`** | `platform-integrity.yml` paths assume platform subtree as CWD | **Required** structural fix when workflow lives under `AOVIAS.Command/CompliancePlatform/` |
| **ComplaincePlatform spelling rename** | Deferred indefinitely | **Out of W4** — move folder as-is |

No decision blocks producing an implementation scope package; items above belong in **W4 approval checklist** (analogous to K-1–K-17 for W3).

---

## 8. Implementation scale (planning metric)

| Metric | Value |
|--------|--------|
| Tracked files to rename (approx.) | **4525** (3157 + 1368) |
| Expected cross-tree code/doc touch count | **Tens** of Studio/layout/publishing/CSM files + Command scripts/CI + `repository-layout.json` + selected root docs |
| Comparable prior wave | W3 (~1197 paths) — W4 **larger** rename surface, **smaller** line-churn expected if mostly `git mv` |

---

## 9. Confirmations

- **No** source edits, staging, commit, merge, push, or worktree sync in Phase 1I-AC.  
- This report is **untracked** and must not be committed unless separately authorized.

---

## 10. Recommendation

The repository **baseline, contract metadata, and W3 integration** support a **bounded W4 implementation** moving `ComplaincePlatform/` and `WebApp/` into `AOVIAS.Command/` with paired **`discovery.current`** and resolver updates.

**Ready for:** human **W4 scope approval package** and **implementation authorization** on the Cursor agent worktree (following `AGENTS.md`), not for execution in this phase.
