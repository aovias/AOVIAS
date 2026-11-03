# AOVIAS Phase 1I-AO — W5 Readiness and Root-Leftovers Report

**Phase:** 1I-AO (read-only inspection — **no delete, move, stage, commit, merge, push, sync, or W5 implementation**)  
**Date:** 2026-10-25  
**Worktree:** `V:\DevelopmentRepo\AOVIAS`

---

## Final status

**`W5 READINESS PASS WITH LEFTOVER DISPOSITION REQUIRED`**

W5 planning inputs are sufficient for a scope-authorization package. Root-level untracked `ComplaincePlatform/` and `WebApp/` are **not** authoritative copies of tracked W4 content; disposition (likely cleanup) needs **separate authorization** after optional human review of leftover-only files.

---

## 1. Verified baseline

| Check | Expected | Actual | Result |
|-------|----------|--------|--------|
| Worktree | `V:\DevelopmentRepo\AOVIAS` | Same | **OK** |
| Branch | `main` | Same | **OK** |
| HEAD | `2b93d2e4dd94d47af2e03045cc7200d0cd69fd4a` | Same | **OK** |
| Tracked working tree | Clean | **0** tracked dirty entries | **OK** |
| W4 in history | `1f03ad4` ancestor of HEAD | `merge-base --is-ancestor` → **0** | **OK** |
| W3 merge in history | `bcd63a6` ancestor | **OK** | **OK** |
| Untracked root leftovers | Reported post–1I-AN | `?? ComplaincePlatform/`, `?? WebApp/` | **Confirmed** |

---

## 2. Root-level leftover inspection (read-only)

### 2.1 Git and metadata

| Directory | Tracked at root? | Nested `.git`? | Status |
|-----------|------------------|----------------|--------|
| `ComplaincePlatform/` | **No** (`??`) | **No** | Untracked on integration worktree |
| `WebApp/` | **No** (`??`) | **No** | Untracked on integration worktree |

**Authoritative tracked W4 locations:** `AOVIAS.Command/ComplaincePlatform/` (**3157** tracked files), `AOVIAS.Command/WebApp/` (**1368** tracked files).

### 2.2 Scale (recursive file counts)

| Location | Files (recursive) | Notes |
|----------|---------------------|--------|
| Untracked `ComplaincePlatform/` | **15,344** | Much larger than tracked Command tree |
| Tracked `AOVIAS.Command/ComplaincePlatform/` | **3,157** | Git source of truth |
| Untracked `WebApp/` | **27,794** | Dominated by `node_modules` / `dist` |
| Tracked `AOVIAS.Command/WebApp/` | **1,368** | Git source of truth |

### 2.3 Comparison vs tracked Command paths

**ComplaincePlatform**

- **All 3157 tracked files** under `AOVIAS.Command/ComplaincePlatform/` are **absent** at the corresponding relative paths under untracked root `ComplaincePlatform/` (e.g. root lacks `.dockerignore`, `Gateway/Gateway.csproj`; spot-check: root `Gateway/` has `bin/`, `obj/`, appsettings, `.csproj.user`, but **no** `Gateway.csproj`).
- **Conclusion:** Root folder is **not** a complete duplicate of the migrated tree; it does **not** supersede tracked content.
- **Leftover-only files** (excluding `bin/`, `obj/`, `_perf_build_out/`): **~977** paths — predominantly **IDE/agent tooling** (`.cursor/`, `.planning/`), `*.user`, launch settings, and **build outputs** under service folders — **not** in `git ls-files`.
- **Artifact-heavy:** ~**14,367** files under paths matching `bin`, `obj`, or `_perf_build_out`; ~**965** non-artifact extensions (`.cs`, `.csproj`, etc.) largely partial copies without matching tracked project files at root.

**WebApp**

- Tracked source markers **missing** at root leftover: e.g. **`angular.json` false**, **`src/app/app.component.ts` false** at root `WebApp/`.
- **~27,670** files under `node_modules`, `.angular`, or `dist`.
- **Leftover-only** (excluding `node_modules`, `dist`, `bin`, `obj`, `.angular`): **~124** files — mostly **`.cursor/`**, **`.claude/`** local agent configs; **not** tracked application source.
- Sample of first **100** tracked WebApp paths: **100** missing at root leftover paths; **0** hash mismatches (no overlapping tracked files to compare).

### 2.4 Classification

| Directory | Classification | Unique tracked content? |
|-----------|----------------|-------------------------|
| `ComplaincePlatform/` | **Stale local working-tree residue** — build outputs (`bin`/`obj`/`_perf_build_out`), IDE folders, partial service trees | **No** — full tracked tree lives under `AOVIAS.Command/` |
| `WebApp/` | **Stale frontend build/cache shell** — `node_modules`/`dist` volume without tracked Angular tree | **No** — tracked app under `AOVIAS.Command/WebApp/` |

### 2.5 Recommended disposition (not authorized in 1I-AO)

1. **Do not treat** untracked root directories as source of truth for Command or WebApp.
2. **Before any deletion:** Optional read-only inventory export of the **977 + 124** leftover-only paths (excluding known artifact dirs) for human sign-off — in case any local-only file must be copied into tracked trees (none identified in this phase).
3. **Separate authorization** recommended for **integration-worktree cleanup** (remove untracked `ComplaincePlatform/` and `WebApp/` at repo root) after sign-off — **not** part of W5.
4. **Do not** `git add` these folders; they would reintroduce pre-W4 layout and pollute history.

---

## 3. W5 readiness summary (website move only)

Planning sources: `repository-layout.json`, `AOVIAS_PHASE1B_MIGRATION_CONTRACT.md`, `AOVIAS_PHASE1IA_TARGET_PHYSICAL_LAYOUT_REPORT.md`, `AOVIAS_PHASE1IB_ARCHITECTURE_DECISIONS_REPORT.md` (O-7), `AOVIAS_PHASE1B_COMPLETE_PATH_INVENTORY.md`.

### 3.1 Source → target

| ID | Source (current) | Target (manifest) | Tracked files (approx.) |
|----|------------------|---------------------|-------------------------|
| **M-W-01** | `AOVIAS Website/AOVIAS Web/` | `AOVIAS.Website/` | **~2341** (`git ls-files "AOVIAS Website"`) |

**O-7 (unresolved decision record):** Flatten nested space path to **`AOVIAS.Website/`** at repo root (preferred in layout target) vs retaining nested segment — **requires scope approval** before implementation (same pattern as W3/W4 spelling decisions).

**Outer folder:** `AOVIAS Website/` (parent) may become empty after inner move — disposition (remove vs keep) is a **W5 housekeeping** decision, not executed here.

### 3.2 Collision / dependency check

| Area | W5 interaction | Risk |
|------|----------------|------|
| `AOVIAS.Command/` | **Distinct product** (`WebApp` = Command portal vs public marketing site) | **Low** — no target path overlap |
| `AOVIAS.Studio/` | No website paths in Studio layout consumers at HEAD | **Low** |
| `AOVIAS.Infrastructure/` | Publish tools reference Command paths post-W4; **no** website path in `scripts/` grep at HEAD | **Low** |
| Root leftovers `ComplaincePlatform/` / `WebApp/` | **Untracked** — must **not** block W5; cleanup separate | **Medium** (human confusion only) |

### 3.3 Directly affected path updates (W5 implementation inventory — planning only)

| Category | Artifacts | Action |
|----------|-----------|--------|
| **Layout contract** | `repository-layout.json` — `products.website.paths.monorepoRoot.current`, `migrationStatus`, optional `discovery` if website markers added | Update **current** to `AOVIAS.Website/` |
| **Website CI** | `AOVIAS Website/AOVIAS Web/.github/workflows/ci.yml` → co-move; verify checkout root / `npm ci` cwd | Path prefix / `defaults.run.working-directory` if needed |
| **Compose (3)** | `deployment/compose/docker-compose.yml`, `docker-compose.prod.yml`, `docker/docker-compose.yml` | Recompute **relative** `context` / build paths after move (Phase 1B inventory) |
| **Dockerfiles** | `deployment/docker/Dockerfile`, `docker/Dockerfile` | COPY paths relative to new monorepo root |
| **npm workspace** | Root `package.json`, `apps/website`, `packages/*` | Internal relative refs usually stable within moved tree |
| **Documentation** | Architecture / migration docs citing `AOVIAS Website/AOVIAS Web` | Structural doc pass (W0-style batch) |
| **.NET / Studio** | None required for website-only wave | Per Phase 1B §6 |

**Not in scope for W5:** Command platform CI (already under `AOVIAS.Command/ComplaincePlatform/.github/`), Studio slnx, Infrastructure config tree.

### 3.4 Untracked siblings (preserve / exclude from W5 commit)

Under current website tree on disk (not necessarily all tracked): `AOVIAS.Website.v*.zip`, `.lighthouseci-*`, local `node_modules` — **exclude** from move commit or leave untracked per prior phase guidance.

### 3.5 Minimal structural checks during W5 implementation (when authorized)

- Moved tree exists at `AOVIAS.Website/`; `git ls-files` rename coverage ~2341 paths.
- `repository-layout.json` **current** website path matches disk.
- Website `ci.yml` paths valid from repo root.
- Compose/Dockerfile relative contexts resolve (spot-check, not full Docker run).
- `git diff --check`; layout tests if website paths added to manifest consumers.
- **No** collision with `AOVIAS.Command/WebApp` or root untracked leftovers.

### 3.6 Unresolved decisions before W5 scope approval

| ID | Topic | Recommendation for scope package |
|----|-------|----------------------------------|
| **O-7** | Flatten to `AOVIAS.Website/` vs nested name | Confirm **flatten** (matches manifest **target**) |
| **K-W5-* | *(not yet issued)* | Mirror W3/W4: readiness → scope approval → sync → implementation gates |
| **Root leftovers** | Cleanup timing vs W5 | **Defer cleanup** to separate authorized housekeeping; document in W5 preflight |

---

## 4. Scope boundaries observed

- No deletion, move, staging, commit, merge, push, or worktree sync.
- No W5 implementation or layout edits.
- No builds, broad tests, Docker, discover/preflight, or npm ReleaseValidation.

---

## 5. Artifact

- **Created (untracked):** `AOVIAS_PHASE1IAO_W5_READINESS_AND_ROOT_LEFTOVERS_REPORT.md`
- All prior untracked reports and operational artifacts **preserved**.
