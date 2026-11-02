# AOVIAS Phase 1I-L — W3 Approval Record and Execution Authorization Gate

**Phase:** 1I-L (approval gate only — **no W3 implementation**)  
**Date:** 2026-10-25  
**Expected baseline:** `b9e2de634b86d04f54b3a1d97ad6bfc44fa646f1`  
**Deliverable Git status:** Untracked (not staged, not committed)

**Authoritative approval state (latest):** **§12 Phase 1I-R** (2026-10-25). **§ Final approval state**, **§8**, **§10.6**, **§11.5**, and earlier sections reflect prior point-in-time audits and are preserved unchanged.

---

## Final approval state

**`W3 APPROVAL UNDECIDED`**

Baseline and scope package are **sufficient** to prepare an implementation brief, but **no explicit human approval evidence** exists for the Phase 1I-K 17-item checklist or for W3 file moves. Phase 1I-B records **recommendations** and states **human sign-off still required**; it does **not** substitute for per-item W3 approval.

**This Phase 1I-L prompt does not authorize W3 implementation.**

---

## 1. Baseline and worktree verification (verified facts)

| Worktree | Path | Branch | HEAD | Tracked changes (`-uno`) |
|----------|------|--------|------|---------------------------|
| Main | `V:\DevelopmentRepo\AOVIAS` | `main` | `b9e2de634b86d04f54b3a1d97ad6bfc44fa646f1` | **None** |
| Cursor | `V:\DevelopmentRepo\AOVIAS.Cursor` | `agent/cursor/active` | `b9e2de634b86d04f54b3a1d97ad6bfc44fa646f1` | **None** |
| Claude | `V:\DevelopmentRepo\AOVIAS.Claude` | `agent/claude/active` | `b9e2de634b86d04f54b3a1d97ad6bfc44fa646f1` | **None** |

**Verified:** All three worktrees match the expected W2 post-merge baseline. No staged or unstaged **tracked** modifications reported.

**Untracked artifacts (preserved — not modified in 1I-L):**

- Main: numerous `AOVIAS_PHASE*.md` reports (including `AOVIAS_PHASE1IK_W3_SCOPE_APPROVAL_PACKAGE.md`), `AOVIAS_PHASE1IJ_inv1_inventory.json`, `AOVIAS_PHASE1IJ_inv1_matrix.csv`, website ZIPs, Lighthouse output directories, and other phase reports listed in `git status`.
- Prior reports note Cursor untracked `scripts/dev/_phase3h_runtime_probe.py` and Claude untracked CommandCenter `Properties/launchSettings.json` — **not re-verified file-by-file in 1I-L**; no agent action taken on them.

**W2 integration evidence:** `AOVIAS_PHASE1IH_W2_MERGE_AND_VALIDATION_REPORT.md`, `AOVIAS_PHASE1II_WORKTREE_SYNCHRONIZATION_REPORT.md` (merge @ `b9e2de6`).

**Blocking baseline issues:** **None.**

---

## 2. Source documents reviewed

| Document | Path | Status |
|----------|------|--------|
| Agent contract | `AGENTS.md` | **Read** (policy reference) |
| Layout manifest | `repository-layout.json` | **Read** (studio targets, `discovery.current` / `discovery.target`, O-3 note) |
| INV-1 report | `AOVIAS_PHASE1IJ_INV1_FULL_DEPENDENCY_INVENTORY_REPORT.md` | **Present** |
| INV-1 JSON | `AOVIAS_PHASE1IJ_inv1_inventory.json` | **Present** |
| INV-1 matrix | `AOVIAS_PHASE1IJ_inv1_matrix.csv` | **Present** |
| W3 scope package | `AOVIAS_PHASE1IK_W3_SCOPE_APPROVAL_PACKAGE.md` | **Present** |
| Architecture decisions | `AOVIAS_PHASE1IB_ARCHITECTURE_DECISIONS_REPORT.md` | **Present** — status **ARCHITECTURE READY FOR HUMAN APPROVAL** (recommendations, not W3 execution auth) |
| W2 merge validation | `AOVIAS_PHASE1IH_W2_MERGE_AND_VALIDATION_REPORT.md` | **Present** |
| Worktree sync | `AOVIAS_PHASE1II_WORKTREE_SYNCHRONIZATION_REPORT.md` | **Present** |
| Target layout / M-S manifest | `AOVIAS_PHASE1IA_TARGET_PHYSICAL_LAYOUT_REPORT.md` | **Present** |
| Phase 1B migration contract | `AOVIAS_PHASE1B_MIGRATION_CONTRACT.md` | **Not opened line-by-line in 1I-L** — M-S rows reconciled via 1I-A + 1I-K + `repository-layout.json` |
| W1 layout manifest | `AOVIAS_PHASE1A_LAYOUT_MANIFEST.md` | **Not required for W3 row reconciliation** — superseded by 1I-A / layout JSON for Studio wave |

**Missing sources that prevent reliable approval:** **None** for scope definition. **Missing human approval records** for W3 checklist items prevent **`W3 SCOPE APPROVED`** or **`W3 IMPLEMENTATION AUTHORIZED`**.

---

## 3. Phase 1I-K checklist — human approval record (17 items)

**Rule applied:** No item marked **APPROVED** without explicit human approval in the current conversation or an attributable signed checklist. Phase 1I-K boxes are **empty (☐)**.

| ID | Scope / decision | Source → destination or contract | Evidence | Decision | Human approval evidence | Notes |
|----|------------------|-----------------------------------|----------|----------|-------------------------|-------|
| **K-1** | M-S-01 Platform slnx move | `Aovias.Platform.slnx` → `AOVIAS.Studio/Aovias.Platform.slnx` | 1I-K §2, §4; `repository-layout.json` L68–70 | **UNDECIDED** | None | 38 project paths |
| **K-2** | M-S-02 PublishingEngine slnx | `Aovias.PublishingEngine.slnx` → `AOVIAS.Studio/…` | 1I-K §2, §4; JSON L72–74 | **UNDECIDED** | None | 14 projects |
| **K-3** | M-S-03–06 CSM cluster | CSM + Tests + Operations (+ Tests) → `AOVIAS.Studio/…` | 1I-K §2; 1I-A M-S-03–06 | **UNDECIDED** | None | Includes orphan Tests tree move |
| **K-4** | M-S-07–09 Configuration, Deployment.Contracts, CommandCenter | Root → `AOVIAS.Studio/…` | JSON L80–90; O-1/O-2 in JSON `approved-planning` | **UNDECIDED** | None | Planning ≠ move auth |
| **K-5** | M-S-10 Runtime.Agent (+ Contracts in-tree) | `Aovias.Runtime.Agent/` → `AOVIAS.Studio/…` | 1I-K §3 O-4; INV-1 | **UNDECIDED** | None | Docker/stager coupling |
| **K-6** | M-S-11–14 Publishing, Bootstrap, Release, Orchestration | Root → `AOVIAS.Studio/…` | 1I-K §2 | **UNDECIDED** | None | Includes Bootstrap `.sln` |
| **K-7** | M-S-15 DesignSystem | → `AOVIAS.Studio/Aovias.DesignSystem/` | JSON L112–114 | **UNDECIDED** | None | |
| **K-8** | M-S-16 DeploymentValidation (O-3) | → `AOVIAS.Studio/Aovias.DeploymentValidation/` | 1I-B recommends Studio; JSON `ownershipNote` still O-3 unresolved | **UNDECIDED** | None | Reject Infrastructure alt unless human selects B |
| **K-9** | M-S-17 nested area slnx co-move | 7 nested slnx under moved trees | 1I-K §4 (8 Studio slnx + 2 root = 9 Studio-facing; Command slnx W4) | **UNDECIDED** | None | Co-move with parent dirs |
| **K-10** | O-3 Docker/stager/discovery DRV paths | Agent `Dockerfile`, stager, `runtimeAgentDockerCopyRoots` | HEAD Dockerfile L17; stager fallback; JSON L201–208 | **UNDECIDED** | None | See §5.C |
| **K-11** | O-4 Contracts co-move | Under `…/Aovias.Runtime.Agent/src/Aovias.Runtime.Contracts/` | 1I-B recommendation A; CSM csproj ref | **UNDECIDED** | None | O-6 Shared remains deferred |
| **K-12** | Nine Studio `.slnx` path rewrites | All Studio slnx + Platform/PublishingEngine entries | 1I-K §4; INV-1 | **UNDECIDED** | None | Excludes `CompliancePlatform.slnx` |
| **K-13** | W2 publish tools ProjectReference updates | Infra tools → Studio Configuration/Publishing SDK | **Verified HEAD:** `PublishAllServices.csproj` L13–14; `PublishRuntimeAgent.csproj` L13–14 use `..\..\..\Aovias.Configuration` and `..\..\..\Aovias.Publishing\…` | **UNDECIDED** | None | Post-W3: `..\..\..\AOVIAS.Studio/Aovias.Configuration` etc. |
| **K-14** | `discovery.current` update; Target not production | `repository-layout.json` current profile | JSON L187–209 vs L211–238 target | **UNDECIDED** | None | 1I-B: do not enable Target in production |
| **K-15** | Preserve 11 solution orphans | No slnx membership changes | 1I-J / 1I-K §4 | **UNDECIDED** | None | |
| **K-16** | Defer discover/preflight/Docker/npm DRV | Out of scoped post-move validation | 1I-K §5.4, §6 | **UNDECIDED** | None | Acknowledgement of deferral policy |
| **K-17** | Authorize separate W3 implementation task | Cursor task branch + later merge | AGENTS.md lifecycle | **UNDECIDED** | None | Distinct from scope approval |

**Checklist tally:** **Approved 0** · **Rejected 0** · **Deferred 0** · **Undecided 17**

---

## 4. Individual manifest rows (M-S-01 … M-S-17)

Canonical checklist mapping in **Notes**. All **UNDECIDED** unless human records otherwise.

| Row | Operation | Current → destination | Checklist | Decision | Evidence |
|-----|-----------|----------------------|-----------|----------|----------|
| **M-S-01** | MOVE | `Aovias.Platform.slnx` → `AOVIAS.Studio/Aovias.Platform.slnx` | K-1 | **UNDECIDED** | 1I-A, 1I-K, JSON |
| **M-S-02** | MOVE | `Aovias.PublishingEngine.slnx` → `AOVIAS.Studio/…` | K-2 | **UNDECIDED** | Same |
| **M-S-03** | MOVE | `ComplianceServiceManager/` → `AOVIAS.Studio/ComplianceServiceManager/` | K-3 | **UNDECIDED** | Same |
| **M-S-04** | MOVE | `ComplianceServiceManager.Tests/` → `AOVIAS.Studio/…` | K-3 | **UNDECIDED** | Orphan preserved |
| **M-S-05** | MOVE | `ComplianceServiceManager.Operations/` → `AOVIAS.Studio/…` | K-3 | **UNDECIDED** | Same |
| **M-S-06** | MOVE | `ComplianceServiceManager.Operations.Tests/` → `AOVIAS.Studio/…` | K-3 | **UNDECIDED** | Same |
| **M-S-07** | MOVE | `Aovias.Configuration/` → `AOVIAS.Studio/Aovias.Configuration/` | K-4 | **UNDECIDED** | O-2a planning in JSON |
| **M-S-08** | MOVE | `Aovias.Deployment.Contracts/` → `AOVIAS.Studio/…` | K-4 | **UNDECIDED** | O-2b planning in JSON |
| **M-S-09** | MOVE | `Aovias.CommandCenter/` → `AOVIAS.Studio/…` | K-4 | **UNDECIDED** | O-1 planning in JSON |
| **M-S-10** | MOVE | `Aovias.Runtime.Agent/` → `AOVIAS.Studio/…` | K-5 | **UNDECIDED** | Includes O-4 tree |
| **M-S-11** | MOVE | `Aovias.Publishing/` → `AOVIAS.Studio/…` | K-6 | **UNDECIDED** | Same |
| **M-S-12** | MOVE | `Aovias.Bootstrap/` → `AOVIAS.Studio/…` | K-6 | **UNDECIDED** | Legacy `.sln` |
| **M-S-13** | MOVE | `Aovias.Release/` → `AOVIAS.Studio/…` | K-6 | **UNDECIDED** | Same |
| **M-S-14** | MOVE | `Aovias.Orchestration/` → `AOVIAS.Studio/…` | K-6 | **UNDECIDED** | Same |
| **M-S-15** | MOVE | `Aovias.DesignSystem/` → `AOVIAS.Studio/…` | K-7 | **UNDECIDED** | Same |
| **M-S-16** | MOVE | `Aovias.DeploymentValidation/` → **`AOVIAS.Studio/Aovias.DeploymentValidation/`** (1I-B / 1I-K binding for planning) | K-8, K-10 | **UNDECIDED** | 1I-A listed Infrastructure alternate — human must confirm Studio |
| **M-S-17** | MOVE | Nested `*.slnx` under product folders | K-9, K-12 | **UNDECIDED** | Moves with M-S-03–14 trees |

---

## 5. Supplementary decision rows (architecture and boundaries)

| Topic | Scope | Decision | Human approval evidence | Notes |
|-------|-------|----------|-------------------------|-------|
| **O-3 ownership** | DRV product bucket = Studio | **UNDECIDED** (W3 binding) | None | 1I-B **recommends** Studio; JSON `ownershipNote` still provisional |
| **O-4 placement** | Contracts co-move with Agent | **UNDECIDED** (W3 binding) | None | 1I-B **recommends** option A |
| **O-5 publish tools** | Infrastructure ownership | **APPROVED** (W2 executed) | Merge `b9e2de6`, task `3a566a8` | **Not** W3 scope approval; historical |
| **O-6 Shared** | Defer Shared population | **DEFERRED** (contract posture) | 1I-B recommendation | No W3 Shared creation |
| **Nine Studio slnx rewrites** | Path updates in 8 Studio slnx + 2 root slnx | **UNDECIDED** | None | Canonical: **K-12** |
| **Legacy `Aovias.Bootstrap.sln`** | Sibling refs to Configuration / Release.Contracts | **UNDECIDED** | None | Covered by **K-6** / M-S-12 |
| **11 solution orphans** | No slnx add/remove | **UNDECIDED** | None | **K-15** |
| **Publish tool csproj refs** | Infra → Studio paths | **UNDECIDED** | None | **K-13**; paths verified at HEAD |
| **`discovery.current`** | Post-W3 current profile | **UNDECIDED** | None | **K-14** |
| **W4 exclusion** | `ComplaincePlatform/`, `WebApp/` | **UNDECIDED** (acknowledgement) | None | Plan states KEEP until W4 — human must agree |
| **W5 exclusion** | Website tree | **UNDECIDED** (acknowledgement) | None | Same |
| **Deferred operational validation** | discover, preflight, Docker, npm | **UNDECIDED** (acknowledgement) | None | **K-16** |
| **Target profile** | Must not enable in production | **UNDECIDED** (acknowledgement) | None | 1I-B verified Current-only production |
| **Commit authorization** | W3 implementation commit | **UNDECIDED** | None | Separate from scope |
| **Merge authorization** | `--no-ff` to `main` | **UNDECIDED** | None | Human on Main only |
| **Push authorization** | Remote | **UNDECIDED** | None | Not assumed |
| **Worktree sync authorization** | Post-merge agent active reset | **UNDECIDED** | None | After merge validation |

**Planning-only manifest approvals (not W3 move authorization):** `repository-layout.json` `ownershipDecisions` entries **O-1**, **O-2a**, **O-2b** carry `"status": "approved-planning"` — recorded as **approved for planning ownership**, not as approval of physical W3 moves.

---

## 6. Scope-completeness audit (against Phase 1I-K)

### A. Studio migration inventory (M-S-01 … M-S-17)

| Criterion | Result |
|-----------|--------|
| Exact source/destination | **Complete** in 1I-K §2 (aligned with 1I-A + JSON targets) |
| MOVE / KEEP / DEFER | **Complete** — all M-S rows MOVE; W4/W5 KEEP documented |
| Solution membership | **Complete** at INV-1 level (counts; matrix CSV for Platform 38) |
| Dependencies / refs | **Complete** at planning level; full csproj graph in INV-1 |
| Path rewrites | **Complete** for slnx, publish tools, Bootstrap sln, Dockerfile/stager/discovery |
| Atomic groups G1–G4 | **Complete** in 1I-K §2, §6 |
| Validation criteria | **Complete** (scoped builds/tests from W2 pattern) |
| Rollback | **Complete** (revert merge; no `git clean`) |

**Gap (non-blocking, implementation choice):** Docker build context strategy — 1I-K allows repo-root context with `COPY AOVIAS.Studio/.../` **or** stager copying into legacy layout names. Package does **not** mandate a single approach; implementer must pick one consistent scheme and update stager + Dockerfile + tests together.

**Clarification (not a package error):** `Aovias.DeploymentValidation` has **no separate Dockerfile** at HEAD; O-3 Docker work is **`Aovias.Runtime.Agent/Dockerfile`** COPY of DRV tree plus stager/discovery (1I-K §3).

**JSON vs 1I-B:** M-S-16 1I-A text allows Infrastructure alternate; 1I-K binds planning to Studio per 1I-B — **human must confirm** (K-8).

### B. Solutions

| Requirement | Verified in 1I-K |
|-------------|------------------|
| Nine `.slnx` at HEAD | **Yes** — 8 Studio + 1 Command (W4) |
| Platform 38 projects | **Yes** |
| PublishingEngine 14 | **Yes** |
| Seven nested area slnx | **Yes** (co-move M-S-17) |
| `Aovias.Bootstrap.sln` | **Yes** |
| `CompliancePlatform.slnx` excluded | **Yes** |
| 11 orphans preserved | **Yes** — no authorize-to-fix |

### C. O-3 DeploymentValidation and Docker paths

| Element | Accounted |
|---------|-----------|
| DRV tree move | **Yes** (M-S-16) |
| Agent Dockerfile COPY lines | **Yes** — HEAD uses top-level `Aovias.*` names (L13–29) |
| Stager fallback + regex | **Yes** — `RuntimeAgentDockerContextStager.cs` |
| `runtimeAgentDockerCopyRoots` | **Yes** — JSON current vs target |
| Tests | **Yes** — `RuntimeAgentDockerContextStagerTests.cs` |
| Build context correctness | **Partially specified** — two strategies noted; requires implementation decision |

### D. O-4 Runtime.Contracts

| Requirement | Verified |
|-------------|----------|
| Stays under Agent `src/` | **Yes** |
| Co-move with Agent | **Yes** (M-S-10) |
| CSM csproj update | **Yes** — `ComplianceServiceManager.csproj` ref at HEAD |
| Relative-path-only changes | **Yes** (planning) |
| O-6 deferred | **Yes** |

### E. W2 publishing tools

| Tool | HEAD refs | Post-W3 per 1I-K |
|------|-----------|------------------|
| `AOVIAS.Infrastructure/tools/PublishAllServices/PublishAllServices.csproj` | `..\..\..\Aovias.Configuration`, `..\..\..\Aovias.Publishing\src\Aovias.Publishing.SDK\…` | `..\..\..\AOVIAS.Studio/Aovias.Configuration`, `..\..\..\AOVIAS.Studio/Aovias.Publishing\…` |
| `AOVIAS.Infrastructure/tools/PublishRuntimeAgent/PublishRuntimeAgent.csproj` | Same | Same |

**Same atomic batch:** **Yes** in 1I-K §5.1, §6.

### F. Discovery and layout contracts

**Proposed `discovery.current` updates** documented in 1I-K §5.2 (Agent, CSM roots, docker copy roots, `products.studio.paths.*.current`). **Target profile not enabled in production** — consistent with 1I-B §4.

**Additional consumer (planning):** `SolutionDeployableProjectDiscoverer.GetScanRoots` hard-codes `Aovias.Runtime.Agent` (HEAD L140–141) — W3 batch must update to `AOVIAS.Studio/Aovias.Runtime.Agent` or equivalent; 1I-K §5.3 references discoverer scoped update.

### G. Boundary protection

| Exclusion | Explicit in 1I-K |
|-----------|------------------|
| W4 Command/WebApp | **Yes** |
| W5 Website | **Yes** |
| W2 except publish ref updates | **Yes** |
| Deferred discover/preflight/Docker/npm | **Yes** |

**No scope expansion required** to honor exclusions identified at audit time.

**Scope-completeness verdict:** **Adequate for human approval and future implementation planning** with the Docker/stager strategy called out as an implementation-time decision.

---

## 7. W3 implementation brief (prepared — **not authorized to execute**)

Use only after:

1. All required checklist items (§3) marked **APPROVED** (or explicit partial scope with rejected rows excluded), and  
2. Separate explicit human instruction: **authorize W3 implementation**.

### 7.1 Baseline and branch

| Item | Value |
|------|--------|
| **Baseline commit** | `b9e2de634b86d04f54b3a1d97ad6bfc44fa646f1` |
| **Worktree** | `V:\DevelopmentRepo\AOVIAS.Cursor` only (per AGENTS.md) |
| **Proposed task branch** | `agent/cursor/phase1il-w3-migration` (or `agent/cursor/w3-studio-relocation` — human may rename; must follow `agent/cursor/<task-slug>` rules) |
| **Base branch** | Up-to-date `main` @ baseline (human verifies before branch create) |

### 7.2 Move manifest (git mv targets)

Create `AOVIAS.Studio/` and move **tracked** trees:

```text
Aovias.Platform.slnx                          → AOVIAS.Studio/Aovias.Platform.slnx
Aovias.PublishingEngine.slnx                  → AOVIAS.Studio/Aovias.PublishingEngine.slnx
ComplianceServiceManager/                     → AOVIAS.Studio/ComplianceServiceManager/
ComplianceServiceManager.Tests/               → AOVIAS.Studio/ComplianceServiceManager.Tests/
ComplianceServiceManager.Operations/          → AOVIAS.Studio/ComplianceServiceManager.Operations/
ComplianceServiceManager.Operations.Tests/    → AOVIAS.Studio/ComplianceServiceManager.Operations.Tests/
Aovias.Configuration/                         → AOVIAS.Studio/Aovias.Configuration/
Aovias.Deployment.Contracts/                  → AOVIAS.Studio/Aovias.Deployment.Contracts/
Aovias.CommandCenter/                         → AOVIAS.Studio/Aovias.CommandCenter/
Aovias.Runtime.Agent/                         → AOVIAS.Studio/Aovias.Runtime.Agent/
Aovias.Publishing/                            → AOVIAS.Studio/Aovias.Publishing/
Aovias.Bootstrap/                             → AOVIAS.Studio/Aovias.Bootstrap/
Aovias.Release/                               → AOVIAS.Studio/Aovias.Release/
Aovias.Orchestration/                         → AOVIAS.Studio/Aovias.Orchestration/
Aovias.DesignSystem/                          → AOVIAS.Studio/Aovias.DesignSystem/
Aovias.DeploymentValidation/                  → AOVIAS.Studio/Aovias.DeploymentValidation/
```

Nested slnx move with parent folders (M-S-17). **Do not** move `ComplaincePlatform/`, `WebApp/`, `AOVIAS Website/`, or Infrastructure product trees except csproj edits in publish tools.

### 7.3 Atomic groups and order

| Order | Group | Content |
|-------|-------|---------|
| 1 | **G2–G4 trees** | All product directories (M-S-03–16, includes Agent+DRV) |
| 2 | **G1 slnx** | Root Platform + PublishingEngine slnx (if not moved in step 1) |
| 3 | **References** | All affected `.slnx`, `Aovias.Bootstrap.sln`, `.csproj` ProjectReferences, CSM → Contracts |
| 4 | **Infra cross-wave** | `PublishAllServices.csproj`, `PublishRuntimeAgent.csproj` |
| 5 | **Docker/stager/discovery** | Agent Dockerfile, `RuntimeAgentDockerContextStager.cs`, `TryResolvePlatformRoot`, `repository-layout.json` (`discovery.current`, studio `paths.current`, migration status), `GetScanRoots` |
| 6 | **Tests/docs** | Layout test expectations, runbook paths as needed |

Single commit strongly preferred (W2 precedent).

### 7.4 Files expected to change (non-exhaustive; INV-1 matrix is authoritative for csproj)

| Category | Paths |
|----------|--------|
| **Solutions** | `AOVIAS.Studio/Aovias.Platform.slnx`, `AOVIAS.Studio/Aovias.PublishingEngine.slnx`, nested area `*.slnx` (7), `AOVIAS.Studio/Aovias.Bootstrap/Aovias.Bootstrap.sln` |
| **Publish tools** | `AOVIAS.Infrastructure/tools/PublishAllServices/PublishAllServices.csproj`, `…/PublishRuntimeAgent/PublishRuntimeAgent.csproj` |
| **Docker** | `AOVIAS.Studio/Aovias.Runtime.Agent/Dockerfile` |
| **Stager** | `AOVIAS.Studio/Aovias.Publishing/src/Aovias.Publishing.Core/Generation/RuntimeAgentDockerContextStager.cs` |
| **Discovery** | `…/Discovery/SolutionDeployableProjectDiscoverer.cs` |
| **Manifest** | `repository-layout.json` |
| **CSM** | `AOVIAS.Studio/ComplianceServiceManager/ComplianceServiceManager.csproj` (+ possible resolver/config paths per layout tests) |
| **Layout tests** | Under `Aovias.Configuration/tests/…` (paths shift with Configuration tree) |
| **Publishing tests** | `RuntimeAgentDockerContextStagerTests.cs`, `PublishingProjectPathsTests` fixtures if path literals |

Plus **many** `.csproj` relative references inside moved trees — validate via build failures and INV-1 matrix.

### 7.5 Pre-migration assertions

- Worktree clean (tracked); on task branch from `main` @ `b9e2de6`
- Human written authorization referencing approved checklist IDs
- No concurrent W4/W5 work on same paths

### 7.6 Post-migration structural checks

- No remaining top-level `Aovias.Platform.slnx` / moved product dirs at old paths (except W4/W5)
- `AOVIAS.Studio/` contains all M-S trees
- Publish tool ProjectReference paths exist on disk
- `repository-layout.json` `discovery.current` matches disk for Agent + docker copy roots

### 7.7 Focused validation (safe after implementation — **requires implementation authorization**)

From W2 validation report (confirm paths after move):

```powershell
dotnet build "AOVIAS.Studio/Aovias.Platform.slnx" -c Release
dotnet build "AOVIAS.Studio/Aovias.PublishingEngine.slnx" -c Release
dotnet test "AOVIAS.Studio/Aovias.Configuration/tests/Aovias.Configuration.Tests/Aovias.Configuration.Tests.csproj" -c Release --filter FullyQualifiedName~Layout
dotnet test "AOVIAS.Studio/Aovias.Publishing/tests/Aovias.Publishing.Core.Tests/Aovias.Publishing.Core.Tests.csproj" -c Release --filter FullyQualifiedName~PublishingProjectPaths
dotnet test "AOVIAS.Studio/Aovias.Publishing/tests/Aovias.Publishing.Core.Tests/Aovias.Publishing.Core.Tests.csproj" -c Release --filter FullyQualifiedName~RuntimeAgentDockerContextStager
dotnet build "AOVIAS.Infrastructure/tools/PublishAllServices/PublishAllServices.csproj" -c Release
dotnet build "AOVIAS.Infrastructure/tools/PublishRuntimeAgent/PublishRuntimeAgent.csproj" -c Release
```

**Label:** Filter names and project paths **require confirmation** after move (paths above assume Studio layout).

### 7.8 Explicitly deferred validation

- `dotnet` / CLI **`--discover`**
- Publishing **preflight**
- **Docker daemon** image build
- **npm** / ReleaseValidation website flows
- Full Platform test suite (unless human expands scope)

### 7.9 Git diff and staging controls

- Review full diff before commit; no secrets, no accidental W4 files
- Stage only W3 scope; do not stage unrelated untracked phase reports unless human directs
- Conventional commit message (W2 precedent: `refactor: …`)

### 7.10 Rollback

- Revert merge commit on `main` (human)
- Do **not** `git clean -fd` on product trees (ignored/local artifacts)
- Preserve task branch until human confirms

### 7.11 Stop conditions

- Unexpected tracked paths outside approved M-S rows
- Unresolved ProjectReference after batch
- Layout or stager tests fail
- Ambiguity on DRV destination or Docker context strategy mid-implementation
- Attempt to add orphans to slnx without approval

### 7.12 Independent gates (W2 workflow — each separate human authorization)

| Gate | Owner |
|------|--------|
| W3 implementation start | Human → Cursor task assignment |
| Independent review | Human or second agent (read-only review report) |
| Staging readiness | Human |
| **Commit** | Human explicit request |
| Merge readiness review | Human (read-only on Main) |
| **Merge** to `main` | Human on Main |
| **Worktree sync** | Human after merge validation |
| **Push** | Human |

No gate auto-authorizes the next.

---

## 8. Approval state machine

```text
[W3 APPROVAL UNDECIDED]  ← current (this report)
        │
        │ human completes §3 checklist with explicit APPROVE/REJECT/DEFER
        ▼
[W3 APPROVAL BLOCKED]     if baseline/package broken or contradictions unresolved
        OR
[W3 SCOPE APPROVED — EXECUTION NOT AUTHORIZED]  if all required scope items APPROVED
        │
        │ separate explicit "authorize W3 implementation" instruction
        ▼
[W3 IMPLEMENTATION AUTHORIZED]  scope limited to named approved items only
```

**Current state:** **`W3 APPROVAL UNDECIDED`**

**Not applicable:** `W3 SCOPE APPROVED — EXECUTION NOT AUTHORIZED` (zero checklist APPROVED)  
**Not applicable:** `W3 IMPLEMENTATION AUTHORIZED`  
**Not applicable:** `W3 APPROVAL BLOCKED` (baseline OK; package present)

---

## 9. Required next human action

1. **Review** `AOVIAS_PHASE1IK_W3_SCOPE_APPROVAL_PACKAGE.md` and this record.  
2. **Record explicit decisions** for checklist items **K-1 through K-17** (and M-S rows if any differ from grouped checklist). Update this file or a human-signed annex with APPROVE/REJECT/DEFER and initials/date — **or** reply in integration channel with itemized approval.  
3. **Confirm M-S-16 / O-3** as Studio (not Infrastructure).  
4. When all required scope items are **APPROVED**, state transitions to **`W3 SCOPE APPROVED — EXECUTION NOT AUTHORIZED`**.  
5. **Separately** authorize W3 implementation (Cursor task branch, scope reference, commit/merge/sync gates).

**Do not start W3 moves until steps 2–5 are complete.**

---

**Report path:** `V:\DevelopmentRepo\AOVIAS\AOVIAS_PHASE1IL_W3_APPROVAL_RECORD.md`  
**Git:** Untracked on Main; no index changes performed in Phase 1I-L.

---

## 10. Phase 1I-N — Record human decisions (approval-record update only)

**Phase:** 1I-N  
**Date:** 2026-10-25  
**Baseline HEAD:** `b9e2de634b86d04f54b3a1d97ad6bfc44fa646f1`  
**Authorization scope:** Update this approval record only — **not** W3 implementation.

**Reconciliation source:** Phase 1I-M human decision reconciliation (user message 2026-10-25; no separate `AOVIAS_PHASE1IM_*.md` file on disk). Checklist subjects match `AOVIAS_PHASE1IK_W3_SCOPE_APPROVAL_PACKAGE.md` §8 (#1–17 = K-1–K-17).

### 10.1 Baseline re-verification (Phase 1I-N)

| Worktree | Branch | HEAD | Tracked changes (`-uno`) |
|----------|--------|------|---------------------------|
| Main | `main` | `b9e2de634b86d04f54b3a1d97ad6bfc44fa646f1` | **None** |
| Cursor | `agent/cursor/active` | `b9e2de634b86d04f54b3a1d97ad6bfc44fa646f1` | **None** |
| Claude | `agent/claude/active` | `b9e2de634b86d04f54b3a1d97ad6bfc44fa646f1` | **None** |

**Related reports present:** `AOVIAS_PHASE1IK_W3_SCOPE_APPROVAL_PACKAGE.md`, this file (1I-L body preserved above). **W3 implementation:** not performed in Phase 1I-N.

### 10.2 Recorded human decisions

**Approval evidence:** Explicit user decisions submitted for Phase 1I-M reconciliation and authorized for recording in Phase 1I-N (same session, 2026-10-25).

| ID | Checklist subject (1I-K §8) | Recorded decision | Evidence |
|----|----------------------------|-------------------|----------|
| **K-1** | M-S-01 MOVE Platform slnx → `AOVIAS.Studio/` | **APPROVED** | User: APPROVE K-1–K-7 |
| **K-2** | M-S-02 MOVE PublishingEngine slnx | **APPROVED** | Same |
| **K-3** | M-S-03–06 MOVE CSM cluster (incl. orphan Tests) | **APPROVED** | Same |
| **K-4** | M-S-07–09 MOVE Configuration, Deployment.Contracts, CommandCenter | **APPROVED** | Same |
| **K-5** | M-S-10 MOVE Runtime.Agent (+ O-4 Contracts in-tree) | **APPROVED** | Same |
| **K-6** | M-S-11–14 MOVE Publishing, Bootstrap, Release, Orchestration | **APPROVED** | Same |
| **K-7** | M-S-15 MOVE DesignSystem | **APPROVED** | Same |
| **K-8** | M-S-16 MOVE DeploymentValidation to `AOVIAS.Studio/…` (O-3) | **APPROVED** | User: APPROVE K-8 |
| **K-9** | M-S-17 Nested slnx co-move with parent folders | **APPROVED** | User: APPROVE K-9–K-15 |
| **K-10** | O-3 Dockerfile + stager + dockerCopyRoots — Studio DRV path | **APPROVED** | Same |
| **K-11** | O-4 Contracts under Agent; CSM ProjectReference update only | **APPROVED** | Same |
| **K-12** | All 9 Studio `.slnx` path rewrites | **APPROVED** | Same |
| **K-13** | W2 publish tools `.csproj` → `AOVIAS.Studio/…` references | **APPROVED** | Same |
| **K-14** | `discovery.current` updated; Target non-production in code | **APPROVED** | Same |
| **K-15** | 11 solution orphans unchanged | **APPROVED** | Same |
| **K-16** | **Deferred:** `--discover`, preflight, Docker run, npm ReleaseValidation | **DEFERRED** | User: DEFER K-16 — **not** APPROVED |
| **K-17** | Authorize separate W3 implementation task | **DEFERRED** | User: DEFER K-17 — **W3 implementation not authorized** |

**Tally (Phase 1I-N):** **Approved 15** · **Rejected 0** · **Deferred 2** · **Undecided 0**

### 10.3 Scoped decision notes (binding for approved items only)

| Items | Recorded scope boundary |
|-------|-------------------------|
| **K-8 + K-10** | DeploymentValidation product tree moves to **`AOVIAS.Studio/Aovias.DeploymentValidation`**. Same approved scope includes Runtime Agent **Dockerfile**, **`RuntimeAgentDockerContextStager`**, and **`repository-layout.json` `runtimeAgentDockerCopyRoots`** (and related **discovery.current** fields per **K-14**) updated for Studio paths. |
| **K-11** | **`Aovias.Runtime.Contracts`** remains under **`Aovias.Runtime.Agent/src/…`**; co-move with Agent; update **`ComplianceServiceManager`** ProjectReference as needed — **no** promotion to `AOVIAS.Shared` (O-6 deferred). |
| **K-13** | Cross-wave **ProjectReference** updates in **`AOVIAS.Infrastructure/tools/PublishAllServices/`** and **`…/PublishRuntimeAgent/`** only — **does not** repeat or alter completed **W2 / O-5** relocations. |
| **K-14** | Updates to **`discovery.current`** (and studio **`paths.*.current`**) in **`repository-layout.json`** only. **Production Target profile must remain disabled**; no authorization to enable Target in production adapters. |

### 10.4 K-16 — deferred (exact meaning)

Checklist **K-16** (1I-K §8 item 16) is **acknowledgement** of the **operational validation boundary**: **`--discover`**, publishing **preflight**, **Docker run** (image build/run), and **npm ReleaseValidation** remain **outside** the authorized W3 migration implementation and scoped post-move validation unless separately approved.

The user decision **DEFERRED** on **K-16** means:

- The user **has not approved** recording agreement to that boundary (and **has not** approved expanding validation into those activities).
- **Do not** document or imply that the user **APPROVED K-16**.
- W3 scope checklist is **not fully closed** while **K-16** remains **DEFERRED**.

**Studio vs Infrastructure (O-3):** Resolved by **K-8 APPROVED** (Studio path). **K-16** does **not** reopen that alternate.

### 10.5 K-17 — deferred (implementation gate)

**K-17 DEFERRED** explicitly means:

- **W3 file moves, source edits, and implementation are not authorized.**
- **Commit**, **merge**, **push**, and **worktree synchronization** each require **separate** explicit human authorization (see §7.12); none are granted by K-1–K-15.

### 10.6 Final approval state (Phase 1I-N — authoritative)

**`W3 SCOPE PARTIALLY APPROVED — EXECUTION NOT AUTHORIZED`**

| Aspect | Status |
|--------|--------|
| **K-1 through K-15** | **APPROVED** for the defined W3 scope in `AOVIAS_PHASE1IK_W3_SCOPE_APPROVAL_PACKAGE.md` |
| **K-16** | **DEFERRED** — validation boundary not acknowledged; scope **not fully closed** |
| **K-17** | **DEFERRED** — W3 implementation **not authorized** |
| **Implementation** | **Unauthorized** (no moves, no manifest/project edits under this phase) |
| **Git integration gates** | **Unauthorized** unless explicitly named in a future instruction |

**Not claimed:** Complete checklist approval; **W3 SCOPE APPROVED — EXECUTION NOT AUTHORIZED** (requires K-16 resolved per human policy); **W3 IMPLEMENTATION AUTHORIZED**.

### 10.7 Required next human action

1. Optional: **APPROVE** or further clarify **K-16** if the operational validation boundary should be formally closed.  
2. When ready for Cursor work: **separate explicit authorization** for **K-17** / W3 implementation (task branch, scope reference, then commit/merge/sync gates).  
3. Do **not** begin W3 migration until **K-17** (or equivalent explicit implementation authorization) is **APPROVED**.

---

**Phase 1I-N update:** Approval record section appended; Phase 1I-L §1–§9 preserved.  
**Git (after 1I-N edit):** File remains **untracked** on Main unless human commits; **not staged** in Phase 1I-N.

---

## 11. Phase 1I-O — K-16 closure: validation boundary approval

**Phase:** 1I-O  
**Date:** 2026-10-25  
**Baseline HEAD:** `b9e2de634b86d04f54b3a1d97ad6bfc44fa646f1`  
**Authorization scope:** Approval-record update only — **not** W3 implementation.

### 11.1 Baseline re-verification (Phase 1I-O)

| Worktree | Branch | HEAD | Tracked changes (`-uno`) |
|----------|--------|------|---------------------------|
| Main | `main` | `b9e2de634b86d04f54b3a1d97ad6bfc44fa646f1` | **None** |
| Cursor | `agent/cursor/active` | `b9e2de634b86d04f54b3a1d97ad6bfc44fa646f1` | **None** |
| Claude | `agent/claude/active` | `b9e2de634b86d04f54b3a1d97ad6bfc44fa646f1` | **None** |

No unexpected tracked or staged changes. Untracked phase reports and operational artifacts were not modified.

### 11.2 K-16 — APPROVED (validation boundary)

| Field | Record |
|-------|--------|
| **Checklist ID** | **K-16** (1I-K §8 item 16) |
| **Recorded decision** | **APPROVED** |
| **Approval evidence** | User explicit instruction **“close K-16 first”** (Phase 1I-O, 2026-10-25), adopting the validation boundary stated in that instruction and in the immediately preceding Phase 1I-M / 1I-N reconciliation context. |
| **K-1 through K-15** | **Unchanged** — remain **APPROVED** as recorded in §10.2. |

**Approved K-16 boundary:** The following operational validation activities remain **outside** the authorized **W3 migration phase** and **must not** be executed as part of W3 **without separate authorization**:

1. **`--discover`**
2. **Full preflight**
3. **Docker execution/build validation**
4. **npm ReleaseValidation**

**Interpretation limits:**

- This decision **defines the W3 scope boundary** for operational validation — it is **not** permission to run any of the four excluded activities during W3.
- **Focused structural checks** and other validation **may** be performed **only when explicitly permitted** by a **separately approved W3 implementation plan** (e.g. scoped `dotnet build` / targeted tests listed in §7.7 of this record when that plan is authorized under **K-17** or equivalent).

### 11.3 K-17 — remains DEFERRED

| Field | Record |
|-------|--------|
| **Checklist ID** | **K-17** |
| **Recorded decision** | **DEFERRED** (unchanged from §10.2) |
| **Effect** | **W3 implementation is NOT AUTHORIZED** — no file moves, no manifest/project edits, no W3 migration work. |

**Separate authorization gates (unchanged):** **Commit**, **merge**, **push**, and **worktree synchronization** each require explicit human authorization (§7.12). None are granted by K-16 closure or K-1–K-16 scope approval.

### 11.4 Approval counts (Phase 1I-O)

| Approved | Rejected | Deferred | Undecided |
|----------|----------|----------|-----------|
| **16** (K-1 through K-16) | **0** | **1** (K-17) | **0** |

### 11.5 Final approval state (Phase 1I-O — authoritative)

**`W3 SCOPE APPROVED — EXECUTION NOT AUTHORIZED`**

| Aspect | Status |
|--------|--------|
| **Scope checklist (K-1–K-16)** | **Closed** — all scope and boundary items through **K-16** are **APPROVED** |
| **K-17** | **DEFERRED** — W3 implementation **not authorized** |
| **Excluded ops validation (K-16)** | **Acknowledged and bound** — four activities listed in §11.2 remain out of W3 unless separately authorized |
| **Implementation** | **Unauthorized** |

**Not claimed:** **`W3 IMPLEMENTATION AUTHORIZED`**.

### 11.6 Required next human action

1. **Separate explicit authorization** for **K-17** (or equivalent W3 implementation instruction) before any W3 migration in `V:\DevelopmentRepo\AOVIAS.Cursor`.  
2. Do **not** run **`--discover`**, **full preflight**, **Docker execution/build validation**, or **npm ReleaseValidation** as part of W3 unless a **new** human authorization explicitly expands scope.

---

**Phase 1I-O update:** §11 appended; §1–§10 preserved.  
**Git (after 1I-O edit):** `AOVIAS_PHASE1IL_W3_APPROVAL_RECORD.md` remains **untracked** on Main; **not staged** in Phase 1I-O.

---

## 12. Phase 1I-R — K-17 implementation authorization addendum (audit trail)

**Phase:** 1I-R  
**Date:** 2026-10-25  
**Purpose:** Record **subsequent** human authorization that **§11.3** (Phase 1I-O) did not include. **§11** text is preserved as the historical point-in-time record.

### 12.1 K-17 — implementation authorized (subsequent)

| Field | Record |
|-------|--------|
| **Checklist ID** | **K-17** |
| **Prior state (§11.3)** | **DEFERRED** — accurate **at Phase 1I-O close** |
| **New recorded decision** | **IMPLEMENTATION AUTHORIZED** (scope-limited; not commit/merge/sync) |
| **Approval evidence** | User explicit instruction: **“authorize K-17 — W3 implementation”** (Phase 1I-P prompt, conversation record **2026-10-25 ~11:45 UTC**) |
| **Authorized scope** | W3 implementation in **`V:\DevelopmentRepo\AOVIAS.Cursor`** on a **dedicated task branch**, limited to approved W3 scope (K-1–K-16, `AOVIAS_PHASE1IK_W3_SCOPE_APPROVAL_PACKAGE.md`, approval record §11) |
| **Implementation branch** | **`agent/cursor/phase1ip-w3-studio-migration`** |
| **Baseline** | `b9e2de634b86d04f54b3a1d97ad6bfc44fa646f1` |

### 12.2 Explicitly not authorized by K-17 / Phase 1I-P

- **Commit**, **merge**, **push**, **worktree synchronization**
- **K-16 excluded operations:** `--discover`, full preflight, Docker execution/build validation, npm ReleaseValidation (unless separately authorized later)

### 12.3 Approval state after this addendum (audit — not execution gate)

| Item | Status |
|------|--------|
| **K-1–K-16** | **APPROVED** (unchanged from §10–§11) |
| **K-17 implementation** | **Authorized** per §12.1 (Phase 1I-P) |
| **K-17 commit/merge/sync** | **Not authorized** unless separately instructed |
| **Operational validation (K-16)** | **Boundary remains** per §11.2 |

**Historical note:** Do not interpret §11.3 “DEFERRED” as contradicting §12 when read in timeline order — §12 documents a **later** authorization event.

---

**Phase 1I-R update:** §12 appended; §1–§11 preserved.  
**Git:** Untracked; **not staged** in Phase 1I-R.
