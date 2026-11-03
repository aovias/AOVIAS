# AOVIAS Phase 1I-JI — Legacy Root Trees: Final Disposition & Approval Package

**Phase:** 1I-JI (read-only — **no** moves, deletes, edits, or Git writes)  
**Date:** 2026-10-25  
**Worktree:** `V:\DevelopmentRepo\AOVIAS`

---

## Required outcome

**`PASS`** — Evidence-based disposition and approval package only; repository content unchanged.

---

## 1. Verified baseline

| Check | Expected | Actual | Result |
|-------|----------|--------|--------|
| Worktree | `V:\DevelopmentRepo\AOVIAS` | Same | **PASS** |
| Branch | `main` | `main` | **PASS** |
| HEAD | `9bcc44691087655ddb1d9c78aeb38c741fcc197c` | Same | **PASS** |
| Tracked paths | **8229** | **8229** | **PASS** |
| Tracked working tree | Clean | **0** modified/staged tracked paths | **PASS** |

**Post–1I-JG/JH note:** Root `ComplianceServiceManager*`, `preflight-docker-probe/`, and root `tools/` shells are **already removed**. This report covers **remaining** legacy content.

---

## 2. Executive summary

| Tree / area | Files | Tracked | Canonical W1–W5 | Primary recommendation |
|-------------|-------|---------|-----------------|---------------------------|
| Root `ComplaincePlatform/` | **977** | **0** | `AOVIAS.Command/ComplaincePlatform/` (**3157** tracked) | **Delete after approval** (whole tree) — after **archive** of unique release/scripts |
| Root `WebApp/` | **124** | **0** | `AOVIAS.Command/WebApp/` (**1368** tracked) | **Delete after approval** — after **archive** of `.planning/release/` |
| `AOVIAS Website/AOVIAS Web/` | **8** | **0** | `AOVIAS.Website/` (**2274** tracked); parent **`AOVIAS Website/`** **67** tracked | **Migrate** env templates + doc, then **delete after approval** |
| Root `Aovias.Configuration/` | **1** | **0** | `AOVIAS.Studio/Aovias.Configuration/` (+ tracked `SecretsSettings.cs`) | **Delete after approval** (duplicate only; hashes **identical**) |
| `Aovias.Bootstrap/bootstrap-reports/` | **15** | **0** | `AOVIAS.Studio/Aovias.Bootstrap/` (**97** tracked) | **Archive for review** or delete after approval |
| Empty root `Aovias.*` shells | **0** each | **0** | Under `AOVIAS.Studio/` or Infrastructure | **Delete after approval** (path-enumerated) |
| Root `Aovias.Runtime.Agent/` log | **1** | **0** | Studio agent tree | **Delete after approval** (single log path) |

**No tracked file** lives under any legacy root tree listed above.

---

## 3. Per-path analysis

### 3.1 Root `ComplaincePlatform/` (977 files, 195 dirs)

| Attribute | Evidence |
|-----------|----------|
| **Tracked** | `git ls-files ComplaincePlatform/` → **0** |
| **Canonical** | `AOVIAS.Command/ComplaincePlatform/` — **3157** tracked; `.dockerignore` present; **46** `.csproj` under Command tree |
| **Root tree** | **0** `.csproj`; not a complete source tree (1I-AO, 1I-JB) |
| **Overlap** | Sample: **0** same-relative-path files vs Command tree — leftovers are **unique paths**, not byte duplicates |
| **Composition (top buckets)** | `logs/` **419**; `artifacts/` **235** (build-verify JSON); `.planning/` **210**; `.cursor/` **85**; `release/` **6**; partial service folders (configs only); `scripts/release/` **2** PS1 |
| **References** | Tracked **`docs/`** still cite root-style `ComplaincePlatform/...` in prose (architecture, G1 runbook narrative) — **documentation drift**, not runtime bind to untracked root folder. **Executable** G1 examples use **`AOVIAS.Command/ComplaincePlatform/`** and **`AOVIAS.Infrastructure/tools/`** |
| **CI/Docker/scripts index** | No tracked workflow `working-directory` pointing at **root** `ComplaincePlatform/` (W4 CI uses Command path) |
| **Unique valuable items** | `ComplaincePlatform/release/1.0.0/` (RELEASE_NOTES, SBOM, checksums, manifest JSON); `scripts/release/generate-checksums.ps1`, `generate-sbom.ps1` — **not** found at same paths under `AOVIAS.Command/ComplaincePlatform/` (spot check) |
| **Risks** | Loss of local IDE/agent settings (`.cursor`, `.planning`); loss of one-off release artifacts if not archived |
| **Disposition** | **Delete after approval** — batch **BATCH-TREE-1** after **BATCH-REVIEW-1** and optional **ARCHIVE-CP-RELEASE** |
| **Future paths** | Entire `ComplaincePlatform/` at repo root only |

---

### 3.2 Root `WebApp/` (124 files, 34 dirs)

| Attribute | Evidence |
|-----------|----------|
| **Tracked** | **0** |
| **Canonical** | `AOVIAS.Command/WebApp/` — **1368** tracked; `angular.json` at Command path only |
| **Composition** | `.cursor`/`.claude` **~95**; `.planning/release/` **29** markdown (RC/certification reports); **no** `angular.json`, no app `src/` |
| **References** | No tracked CI/scripts requiring root `WebApp/` |
| **Unique** | **29** release/certification docs under `.planning/release/` — historical, not in Command tree |
| **Risks** | Discarding local release certification narrative |
| **Disposition** | **Archive for review** (planning docs) → **Delete after approval** — **BATCH-REVIEW-2** + **BATCH-TREE-2** |
| **Future paths** | Entire `WebApp/` at repo root |

---

### 3.3 `AOVIAS Website/AOVIAS Web/` (8 files, 18 dirs)

| Attribute | Evidence |
|-----------|----------|
| **Tracked** | **0** under nested shell; **`AOVIAS Website/`** **67** tracked siblings **must retain** |
| **Canonical site** | `AOVIAS.Website/` (**2274** tracked) |
| **Files (exact)** | See §3.3.1 |
| **Disposition** | **Migrate** then **Delete after approval** — **WEB-MIGRATE** + **BATCH-REVIEW-3** + **BATCH-TREE-5** |

#### 3.3.1 File-level table

| Relative path | Size | Class | Canonical equivalent | Recommendation |
|---------------|------|-------|----------------------|----------------|
| `.../AOVIAS.Website.v1.0.0.zip` | ~23.1 MB | Release archive | None in tracked tree | **Archive for review** → delete after approval |
| `.../AOVIAS.Website.v1.1.0.zip` | ~23.6 MB | Release archive | None | Same |
| `.../apps/website/next-env.d.ts` | 262 B | Generated | Regenerates; in `.gitignore` patterns | **Delete after approval** (with tree) |
| `.../apps/website/tsconfig.tsbuildinfo` | ~318 KB | Generated | N/A | **Delete after approval** |
| `.../deployment/environments/.env.preview.example` | 450 B | Template | **Missing** at `AOVIAS.Website/deployment/environments/`; guide references `.env.preview.example` | **Migrate** to canonical tree (separate commit) — do not overwrite `apps/website/.env.example` |
| `.../.env.production.example` | 923 B | Template | Same gap; compose references `deployment/environments/.env.production` | **Migrate** |
| `.../.env.staging.example` | 507 B | Template | Same gap | **Migrate** |
| `.../docs/release/AOVIAS-Release-Specification-v1.0.md` | ~26 KB | Documentation | No matching file under `AOVIAS.Website/docs/` | **Migrate** or **Archive for review** |

**Env references:** Tracked `AOVIAS.Website/docs/development/r1.8-environment-configuration-guide.md` documents `deployment/environments/.env.*` layout; **`deployment/environments/` directory is absent** on canonical disk (only `apps/website/.env.example` present). Leftover templates are **aligned with documented intent**, not redundant copies of `.env.example`.

**Secrets:** Only example templates inspected; **no values** recorded.

---

### 3.4 Root `Aovias.Configuration/` and duplicate `SecretsSettings.cs`

| Attribute | Evidence |
|-----------|----------|
| **Files** | **1** — `Secrets/SecretsSettings.cs` only |
| **Tracked** | **0** at root |
| **Canonical** | `AOVIAS.Studio/Aovias.Configuration/Secrets/SecretsSettings.cs` — **tracked** (commit `9bcc446`) |
| **Equivalence** | SHA-256 **IDENTICAL** (root vs canonical) |
| **Compile** | Project builds with **canonical** file only; root copy **outside** `.csproj` |
| **References** | Studio `ModularConfigurationExtensions`, `ProfileConfigurationLoader`, CSM settings VM, tests — all use type from **Studio assembly** |
| **Disposition** | **Delete after approval** — duplicate only, after human confirms canonical on `main` is sufficient — batch **DUP-SECRETS-1** (single file + empty parent dirs), **not** before explicit sign-off |
| **Defer** | Do **not** delete until checklist item approved (1I-JE recovery verified) |

---

### 3.5 `Aovias.Bootstrap/bootstrap-reports/` (15 files)

| Attribute | Evidence |
|-----------|----------|
| **Content** | Demo transcript `.log` files (`*-demo-*-transcript.log`) |
| **Tracked** | **0** |
| **Canonical** | `AOVIAS.Studio/Aovias.Bootstrap/` — product source tracked; logs **not** part of index |
| **References** | None in tracked build/CI |
| **Disposition** | **Archive for review** or **Delete after approval** — **BATCH-REVIEW-4** |
| **Future path** | `Aovias.Bootstrap/bootstrap-reports/` (not entire Bootstrap product tree) |

---

### 3.6 Other remaining root-level items (from 1I-IZ / 1I-JB / post-JG/JH)

| Path | Files | Tracked | Canonical | Disposition |
|------|-------|---------|-----------|-------------|
| `Aovias.CommandCenter/` | **0** | **0** | `AOVIAS.Studio/Aovias.CommandCenter/` | **Delete after approval** (empty shell) — **SHELL-AOV-1** |
| `Aovias.Deployment.Contracts/` | **0** | **0** | Studio path | **SHELL-AOV-1** |
| `Aovias.DeploymentValidation/` | **0** | **0** | Studio path | **SHELL-AOV-1** |
| `Aovias.DesignSystem/` | **0** | **0** | Studio path | **SHELL-AOV-1** |
| `Aovias.Orchestration/` | **0** | **0** | Studio path | **SHELL-AOV-1** |
| `Aovias.Publishing/` | **0** | **0** | Studio path | **SHELL-AOV-1** |
| `Aovias.Release/` | **0** | **0** | Studio path | **SHELL-AOV-1** |
| `Aovias.ReleaseValidation/` | **0** | **0** | W2: `AOVIAS.Infrastructure/Aovias.ReleaseValidation/` | **SHELL-AOV-1** |
| `Aovias.Runtime.Contracts/` | **0** | **0** | Studio path | **SHELL-AOV-1** |
| `Aovias.Runtime.Agent/` | **1** log | **0** | `AOVIAS.Studio/Aovias.Runtime.Agent/` | **Delete after approval** — path `Aovias.Runtime.Agent/src/Aovias.Runtime.Agent.Api/logs/runtime-agent/runtime-agent-20260626.log` only (**LOG-1**), then remove empty dirs if policy allows |

**Already removed (1I-JG/JH):** `ComplianceServiceManager*`, `preflight-docker-probe/`, root `tools/`.

**Protected confirmed:** `AOVIAS.Infrastructure/tools/` — **4** tracked files intact.

---

## 4. Proposed cleanup batches (ordered by risk — lowest first)

| Order | Batch ID | Scope | Risk | Preconditions |
|-------|----------|-------|------|----------------|
| 1 | **LOG-1** | Single runtime-agent log under root `Aovias.Runtime.Agent/` | Low | Path-exact; confirm no other files |
| 2 | **SHELL-AOV-1** | Listed **empty** root `Aovias.*` directories (0 files) | Low | Re-verify empty; **exclude** `Aovias.Configuration/` and `Aovias.Bootstrap/` until batches 3–4 |
| 3 | **DUP-SECRETS-1** | Root `Aovias.Configuration/Secrets/SecretsSettings.cs` + empty parents | Low (duplicate) | Canonical tracked + hash match confirmed |
| 4 | **BATCH-REVIEW-4** + delete | `Aovias.Bootstrap/bootstrap-reports/` | Low–medium | Optional archive |
| 5 | **WEB-MIGRATE** | Copy (not overwrite) 3 env examples + release spec into `AOVIAS.Website/` | Medium | **Separate commit** on `main`; human content review |
| 6 | **ARCHIVE-CP-RELEASE** | Root `ComplaincePlatform/release/`, `scripts/release/` | Medium | Off-repo or docs attachment |
| 7 | **ARCHIVE-WA-PLAN** | `WebApp/.planning/release/` | Medium | Optional |
| 8 | **BATCH-REVIEW-3** + **BATCH-TREE-5** | Entire `AOVIAS Website/AOVIAS Web/` | Medium | After WEB-MIGRATE or archive of 8 files; **never** delete parent **67** tracked |
| 9 | **BATCH-REVIEW-2** + **BATCH-TREE-2** | Entire root `WebApp/` | Medium | After archive |
| 10 | **BATCH-REVIEW-1** + **BATCH-TREE-1** | Entire root `ComplaincePlatform/` | Higher | Last; largest volume |

**Not in scope:** `git clean`, wildcards, or doc-only fixes (separate W0 doc alignment for `docs/` root path prose).

---

## 5. Explicit approval checklist (human)

- [ ] **LOG-1** — delete single agent log path (exact)  
- [ ] **SHELL-AOV-1** — delete enumerated empty root `Aovias.*` dirs (list in §3.6)  
- [ ] **DUP-SECRETS-1** — delete root duplicate `SecretsSettings.cs` after canonical confirmed  
- [ ] **BATCH-REVIEW-4** — bootstrap logs archive/disposition  
- [ ] **WEB-MIGRATE** — integrate website env templates + optional release spec (commit)  
- [ ] **ARCHIVE-CP-RELEASE** — platform release folder + scripts before tree delete  
- [ ] **ARCHIVE-WA-PLAN** — WebApp planning docs before tree delete  
- [ ] **BATCH-REVIEW-3** + **BATCH-TREE-5** — nested `AOVIAS Website/AOVIAS Web/`  
- [ ] **BATCH-REVIEW-2** + **BATCH-TREE-2** — root `WebApp/`  
- [ ] **BATCH-REVIEW-1** + **BATCH-TREE-1** — root `ComplaincePlatform/`  

**Deny by default:** Any whole-tree delete without its **REVIEW** batch; any change to **67** tracked `AOVIAS Website/` siblings; any delete of **`AOVIAS_PHASE*`** reports.

---

## 6. Unresolved questions (retain until decided)

| ID | Question | Default |
|----|----------|---------|
| **Q-1** | Are root CP `release/1.0.0` SBOM/checksums authoritative history or reproducible? | **Retain** until archived |
| **Q-2** | Should `generate-checksums.ps1` / `generate-sbom.ps1` be ported into Command tree before root CP delete? | **Defer** — port vs archive |
| **Q-3** | Are WebApp RC certification markdowns needed for compliance evidence? | **Archive for review** |
| **Q-4** | Should website ZIPs be kept off-repo indefinitely? | **Archive for review** |
| **Q-5** | Batch update tracked `docs/` root `ComplaincePlatform/` paths to `AOVIAS.Command/...`? | **Defer** — separate doc task |
| **Q-6** | After **DUP-SECRETS-1**, remove empty root `Aovias.Configuration/`? | **Yes** when empty — part of SHELL cleanup |

---

## 7. Dependency / runtime risks (summary)

| Risk | Severity | Mitigation |
|------|----------|------------|
| Deleting root CP/WebApp before Command tree validated in CI | Medium | Keep canonical W4 paths; defer TREE batches until human validation window |
| Deleting env examples before WEB-MIGRATE | High | Migrate or archive first |
| Deleting root `SecretsSettings` before canonical verified | Low (hashes match) | **DUP-SECRETS-1** checklist |
| Confusing `docs/` prose with disk layout | Low | Doc alignment deferred (Q-5) |

---

## 8. References used

- `AOVIAS_PHASE1IIZ_MAIN_WORKTREE_CLEANUP_PLAN.md` (batch IDs)  
- `AOVIAS_PHASE1IJB_LEFTOVER_CONTENT_REVIEW.md`, `AOVIAS_PHASE1IJC_*`, `AOVIAS_PHASE1IJD/IJE` (P0 recovery)  
- `AOVIAS_PHASE1IJF/IJG/IJH` (empty-shell progress)  
- Live `git ls-files`, filesystem counts, `dotnet build` history from 1I-JD/JE  

---

## 9. Artifact

- **Created (untracked):** `AOVIAS_PHASE1IJI_LEGACY_TREE_DISPOSITION_REPORT.md` — **not staged**

No repository content was modified in Phase 1I-JI.
