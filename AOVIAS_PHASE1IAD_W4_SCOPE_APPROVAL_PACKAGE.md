# AOVIAS Phase 1I-AD — W4 Scope Approval Package

**Phase:** 1I-AD (scope package only — **no W4 implementation**)  
**Date:** 2026-10-25  
**Prepared in:** `V:\DevelopmentRepo\AOVIAS.Cursor`  
**Authoritative integration baseline:** `V:\DevelopmentRepo\AOVIAS` · `main`

**Related:** `AOVIAS_PHASE1IAC_W4_MIGRATION_READINESS_REPORT.md` (Main, untracked)

---

## Final status (latest)

**`W4 SCOPE PARTIALLY APPROVED — IMPLEMENTATION DEFERRED — AWAITING SYNC AUTHORIZATION`**

§7 reflects the initial **UNDECIDED** checklist (Phase 1I-AD). **§9** records human decisions (Phase 1I-AE). **§10** requests separate Cursor worktree sync authorization.

---

## 1. Verified baseline and worktree state

| Worktree | Branch | HEAD | Tracked dirty | Notes |
|----------|--------|------|---------------|--------|
| **Main** (`V:\DevelopmentRepo\AOVIAS`) | `main` | `bcd63a6ecef7d67090be2104b3fd662635c1fa60` | **None** | **Matches** expected post–W3-merge baseline |
| **Cursor** (`V:\DevelopmentRepo\AOVIAS.Cursor`) | `agent/cursor/phase1ip-w3-studio-migration` | `6e66c37ab18852cb6cfd06e8d3dae2a67ed44c17` | **None** | W3 **commit** present; **does not** include merge commit `bcd63a6` (worktree not synced — expected until authorized) |

| Check | Result |
|-------|--------|
| W3 in `main` history | **Yes** (`6e66c37` ancestor of `bcd63a6`) |
| `AOVIAS.Command/` exists | **No** |
| Source trees tracked | `ComplaincePlatform/`, `WebApp/` present |
| Command slnx | `ComplaincePlatform/CompliancePlatform.slnx` |
| WebApp manifest | `WebApp/angular.json` |

No fetch, merge, stage, commit, or worktree sync performed in this phase.

**Implementation note:** Before W4 execution on Cursor, agent worktree should be aligned to **`main` @ `bcd63a6`** (or later validated integration) per `AGENTS.md` — **not** part of this approval package.

---

## 2. Exact directory moves (approved mapping)

| ID | Action | Source | Destination |
|----|--------|--------|-------------|
| **M-C-01** | `git mv` tree | `ComplaincePlatform/` | `AOVIAS.Command/CompliancePlatform/` |
| **M-C-02** | `git mv` tree | `WebApp/` | `AOVIAS.Command/WebApp/` |
| **M-C-03** | Co-move | `ComplaincePlatform/CompliancePlatform.slnx` | `AOVIAS.Command/CompliancePlatform/CompliancePlatform.slnx` |

**Spelling:** Directory segment **`ComplaincePlatform`** is preserved under `AOVIAS.Command/` (repository policy; separate rename deferred).

**Scale (planning):** ~**3157** + **~1368** tracked paths (rename-heavy commit, similar pattern to W3).

---

## 3. Minimal path-update inventory

Only changes **required** for structural validity after M-C-01–03. Not a general remediation backlog.

### 3.1 `repository-layout.json`

| Field / section | Expected change | Why |
|-----------------|-----------------|-----|
| `products.command.paths.compliancePlatform.current` | → `AOVIAS.Command/CompliancePlatform` | Reflect on-disk Command platform root |
| `products.command.paths.webApp.current` | → `AOVIAS.Command/WebApp` | Reflect WebApp root |
| `products.command.migrationStatus` | → command-at-target (or equivalent) | Contract status after W4 |
| `solutions[]` Command slnx `path.current` | → `AOVIAS.Command/CompliancePlatform/CompliancePlatform.slnx` | Solution entry points |
| `discovery.current.platformRoot.requiredAny` | Replace root `ComplaincePlatform` with **`AOVIAS.Command/CompliancePlatform`** | Platform-root marker checks |
| `discovery.current.defaultApplicationRoots` | Command-prefixed CompliancePlatform + WebApp paths | Default app roots |
| `discovery.current.deployableProjectScanRoots` | Command CompliancePlatform + unchanged Studio Agent path | Publishing discovery |
| `discovery.current.webAppAngularManifestRelative` | → `AOVIAS.Command/WebApp/angular.json` | Manifest-driven WebApp locate |
| `policies.knowledgeBaseCurrentPath` | → `AOVIAS.Command/CompliancePlatform/KnowledgeBase` | KB policy path |
| `compatibilityConstraints` (prose) | Document **Current** markers using Command paths; optional **legacy** root-folder fallback (W3-style transition) | Resolver contract text |

**Do not** enable **Target** profile for production adapters (same rule as W3/K-14).

### 3.2 Studio consumers (`AOVIAS.Studio/`)

| Area | Expected change | Why |
|------|-----------------|-----|
| `SolutionDeployableProjectDiscoverer.GetScanRoots` | Scan `AOVIAS.Command/CompliancePlatform` instead of root `ComplaincePlatform` | Deployable .NET discovery |
| `PlatformRootResolver.ResolveWebAppRoot` | Resolve `AOVIAS.Command/WebApp` | Angular portal path |
| `RepositoryLayoutPlatformRootDiscovery` | Accept Command CompliancePlatform directory in publishing legacy markers (+ optional old path during transition) | Fail-fast root walks when JSON missing |
| `SolutionPublishingPreflightService` | Platform-root directory probe uses Command path | Preflight consistency |
| `RootPathResolver` (CSM) | `ResolveCompliancePlatformRoot` / `ResolveWebAppRoot` (+ fallbacks) | CSM sibling discovery |
| `SourceDiscoveryBoundary` | Recognize Command folder names (and legacy names if transition approved) | Local-dev folder registry |
| **Layout tests** (`Aovias.Configuration.Tests/Layout/*`) | Fixture directories + embedded JSON **Current** paths | Contract tests match manifest |
| **Publishing tests** | `PublishingProjectPathsTests`, `ContractDrivenPublishingTests`, deployment verifier tests, etc. — path strings | Tests assert post-W4 layout |
| **CSM tests** | Source context / workspace tests using `ComplaincePlatform` paths | Test fixtures match repo layout |

**Exclude:** Changes to Studio Dockerfile COPY lists, `runtimeAgentDockerCopyRoots`, or Agent stager **unless** a direct W4 dependency is found (none identified at readiness).

### 3.3 Command consumers (inside moved trees + CI)

| Area | Expected change | Why |
|------|-----------------|-----|
| `ComplaincePlatform/scripts/build-praman-aovpkg.ps1` | `$repoRoot/ComplaincePlatform` → Command-prefixed path | Script locates platform root |
| `ComplaincePlatform/scripts/ops/*.ps1`, deploy scripts | Root resolution comments/paths | Ops scripts assume old layout |
| `.github/workflows/platform-integrity.yml` | Add **`working-directory: AOVIAS.Command/CompliancePlatform`** (or prefix all `dotnet` paths) | Workflow currently uses paths relative to platform subtree while checkout is monorepo root |
| `docker-compose*.yml` | Review `context:` / volume paths; update only if references break when compose file moves | Relative compose usually moves with tree; verify `../` to WebApp or repo root |

Internal slnx `<Project Path=` entries are **tree-relative** — expect minimal change after co-move; **validate** with focused Command slnx build in implementation phase.

### 3.4 Documentation (structural only)

| Area | Expected change | Why |
|------|-----------------|-----|
| `docs/milestones/G1-EXECUTION-RUNBOOK.md` | Publish CLI examples: `ComplaincePlatform/Gateway/…` → Command-prefixed paths | Operator copy-paste correctness |
| `docs/Architecture/**`, runbooks (subset) | **Optional** same-wave doc path pass for entry points cited in CI/runbooks | Consistency; defer deep doc sweep if out of scope |

No requirement to rewrite all historical architecture markdown in W4.

---

## 4. W2 / W3 integration constraints (must remain valid)

| Constraint | W4 impact |
|------------|-----------|
| `AOVIAS.Infrastructure/aovias-config`, tools, ReleaseValidation | **No path moves** in W4 |
| `AOVIAS.Studio/**` product trees, slnx, publish-tool ProjectReferences | **No moves**; **update references to Command/WebApp only** |
| Runtime Agent Dockerfile / stager | **Studio-only COPY** — unchanged unless proven otherwise |
| CSM platform root markers | Still **Infrastructure + Studio Runtime Agent** (not Command folder) |
| Command ↔ Studio csproj graph | **No new ProjectReference** across boundary (existing layout constraint) |

---

## 5. W5 and unrelated-project exclusions

| Excluded | Reason |
|----------|--------|
| `AOVIAS Website/AOVIAS Web/**` | **W5** → `AOVIAS.Website` |
| `AOVIAS.Shared` | Not instantiated |
| `AOVIAS.AccredaOne/` | Separate wave |
| Solution orphans → Platform slnx | Policy unchanged |
| `ComplaincePlatform` **spelling rename** | Separate decision |
| Application behavior, refactors, NU/npm/Docker validation | Out of restructuring scope |

---

## 6. Focused structural checks (proposed for implementation / post-move — **not run in 1I-AD**)

| Check | Purpose |
|-------|---------|
| `git diff --check` | Whitespace/conflict markers before commit |
| `dotnet build AOVIAS.Command/CompliancePlatform/CompliancePlatform.slnx` | Command area entry (when SDK available) |
| Layout / publishing path unit tests (`*Layout*`, `PublishingProjectPathsTests`, stager tests if touched) | Regression on path contracts |
| Publish tool **build** (not full publish run) | Ensures Infrastructure still compiles |
| Spot-check CI workflow path resolution (static review of `platform-integrity.yml`) | CI structural fix verification |

**Explicitly deferred** (unless separately authorized): `--discover`, full preflight, Docker execution/build, npm ReleaseValidation, full monorepo test matrix.

---

## 7. Implementation authorization checklist

Human decisions — **all UNDECIDED** (do not infer from W3):

| # | Decision | APPROVE | DEFER | REJECT | Evidence / notes |
|---|----------|---------|-------|--------|------------------|
| **K-W4-1** | W4 directory moves (M-C-01–M-C-03) approved | ☐ | ☐ | ☐ | §2 |
| **K-W4-2** | Structural path updates (§3.1–3.3 minimum) approved | ☐ | ☐ | ☐ | §3 |
| **K-W4-3** | Focused restructuring checks (§6) approved for implementation phase | ☐ | ☐ | ☐ | §6 |
| **K-W4-4** | **W4 implementation authorized** (Cursor task branch from synced `main`) | ☐ | ☐ | ☐ | Requires K-W4-1–2; worktree sync separate |

**Summary gates (for human use):**

- W4 directory moves approved: **UNDECIDED**
- Structural path updates approved: **UNDECIDED**
- Focused restructuring checks approved: **UNDECIDED**
- W4 implementation authorized: **UNDECIDED**

**Not authorized by this package:** commit, merge to `main`, push, worktree sync, W5, runtime validation boundary (carry K-16-style deferral unless explicitly closed for W4).

---

## 8. Confirmations (Phase 1I-AD)

- **No** source file edits or Git history changes in Phase 1I-AD.  
- This document is **untracked** in the Cursor worktree and is **not** staged.

---

## 9. Phase 1I-AE — Recorded human decisions (2026-10-25)

**Approval evidence:** User explicit decisions in Phase 1I-AE prompt.

| ID | Decision | Status | Record |
|----|----------|--------|--------|
| **K-W4-1** | W4 directory moves (M-C-01–M-C-03) | **APPROVED** | `ComplaincePlatform/` → `AOVIAS.Command/CompliancePlatform/`; `WebApp/` → `AOVIAS.Command/WebApp/`; preserve **`ComplaincePlatform`** spelling under Command parent; Command solution co-moves with owning tree (§2). |
| **K-W4-2** | Structural path updates | **APPROVED** | Minimum path and metadata changes in §3.1–3.3 (and structural G1 runbook examples in §3.4) **only** as directly required by approved moves. |
| **K-W4-3** | Focused restructuring checks | **APPROVED** | §6 checks allowed **after** implementation for restructuring validation; broad project validation and deferred operational checks (discover, full preflight, Docker execution/build, npm ReleaseValidation) **remain out of scope** unless separately authorized. |
| **K-W4-4** | W4 implementation authorization | **DEFERRED** | Implementation **not** authorized until Cursor worktree is aligned to approved local **`main`** baseline and a **separate** implementation authorization is issued. |

**Summary gates (current):**

- W4 directory moves approved: **YES**
- Structural path updates approved: **YES**
- Focused restructuring checks approved: **YES**
- W4 implementation authorized: **NO** (deferred)

**Not granted by K-W4-1–3:** commit, merge to `main`, push, worktree sync, or W4 file moves on any worktree.

---

## 10. Pending Authorization — Cursor Worktree Synchronization

**Purpose:** Align **`V:\DevelopmentRepo\AOVIAS.Cursor`** with local integration **`main`** at **`bcd63a6ecef7d67090be2104b3fd662635c1fa60`** (W3 merge commit), so a future W4 task branch starts from the same tree as human integration.

**Proposed outcome (conceptual — not executed in 1I-AE):**

- Cursor checkout reflects **`main` @ `bcd63a6`** (includes W3 merge commit, not only `6e66c37`).
- Dedicated W3 branch history (**`agent/cursor/phase1ip-w3-studio-migration`**, commit **`6e66c37`**) remains reachable in the repository; sync must **not** delete that branch or its commits without explicit human approval.
- All **untracked** phase reports and operational artifacts in Cursor are **preserved** (no `git clean`, no discard of untracked files).

**Preconditions before any sync operation (future phase):**

1. Verified **clean tracked** state in **both** Main and Cursor worktrees.
2. Confirmed **actual** `main` HEAD at Cursor remote-tracking or local ref matches authorized baseline (or documented newer integration commit).
3. **No** push or remote fetch unless separately authorized.
4. **Document** the exact Git commands (e.g. checkout `main`, merge/rebase strategy, return to `agent/cursor/active`) and their effect on branch pointers.
5. **Stop** if Cursor branch contains unexpected commits, dirty tracked files, or if the chosen operation could **discard** uncommitted or untracked work without preservation.

**Human action requested:** Explicitly authorize a **separate** Phase (Cursor worktree synchronization) to perform the alignment. This package **does not** authorize sync or select a destructive command.

---

## 11. Confirmations (Phase 1I-AE update)

- Phase 1I-AE updated **this approval package only** (decision record + sync request).  
- **No** Git history changes, **no** product source edits, **no** worktree synchronization in Phase 1I-AE.
