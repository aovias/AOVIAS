# AOVIAS Phase 1I-IZ — Main Worktree Cleanup Plan & Authorization Gate

**Phase:** 1I-IZ (planning and classification only — **no deletions, moves, staging, commits, or sync**)  
**Date:** 2026-10-25  
**Worktree:** `V:\DevelopmentRepo\AOVIAS`  
**Inputs:** `AOVIAS_PHASE1IIY_MAIN_WORKTREE_LAYOUT_RECONCILIATION.md`, `AOVIAS_PHASE1IAO_W5_READINESS_AND_ROOT_LEFTOVERS_REPORT.md`, targeted re-inspection this phase

---

## Final status

**`CLEANUP PLAN PARTIAL — REVIEW REQUIRED`**

The plan is **actionable in phases**, but **whole-tree removal** of root **`ComplaincePlatform/`**, **`WebApp/`**, and nested **`AOVIAS Website/AOVIAS Web/`** must not proceed until a human reviews **non-generated leftovers** called out in §3B and §3E. **Generated-only** subtrees (§3A, §3C–D artifact paths, §3F) are **low risk** relative to tracked content and can be approved as a **separate batch** after checklist sign-off.

**Cleanup is not authorized by this document.**

---

## 1. Baseline verification

| Check | Expected | Actual | Result |
|-------|----------|--------|--------|
| Worktree | `V:\DevelopmentRepo\AOVIAS` | Same | **PASS** |
| Branch | `main` | `main` | **PASS** |
| HEAD | `bc1365dc9dc0149ef46f5b66a828501387eae9dc` | Same | **PASS** |
| Tracked working tree | Clean | **0** modified/staged tracked paths | **PASS** |
| Reconciliation report | Present | `AOVIAS_PHASE1IIY_MAIN_WORKTREE_LAYOUT_RECONCILIATION.md` (**untracked**) | **PASS** |
| Tracked paths at HEAD | W1–W5 complete | **8228** paths (`git ls-files`) | **PASS** |

**Preserved:** All existing untracked phase reports and artifacts ( **68** `??` porcelain lines including this plan once written). No report was deleted or moved.

**Global safety rule:** Every candidate below was checked with `git ls-files` under that path → **0 tracked files** at repository root for listed cleanup targets.

---

## 2. Safe cleanup rules (mandatory for any future execution)

1. **Never delete tracked paths** — before any removal, re-run `git ls-files -- <path>`; abort if non-zero.
2. **No broad wildcards** — e.g. do not `Remove-Item *` at repo root; remove **named directories or named artifact subtrees** only, one approval item at a time.
3. **Preserve all audit deliverables** — every `AOVIAS_PHASE*.md`, `AOVIAS_PHASE*.json`, `AOVIAS_PHASE*.csv`, and `AOVIAS_GIT_PATH_ANOMALY_REPORT.md` at repo root; do not delete or move unless a **separate** archive policy is approved.
4. **Preserve tracked scaffolding** — **`AOVIAS.AccredaOne/`** (tracked `README.md`), all **`AOVIAS.*` / `Aovias.*` under canonical prefixes**, **`AOVIAS Website/`** tracked **67** paths, **`docs/`**, **`scripts/`**, **`ai-governance/`**, **`repository-layout.json`**, etc.
5. **Do not modify** `.gitignore`, manifests, application source, branches, commits, or history.
6. **Prefer phased execution:** (1) generated artifact subtrees, (2) empty shells, (3) whole legacy root trees **only after** leftover review for §3B/§3E.
7. **Do not `git add`** any legacy root tree — would reintroduce pre-W4/W3 layout.
8. **Re-verify HEAD on `main`** immediately before execution; abort if tracked tree is dirty.

---

## 3. Candidate classification and proposed paths

Path roots below use **`V:\DevelopmentRepo\AOVIAS\`**; in commands, use repo-relative paths from that root.

### 3A. Generated residue (artifact subtrees)

**Classification:** Generated / cache / build output — **not in Git index**. Safe to remove **in isolation** after approval, without affecting tracked sources **if** paths stay within untracked legacy shells.

| Proposed path (repo-relative) | Evidence | Risk |
|--------------------------------|----------|------|
| `ComplianceServiceManager/bin/` | 1987 files under root CSM; **1986** match `bin|obj|packages` pattern; **0** tracked | **Low** |
| `ComplianceServiceManager/obj/` | (included above) | **Low** |
| `ComplianceServiceManager/packages/` | NuGet cache; appears in `git status -u` as `??` | **Low** |
| `ComplianceServiceManager/tools/` | Under untracked root CSM only | **Low** — confirm not a copy of tracked infra tools |
| `ComplianceServiceManager/.agents/` | Agent metadata under untracked root | **Low** |
| `ComplianceServiceManager/write-test.tmp` | Temp file at CSM root | **Low** |
| `ComplianceServiceManager.Operations/bin/` | **20** files, all artifact-pattern | **Low** |
| `ComplianceServiceManager.Operations/obj/` | (same) | **Low** |
| `ComplianceServiceManager.Operations.Tests/bin/` | **108** files, all artifact-pattern | **Low** |
| `ComplianceServiceManager.Operations.Tests/obj/` | (same) | **Low** |
| `ComplianceServiceManager.Tests/bin/` | **212** files, all artifact-pattern | **Low** |
| `ComplianceServiceManager.Tests/obj/` | (same) | **Low** |
| `Aovias.Configuration/bin/`, `Aovias.Configuration/obj/` | **316/317** files artifact-class | **Low** |
| `Aovias.Deployment.Contracts/bin/`, `.../obj/` | All **39** files artifact-class | **Low** |
| `Aovias.DeploymentValidation/bin/`, `.../obj/` | **390/390** artifact-class | **Low** |
| `Aovias.DesignSystem/bin/`, `.../obj/` | **70/70** artifact-class | **Low** |
| `Aovias.Orchestration/bin/`, `.../obj/` | **149/149** artifact-class | **Low** |
| `Aovias.Publishing/bin/`, `.../obj/` | **1056/1056** artifact-class | **Low** |
| `Aovias.Release/bin/`, `.../obj/` | **288/288** artifact-class | **Low** |
| `Aovias.Runtime.Agent/bin/`, `.../obj/` | **717/718** artifact-class | **Low** |
| `Aovias.CommandCenter/bin/`, `.../obj/` | **178/178** artifact-class | **Low** |
| `Aovias.Bootstrap/bin/`, `Aovias.Bootstrap/obj/` | **313/328** artifact-class | **Low** |
| `tools/PublishAllServices/bin/`, `tools/PublishAllServices/obj/` | **235/235** under root `tools/`; canonical publish tools = `AOVIAS.Infrastructure/tools/` (**4** tracked) | **Low** |
| `tools/PublishRuntimeAgent/bin/`, `tools/PublishRuntimeAgent/obj/` | Same root `tools/` shell | **Low** |
| `ComplaincePlatform/**/bin/`, `ComplaincePlatform/**/obj/`, `ComplaincePlatform/**/_perf_build_out/` | **~14,367** files (1I-AO); **0** tracked under root `ComplaincePlatform/` | **Low** per subtree — **do not** use recursive delete on entire `ComplaincePlatform/` in this batch |
| `WebApp/node_modules/`, `WebApp/dist/`, `WebApp/.angular/`, `WebApp/**/bin/`, `WebApp/**/obj/` | **~27,670** files (1I-AO); **0** tracked at root `WebApp/` | **Low** per subtree |
| `AOVIAS Website/AOVIAS Web/node_modules/`, `.../apps/website/node_modules/`, `.../apps/website/.next/`, `.../apps/website/.lighthouseci/`, `.../apps/website/.lighthouseci-r1-4/`, `.../apps/website/.lighthouseci-r1-art/`, `.../apps/website/.lighthouseci-r1-ui/`, `.../artifacts/`, `.../release/` | **~47,461** of **47,468** files; **0** tracked under `AOVIAS Website/AOVIAS Web/` | **Low** for artifact subtrees only |

**Canonical counterparts (do not touch):**

- CSM: `AOVIAS.Studio/ComplianceServiceManager/` (**318** tracked)
- Studio products: `AOVIAS.Studio/Aovias.*` (**1191** total Studio tracked)
- W2 tools: `AOVIAS.Infrastructure/tools/`
- W4: `AOVIAS.Command/ComplaincePlatform/`, `AOVIAS.Command/WebApp/`
- W5: `AOVIAS.Website/`

---

### 3B. Root `ComplaincePlatform/` and `WebApp/` (high volume — separate gate)

**Classification:** Untracked **legacy W4 shells** — **not** authoritative (1I-AO, 1I-IY).  
**Tracked canonical:** `AOVIAS.Command/ComplaincePlatform/` (**3157**), `AOVIAS.Command/WebApp/` (**1368**).

| Evidence | Detail |
|----------|--------|
| Index | `git ls-files ComplaincePlatform/` → **0**; `WebApp/` → **0** |
| Authoritative markers | `AOVIAS.Command/ComplaincePlatform/.dockerignore` **exists**; root `ComplaincePlatform/.dockerignore` **missing** |
| Authoritative markers | `AOVIAS.Command/WebApp/angular.json` **exists**; root `WebApp/angular.json` **missing** |
| Scale | Root **ComplaincePlatform/** **15,344** files; root **WebApp/** **27,794** files |
| Overlap with tracked | **All 3157** tracked Command platform paths **absent** at corresponding root paths (1I-AO); first **100** tracked WebApp paths **100% missing** at root |
| Non-generated leftovers | **~977** paths under root `ComplaincePlatform/` excluding `bin/`, `obj/`, `_perf_build_out/` — mostly **IDE/agent** (`.cursor/`, `.planning/`), `*.user`, launch settings, partial service folders **without** tracked `.csproj` files |
| Non-generated leftovers | **~124** paths under root `WebApp/` excluding `node_modules`, `dist`, `bin`, `obj`, `.angular` — mostly **`.cursor/`**, **`.claude/`** local agent configs |

**Recommended disposition (after human review):**

| Proposed path | Batch | Precondition |
|---------------|-------|--------------|
| Entire `ComplaincePlatform/` (repo root) | **Batch C-W4-1** | Optional export of **977** leftover paths reviewed; confirm nothing must merge into `AOVIAS.Command/` |
| Entire `WebApp/` (repo root) | **Batch C-W4-2** | Optional export of **124** leftover paths reviewed |

**Risk if removed without review:** **Medium** — unlikely loss of **tracked** source, but possible loss of **local-only** IDE/agent settings user may want to keep.

**Not proposed in this phase:** Deletion (forbidden until explicit authorization).

---

### 3C. Root `ComplianceServiceManager*` directories

**Classification:** Untracked **pre-W3 / local build** residue overlapping **`AOVIAS.Studio/ComplianceServiceManager/`**.

| Path | Tracked at root | Files | Generated-only? |
|------|-----------------|-------|-----------------|
| `ComplianceServiceManager/` | **0** | **1987** | **Yes** (no `.cs` outside `bin`/`obj`/`packages` in spot check; no slnx at root) |
| `ComplianceServiceManager.Operations/` | **0** | **20** | **Yes** (`bin`/`obj` only) |
| `ComplianceServiceManager.Operations.Tests/` | **0** | **108** | **Yes** |
| `ComplianceServiceManager.Tests/` | **0** | **212** | **Yes** |

**Proposed cleanup (after approval):**

1. **Batch CSM-A:** Remove artifact subtrees in §3A (`ComplianceServiceManager/**` bin/obj/packages/tools/.agents`, Operations*, Tests* `bin`/`obj`).
2. **Batch CSM-B:** Remove empty or artifact-only root directories:  
   `ComplianceServiceManager/`, `ComplianceServiceManager.Operations/`, `ComplianceServiceManager.Operations.Tests/`, `ComplianceServiceManager.Tests/` **only if** step 1 leaves no non-generated files (re-list directory before removal).

**Risk:** **Low** for tracked repo; canonical CSM source remains under **`AOVIAS.Studio/ComplianceServiceManager/`**.

---

### 3D. Root `Aovias.*` legacy shells

**Classification:** Untracked directories whose names mirror **`AOVIAS.Studio/`** (or pre-W2 **`Aovias.ReleaseValidation`** vs **`AOVIAS.Infrastructure/Aovias.ReleaseValidation/`**) products. **Do not assume** name match implies safe delete without content check.

| Path | Tracked at root | Files | Artifact vs local-only |
|------|-----------------|-------|-------------------------|
| `Aovias.Configuration/` | **0** | **317** | **316** artifact (`bin`/`obj`) |
| `Aovias.Deployment.Contracts/` | **0** | **39** | All artifact |
| `Aovias.DeploymentValidation/` | **0** | **390** | All artifact |
| `Aovias.DesignSystem/` | **0** | **70** | All artifact |
| `Aovias.Orchestration/` | **0** | **149** | All artifact |
| `Aovias.Publishing/` | **0** | **1056** | All artifact |
| `Aovias.Release/` | **0** | **288** | All artifact |
| `Aovias.Runtime.Agent/` | **0** | **718** | **717** artifact |
| `Aovias.CommandCenter/` | **0** | **178** | All artifact |
| `Aovias.Bootstrap/` | **0** | **328** | **313** artifact; **~15** under `bootstrap-reports/*.log` (local run transcripts) |
| `Aovias.ReleaseValidation/` | **0** | **106** | **Run artifacts** under `artifacts/<timestamp>/` (not under `bin`/`obj` pattern but generated validation output) |
| `Aovias.Runtime.Contracts/` | **0** | **0** files | Empty **`Deployment/`** subdirectory only |

**Canonical tracked destinations (preserve):**  
`AOVIAS.Studio/Aovias.Configuration`, `.../Aovias.Runtime.Agent`, etc.; W2 **`AOVIAS.Infrastructure/Aovias.ReleaseValidation/`** (not root `Aovias.ReleaseValidation/`).

**Proposed cleanup (after approval):**

| Batch | Paths |
|-------|--------|
| **STU-A** | All `bin/` and `obj/` under each root `Aovias.*` listed above (§3A table) |
| **STU-B** | `Aovias.ReleaseValidation/artifacts/` (entire tree) |
| **STU-C** | `Aovias.Bootstrap/bootstrap-reports/` — **review logs** if transcripts matter; otherwise generated |
| **STU-D** | Remove each root `Aovias.*` directory **only after** STU-A/B/C and confirming no remaining non-generated files |
| **STU-E** | `Aovias.Runtime.Contracts/` — empty shell; **low risk** after listing confirms no files |

**Risk:** **Low–medium** for **`Aovias.Bootstrap/bootstrap-reports/`** (local-only logs); **low** for pure `bin`/`obj`.

---

### 3E. Website siblings

**Protected (do not delete):**

- **`AOVIAS Website/`** — **67** tracked paths (`git ls-files "AOVIAS Website/"` → **67**), Guidelines and creative-direction assets.

**Separate candidate — untracked nested shell:**

| Path | Tracked | Files | Notes |
|------|---------|-------|-------|
| `AOVIAS Website/AOVIAS Web/` | **0** | **47,468** | Post-W5 **physical leftover**; canonical site is **`AOVIAS.Website/`** (**2274** tracked) |

**Non-artifact files inside shell (7 paths — human review):**

1. `AOVIAS Website/AOVIAS Web/AOVIAS.Website.v1.0.0.zip`  
2. `AOVIAS Website/AOVIAS Web/AOVIAS.Website.v1.1.0.zip`  
3. `AOVIAS Website/AOVIAS Web/apps/website/next-env.d.ts`  
4. `AOVIAS Website/AOVIAS Web/apps/website/tsconfig.tsbuildinfo`  
5. `AOVIAS Website/AOVIAS Web/deployment/environments/.env.preview.example`  
6. `AOVIAS Website/AOVIAS Web/deployment/environments/.env.production.example`  
7. `AOVIAS Website/AOVIAS Web/deployment/environments/.env.staging.example`  

**Evidence:** Canonical `AOVIAS.Website/apps/website/package.json` **exists**; same path under nested shell **does not** (incomplete/stale tree dominated by **node_modules** / **.next** / Lighthouse CI output). Tracked app source lives under **`AOVIAS.Website/`**.

**Proposed cleanup (after approval):**

| Batch | Action |
|-------|--------|
| **WEB-A** | Remove artifact subtrees listed in §3A under `AOVIAS Website/AOVIAS Web/` only |
| **WEB-B** | After WEB-A, review **7** files above; confirm duplicates exist under `AOVIAS.Website/` or are expendable |
| **WEB-C** | Remove **`AOVIAS Website/AOVIAS Web/`** directory entirely — **never** remove parent **`AOVIAS Website/`** |

**Risk:** **Low** for tracked website; **medium** for zip archives and env examples if no other copy (verify against `AOVIAS.Website/` before WEB-C).

---

### 3F. Other candidates

| Path | Tracked | Classification | Proposed action |
|------|---------|----------------|-----------------|
| `tools/` (repo root) | **0** | Pre-W2 **`PublishAllServices` / `PublishRuntimeAgent`** build output only | **Batch TOOL-1:** delete `tools/PublishAllServices/bin`, `obj`, `tools/PublishRuntimeAgent/bin`, `obj`; then remove `tools/` if empty |
| `preflight-docker-probe/` | **0** | **Empty** directory; manifest **`unresolvedDecisions` O-8** | **Batch PREF-1:** remove empty dir **or** defer until O-8 decided — **low** structural risk |
| Root `AOVIAS_PHASE*.md/json/csv` | **0** | Audit artifacts | **No removal** in default cleanup (§2 rule 3) |
| `ComplaincePlatform/`, `WebApp/` (entire) | **0** | See §3B | Batches **C-W4-1/2** after review |

---

## 4. Protected paths and exclusions (never delete in default plan)

```text
# Tracked product / contract roots
AOVIAS.Infrastructure/
AOVIAS.Studio/
AOVIAS.Command/
AOVIAS.Website/
AOVIAS.AccredaOne/
AOVIAS Website/          # except untracked AOVIAS Website/AOVIAS Web/ after WEB-* approval
docs/
scripts/
ai-governance/
repository-layout.json
AGENTS.md
.gitattributes
.gitignore
ACCREDACONE_KNOWLEDGEBASE_READINESS_ASSESSMENT.md
AOVIAS_REPOSITORY_RESTRUCTURING_ASSESSMENT.md

# All untracked phase / audit reports at repo root (AOVIAS_PHASE*, AOVIAS_GIT_*)
# Including:
#   AOVIAS_PHASE1IIY_MAIN_WORKTREE_LAYOUT_RECONCILIATION.md
#   AOVIAS_PHASE1IIZ_MAIN_WORKTREE_CLEANUP_PLAN.md
```

---

## 5. Risks and unresolved decisions

| ID | Risk / decision | Impact |
|----|-----------------|--------|
| R-01 | **977 + 124** non-artifact paths in root W4 shells | Local IDE/agent settings; not tracked |
| R-02 | **7 files** in `AOVIAS Website/AOVIAS Web/` | Zips and env examples may need archival |
| R-03 | **`Aovias.Bootstrap/bootstrap-reports/`** | Local demo transcripts |
| R-04 | **`preflight-docker-probe/`** | Manifest **O-8** — policy vs empty-dir delete |
| R-05 | Accidental **`git clean -fdx`** at repo root | Could delete **all** untracked reports — **forbidden** approach |
| R-06 | Re-running builds before cleanup | May recreate artifact trees — expected; cleanup is repeatable |

**Unresolved manifest items (unchanged):** O-3–O-8, INV-1/2 — cleanup does **not** resolve these; do not edit manifest in cleanup phase.

---

## 6. Human-approval checklist

Use checkboxes when authorizing a **future** execution phase (not 1I-IZ).

### Pre-flight (required once)

- [ ] Worktree is **`V:\DevelopmentRepo\AOVIAS`**, branch **`main`**, HEAD **`bc1365d`**, tracked tree **clean**
- [ ] Re-run `git ls-files` spot checks on any path before deletion
- [ ] Confirm **no** use of `git clean -fdx` at repository root

### Batch approvals (select any; order recommended)

- [ ] **BATCH-GEN-1:** §3A CSM + CSM test/ops artifact subtrees (§3C steps CSM-A)
- [ ] **BATCH-GEN-2:** §3A root `Aovias.*` `bin/`/`obj/` + `Aovias.ReleaseValidation/artifacts/` (STU-A, STU-B)
- [ ] **BATCH-GEN-3:** §3A root `tools/Publish*/bin|obj` (TOOL-1)
- [ ] **BATCH-GEN-4:** §3A `ComplaincePlatform/**/bin|obj|_perf_build_out` and `WebApp` cache dirs **only** (not whole trees)
- [ ] **BATCH-GEN-5:** §3A `AOVIAS Website/AOVIAS Web/` artifact subtrees (WEB-A)
- [ ] **BATCH-REVIEW-1:** Reviewed **977** path export for root `ComplaincePlatform/` (or accepted 1I-AO sampling)
- [ ] **BATCH-REVIEW-2:** Reviewed **124** path export for root `WebApp/`
- [ ] **BATCH-REVIEW-3:** Reviewed **7** non-artifact files under `AOVIAS Website/AOVIAS Web/`
- [ ] **BATCH-REVIEW-4:** Reviewed `Aovias.Bootstrap/bootstrap-reports/` logs
- [ ] **BATCH-TREE-1:** Remove entire root `ComplaincePlatform/` (C-W4-1)
- [ ] **BATCH-TREE-2:** Remove entire root `WebApp/` (C-W4-2)
- [ ] **BATCH-TREE-3:** Remove root `ComplianceServiceManager*` dirs after CSM-A (CSM-B)
- [ ] **BATCH-TREE-4:** Remove root `Aovias.*` dirs after STU-A/B/C (STU-D, STU-E)
- [ ] **BATCH-TREE-5:** Remove `AOVIAS Website/AOVIAS Web/` after WEB-A/B (WEB-C)
- [ ] **BATCH-MISC-1:** Remove empty `preflight-docker-probe/` (PREF-1) — optional pending O-8

### Explicit denials (default)

- [ ] **Do not** delete tracked **`AOVIAS Website/`** sibling content (67 paths)
- [ ] **Do not** delete untracked **`AOVIAS_PHASE*`** reports unless separate archive policy approved
- [ ] **Do not** delete **`AOVIAS.AccredaOne/`**

---

## 7. Suggested execution order (when authorized)

1. **GEN batches** (§3A) — smallest blast radius.  
2. **STU-B/C**, **CSM-B**, **TOOL-1** — remove now-empty or artifact-only root folders.  
3. **WEB-A → WEB-B → WEB-C** — nested website shell.  
4. **REVIEW + C-W4-1/2** — root Command/WebApp legacy trees last (largest, local-only content).

After each batch: `git status` must show **only** expected reduction in `??` directories; tracked file count remains **8228**.

---

## 8. Checks run vs not run (this phase)

| Activity | Status |
|----------|--------|
| Baseline verification | **Run** |
| Reconciliation report presence | **Run** |
| Per-candidate `git ls-files` (0 tracked) | **Run** |
| File counts / artifact heuristics | **Run** (pattern-based, not full hash audit) |
| Full export of 977 + 124 leftover paths | **Not run** (recommended before C-W4-1/2) |
| Byte-for-byte compare nested `AOVIAS Web` vs `AOVIAS.Website` | **Not run** |
| Deletion or `git clean` | **Not run** (forbidden) |

---

## 9. Artifact

- **Created (untracked):** `AOVIAS_PHASE1IIZ_MAIN_WORKTREE_CLEANUP_PLAN.md` — **not staged**

No files or directories were removed, moved, or modified except this report.
