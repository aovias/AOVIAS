# AOVIAS Phase 1I-AP — W5 Website Migration Scope Approval Package

**Phase:** 1I-AP (scope package only — **no W5 implementation, no root-leftover cleanup, no Git changes**)  
**Date:** 2026-10-25  
**Prepared in:** `V:\DevelopmentRepo\AOVIAS`  
**Integration baseline:** `main` @ `2b93d2e4dd94d47af2e03045cc7200d0cd69fd4a` (post–W4 merge)

**Related:** `AOVIAS_PHASE1IAO_W5_READINESS_AND_ROOT_LEFTOVERS_REPORT.md` (untracked)

---

## Final status (latest)

**`W5 PARTIALLY PROPOSED — IMPLEMENTATION DEFERRED — AWAITING K-W5-5`**

Phase **1I-AP:** all keys **`UNDECIDED`**. Phase **1I-AQ** (below) records **proposed** human positions — **not** final approval until explicitly confirmed per key. **K-W5-5** remains **unauthorized**; no W5 `git mv` or structural edits.

---

## 1. Verified baseline

| Check | Expected | Actual | Result |
|-------|----------|--------|--------|
| Worktree | `V:\DevelopmentRepo\AOVIAS` | Same | **OK** |
| Branch | `main` | Same | **OK** |
| HEAD | `2b93d2e4dd94d47af2e03045cc7200d0cd69fd4a` | Same | **OK** |
| Tracked working tree | Clean | **0** tracked dirty | **OK** |
| W5 source monorepo | `AOVIAS Website/AOVIAS Web/` exists | **`package.json` present** | **OK** |
| Proposed target on disk | Not pre-created | **`AOVIAS.Website/` absent** | **OK** (expected pre-W5) |
| W4 Command layout | At target | `AOVIAS.Command/ComplaincePlatform/`, `AOVIAS.Command/WebApp/` tracked | **OK** |
| Untracked root leftovers | Documented 1I-AO | `?? ComplaincePlatform/`, `?? WebApp/` | **Noted — out of W5 scope** |

**Tracked file counts (Git index at HEAD):**

| Path prefix | Tracked files |
|-------------|---------------|
| `AOVIAS Website/AOVIAS Web/` (proposed **move source**) | **2274** |
| `AOVIAS Website/` **excluding** inner `AOVIAS Web/` | **67** (Guidelines, creative-direction assets, etc.) |
| **Total** under `AOVIAS Website/` | **2341** |

Planning uses **2274** as the primary W5 move batch unless a separate decision expands scope to the outer **67** files.

No stage, commit, merge, push, fetch, or worktree sync was performed in this phase.

---

## 2. Exact source-to-target mapping and flattening consequences

### 2.1 Approved mapping (proposed — pending K-W5-1 / K-W5-2)

| ID | Action | Source | Destination |
|----|--------|--------|-------------|
| **M-W-01** | `git mv` tree | `AOVIAS Website/AOVIAS Web/` | `AOVIAS.Website/` |

**Contract reference:** `repository-layout.json` → `products.website.paths.monorepoRoot` — **current** `AOVIAS Website/AOVIAS Web`, **target** `AOVIAS.Website` (O-7).

### 2.2 Flattening consequences (K-W5-2)

| Topic | Consequence |
|-------|-------------|
| **Path shape** | Removes nested folder with **space** (`AOVIAS Website`) from the **monorepo root** path; public npm/Next workspace root becomes `AOVIAS.Website/` at repository root. |
| **In-tree relative paths** | Files **inside** the moved tree (e.g. `apps/website/`, `packages/*`, internal compose `context: .`) largely **unchanged** relative to monorepo root **after** co-move. |
| **Repo-root references** | Any tooling, CI, or docs that cite `AOVIAS Website/AOVIAS Web` must switch to `AOVIAS.Website` (K-W5-3). |
| **Outer folder** | `AOVIAS Website/` may retain **67** tracked siblings **not** in M-W-01 unless separately decided — avoids accidental co-move of Guidelines/creative assets. |
| **Product naming** | `AOVIAS.Command/WebApp` (Command portal) and `AOVIAS.Website` (marketing/public site) remain **distinct** products — no path collision. |

### 2.3 Scale (planning)

- Rename-heavy commit pattern similar to W3/W4 (~**2274** paths for M-W-01).
- No .NET solution slnx move for website (npm workspace monorepo).

---

## 3. Minimal path-update inventory (K-W5-3 scope)

Only changes **required** for structural validity after M-W-01. Not a general remediation backlog.

### 3.1 `repository-layout.json`

| Field / section | Expected change | Why |
|-----------------|-----------------|-----|
| `products.website.paths.monorepoRoot.current` | → `AOVIAS.Website` | Reflect on-disk website root |
| `products.website.migrationStatus` | → website-at-target (or equivalent) | Contract status after W5 |
| `discovery.*` (if website markers exist or are added) | Website-relative paths only | Only if manifest already references website roots |
| `compatibilityConstraints` / prose | Document **Current** website root | If website cited in constraints |

**Do not** enable **Target** profile for production adapters unless already governed by Phase 1C rules (same as W3/W4).

### 3.2 Website CI and GitHub Actions

| Artifact | Expected change | Why |
|----------|-----------------|-----|
| `AOVIAS.Website/.github/workflows/ci.yml` (co-moved) | Confirm checkout assumes repo root; add `defaults.run.working-directory: AOVIAS.Website` **if** jobs assume monorepo cwd | GitHub Actions runs from repository root by default |
| Path literals in workflow steps | Replace `AOVIAS Website/AOVIAS Web` if any | Structural validity |

### 3.3 Compose and Docker (within moved tree)

| Artifact | Expected change | Why |
|----------|-----------------|-----|
| `deployment/compose/docker-compose.yml` | Review `context` / relative build paths | Phase 1B: paths relative to compose file location |
| `deployment/compose/docker-compose.prod.yml` | Same | Production compose |
| `docker/docker-compose.yml` | Same | Dev compose variant |
| `deployment/docker/Dockerfile`, `docker/Dockerfile` | COPY paths from monorepo root | Build context after move |

### 3.4 Repo-root and cross-product references

At HEAD, **no** matches in `docs/`, `AGENTS.md`, `ai-governance/`, or `scripts/` for `AOVIAS Website` (grep). Primary **tracked** cross-repo citation: **`repository-layout.json`**.

**Studio / Command / Infrastructure code:** **No** website path references required at HEAD (per 1I-AO). **Exception rule:** only edit those trees if a **direct** website path string is discovered during implementation — not anticipated in this package.

### 3.5 Documentation (structural only)

- Migration/architecture **tracked** docs that cite the old path (inventory in phase reports; mass untracked `AOVIAS_PHASE*.md` **excluded** from W5 commit unless separately authorized).
- Website **in-tree** README/docs co-move with tree; update only if they reference the old **repository-root** prefix.

---

## 4. W2 / W3 / W4 integration boundaries

| Wave | State at HEAD | W5 interaction |
|------|---------------|----------------|
| **W2** | `AOVIAS.Infrastructure/aovias-config`, ReleaseValidation, tools | **No move** — verify no accidental edits |
| **W3** | `AOVIAS.Studio/*` at target | **No move** — layout tests unchanged except any future website marker |
| **W4** | `AOVIAS.Command/ComplaincePlatform`, `AOVIAS.Command/WebApp` | **No move** — distinct from public website |
| **W5** | This package | **M-W-01 only** (inner monorepo) |

Merge history expected on implementation branch: W4 merge `2b93d2e` → W5 task commit(s) on Cursor worktree per `AGENTS.md` (implementation not authorized here).

---

## 5. Explicit exclusions

| Exclusion | Rationale |
|-----------|-----------|
| **Root untracked `ComplaincePlatform/` and `WebApp/`** | Stale integration-worktree residue (1I-AO) — **no** delete/move in W5 |
| **Outer `AOVIAS Website/` siblings (67 files)** | Not part of M-W-01 unless separate decision |
| Untracked ZIPs, `.lighthouseci-*`, local `node_modules` under website tree | Artifacts — do not stage in W5 commit |
| Untracked phase reports, probe script | Preserve; exclude from W5 commit |
| `AOVIAS.Shared`, AccredaOne scaffold expansion, Command/Studio/Infrastructure product moves | Out of W5 |
| Application behavior, feature work, dependency upgrades | Out of W5 |
| Full builds, broad npm test matrices, Docker runs, discover/preflight, ReleaseValidation, publish/deploy | Out of implementation scope unless later authorized |
| Root-leftover **cleanup authorization** | Separate phase (1I-AO disposition) |

---

## 6. Decision record

### 6.1 Initial (Phase 1I-AP)

All keys **`UNDECIDED`**.

### 6.2 Proposed positions (Phase 1I-AQ — not final approval)

Recorded from human Phase **1I-AQ** prompt. **Proposed** does **not** authorize implementation. Confirm each key explicitly to move to **`APPROVED`**.

| Key | Proposed decision | Authorization status (1I-AQ) | Practical consequence if **APPROVED** |
|-----|-------------------|--------------------------------|--------------------------------------|
| **K-W5-1** | **APPROVE** — M-W-01 move `AOVIAS Website/AOVIAS Web/` → `AOVIAS.Website/` | **PROPOSED APPROVE** (await explicit confirm) | ~**2274** tracked paths relocate; rename-heavy commit on agent branch. |
| **K-W5-2** | **APPROVE** — flatten to root-level `AOVIAS.Website/` | **PROPOSED APPROVE** (await explicit confirm) | Closes O-7 flatten; outer `AOVIAS Website/` may retain **67** sibling files. |
| **K-W5-3** | **APPROVE** — structural updates only (layout, website CI, compose/Docker, confirmed cross-repo refs) | **PROPOSED APPROVE** (await explicit confirm) | Path coherence; no Command/Studio/Infrastructure scope expansion. |
| **K-W5-4** | **APPROVE** — `git diff --check` + focused path/CI/compose checks; no broad builds/ReleaseValidation | **PROPOSED APPROVE** (await explicit confirm) | Narrow verification gate after implementation. |
| **K-W5-5** | **AWAIT HUMAN APPROVAL** — implementation on agent worktree | **NOT AUTHORIZED** | **No** W5 moves/edits until explicit K-W5-5 approval (mirror K-W4-4). |

**Implementation gate:** Requires **`APPROVED`** on **K-W5-1 through K-W5-5** (including explicit **K-W5-5**). Current state: **blocked on K-W5-5**.

**Decision report:** `AOVIAS_PHASE1IAQ_W5_DECISION_AND_IMPLEMENTATION_REPORT.md` (untracked).

---

## 7. Suggested implementation sequence (informational — not authorized)

1. Human decisions on K-W5-1 … K-W5-5.  
2. Cursor worktree sync to validated `main` (when separately authorized).  
3. Task branch `agent/cursor/phase1ig-w5-website-migration` (or assigned slug).  
4. M-W-01 + K-W5-3 updates + K-W5-4 checks.  
5. Staging/commit/merge gates (mirror W4 1I-AJ … 1I-AN).

---

## 8. Artifact

- **Created (untracked):** `AOVIAS_PHASE1IAP_W5_SCOPE_APPROVAL_PACKAGE.md`
- **No** source or Git history changes in Phase 1I-AP.
