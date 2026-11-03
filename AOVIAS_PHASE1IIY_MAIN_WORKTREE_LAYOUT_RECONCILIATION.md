# AOVIAS Phase 1I-IY — Main Worktree Physical Layout Reconciliation

**Phase:** 1I-IY (read-only inventory — **no** moves, copies, deletes, staging, commits, sync, or cleanup)  
**Date:** 2026-10-25  
**Compared worktrees:**  
- **Canonical main:** `V:\DevelopmentRepo\AOVIAS`  
- **Cursor agent:** `V:\DevelopmentRepo\AOVIAS.Cursor`

**Context:** Git migrations W1–W5 are complete on `main`. This report explains **physical** differences between worktrees and classifies **main-only** top-level entries. It does not assume identical untracked or generated content between worktrees.

---

## Executive summary

| Dimension | Finding |
|-----------|---------|
| **Tracked layout (Git index)** | **Identical** between the two worktrees at their respective HEADs (**8228** paths; `Compare-Object` on `git ls-files` → **0** differences). |
| **Commit pointer** | **Different:** `main` is at W5 **merge** `bc1365d`; Cursor is checked out on **`agent/cursor/phase1ir-w5-website-migration`** at W5 **feat** `f2dcfcc`. Merge commit `bc1365d` is a **same-tree** merge (`f2dcfcc^{tree}` = `bc1365d^{tree}` = `078a1ccf…`). |
| **Physical top level** | **Main** has **19** extra top-level **directories** not present on Cursor — almost entirely **untracked** pre-migration shells, **build/cache residue**, and **empty/artifact-only** folders. **Cursor** is closer to “index-only” product layout. |
| **Canonical W1–W5 on disk (tracked)** | **Present on both** under `AOVIAS.Infrastructure/`, `AOVIAS.Studio/`, `AOVIAS.Command/`, `AOVIAS.Website/`, plus intentional `AOVIAS Website/` siblings. |

---

## 1. Both worktree baselines

### 1.1 Canonical main — `V:\DevelopmentRepo\AOVIAS`

| Item | Value |
|------|--------|
| Git root | `V:/DevelopmentRepo/AOVIAS` |
| Branch | `main` |
| HEAD | `bc1365dc9dc0149ef46f5b66a828501387eae9dc` |
| HEAD subject | `merge: integrate W5 website repository migration` |
| Tracked working tree | **Clean** (0 modified/staged tracked paths) |
| Untracked (`??`) porcelain lines | **67** (phase reports, root leftovers, nested `AOVIAS Website/AOVIAS Web/`, CSM `packages/` entries under status `-u`) |

**Top-level tracked name segments** (from `git ls-files`, first path component):  
`.gitattributes`, `.gitignore`, `ACCREDACONE_KNOWLEDGEBASE_READINESS_ASSESSMENT.md`, `AGENTS.md`, `ai-governance`, `AOVIAS Website`, `AOVIAS.AccredaOne`, `AOVIAS.Command`, `AOVIAS.Infrastructure`, `AOVIAS.Studio`, `AOVIAS.Website`, `AOVIAS_REPOSITORY_RESTRUCTURING_ASSESSMENT.md`, `docs`, `repository-layout.json`, `scripts`.

**Tracked path counts (W1–W5 and peers):**

| Prefix | Tracked files |
|--------|----------------|
| `AOVIAS.Infrastructure/` | 33 |
| `AOVIAS.Studio/` | 1191 |
| `AOVIAS.Command/` | 4525 |
| `AOVIAS.Website/` | 2274 |
| `AOVIAS Website/` (siblings, not inner web app) | 67 |
| `docs/` | 107 |
| `scripts/` | 22 |
| **Total** | **8228** |

### 1.2 Cursor worktree — `V:\DevelopmentRepo\AOVIAS.Cursor`

| Item | Value |
|------|--------|
| Git root | `V:/DevelopmentRepo/AOVIAS.Cursor` |
| Branch | `agent/cursor/phase1ir-w5-website-migration` |
| HEAD | `f2dcfccbb1eb7f7b05f3ca2859650dc04cc63a03` |
| HEAD subject | `feat(website): migrate website into AOVIAS.Website` |
| Tracked working tree | **Clean** |
| Untracked porcelain lines | **41** (Cursor-local W1–W5 phase reports; `scripts/dev/_phase3h_runtime_probe.py`) |

**Top-level tracked segments:** Same set as main (§1.1). **Same** W1–W5 counts (**8228** total).

**Branch ref note (read-only):** `agent/cursor/active` in this repo still points at **`b9e2de6`** (post-W2 `main`), not `bc1365d`. Cursor is on a **task branch** at the W5 feat commit; that is a **checkout/sync lag**, not a tracked layout divergence.

### 1.3 Same canonical W1–W5 layout?

**Yes, for Git-tracked content.** At Cursor HEAD (`f2dcfcc`) and at `main`’s merge parent content, the tree is the same (**8228** paths; tree OID `078a1ccf…`).  
**No, for physical disk:** Main retains many **untracked** directories that Cursor does not have (§2).

---

## 2. Classified inventory — main-worktree extras and notable top-level entries

Legend:

- **Git status:** Tracked / Untracked (`??`) / Not in index (physical only; may be ignored artifacts inside).
- **Role:** Canonical component / Legitimate sibling / Historical remnant / Unresolved / Mixed.
- **Overlap:** Whether the name mirrors a **tracked** W1–W5 path elsewhere.

### 2.1 Required entries (user list)

| Entry | Tracked at root? | Git / ignore | Content (summary) | Role | Overlap with canonical | Safe to remove? |
|-------|------------------|--------------|-------------------|------|------------------------|-----------------|
| **`ComplaincePlatform/`** | **No** (`0`); **`??`** | Untracked | **~15,344** files — slnx present but **not** authoritative; heavy `bin`/`obj`/`_perf_build_out`, IDE (`.cursor`, `.planning`); **missing** tracked project files vs Command tree (per 1I-AO) | **Historical remnant** — pre-W4 local shell | **Yes** — canonical **`AOVIAS.Command/ComplaincePlatform/`** (3157 tracked) | **Likely yes** after optional leftover-only path review (1I-AO: ~977 non-artifact leftover paths); **requires human authorization** |
| **`WebApp/`** | **No**; **`??`** | Untracked | **~27,794** files — **`node_modules`/`dist`/`.angular`**; no root `angular.json` / app source vs tracked tree | **Historical remnant** — frontend cache shell | **Yes** — **`AOVIAS.Command/WebApp/`** (1368 tracked) | **Likely yes** (1I-AO: ~124 leftover-only non-cache files); **requires authorization** |
| **`ComplianceServiceManager/`** | **No**; **`??`** (e.g. under `packages/`) | Untracked artifact tree | Top: `.agents`, **`bin`**, **`obj`**, **`packages`**, `tools`, temp file — **no** slnx at root; **~1987** files | **Historical remnant** — local build/NuGet cache | **Yes** — canonical **`AOVIAS.Studio/ComplianceServiceManager/`** (**318** tracked) | **Likely yes** (artifact/cache); confirm no unique source under `packages/` |
| **`ComplianceServiceManager.Operations/`** | **No** | Physical only; mostly **`bin`/`obj`** | Empty of tracked projects at root | **Historical remnant** | **Yes** — Operations projects live under **`AOVIAS.Studio/ComplianceServiceManager/`** | **Likely yes** if only build output |
| **`ComplianceServiceManager.Operations.Tests/`** | **No** | Physical only; **`bin`/`obj`** | Test build residue | **Historical remnant** | Same Studio CSM tree | **Likely yes** |
| **`ComplianceServiceManager.Tests/`** | **No** | Physical only; **`bin`/`obj`** | Test build residue | **Historical remnant** | Same Studio CSM tree | **Likely yes** |
| **`AOVIAS Website/`** | **Partial** — **67** tracked | Mixed | Tracked: Guidelines + creative-direction assets. **Untracked:** **`AOVIAS Website/AOVIAS Web/`** (post-W5 empty/nested shell — inner app moved to **`AOVIAS.Website/`**) | **Legitimate sibling** (outer brand folder) + **untracked nested leftover** | W5 canonical site = **`AOVIAS.Website/`**; outer folder **by design** | **Do not remove** tracked siblings without product decision; **nested `AOVIAS Web/`** cleanup = **separate authorization** |
| **`AOVIAS.AccredaOne/`** | **Yes** — **1** file (`README.md`) | Tracked | Scaffold readme only | **Legitimate additional product** (Phase 1D-A) | None — not W1–W5 relocation | **No** — tracked canonical scaffold |
| **`Aovias.Bootstrap/`** | **No** at root | Physical only | Small dir (**3** top children); no csproj at root in spot check | **Historical remnant** (pre-W3 root name) | **Yes** — **`AOVIAS.Studio/Aovias.Bootstrap/`** (**97** tracked) | **Likely yes** if only stale copy; verify before delete |
| **`Aovias.DesignSystem/`** | **No** at root | Physical only | **`bin`/`obj`** present | **Historical remnant** — build residue | **Yes** — **`AOVIAS.Studio/Aovias.DesignSystem/`** (**45** tracked) | **Likely yes** |

*User prompt listed `AOVIAS.Bootstrap` / `AOVIAS.DesignSystem`; on disk the names are **`Aovias.Bootstrap`** and **`Aovias.DesignSystem`** (Studio product casing).*

### 2.2 Other main-only top-level directories (not on Cursor)

| Entry | Tracked at root? | Content | Role | Overlap | Safe to remove? |
|-------|------------------|---------|------|---------|-----------------|
| **`Aovias.Configuration/`** | **No** | **~317** files, **~316** under `bin`/`obj`-like paths | **Historical remnant** — build output shell | **`AOVIAS.Studio/Aovias.Configuration/`** | **Likely yes** |
| **`Aovias.Deployment.Contracts/`** | **No** | Physical dir (pre-W3 root ghost) | **Historical remnant** | Studio path | **Likely yes** after spot-check |
| **`Aovias.DeploymentValidation/`** | **No** | Physical dir | **Historical remnant** | Studio path | **Likely yes** |
| **`Aovias.Orchestration/`** | **No** | Physical dir | **Historical remnant** | Studio path | **Likely yes** |
| **`Aovias.Publishing/`** | **No** | **~1056** files, all artifact-class in sample | **Historical remnant** | Studio path | **Likely yes** |
| **`Aovias.Release/`** | **No** | Physical dir | **Historical remnant** | Studio path | **Likely yes** |
| **`Aovias.ReleaseValidation/`** | **No** | Physical dir | **Historical remnant** | Note: W2 canonical is **`AOVIAS.Infrastructure/Aovias.ReleaseValidation/`** | **Likely yes** (do not confuse with Infrastructure) |
| **`Aovias.Runtime.Agent/`** | **No** | **~718** files, **~717** artifact-class | **Historical remnant** | **`AOVIAS.Studio/Aovias.Runtime.Agent/`** | **Likely yes** |
| **`Aovias.Runtime.Contracts/`** | **No** | Physical dir | **Historical remnant** | Studio path | **Likely yes** |
| **`Aovias.CommandCenter/`** | **No** | Physical dir (**1** top child) | **Historical remnant** / local experiment | May relate to Command product (not root-tracked) | **Human confirmation** |
| **`tools/`** (repo root) | **No** | **`PublishAllServices/bin/Debug/...`** DLLs — pre-W2 local tool build output | **Generated / historical** | Canonical tools = **`AOVIAS.Infrastructure/tools/`** (**4** tracked) | **Likely yes** (artifact tree) |
| **`preflight-docker-probe/`** | **No** | **Empty** (0 children) | **Unresolved** (manifest **O-8**) | Was pre-layout probe; not in index | **Low risk** empty dir; disposition = **contract decision** |

### 2.3 Main top-level entries aligned with Cursor (tracked canonical)

| Entry | Tracked | Notes |
|-------|---------|--------|
| `AOVIAS.Infrastructure/` | Yes (33) | W1 + W2 |
| `AOVIAS.Studio/` | Yes (1191) | W3 |
| `AOVIAS.Command/` | Yes (4525) | W4 |
| `AOVIAS.Website/` | Yes (2274) | W5 |
| `docs/`, `scripts/`, `ai-governance/` | Yes | Shared repo infrastructure |
| `repository-layout.json`, `AGENTS.md`, assessments | Yes | Contract / governance |

### 2.4 Untracked files at main root (not directories)

**~60+** `AOVIAS_PHASE*.md` / JSON / CSV audit artifacts — **untracked by design**, preserved for human review. Same class of reports on Cursor differs by **filename set** (agent-local W2–W5 implementation reports). **Not** a layout defect.

---

## 3. Tracked layout comparison (committed vs physical)

| Check | Main @ `bc1365d` | Cursor @ `f2dcfcc` | Result |
|-------|------------------|--------------------|--------|
| `git ls-files` path set | 8228 | 8228 | **Identical** |
| Tree OID | `078a1ccf…` | `078a1ccf…` (feat commit) | **Same tree** as W5 feat; merge commit adds no file changes |
| W1–W5 prefix counts | §1.1 | Same | **Match** |
| Physical-only dirs on main | §2.1–2.2 | **Absent** on Cursor | **Untracked/artifact only** — not a commit layout difference |

**Conclusion:** Any layout **mismatch** between worktrees is **physical and untracked**, not a difference in **committed** W1–W5 structure. Cursor at `f2dcfcc` already contains the same migrated tree as `main`; `main` adds only the merge commit metadata.

---

## 4. Proposed top-level physical layout for `V:\DevelopmentRepo\AOVIAS`

This is a **target organization model** for human decision-making — **not** an instruction to move files in this phase.

```text
V:\DevelopmentRepo\AOVIAS\
├── AGENTS.md, repository-layout.json, .gitattributes, .gitignore
├── ACCREDAONE_…md, AOVIAS_REPOSITORY_RESTRUCTURING_ASSESSMENT.md
│
├── [Canonical product roots — TRACKED, required]
│   ├── AOVIAS.Infrastructure/          ← W1 config + W2 ReleaseValidation/tools
│   ├── AOVIAS.Studio/                  ← W3 Studio + CSM + Aovias.*
│   ├── AOVIAS.Command/                 ← W4 ComplaincePlatform + WebApp
│   ├── AOVIAS.Website/                 ← W5 website monorepo app
│   └── AOVIAS.AccredaOne/              ← tracked scaffold (README)
│
├── [Legitimate sibling — TRACKED partial]
│   └── AOVIAS Website/                 ← Guidelines + creative assets (67 tracked)
│       └── AOVIAS Web/                 ← UNTRACKED leftover shell → disposition candidate
│
├── [Shared repo infrastructure — TRACKED]
│   ├── docs/
│   ├── scripts/
│   └── ai-governance/
│
├── [Audit / phase artifacts — UNTRACKED, optional archive]
│   └── AOVIAS_PHASE*.md, *.json, *.csv at repo root
│
└── [Disposition candidates — UNTRACKED / physical-only, NOT canonical]
    ├── ComplaincePlatform/             → maps to AOVIAS.Command/ComplaincePlatform/
    ├── WebApp/                         → maps to AOVIAS.Command/WebApp/
    ├── ComplianceServiceManager*/      → maps to AOVIAS.Studio/ComplianceServiceManager/
    ├── Aovias.* (root Studio ghosts)   → maps to AOVIAS.Studio/Aovias.*
    ├── tools/ (root)                   → maps to AOVIAS.Infrastructure/tools/
    └── preflight-docker-probe/         → O-8 contract item
```

**Necessary on disk for a clean canonical view:** Tracked directories in the second group only.  
**Candidates for later cleanup (main worktree):** Entire disposition group + untracked `AOVIAS Website/AOVIAS Web/` + root phase reports (relocate/archive policy optional).  
**Requires human confirmation before delete:** All cleanup candidates in §2.1 (especially 1I-AO leftover-only file lists for Command/WebApp).

---

## 5. Overlap and safety findings

| Risk | Assessment |
|------|------------|
| **Duplicate source of truth** | Root `ComplaincePlatform/` and `WebApp/` **do not** contain the tracked W4 file set; authoritative trees are under **`AOVIAS.Command/`** (1I-AO). |
| **Accidental `git add`** | Adding root leftovers would **reintroduce pre-W4 layout** — avoid. |
| **CSM root folders** | Dominated by **`bin`/`obj`/`packages`**; canonical CSM is **`AOVIAS.Studio/ComplianceServiceManager/`**. |
| **Studio root `Aovias.*` dirs** | Name collision with Studio paths; content largely **build artifacts**, not parallel tracked trees. |
| **Root `tools/`** | **PublishAllServices** build output; W2 canonical is under **Infrastructure**. |
| **Cursor vs main disk parity** | Cursor worktree demonstrates **expected post-migration physical shape**; main integration worktree accumulated **local execution residue** during merges and local builds. |

---

## 6. Actions requiring human approval (not performed in 1I-IY)

1. **Authorized deletion** of untracked root **`ComplaincePlatform/`** and **`WebApp/`** after optional export of leftover-only paths (1I-AO).
2. **Authorized deletion** of root **`ComplianceServiceManager/`** (+ Operations/Tests folders) and root **`Aovias.*`** ghost directories after confirming no non-artifact source.
3. **Authorized deletion** of root **`tools/`** artifact tree and empty **`preflight-docker-probe/`** (or repurpose per **O-8**).
4. **Disposition** of untracked **`AOVIAS Website/AOVIAS Web/`** (nested shell post-W5).
5. **Archive policy** for **`AOVIAS_PHASE*.md`** at repo root (keep untracked vs move to `docs/audits/` — product decision).
6. **Cursor worktree branch hygiene:** return to **`agent/cursor/active`** and fast-forward to **`main` @ `bc1365d`** when human assigns (per AGENTS.md) — **not** done in this phase.
7. **Outer `AOVIAS Website/`** long-term naming (O-7) — separate from leftover cleanup.

---

## 7. Checks run vs not run

| Check | Status |
|-------|--------|
| Branch, HEAD, tracked cleanliness (both worktrees) | **Run** |
| Top-level directory inventory (both worktrees) | **Run** |
| `git ls-files` counts and path-set diff | **Run** |
| Tree equality `f2dcfcc` vs `bc1365d` | **Run** (identical tree) |
| Targeted content sampling (artifacts, slnx, packages) | **Run** (limited depth) |
| Full recursive hash compare root vs canonical trees | **Not run** |
| Exhaustive leftover-only path export | **Not run** (see 1I-AO) |
| Cleanup, sync, merge, fetch, push | **Not run** (forbidden) |

---

## 8. Artifact

- **Created (untracked):** `AOVIAS_PHASE1IIY_MAIN_WORKTREE_LAYOUT_RECONCILIATION.md` — **not staged**

No files were moved, copied, deleted, or modified except this report.
