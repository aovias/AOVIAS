# AOVIAS Phase 0 — Migration Readiness and Baseline

**Date:** 2026-10-25  
**Repository:** `V:\DevelopmentRepo\AOVIAS`  
**Scope:** Readiness and baseline only — **no restructuring, no branch/worktree mutations**  
**Related:** `AOVIAS_REPOSITORY_RESTRUCTURING_ASSESSMENT.md`

---

## Executive recommendation

### `BLOCKED — HUMAN DECISION REQUIRED`

Phase 0 **did not move any directories**. The **intended migration baseline commit is identified** (`main` @ `5a1526d99a68241d40ccba759d8fc62586f4af86`), and **recent integration merges account for prior assessment-branch work**. However:

1. **`git status --short` was not executed** (terminal/sandbox unavailable). Per safety rule 10, repository cleanliness **cannot be certified** from this environment.
2. **Cursor and Claude worktrees remain on task branches** that are **behind** the current `main` tip and are **not** on `agent/*/active` baselines per `AGENTS.md`.
3. **No build/test commands were executed** here; baseline runtime verification is **unverified**.

**Unblock path:** Human runs the verification checklist in §8 on all three worktrees, normalizes agent worktrees to merged `main`, then re-issues Phase 1 with **`READY FOR PHASE 1`**.

---

## 1. Verified Git baseline

Evidence source: `.git/HEAD`, `.git/refs/heads/*`, `.git/logs/HEAD`, `.git/worktrees/*/HEAD`, `.git/worktrees/*/logs/HEAD`.  
**Not verified:** `git status`, `git diff`, `git log` (full), `git merge-base`, `git branch --contains`.

| Item | Verified value |
|------|----------------|
| **Repository root** | `V:\DevelopmentRepo\AOVIAS` |
| **Integration worktree path** | `V:\DevelopmentRepo\AOVIAS` |
| **Current branch (integration)** | `main` |
| **HEAD (integration)** | `5a1526d99a68241d40ccba759d8fc62586f4af86` |
| **`main` tip ref** | `5a1526d99a68241d40ccba759d8fc62586f4af86` (HEAD matches `main`) |

### 1.1 Integration branch history (verified from `.git/logs/HEAD`)

Recent integration activity on `main` (newest last):

| Step | Resulting commit | Action (from reflog) |
|------|------------------|----------------------|
| … | `a2cb3d5af8690fba9b75a7e580b81d3948f7cde0` | merge `agent/cursor/studio-v2.9-source-context` |
| … | `800b4a250485653a8c6f06041894cb6f00c4db9e` | merge `agent/claude/def-env-006-angular-lifecycle` |
| … | `800b4a250485653a8c6f06041894cb6f00c4db9e` | merge `agent/cursor/phase3g-runtime-verification` |
| … | `92577e28aea099f8228e689941f23b4d5c2783a2` | merge `agent/cursor/phase3h-propagation-contract-closure` |
| … | `7d86e40b1aee1120a24aba05e97fcb8da11d07bb` | merge `codex/accredaone-kb-assessment` |
| … | `4730bf32672c2507558a2c4dabf9a71a37bece73` | merge `agent/cursor/studio-local-dev-orchestration-assessment` |
| **Current `main`** | `5a1526d99a68241d40ccba759d8fc62586f4af86` | merge `docs/website-and-restructuring-assessment` |

**Inference (not git-log verified):** Integration worktree is **not ahead/behind `main`** at ref level — it **is** `main` at `5a1526d`.

### 1.2 Registered worktrees

| Worktree path | Branch (HEAD ref) | HEAD commit | Verified via |
|---------------|-------------------|-------------|--------------|
| `V:\DevelopmentRepo\AOVIAS` | `main` | `5a1526d99a68241d40ccba759d8fc62586f4af86` | `.git/HEAD`, `refs/heads/main` |
| `V:\DevelopmentRepo\AOVIAS.Cursor` | `agent/cursor/phase3h-propagation-contract-closure` | `44e83120a74c2de8736206dcec789f930003740a` | worktree `HEAD`, branch ref, Cursor `logs/HEAD` |
| `V:\DevelopmentRepo\AOVIAS.Claude` | `agent/claude/def-env-006-angular-lifecycle` | `ddcb59ee537748649ff079749fda0163feec9f15` | worktree `HEAD`, branch ref, Claude `logs/HEAD` |

Common Git dir: `V:\DevelopmentRepo\AOVIAS\.git` (worktrees use `gitdir:` link — verified `AOVIAS.Cursor/.git`).

### 1.3 Branch tips relevant to preservation

| Branch | Tip commit | Notes |
|--------|------------|--------|
| `main` | `5a1526d99a68241d40ccba759d8fc62586f4af86` | Integration baseline |
| `codex/accredaone-kb-assessment` | `aa87317e3ace26145770aed741d3d2dddd07790d` | **Merged into `main`** (reflog) |
| `docs/website-and-restructuring-assessment` | `557d0df9d370283fe27d149d70425cbd1fc9a32f` | **Merged into `main`** (reflog) |
| `agent/cursor/phase3h-propagation-contract-closure` | `44e83120a74c2de8736206dcec789f930003740a` | **Merged into `main`** (reflog); Cursor still checked out here |
| `agent/claude/def-env-006-angular-lifecycle` | `ddcb59ee537748649ff079749fda0163feec9f15` | **Merged into `main`** (reflog); Claude still checked out here |
| `agent/cursor/active` | `f5a550d9784e274acd86e35313c4fa079623f0d0` | **Stale** vs `main` |
| `agent/claude/active` | `0c7ebacebc082509ce98296807926471135b52f1` | **Stale** vs `main` |

**Unverified:** Whether `44e83120` is an ancestor of `5a1526d` (expected yes given merge timing in reflog). Run:  
`git merge-base --is-ancestor 44e83120a74c2de8736206dcec789f930003740a main`

### 1.4 Working tree status

| Worktree | `git status --short` |
|----------|----------------------|
| `AOVIAS` | **NOT VERIFIED** |
| `AOVIAS.Cursor` | **NOT VERIFIED** |
| `AOVIAS.Claude` | **NOT VERIFIED** |

**Known filesystem fact:** This Phase 0 report file is being **added** at `AOVIAS_PHASE0_MIGRATION_READINESS.md` — after write, integration worktree will show at least one **untracked or new** path until committed.

---

## 2. Worktree and branch comparison

```text
                    main @ 5a1526d  (integration — canonical baseline)
                           ^
           merges (reflog) |
                           |
     +---------------------+---------------------+
     |                     |                     |
  phase3h @ 44e83120   def-env-006 @ ddcb59e   docs @ 557d0df
  (Cursor CHECKOUT)    (Claude CHECKOUT)      (merged)
     |                     |
     +-- likely contained in main; worktrees NOT updated to main
```

| Comparison | Assessment |
|------------|------------|
| Integration vs `main` | **Aligned** at same commit ref |
| Cursor vs `main` | **Checked out on merged task branch**; almost certainly **behind** `main` (missing merges after phase3h) |
| Claude vs `main` | **Checked out on merged task branch**; **behind** `main` (missing post-merge commits) |
| `agent/*/active` vs `main` | **Stale** — not suitable as agent baseline without `merge --ff-only main` |

**AGENTS.md alignment:** Integration on `main` matches contract (`AGENTS.md` L28, L234). Agent worktrees **do not** match post-task expectation to return to `agent/<agent>/active` updated from `main` (`AGENTS.md` L106–109, L251–254).

---

## 3. Preservation requirements

### 3.1 Work preserved in Git (merged — do not overwrite)

| Artifact / work | Branch / commit | Status |
|-----------------|-----------------|--------|
| `AOVIAS_REPOSITORY_RESTRUCTURING_ASSESSMENT.md` | `docs/website-and-restructuring-assessment` → `557d0df` → merged to `main` | On `main` |
| `ACCREDAONE_KNOWLEDGEBASE_READINESS_ASSESSMENT.md` | `codex/accredaone-kb-assessment` → merged to `main` | On `main` |
| `docs/Architecture/studio-orchestration-s0/AOVIAS-STUDIO-LOCAL-DEV-ORCHESTRATION-ASSESSMENT.md` | `agent/cursor/studio-local-dev-orchestration-assessment` → merged | On `main` |
| Phase 3 Command fixes/docs (phase3g, phase3h, etc.) | Agent branches → merged per reflog | On `main` |
| Claude env fixes (DEF-ENV-006, etc.) | Merged per reflog | On `main` |

### 3.2 Changes that may exist only in agent worktrees (unverified)

Without `git status` / `git diff` on Cursor and Claude paths, **local uncommitted changes cannot be ruled out**. Treat any dirty state in agent worktrees as **preservation-critical** until inspected.

**Do not:** `reset --hard`, `clean -fd`, or restructure while agent worktrees are uninspected (`AGENTS.md` L152–165).

### 3.3 Files that must not be overwritten during baseline normalization

- Any **uncommitted** edits in `AOVIAS.Cursor` or `AOVIAS.Claude`
- Operator data outside repo: `%LocalAppData%\AoviasStudio\development-workspaces.json` (paths may still reference old layout — update only after Phase 1 layout contract)

### 3.4 Human approval decisions before Phase 1

| Decision | Why |
|----------|-----|
| Confirm integration `git status` clean (except approved Phase 0 commit) | Safety rule 10 |
| Normalize Cursor/Claude: inspect dirty state → commit or stash on task branch → checkout `agent/*/active` → `merge --ff-only main` | `AGENTS.md`, avoid losing agent WIP |
| Confirm no other human/agent migration in progress | Parallel `git mv` risk |
| Approve Phase 1 starting commit **`5a1526d`** (or newer `main` after Phase 0 doc commit) | Baseline tag |

---

## 4. Changes since `AOVIAS_REPOSITORY_RESTRUCTURING_ASSESSMENT.md`

The restructuring assessment **baseline section is outdated** (it recorded integration on `codex/accredaone-kb-assessment` @ `aa87317` and `main` @ `a2cb3d5`). **Current verified state supersedes §2 of that document.**

| Topic | Prior assessment | Current Phase 0 |
|-------|------------------|-----------------|
| Integration branch | `codex/accredaone-kb-assessment` | **`main`** |
| Integration HEAD | `aa87317` | **`5a1526d`** |
| `main` tip | `a2cb3d5` | **`5a1526d`** |
| Restructuring report on `main` | Planned / uncommitted | **Merged** via `docs/website-and-restructuring-assessment` |
| AccredaOne KB report | Feature branch | **Merged** via `codex/accredaone-kb-assessment` |
| Product folder boundaries | None | **Still none** — no `AOVIAS.Command` etc. on disk |
| `RootPathResolver` | `ComplaincePlatform`, `WebApp`, `aovias-config` markers | **Unchanged** (verified file read) |
| `SourceDiscoveryBoundary` | Default roots `ComplaincePlatform`, `WebApp` | **Unchanged** |
| `PlatformRootResolver` | Same markers | **Unchanged** |
| `RepositoryLayout.json` / layout manifest | Recommended Phase 1 | **Not present** (grep) |
| Runtime Agent `Dockerfile` COPY layout | Top-level folder names | **Not re-verified line-by-line**; no path refactor observed |

**Conclusion:** Technical findings in the restructuring assessment (path sensitivity, ownership, phases) **remain applicable**. Git/integration findings **must use this Phase 0 document**.

---

## 5. Migration readiness checklist (static verification)

| Check | Result |
|-------|--------|
| `Aovias.Platform.slnx` at repo root | Present |
| `Aovias.PublishingEngine.slnx` at repo root | Present |
| `ComplianceServiceManager/ComplianceServiceManager.slnx` | Present |
| `ComplaincePlatform/CompliancePlatform.slnx` | Present |
| Path resolvers hard-code pre-migration folders | **Yes** — still blocking for physical moves |
| CI: Command workflow under `ComplaincePlatform/.github/` | Present (per prior assessment) |
| `scripts/verify-build.sh` → `Aovias.Platform.slnx` | Present |
| Worktree scripts default `V:\DevelopmentRepo\AOVIAS*` | Present (per prior assessment) |

---

## 6. Baseline build / test commands (required, not executed here)

Run from **`V:\DevelopmentRepo\AOVIAS`** on **clean `main`** after human status check. Record outputs in a follow-up commit or Phase 1 gate log.

### 6.1 .NET — Studio (cross-platform)

```powershell
dotnet --version
dotnet restore Aovias.Platform.slnx --locked-mode
dotnet build Aovias.Platform.slnx -c Release --no-restore
dotnet test Aovias.Platform.slnx -c Release --no-build
```

Or: `bash scripts/verify-build.sh Release` and `bash scripts/verify-tests.sh Release` (Linux/macOS or Git Bash).

### 6.2 .NET — Studio (Windows / WPF)

```powershell
dotnet build ComplianceServiceManager/ComplianceServiceManager.slnx -c Release
dotnet test ComplianceServiceManager.Tests/ComplianceServiceManager.Tests.csproj -c Release --no-build
```

Requires Windows + .NET Desktop workload.

### 6.3 .NET — Publishing focus

```powershell
dotnet build Aovias.PublishingEngine.slnx -c Release
dotnet test Aovias.PublishingEngine.slnx -c Release --no-build
```

### 6.4 .NET — Command (PRAMAN)

```powershell
cd ComplaincePlatform
dotnet restore CompliancePlatform.slnx
dotnet build CompliancePlatform.slnx -c Release --no-restore
dotnet test tests/Aovias.Platform.IntegrationTests/Aovias.Platform.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~QueryIsolationGovernanceTests|FullyQualifiedName~ExecutionContextTests"
```

### 6.5 Angular — Command WebApp

```powershell
cd WebApp
npm ci
npm run build
npm test
```

### 6.6 Corporate website

```powershell
cd "AOVIAS Website/AOVIAS Web"
npm ci
npm run ci:local
```

(Or individual: `npm run build`, `npm run lint`, `npm run typecheck` per `package.json`.)

### 6.7 Runtime Agent Docker image

```powershell
cd V:\DevelopmentRepo\AOVIAS
docker build -f Aovias.Runtime.Agent/Dockerfile -t aovias-runtime-agent:phase0-baseline .
```

Build context **must** remain repository root until Dockerfile COPY paths are updated in Phase 1+.

### 6.8 Execution status (this assessment)

| Command group | Executed? | Result |
|---------------|-----------|--------|
| All above | **No** | Terminal unavailable in assessment environment |
| Static file/path review | **Yes** | Entry points exist; resolvers unchanged |

---

## 7. Blocking decisions (carried forward)

From restructuring assessment — **still open**:

1. **`Aovias.CommandCenter` ownership** (Studio fleet API vs PRAMAN “Deficiency Command Center” UX naming).
2. **Which contract assemblies live in `AOVIAS.Shared`** vs remain under Studio trees.
3. **`ComplaincePlatform` rename** during migration vs retain typo for compatibility.
4. **Command CI location** after move (nested vs root workflows).
5. **AccredaOne vs KnowledgeBase** boundary (folder scaffold vs service extraction).

**New Phase 0 blocker:**

6. **Certify clean working trees** on all three paths before any Phase 1 file moves.

---

## 8. Recommended migration baseline and branch strategy

| Element | Recommendation |
|---------|----------------|
| **Baseline commit** | `5a1526d99a68241d40ccba759d8fc62586f4af86` (current `main`), or immediately after human commits this Phase 0 report |
| **Integration branch** | `main` only for migration merges |
| **Phase 1 branch** | `agent/human/repo-restructure-phase1` or human-integrator branch — **not** agent cursor/claude task branches |
| **Agent worktrees** | Before Phase 1: inspect → commit WIP → `checkout agent/cursor/active` / `agent/claude/active` → `merge --ff-only main` |
| **Migration freeze** | No concurrent agent product tasks touching path-sensitive files (`RootPathResolver`, publishing discovery, Docker, slnx, CI) |
| **First Phase 1 scope** | Layout manifest + resolver updates **without** mass `git mv` (per restructuring assessment §14 Phase 1) |

---

## 9. Human verification checklist (unblocks Phase 1)

Run locally and attach results to the next governance commit:

```powershell
git -C "V:\DevelopmentRepo\AOVIAS" status --short --branch
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" status --short --branch
git -C "V:\DevelopmentRepo\AOVIAS.Claude" status --short --branch
git -C "V:\DevelopmentRepo\AOVIAS" worktree list
git -C "V:\DevelopmentRepo\AOVIAS" log --oneline -5 main
git -C "V:\DevelopmentRepo\AOVIAS" branch --contains 44e83120a74c2de8736206dcec789f930003740a
```

Then run §6 build/test matrix on integration worktree.

If all statuses are clean (or only approved docs commits pending) and builds pass → declare **`READY FOR PHASE 1`**.

---

## 10. Go / no-go

| Criterion | Status |
|-----------|--------|
| Intended baseline identified | **Yes** — `main` @ `5a1526d` |
| Important merged work accounted for | **Yes** — via reflog |
| Path/resolver assessment still valid | **Yes** |
| `git status` verified clean | **No** — **BLOCKING** |
| Agent worktrees normalized | **No** — **BLOCKING** |
| Build/test baseline executed | **No** — **BLOCKING for execution**, not for planning Phase 1 design |

### Final status: **`BLOCKED — HUMAN DECISION REQUIRED`**

---

*Phase 0 complete. No directories moved. Only this report added.*

---

## 11. Follow-up execution log (2026-10-25 — agent session)

### 11.1 Git CLI availability

**Result:** `git` and `dotnet` **could not be executed** from the Cursor agent environment (including after `move_agent_to_root` to `V:\DevelopmentRepo\AOVIAS`). Every `Shell` invocation failed with:

```text
Terminal unavailable: this machine cannot enforce the 'workspace_readwrite' sandbox policy ...
```

**Implication:** Steps 1–3 in the human request (verify status, normalize worktrees, run baseline builds) **must be run in local PowerShell** on the integration machine. The agent **did not** commit the Phase 0 report, **did not** fast-forward agent worktrees, and **did not** run builds.

### 11.2 Filesystem cross-check (verified without git)

| Observation | Integration (`AOVIAS`) | Cursor | Claude |
|-------------|------------------------|--------|--------|
| `AOVIAS_PHASE0_MIGRATION_READINESS.md` | **Present** (this file) | Absent | Absent |
| `AOVIAS_REPOSITORY_RESTRUCTURING_ASSESSMENT.md` | Present | **Absent** | **Absent** |
| Checked-out branch (from `.git` HEAD) | `main` @ `5a1526d` | `phase3h` @ `44e83120` | `def-env-006` @ `ddcb59ee` |

**Interpretation:** Agent worktrees are **behind `main`** at the filesystem level (expected after integration merges). This is **not proof** of clean `git status` on those trees — only that they have not been updated to include post-merge commits.

### 11.3 Authoritative verification (run locally — paste output into §11.4)

```powershell
# --- A. Status (required before any checkout/merge) ---
git -C "V:\DevelopmentRepo\AOVIAS" status --short --branch
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" status --short --branch
git -C "V:\DevelopmentRepo\AOVIAS.Claude" status --short --branch

# --- B. Unique commits vs main (each worktree) ---
git -C "V:\DevelopmentRepo\AOVIAS" log --oneline main..HEAD
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" log --oneline main..HEAD
git -C "V:\DevelopmentRepo\AOVIAS.Claude" log --oneline main..HEAD

# --- C. Commits on main not in worktree (how far behind) ---
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" log --oneline HEAD..main
git -C "V:\DevelopmentRepo\AOVIAS.Claude" log --oneline HEAD..main

# --- D. Uncommitted diff summary ---
git -C "V:\DevelopmentRepo\AOVIAS" diff --stat
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" diff --stat
git -C "V:\DevelopmentRepo\AOVIAS.Claude" diff --stat
git -C "V:\DevelopmentRepo\AOVIAS" diff --cached --stat
```

**Safe interpretation rules:**

- Proceed to normalization only if **`main..HEAD` is empty** on an agent worktree **and** `status`/`diff` show **no uncommitted** work (or WIP is committed/stashed on the correct task branch first).
- If **`main..HEAD` is non-empty**, that worktree has commits **not on `main`** — integrate via human merge to `main` before migration; do not discard.

### 11.4 Recorded command output (fill in after local run)

| Command block | Result |
|---------------|--------|
| §11.3 A — status | *(pending local run)* |
| §11.3 B — `main..HEAD` | *(pending local run)* |
| §11.3 C — `HEAD..main` | *(pending local run)* |
| §11.3 D — diff stat | *(pending local run)* |

### 11.5 Safe worktree normalization (`AGENTS.md`)

**Only after §11.3 confirms no uncommitted loss risk.**

Preservation-first sequence (human or authorized integrator on each machine):

**Cursor worktree**

```powershell
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" status --short --branch
# If dirty: commit on current task branch OR stash with message; do NOT reset --hard

git -C "V:\DevelopmentRepo\AOVIAS.Cursor" log --oneline main..HEAD
# If non-empty: stop — integrate those commits to main first

git -C "V:\DevelopmentRepo\AOVIAS.Cursor" checkout agent/cursor/active
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" merge --ff-only main
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" status --short --branch
git -C "V:\DevelopmentRepo\AOVIAS.Cursor" rev-parse HEAD
# Expect HEAD = main tip (5a1526d or newer)
```

**Claude worktree**

```powershell
git -C "V:\DevelopmentRepo\AOVIAS.Claude" status --short --branch
git -C "V:\DevelopmentRepo\AOVIAS.Claude" log --oneline main..HEAD

git -C "V:\DevelopmentRepo\AOVIAS.Claude" checkout agent/claude/active
git -C "V:\DevelopmentRepo\AOVIAS.Claude" merge --ff-only main
git -C "V:\DevelopmentRepo\AOVIAS.Claude" status --short --branch
git -C "V:\DevelopmentRepo\AOVIAS.Claude" rev-parse HEAD
```

**Integration worktree**

```powershell
git -C "V:\DevelopmentRepo\AOVIAS" status --short --branch
# Expected: ?? AOVIAS_PHASE0_MIGRATION_READINESS.md until committed
# Commit Phase 0 report ONLY after status review:
# git -C "V:\DevelopmentRepo\AOVIAS" add AOVIAS_PHASE0_MIGRATION_READINESS.md
# git -C "V:\DevelopmentRepo\AOVIAS" commit -m "docs: phase 0 migration readiness baseline"
```

### 11.6 Baseline build/test matrix (run on integration @ main)

Run from `V:\DevelopmentRepo\AOVIAS` after Git normalization. **Record pass/fail; do not fix unrelated failures during baseline capture.**

| # | Command | Pass/Fail | Notes |
|---|---------|-----------|-------|
| 1 | `dotnet --version` | *(pending)* | |
| 2 | `dotnet build Aovias.Platform.slnx -c Release` | *(pending)* | |
| 3 | `dotnet test Aovias.Platform.slnx -c Release --no-build` | *(pending)* | |
| 4 | `dotnet build Aovias.PublishingEngine.slnx -c Release` | *(pending)* | |
| 5 | `dotnet build ComplianceServiceManager/ComplianceServiceManager.slnx -c Release` | *(pending)* | Windows + Desktop workload |
| 6 | `dotnet build ComplaincePlatform/CompliancePlatform.slnx -c Release` | *(pending)* | cwd or path as listed |
| 7 | `cd WebApp; npm ci; npm run build` | *(pending)* | |
| 8 | `cd "AOVIAS Website/AOVIAS Web"; npm ci; npm run ci:local` | *(pending)* | |
| 9 | `docker build -f Aovias.Runtime.Agent/Dockerfile -t aovias-runtime-agent:phase0 .` | *(pending)* | Optional if Docker available |

**Shell alternatives:** `bash scripts/verify-build.sh Release` and `bash scripts/verify-tests.sh Release` for rows 2–3.

### 11.7 Updated go/no-go after local completion

When §11.4 and §11.6 are filled with real output:

- **`READY FOR PHASE 1`** if: integration on `main`, clean or only approved doc commit, agent worktrees on `agent/*/active` at `main` tip, baseline builds/tests recorded (failures documented, not hidden).
- **`BLOCKED — HUMAN DECISION REQUIRED`** until then.
