# AOVIAS Phase 1I-AJ — W4 Staging-Readiness Report

**Phase:** 1I-AJ (read-only Git and scope review — **no edit, stage, commit, merge, push, or sync**)  
**Date:** 2026-10-25  
**Worktree:** `V:\DevelopmentRepo\AOVIAS.Cursor`  
**References:** `AOVIAS_PHASE1IAH_W4_IMPLEMENTATION_REPORT.md`, `AOVIAS_PHASE1IAI_W4_FOCUSED_REVIEW_REPORT.md`

---

## Final status

**`W4 STAGING READY — AWAITING EXPLICIT STAGING AUTHORIZATION`**

The full change set vs `bcd63a6` is scope-consistent with approved W4 relocation. The index is **partially** staged (renames only); the authorized staging phase must include **all** tracked W4 paths before commit.

---

## 1. Branch, HEAD, and working-tree state

| Item | Expected | Actual | Result |
|------|----------|--------|--------|
| Worktree | `V:\DevelopmentRepo\AOVIAS.Cursor` | Same | **OK** |
| Branch | `agent/cursor/phase1ig-w4-command-migration` | Same | **OK** |
| HEAD | `bcd63a6ecef7d67090be2104b3fd662635c1fa60` | Same | **OK** |
| Commit on branch | None beyond baseline | None | **OK** |

### Index vs working tree (observed — not modified in this phase)

| Layer | Count / detail |
|-------|----------------|
| **`git diff bcd63a6` (full W4 change set)** | **4542** paths; **+125** / **−87** lines |
| **`git diff --cached bcd63a6`** | **4525** paths (rename-only entries; **0** line delta in cached stat) |
| **Unstaged content on top of index** | **19** paths with line edits (see §7 staging boundary) |
| **Untracked** | **34** entries (phase reports + `scripts/dev/_phase3h_runtime_probe.py`) |
| **Reports in index** | **None** (no `AOVIAS_PHASE*` or probe in `--cached`) |

This split index state is **operational**, not a scope defect: renames were indexed during implementation; structural edits remain unstaged. Explicit staging authorization should **complete** the index to match the full diff vs `bcd63a6` (see §7).

---

## 2. Changed-path counts and rename summary

| Category | Path count (approx.) | Notes |
|----------|----------------------|--------|
| Command tree renames | **~4523** | `ComplaincePlatform/` → `AOVIAS.Command/ComplaincePlatform/`; `WebApp/` → `AOVIAS.Command/WebApp/` (includes slnx, compose, scripts, binaries, and assets co-moved with trees) |
| Command co-move content edits | **2** | `platform-integrity.yml` (CI `working-directory`); `build-praman-aovpkg.ps1` (repo/platform root resolution) |
| Studio + layout consumers | **15** | Configuration layout, Publishing discovery/preflight, CSM resolvers, publishing/verification tests |
| Repo metadata / runbook | **2** | `repository-layout.json`, `docs/milestones/G1-EXECUTION-RUNBOOK.md` |
| **Total vs `bcd63a6`** | **4542** | Dominated by pure renames; **19** paths carry non-zero line deltas |

**Old root paths in diff:** **0** paths still named `ComplaincePlatform/...` or `WebApp/...` at repository root (mappings use `{ComplaincePlatform => AOVIAS.Command/ComplaincePlatform}` / `{WebApp => AOVIAS.Command/WebApp}`).

---

## 3. W4 scope and W5 exclusion

### 3.1 Confirmed in scope (complete diff vs `bcd63a6`)

- **M-C-01–03:** Command platform tree, WebApp tree, and `CompliancePlatform.slnx` under `AOVIAS.Command/ComplaincePlatform/`.
- **Structural follow-through:** `repository-layout.json` **current** profile, solution entry, knowledge base path, compatibility text; Studio scan roots, WebApp root, CSM `RootPathResolver`, `SourceDiscoveryBoundary`, publishing preflight walk; affected layout and publishing tests; G1 runbook examples; Command platform CI workflow paths.
- **CSM fixture correction:** `RepositoryLayoutCsmResolverContractTests` — agent marker **`AOVIAS.Studio/Aovias.Runtime.Agent`** and embedded manifest JSON aligned with **post-W3** `discovery.current.csmPlatformRoot` and **post-W4** Command paths. **Justified** as part of W4 handoff (tests must reflect production **Current** profile after relocation).

### 3.2 Confirmed out of scope / absent

| Check | Result |
|-------|--------|
| W5 website | **0** diff paths under `AOVIAS Website/` |
| `AOVIAS.Shared` | **Not** in diff |
| `AOVIAS.Infrastructure/` | **0** diff paths (W2 intact) |
| W3 Studio product moves | **No** new Studio tree moves; only **15** consumer/test files under `AOVIAS.Studio/` |
| Solution orphans added | **None** observed |
| Untracked phase reports / probe staged | **None** |

### 3.3 `repository-layout.json` and Target profile

- **`discovery.current`** and **`products.command`** use on-disk spelling **`AOVIAS.Command/ComplaincePlatform`** — **matches** directory layout.
- W4 diff **does not** modify the **`discovery.target`** block (Target remains non-production metadata).
- W4 **does** align **`products.command.paths.compliancePlatform.target`** and Command solution **target** path from planning typo `CompliancePlatform` → **`ComplaincePlatform`** — **not** an expansion of Target-profile production scope; **current**-profile and product-path correction only.

---

## 4. Untracked reports and operational artifacts

**34** untracked paths, including all `AOVIAS_PHASE*.md` reports (1I-AH, 1I-AI, W3 series, W2, etc.) and `scripts/dev/_phase3h_runtime_probe.py`.

- **Not** included in `git diff bcd63a6` or `--cached`.
- **Must remain excluded** from the W4 commit unless a separate authorization covers them.

*(This report, `AOVIAS_PHASE1IAJ_W4_STAGING_READINESS_REPORT.md`, is also untracked and excluded.)*

---

## 5. `git diff --check`

**Command:** `git diff --check bcd63a6ecef7d67090be2104b3fd662635c1fa60`

**Result:** **Pass** (no conflict markers or whitespace errors reported).

---

## 6. Defects, uncertainties, and validation gaps

### 6.1 Scope / Git defects blocking staging

**None identified.**

### 6.2 Operational notes (address at staging authorization)

| Item | Severity | Action at staging |
|------|----------|-------------------|
| **Partial index** (4525 renames staged, 19 paths unstaged) | Operational | Authorized staging must add remaining paths so **`git diff --cached bcd63a6`** equals full W4 diff (see §7) |
| Post-commit validation | Gap | Full slnx builds, broad tests, CI run, Docker, discover/preflight — **not** required for staging-readiness sign-off per phase rules |
| Untracked probe stale root path | Low | Out of commit scope; update separately if still used |

### 6.3 Co-moved tracked content (not new scope)

The Command move includes **pre-existing tracked** binaries and uploads under `ComplaincePlatform/` (e.g. `_perf_build_out/`, `App_Data/evidence_uploads/`, demo PDFs) — **renames only**, not new generated junk introduced by W4.

**`.env.example`** is co-moved template metadata; **not** a live `.env` secret file.

---

## 7. Proposed staging boundary (next phase)

When **explicit staging authorization** is granted, the commit should include **exactly**:

**All tracked changes in `git diff bcd63a6` (4542 paths, +125/−87 lines), comprising:**

1. Full **`AOVIAS.Command/`** tree from root relocations of **`ComplaincePlatform/`** and **`WebApp/`**.
2. **`repository-layout.json`**
3. **`docs/milestones/G1-EXECUTION-RUNBOOK.md`**
4. **Fifteen** files under **`AOVIAS.Studio/`** (list in §2 — layout, publishing, CSM, tests only).

**Explicitly exclude:**

- All **`??`** untracked files (phase reports, probe, any local zips/Lighthouse artifacts).
- **`AOVIAS Website/`**, **`AOVIAS.Infrastructure/`** (no changes), **`AOVIAS.Shared`**, and any path outside the diff vs `bcd63a6`.

**Recommended staging hygiene (human/agent at authorization time):**

- Verify **`git diff --cached bcd63a6 --stat`** matches **`git diff bcd63a6 --stat`** before commit.
- If completing partial index: stage the **19** unstaged paths (including **`RM`** content for `platform-integrity.yml` and `build-praman-aovpkg.ps1`) via scoped `git add` on those paths or a verified `git add -u` limited to the allowlist above — **without** `git add` on untracked reports.

**Do not** stage untracked `AOVIAS_PHASE*.md`, this 1I-AJ report, or `_phase3h_runtime_probe.py`.

---

## 8. Confirmation statement

- **W4 restructuring diff** is **scope-complete** and **W5-clean** for a single relocation commit.
- **Untracked** operational and report artifacts remain **outside** the proposed commit.
- **Staging, commit, merge, push, and worktree sync** were **not** performed in Phase 1I-AJ.

**Agent context:** Cursor worktree read-only review  
**Ready for:** Human **explicit staging authorization** (Phase subsequent to 1I-AJ)
