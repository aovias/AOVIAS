# AOVIAS Phase 1I-JV — Legacy Tree Retirement Report

**Phase:** 1I-JV  
**Date:** 2026-10-25  
**Worktree:** `V:\DevelopmentRepo\AOVIAS`  
**Authorization:** **WEB-MIGRATE** (copy); **TREE-1** / **TREE-2** only if archive destination unambiguous — **not met**

---

## Completion status

**`PARTIAL`**

**WEB-MIGRATE (A)** completed as **copy** into canonical **`AOVIAS.Website/`** (four files; sources preserved). **TREE-2 (B)** and **TREE-1 (C)** **skipped** — prior reports specify **off-repo** or **optional** archive without an approved in-repository destination. **Bootstrap logs (D)** preserved. No commit; migrated files are **untracked** until a future integration commit.

---

## 1. Baseline

| Check | Expected | Actual | Result |
|-------|----------|--------|--------|
| Branch | `main` | `main` | **PASS** |
| HEAD | `10353ef51d48464ed9aa412a57562996aba16492` | Same | **PASS** |
| Tracked paths | **8229** | **8229** | **PASS** |
| Tracked working tree (pre-action) | Clean | **0** modified/staged tracked paths | **PASS** |
| `AOVIAS_PHASE1IJU_PROJECT_LAYOUT_ALIGNMENT_REPORT.md` | Present | **Yes** | **PASS** |

Evidence reused: **1I-JI** §3–§5, **1I-JQ** remaining-work matrix — no new broad inventory.

---

## 2. A — WEB-MIGRATE

### Reference alignment

Tracked documentation already expects:

- `AOVIAS.Website/deployment/environments/` (e.g. `r1.8-environment-configuration-guide.md`, `docker-compose.yml` references)
- Canonical tree already had **tracked** `.env.development` and `.env.production` under `deployment/environments/`; **`.example`** templates were **missing** (gap noted in **1I-JI** §3.3.1).

### Migrated files (copy, not move)

| Source (nested shell) | Destination (canonical) | Pre-check | Action |
|------------------------|----------------------|-----------|--------|
| `AOVIAS Website/AOVIAS Web/deployment/environments/.env.preview.example` | `AOVIAS.Website/deployment/environments/.env.preview.example` | Dest absent | **Copied** |
| `AOVIAS Website/AOVIAS Web/deployment/environments/.env.production.example` | `AOVIAS.Website/deployment/environments/.env.production.example` | Dest absent | **Copied** |
| `AOVIAS Website/AOVIAS Web/deployment/environments/.env.staging.example` | `AOVIAS.Website/deployment/environments/.env.staging.example` | Dest absent | **Copied** |
| `AOVIAS Website/AOVIAS Web/docs/release/AOVIAS-Release-Specification-v1.0.md` | `AOVIAS.Website/docs/release/AOVIAS-Release-Specification-v1.0.md` | Dest absent; created `docs/release/` | **Copied** |

**Post-copy verification:** SHA-256 **match** source ↔ destination for all four files.

**Preserved at source (unchanged):**

- `AOVIAS Website/AOVIAS Web/AOVIAS.Website.v1.0.0.zip`
- `AOVIAS Website/AOVIAS Web/AOVIAS.Website.v1.1.0.zip`
- Generated artifacts (`next-env.d.ts`, `tsconfig.tsbuildinfo`) — not migrated (disposition: delete with **TREE-5** later)
- All **67** tracked paths under outer `AOVIAS Website/` — **unchanged**

**Reference scan:** Tracked website docs and `AOVIAS.Website/docker/docker-compose.yml` already reference `deployment/environments/` layout; **no** tracked code changes required for path resolution. Nested-shell path appears only in phase/historical docs, not production adapters.

**Git note:** New canonical copies are **untracked** (`??`) — not staged per phase rules. Index count remains **8229** until human commit adds these paths.

---

## 3. B — `WebApp/` retirement (`TREE-2`)

| Requirement | Finding |
|-------------|---------|
| Preserve **29** `.planning/release/` docs | **In place** — **29** files under `WebApp/.planning/release/` (unchanged) |
| Approved archive destination | **1I-JI** **ARCHIVE-WA-PLAN**: “Optional”; **1I-JC** proposes **ARCHIVE-OFFREPO** — **no** fixed in-repo path |
| Authorization gate | **No** unambiguous approved destination → **tree retirement not executed** |

**Outcome:** Root **`WebApp/`** **retained** (**124** files total). **Decision needed:** off-repo archive path or explicit in-repo target before **BATCH-REVIEW-2** + **TREE-2**.

---

## 4. C — `ComplaincePlatform/` retirement (`TREE-1`)

| Requirement | Finding |
|-------------|---------|
| Unique `release/`, SBOM, two PS1 scripts | **Present** at root `ComplaincePlatform/release/` and `ComplaincePlatform/scripts/release/` (per **1I-JI**) |
| **ARCHIVE-CP-RELEASE** | **1I-JI**: “Off-repo or docs attachment” — **not** a repo-relative destination |
| Blind copy into Command tree | **Not performed** (forbidden) |
| Authorization gate | Archive disposition **undecided** → **tree retirement not executed** |

**Outcome:** Root **`ComplaincePlatform/`** **retained** (~**977** files). **Decision needed:** archive location and whether to port `generate-checksums.ps1` / `generate-sbom.ps1` (**1I-JI** Q-2) before **BATCH-REVIEW-1** + **TREE-1**.

---

## 5. D — Bootstrap logs

| Item | Status |
|------|--------|
| `Aovias.Bootstrap/bootstrap-reports/` | **15** files — **preserved**; no delete/archive |

---

## 6. Retired trees

| Tree | Retired? |
|------|----------|
| `ComplaincePlatform/` | **No** — see §4 |
| `WebApp/` | **No** — see §3 |
| `AOVIAS Website/AOVIAS Web/` | **No** — migrate copy only; **TREE-5** not authorized |
| Nested shell sources | **Intentionally retained** (copies, not moves) |

---

## 7. W1–W5 canonical layout

| Path | Status |
|------|--------|
| `AOVIAS.Command/` | **Intact** |
| `AOVIAS.Studio/` | **Intact** |
| `AOVIAS.Infrastructure/` | **Intact** (`tools/` **4** tracked files) |
| `AOVIAS.Website/` | **Intact**; **four** new **untracked** files under deployment/docs |
| `AOVIAS.AccredaOne/` | **Intact** |
| `docs/`, `scripts/` | **Intact** |

No canonical directory renamed or moved.

---

## 8. Final Git state

| Item | Value |
|------|--------|
| HEAD | `10353ef51d48464ed9aa412a57562996aba16492` |
| Tracked path count | **8229** |
| Tracked modifications | **None** |
| Untracked (migration) | `AOVIAS.Website/deployment/environments/.env.preview.example`, `.env.production.example`, `.env.staging.example`; `AOVIAS.Website/docs/release/AOVIAS-Release-Specification-v1.0.md` |
| `git diff --check` | **Pass** |
| Commit / push | **None** |
| Phase reports | **Not** reorganized |

---

## 9. Unresolved decisions (human)

| ID | Decision |
|----|----------|
| **JV-1** | Commit **WEB-MIGRATE** files to index (separate integration phase) |
| **JV-2** | Approve **off-repo** (or explicit in-repo) archive for **ARCHIVE-WA-PLAN** then **TREE-2** |
| **JV-3** | Approve **ARCHIVE-CP-RELEASE** destination then **TREE-1** |
| **JV-4** | ZIP retention + **BATCH-REVIEW-3** + **TREE-5** for nested `AOVIAS Website/AOVIAS Web/` |
| **JV-5** | **BATCH-REVIEW-4** for root bootstrap demo logs |

---

## 10. Artifact

- **Created (untracked):** `AOVIAS_PHASE1IJV_LEGACY_TREE_RETIREMENT_REPORT.md` — **not staged**

**Exact disk changes:** four **new files** under `AOVIAS.Website/` (listed §8); no deletions; no tracked file edits.
