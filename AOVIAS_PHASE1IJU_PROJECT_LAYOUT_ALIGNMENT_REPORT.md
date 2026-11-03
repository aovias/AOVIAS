# AOVIAS Phase 1I-JU — Main Worktree Project Layout Alignment Report

**Phase:** 1I-JU  
**Date:** 2026-10-25  
**Worktree (main):** `V:\DevelopmentRepo\AOVIAS`  
**Reference worktree:** `V:\DevelopmentRepo\AOVIAS.Cursor`  
**Mode:** Project-directory alignment — **no** commit, stage, or report reorganization

---

## Completion status

**`PARTIAL`**

Canonical W1–W5 project directories on `main` are **intact**. One **authorized** empty legacy shell was removed (completion of **DUP-SECRETS-1** empty parents after **1I-JT**). Large legacy duplicate trees **remain** because disposition batches (**TREE-***, **WEB-MIGRATE**, **BATCH-REVIEW-***) are **not** explicitly authorized in this phase and trees contain **unique or unresolved** content per **1I-JI** / **1I-JQ**.

---

## 1. Baseline (main)

| Check | Expected | Actual | Result |
|-------|----------|--------|--------|
| Branch | `main` | `main` | **PASS** |
| HEAD | `10353ef51d48464ed9aa412a57562996aba16492` | Same (before/after physical cleanup) | **PASS** |
| Tracked paths | **8229** | **8229** | **PASS** |
| Tracked working tree (pre-action) | Clean | **0** modified/staged tracked paths | **PASS** |

**Cursor worktree (reference only — not assumed identical):**

| Item | Actual |
|------|--------|
| Branch | `agent/cursor/phase1ir-w5-website-migration` |
| HEAD | `f2dcfccbb1eb7f7b05f3ca2859650dc04cc63a03` (older than main; **not** used as Git baseline) |
| Physical root | Closer to target layout: **no** root `ComplaincePlatform/`, `WebApp/`, `Aovias.Bootstrap/`, `Aovias.Configuration/` |

---

## 2. Desired layout vs main (before action)

**Canonical project roots (tracked W1–W5 — source of truth):**

| Directory | On main | Tracked paths (prefix) |
|-----------|---------|-------------------------|
| `AOVIAS.AccredaOne/` | Yes | **1** |
| `AOVIAS.Command/` | Yes | **4525** |
| `AOVIAS.Infrastructure/` | Yes | **33** |
| `AOVIAS.Studio/` | Yes | **1192** |
| `AOVIAS.Website/` | Yes | **2274** |
| `docs/` | Yes | **107** |
| `scripts/` | Yes | **22** |

**Legitimate non-canonical / transitional at main root (pre-action):**

| Directory | Role | Action in 1I-JU |
|-----------|------|-----------------|
| `AOVIAS Website/` | **67** tracked siblings (W5 outer tree) | **Preserved** (required) |
| `ComplaincePlatform/` | Untracked duplicate of Command CP | **Retained** — **BATCH-REVIEW-1** + **TREE-1** + optional **ARCHIVE-CP-RELEASE** |
| `WebApp/` | Untracked duplicate | **Retained** — **ARCHIVE-WA-PLAN** + **BATCH-REVIEW-2** + **TREE-2** |
| `AOVIAS Website/AOVIAS Web/` | Nested shell (8 files + dirs) | **Retained** — **WEB-MIGRATE** + **BATCH-REVIEW-3** + **TREE-5** |
| `Aovias.Bootstrap/` | Root demo logs (**15** files) | **Retained** — **BATCH-REVIEW-4** |
| `Aovias.Configuration/` | Empty legacy shell (**0** files post-**1I-JT**) | **Removed** — see §3 |
| `ai-governance/` | Governance docs | **Preserved** (not in scope to delete) |

Prior phases already removed other empty root `Aovias.*` shells (**1I-JP**, **1I-JT**); not repeated here.

---

## 3. Executed action (authorized)

### Empty root `Aovias.Configuration/` — **DUP-SECRETS-1** empty parents

| Gate | Evidence |
|------|----------|
| Disposition | **1I-JI** batch **DUP-SECRETS-1**: duplicate file **+ empty parent dirs**; **1I-JT** deleted file; **Q-6**: remove empty root tree when empty |
| Files (recursive) | **0** |
| Tracked under prefix | **0** |
| Reparse points | **0** |
| Canonical | `AOVIAS.Studio/Aovias.Configuration/` **present**; `SecretsSettings.cs` **tracked** (**1** path) |

**Method:** Bottom-up removal of **empty directory nodes** only.

**Outcome:** Root `Aovias.Configuration/` **absent**. No tracked files modified.

---

## 4. Actions not executed (authorization / content gates)

| Candidate | Reason retained |
|-----------|-----------------|
| `ComplaincePlatform/` (~**977** files) | Non-empty; release/SBOM/scripts and agent residue (**1I-JI**); requires **ARCHIVE-CP-RELEASE** + **BATCH-REVIEW-1** + **TREE-1** |
| `WebApp/` (**124** files) | Non-empty; **29** certification/planning docs (**1I-JI**); requires **ARCHIVE-WA-PLAN** + review + **TREE-2** |
| `AOVIAS Website/AOVIAS Web/` | Env templates, ZIPs, release spec — **WEB-MIGRATE** before **TREE-5**; must not remove **67** tracked parent siblings |
| `Aovias.Bootstrap/bootstrap-reports/` | **15** demo logs; **BATCH-REVIEW-4** not authorized for deletion |
| Broad match to Cursor by deleting non-empty trees | **Forbidden** — no broad recursive delete; no “untracked = disposable” rule |

No **WEB-MIGRATE**, **TREE-1/2/5**, or archive batches were run.

---

## 5. Main vs Cursor — project directory alignment snapshot

| Root project-related dir | Main (after 1I-JU) | Cursor (physical) |
|--------------------------|--------------------|-------------------|
| `AOVIAS.AccredaOne/` | Yes | Yes |
| `AOVIAS.Command/` | Yes | Yes |
| `AOVIAS.Infrastructure/` | Yes | Yes |
| `AOVIAS.Studio/` | Yes | Yes |
| `AOVIAS.Website/` | Yes | Yes |
| `docs/`, `scripts/` | Yes | Yes |
| `AOVIAS Website/` | Yes (**67** tracked + nested untracked shell) | Yes (**67** tracked only — no nested `AOVIAS Web/` bulk) |
| `ComplaincePlatform/` | **Yes** (legacy) | **No** |
| `WebApp/` | **Yes** (legacy) | **No** |
| `Aovias.Bootstrap/` | **Yes** (15 files) | **No** |

**Gap to Cursor-like root:** three legacy duplicate / residue areas above, plus nested website shell under `AOVIAS Website/`. Closing the gap requires **approved** migrate/archive/delete phases, not layout-only deletion.

---

## 6. Final verification

| Check | Result |
|-------|--------|
| HEAD | `10353ef51d48464ed9aa412a57562996aba16492` — **unchanged** |
| Tracked path count | **8229** |
| Tracked working tree | **Clean** (no staged/modified tracked files) |
| `git diff --check` | **Pass** |
| W1–W5 canonical paths | **Intact** (see §2 table) |
| `AOVIAS.Infrastructure/tools/` | **4** tracked files |
| Studio `SecretsSettings.cs` | **1** tracked path — **unchanged** |
| `AOVIAS Website/` tracked siblings | **67** paths — **unchanged** |
| Phase reports / root documents | **Not** moved or reorganized |
| Commit / push / remote | **None** |

**Main root directories after 1I-JU:** `ai-governance`, `AOVIAS Website`, `AOVIAS.AccredaOne`, `Aovias.Bootstrap`, `AOVIAS.Command`, `AOVIAS.Infrastructure`, `AOVIAS.Studio`, `AOVIAS.Website`, `ComplaincePlatform`, `docs`, `scripts`, `WebApp`.

---

## 7. Human input still required

| Priority | Decision / batch | Paths |
|----------|------------------|-------|
| 1 | **WEB-MIGRATE** (commit) | Env examples + release spec from nested `AOVIAS Website/AOVIAS Web/` → `AOVIAS.Website/` |
| 2 | **BATCH-REVIEW-4** | Root `Aovias.Bootstrap/bootstrap-reports/` retention |
| 3 | **ARCHIVE-CP-RELEASE** + **BATCH-REVIEW-1** + **TREE-1** | Root `ComplaincePlatform/` |
| 4 | **ARCHIVE-WA-PLAN** + **BATCH-REVIEW-2** + **TREE-2** | Root `WebApp/` |
| 5 | **BATCH-REVIEW-3** + **BATCH-TREE-5** | Nested `AOVIAS Website/AOVIAS Web/` after migrate/archive |

---

## 8. Artifact

- **Created (untracked):** `AOVIAS_PHASE1IJU_PROJECT_LAYOUT_ALIGNMENT_REPORT.md` — **not staged**

Physical change on disk: removal of empty root **`Aovias.Configuration/`** only.
