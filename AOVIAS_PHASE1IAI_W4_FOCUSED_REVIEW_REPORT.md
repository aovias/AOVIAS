# AOVIAS Phase 1I-AI — W4 Focused Structural Review Report

**Phase:** 1I-AI (read-only structural review — **no stage, edit, commit, merge, push, or sync**)  
**Date:** 2026-10-25  
**Worktree:** `V:\DevelopmentRepo\AOVIAS.Cursor`  
**Reviewed implementation:** Phase 1I-AH (`AOVIAS_PHASE1IAH_W4_IMPLEMENTATION_REPORT.md`)

---

## Final status

**`W4 FOCUSED REVIEW PASS — READY FOR STAGING-READINESS REVIEW`**

No W4-introduced structural defect was found in the approved relocation scope. Residual items are documented as non-blocking metadata or pre/post-staging validation gaps.

---

## 1. Verified baseline

| Check | Expected | Actual | Result |
|-------|----------|--------|--------|
| Worktree | `V:\DevelopmentRepo\AOVIAS.Cursor` | Same | **OK** |
| Branch | `agent/cursor/phase1ig-w4-command-migration` | Same | **OK** |
| HEAD | `bcd63a6ecef7d67090be2104b3fd662635c1fa60` | Same | **OK** |
| Commit on W4 work | None | None (diff vs `bcd63a6` only) | **OK** |
| Staging | None | ~4525 tracked path entries unstaged (`git status --porcelain`) | **OK** |
| Untracked reports | Preserved | 33 untracked entries (phase reports + probe); **no** `AOVIAS_PHASE1IAI_*` prior to this report | **OK** |

**Diff summary vs `bcd63a6`:** 4542 files changed, 125 insertions, 87 deletions. **`git diff --check bcd63a6`:** no reported issues (consistent with 1I-AH).

---

## 2. Directory moves

| Requirement | Evidence | Result |
|-------------|----------|--------|
| `ComplaincePlatform/` → `AOVIAS.Command/ComplaincePlatform/` | Root `ComplaincePlatform` absent; Command path present; rename-heavy diff | **Confirmed** |
| `WebApp/` → `AOVIAS.Command/WebApp/` | Root `WebApp` absent; Command path present | **Confirmed** |
| `CompliancePlatform.slnx` co-moved | `AOVIAS.Command/ComplaincePlatform/CompliancePlatform.slnx` exists | **Confirmed** |
| W5 website not moved | `AOVIAS Website/AOVIAS Web` present; **0** diff paths under `AOVIAS Website` | **Confirmed** |
| W2/W3 trees | Diff limited to Command renames + listed structural consumers; Studio/Infrastructure product roots unchanged in role | **Confirmed** |

Compose files under Command (`docker-compose.dev.yml`, `docker-compose.commercial.yml`) appear in diff as **renames with unchanged in-tree relative paths** (`context: .`, service-relative `working_dir`) — appropriate for co-move with the platform directory.

---

## 3. Repository layout (`repository-layout.json`)

### 3.1 Production-relevant (`current`) — aligned with on-disk W4

| Field | On-disk / intent | Review |
|-------|------------------|--------|
| `products.command.migrationStatus` | `command-at-target` | **Matches** W4 completion |
| `products.command.paths.compliancePlatform` | `AOVIAS.Command/ComplaincePlatform` (current + target) | **Matches** folder spelling policy |
| `products.command.paths.webApp` | `AOVIAS.Command/WebApp` | **Matches** |
| Command solution entry | `AOVIAS.Command/ComplaincePlatform/CompliancePlatform.slnx` | **Matches** |
| `discovery.current` markers, app roots, scan roots, WebApp manifest | Command-prefixed paths + Studio agent | **Matches** |
| `policies.knowledgeBaseCurrentPath` | `AOVIAS.Command/ComplaincePlatform/KnowledgeBase` | **Matches** |
| `compatibilityConstraints` prose | Documents Command `current` markers + legacy root fallbacks | **Matches** resolver behavior |

W4 **corrected** `products.command.paths.*.target` from the pre-W4 planning segment `AOVIAS.Command/CompliancePlatform` to **`ComplaincePlatform`**, consistent with `policies.compliancePlatformFolderSpelling`.

### 3.2 `discovery.target` spelling discrepancy

| Observation | Detail |
|-------------|--------|
| What | `discovery.target` still uses **`AOVIAS.Command/CompliancePlatform`** (correct spelling) in `platformRoot.requiredAny`, `defaultApplicationRoots`, and `deployableProjectScanRoots`. |
| W4 diff | **`discovery.target` block was not modified** in the W4 diff; this predates W4 execution on the integration baseline. |
| Production impact | **None.** `RepositoryLayoutProductionAdapter` and layout tests assert **`Target` is not a permitted production profile**; adapters use **`discovery.current`**. |
| Test fixtures | Target-profile contract tests intentionally create **`AOVIAS.Command/CompliancePlatform`** directories to exercise the non-production Target profile — separate from on-disk `ComplaincePlatform`. |

**Disposition:** **`verified non-blocking`** — intentional non-production contract metadata carried from pre-W4 planning. Optional future hygiene (align Target markers to `ComplaincePlatform` or document Target as “logical name vs folder spelling”) is a **separate contract decision**, not a W4 relocation blocker and **not required before staging** the W4 move commit.

---

## 4. Direct consumers and structural paths

### 4.1 Studio / CSM (post-move paths + transition fallbacks)

| Component | Post-W4 behavior | Review |
|-----------|------------------|--------|
| `SolutionDeployableProjectDiscoverer.GetScanRoots` | `AOVIAS.Command/ComplaincePlatform` + legacy root scan | **Valid** |
| `PlatformRootResolver.ResolveWebAppRoot` | Prefers `AOVIAS.Command/WebApp`, legacy `WebApp` | **Valid** |
| `RepositoryLayoutPlatformRootDiscovery` | Command + legacy root markers | **Valid** |
| `RootPathResolver` (CSM) | Walks for Command paths; legacy root; fallback relative paths under `AOVIAS.Command` | **Valid** for W4 layout |
| `SourceDiscoveryBoundary` | Command paths first, then legacy roots | **Valid** |
| `SolutionPublishingPreflightService` | Dockerfile walk stops on Command or legacy platform folder | **Valid** |

### 4.2 CSM layout contract test fixture (1I-AH fix)

`RepositoryLayoutCsmResolverContractTests.CreateFixture` creates the runtime agent marker at **`AOVIAS.Studio/Aovias.Runtime.Agent`**, matching **`discovery.current.csmPlatformRoot`** and W3 Studio placement. **Aligned.**

### 4.3 Command scripts and CI

| Artifact | Change | Review |
|----------|--------|--------|
| `AOVIAS.Command/ComplaincePlatform/scripts/build-praman-aovpkg.ps1` | Resolves platform dir from script location; repo root two levels above `AOVIAS.Command` | **Valid** for nested Command layout |
| `AOVIAS.Command/ComplaincePlatform/.github/workflows/platform-integrity.yml` | `defaults.run.working-directory: AOVIAS.Command/ComplaincePlatform` (repo-root-relative for checkout) | **Valid** |

Repo-root `scripts/` (tracked): **no** remaining `Join-Path $repoRoot "ComplaincePlatform"` patterns found in review grep.

### 4.4 Documentation (structural)

`docs/milestones/G1-EXECUTION-RUNBOOK.md`: PublishAllServices example project paths updated to **`AOVIAS.Command/ComplaincePlatform/...`**. **Valid.**

### 4.5 W2/W3 paths

Studio solution entries, `discovery.current` Studio agent/docker copy roots, and Infrastructure config paths in `repository-layout.json` remain **unchanged in substance** by the W4 diff. **No regression observed** in reviewed structural edits.

---

## 5. Focused evidence (existing — not re-run)

Per Phase 1I-AH and scope limits, the following were **accepted from prior execution** without re-running broad validation:

| Evidence | Result cited |
|----------|----------------|
| Layout tests (`FullyQualifiedName~Layout`) | **39 / 39 passed** |
| `PublishingProjectPathsTests` | **2 / 2 passed** |
| `git diff --check` | **Pass** |

No additional tests or builds were executed in 1I-AI; review of the diff and on-disk layout did not surface an uncertainty that required a new focused test run.

---

## 6. Defects vs deferred / pre-existing

### 6.1 Confirmed W4 structural defects

**None.**

### 6.2 Non-blocking / out of scope (not staging blockers for W4 move)

| Item | Classification | Notes |
|------|----------------|-------|
| `discovery.target` `CompliancePlatform` vs on-disk `ComplaincePlatform` | Pre-existing Target metadata | See §3.2 — non-blocking |
| Full Command/Studio solution builds, broad CSM suites, CI workflow execution | Validation gap (1I-AH deferred) | Address in staging-readiness / post-commit validation |
| Untracked `scripts/dev/_phase3h_runtime_probe.py` still references root `ComplaincePlatform/...` | Pre-existing operational artifact | **Not** in W4 tracked diff; update separately if probe is still used |
| Historical markdown under Command/WebApp using in-tree `WebApp/src/...` prose | Documentation convention | Not repo-root path references |

---

## 7. Staging-readiness disposition

| Question | Answer |
|----------|--------|
| W4 moves match approved scope? | **Yes** |
| `discovery.current` and Command product paths match disk? | **Yes** |
| Structural consumers updated consistently? | **Yes**, with documented legacy fallbacks |
| W5 boundary respected? | **Yes** |
| Ready for human **staging-readiness** review of the unstaged W4 diff? | **Yes** |

Staging, commit, merge, push, and worktree sync remain **separate authorization** steps.

---

## 8. Artifacts

- **Created (untracked):** `AOVIAS_PHASE1IAI_W4_FOCUSED_REVIEW_REPORT.md`
- **Preserved:** All prior untracked phase reports and operational artifacts
- **No** file modifications beyond this report in Phase 1I-AI

**Reviewer mode:** Read-only structural review (Cursor agent context)  
**Implementation reference:** `AOVIAS_PHASE1IAH_W4_IMPLEMENTATION_REPORT.md`
