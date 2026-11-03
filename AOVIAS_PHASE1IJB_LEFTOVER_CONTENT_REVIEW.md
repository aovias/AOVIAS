# AOVIAS Phase 1I-JB — Leftover Content Review

**Phase:** 1I-JB (read-only disposition — **no** moves, archives, deletions, or edits)  
**Date:** 2026-10-25  
**Worktree:** `V:\DevelopmentRepo\AOVIAS`  
**References:** `AOVIAS_PHASE1IIZ_MAIN_WORKTREE_CLEANUP_PLAN.md`, `AOVIAS_PHASE1IJA_GENERATED_ARTIFACT_CLEANUP_REPORT.md`, `AOVIAS_PHASE1IIY_MAIN_WORKTREE_LAYOUT_RECONCILIATION.md`

---

## Final status

**`REVIEW COMPLETE — DISPOSITION READY FOR APPROVAL`**

All major leftover areas were inspected at file or bucket level. **One item requires explicit human investigation before any root `Aovias.*` tree deletion:** untracked `Aovias.Configuration/Secrets/SecretsSettings.cs` (see §4). No secrets or credential **values** were printed; environment **example** files were checked for presence and structure only.

---

## 1. Baseline verification

| Check | Expected | Actual | Result |
|-------|----------|--------|--------|
| Worktree | `V:\DevelopmentRepo\AOVIAS` | Same | **PASS** |
| Branch | `main` | `main` | **PASS** |
| HEAD | `bc1365dc9dc0149ef46f5b66a828501387eae9dc` | Same | **PASS** |
| Tracked working tree | Clean | **0** modified/staged tracked paths | **PASS** |
| Tracked file count | **8228** | **8228** | **PASS** |
| Reference reports (3) | Present | All **True** | **PASS** |

### Remaining file counts (post–1I-JA, measured this phase)

| Location | Files (recursive) | Notes |
|----------|-------------------|--------|
| `ComplaincePlatform/` (root) | **977** | Unchanged vs 1I-JA post-GEN-4 expectation |
| `WebApp/` (root) | **124** | Unchanged vs 1I-JA |
| `AOVIAS Website/AOVIAS Web/` | **8** | Was 8 in 1I-JA; **+1** doc path now explicitly listed |
| `ComplianceServiceManager/` (+ Operations*, Tests*) | **0** each | Empty directory shells may remain |
| `tools/` (root) | **0** | Empty shell (~2 items) |
| `preflight-docker-probe/` | **0** | Empty |
| `Aovias.Bootstrap/` | **15** | All under `bootstrap-reports/` |
| `Aovias.Configuration/` | **1** | See §4 |
| `Aovias.Runtime.Agent/` | **1** | Log file only |
| Other root `Aovias.*` | **0** | Empty shells after GEN-2 |
| Root `AOVIAS_PHASE*` files | **65** | Audit deliverables — **protected** |

---

## 2.A — Nested website: `AOVIAS Website/AOVIAS Web/` (8 files)

**Tracked canonical site:** `AOVIAS.Website/` (**2274** tracked paths).  
**Protected siblings:** **67** tracked paths under `AOVIAS Website/` (not under `AOVIAS Web/`).

| # | Relative path | Size | Type | Classification | Counterpart under `AOVIAS.Website/` | Disposition |
|---|---------------|------|------|----------------|--------------------------------------|-------------|
| 1 | `AOVIAS Website/AOVIAS Web/AOVIAS.Website.v1.0.0.zip` | ~23.1 MB | Release archive | Archive / build artifact | **None** (no `.zip` found under `AOVIAS.Website/` in spot check) | **ARCHIVE** — optional off-repo retention before delete |
| 2 | `AOVIAS Website/AOVIAS Web/AOVIAS.Website.v1.1.0.zip` | ~23.6 MB | Release archive | Same | **None** | **ARCHIVE** |
| 3 | `AOVIAS Website/AOVIAS Web/apps/website/next-env.d.ts` | 262 B | TypeScript env stub | **Generated** (Next.js) | Path **missing** at `AOVIAS.Website/apps/website/next-env.d.ts` on disk (regenerates on build) | **DELETE CANDIDATE** |
| 4 | `AOVIAS Website/AOVIAS Web/apps/website/tsconfig.tsbuildinfo` | ~318 KB | TS incremental build cache | **Generated** | **Absent** at canonical app path | **DELETE CANDIDATE** |
| 5 | `AOVIAS Website/AOVIAS Web/deployment/environments/.env.preview.example` | 450 B | Env **template** (placeholders) | Example / template | **No** matching path; canonical has `AOVIAS.Website/apps/website/.env.example` only (different layout, **48** lines vs **28** for production example here) | **REVIEW REQUIRED** — may be **ARCHIVE** if multi-env templates are still desired |
| 6 | `AOVIAS Website/AOVIAS Web/deployment/environments/.env.production.example` | 923 B | Env template | Example | **No** same path under `AOVIAS.Website/` | **REVIEW REQUIRED** / **ARCHIVE** |
| 7 | `AOVIAS Website/AOVIAS Web/deployment/environments/.env.staging.example` | 507 B | Env template | Example | **No** same path | **REVIEW REQUIRED** / **ARCHIVE** |
| 8 | `AOVIAS Website/AOVIAS Web/docs/release/AOVIAS-Release-Specification-v1.0.md` | ~26 KB | Documentation | Meaningful doc | **No** file match under `AOVIAS.Website/` (glob) | **ARCHIVE** or **KEEP** until copied into tracked `AOVIAS.Website/docs/` if still authoritative |

**Evidence:** Hash compare not applicable for most pairs (canonical paths absent). `.env` **values** were not read; only line counts and header comments were inspected.

**Batch mapping:** **WEB-B** (review) then **WEB-C** (remove shell) per 1I-IZ — **not authorized in 1I-JB**.

---

## 2.B — Root `ComplaincePlatform/` (977 files)

**Canonical tracked tree:** `AOVIAS.Command/ComplaincePlatform/` (**3157** tracked; **46** `.csproj` files).

**Index:** `git ls-files ComplaincePlatform/` → **0**.  
**Path overlap:** Sample of **400** non-`.cursor`/non-`.planning` files — **0** had an same-relative-path file under `AOVIAS.Command/ComplaincePlatform/` (leftover tree is **not** a path-aligned duplicate).

### Bucket summary (by top-level area / pattern)

| Bucket | ~Files | Category | Disposition |
|--------|--------|----------|-------------|
| `logs/` | **419** | Generated logs (`.gitignore` `**/logs/`, `**/*.log`) | **DELETE CANDIDATE** |
| `artifacts/build-verify/` | **235** | Build verification output (`.deps.json`, `appsettings*.json`, `deployment.metadata.json` copies) | **DELETE CANDIDATE** |
| `.planning/` | **210** | Local planning / agent workflow | **ARCHIVE** or **DELETE CANDIDATE** after confirm |
| `.cursor/` | **85** | IDE/agent metadata | **DELETE CANDIDATE** |
| `release/` (incl. `1.0.0/`) | **6** + nested | Release notes, checksums, SBOM, manifest JSON | **ARCHIVE** if not reproduced elsewhere; else **DELETE CANDIDATE** |
| Partial service folders (`Gateway/`, `IdentityService/`, etc.) | **≤5** each | Residual configs without root `.csproj` (**0** `.csproj` in entire root tree) | **DELETE CANDIDATE** — not a source tree |
| `.vscode/` | **2** | `launch.json`, `tasks.json` | **DELETE CANDIDATE** (local IDE) |
| `scripts/release/` | **2** | `generate-checksums.ps1`, `generate-sbom.ps1` | **REVIEW REQUIRED** — compare with `AOVIAS.Command/ComplaincePlatform/scripts/` before delete |
| IDE user/suo | **8** | Generated IDE | **DELETE CANDIDATE** |
| Other extensions (mixed) | **643** classed “other” in extension scan | Mostly logs/artifacts adjacent | **DELETE CANDIDATE** with batch |

### Unique / materially different meaningful files

- **No** tracked-equivalent path matches found for sampled files — leftovers are **unique to root shell**, not byte-diff duplicates of canonical files.
- **Release artifacts** at `ComplaincePlatform/release/` (e.g. `RELEASE_NOTES_v1.0.0.md`, `sbom-1.0.0.json`, `checksums-sha256.txt`) — verify whether duplicates exist under **`AOVIAS.Command/ComplaincePlatform/release/`** before **DELETE CANDIDATE** → treat as **ARCHIVE** if sole copy.
- **Operational scripts** at `ComplaincePlatform/scripts/release/*.ps1` — **REVIEW REQUIRED** against canonical Command tree scripts.

**Recommended batch:** **BATCH-REVIEW-1** then **BATCH-TREE-1** (entire root tree) per 1I-IZ — only after review sign-off.

---

## 2.C — Root `WebApp/` (124 files)

**Canonical tracked app:** `AOVIAS.Command/WebApp/` (**1368** tracked).  
**Index:** `git ls-files WebApp/` → **0**.  
**No** `angular.json`, `package.json`, or `src/app` tree at root (confirmed post–1I-JA).

| Bucket | Count | Category | Disposition |
|--------|-------|----------|-------------|
| `.cursor/` + `.claude/` | **95** | IDE/agent configs, GSD commands | **DELETE CANDIDATE** |
| `.planning/release/` | **29** | RC/release certification markdown (WebApp product) | **ARCHIVE** — local release history; **not** in tracked Command WebApp tree |

**Sample non-IDE paths (all under `.planning/release/`):**  
`WebApp/.planning/release/RC2-EXIT-REVIEW.md`, `WebApp/.planning/release/RC1/RC1-GO-NO-GO.md`, … (RC2S1/RC2S2/RC2W* certification reports).

**Unique source:** **None** identified — no application source remaining.  
**Material docs:** **29** planning/release reports — **ARCHIVE** recommended before **BATCH-TREE-2**.

---

## 2.D — Root CSM / `Aovias.*` / `tools/`

### ComplianceServiceManager* (root)

| Path | Files | Canonical | Finding |
|------|-------|-----------|---------|
| `ComplianceServiceManager/` | **0** | `AOVIAS.Studio/ComplianceServiceManager/` (**318** tracked) | Empty shell after GEN-1 — **DELETE CANDIDATE** (directory only) |
| `ComplianceServiceManager.Operations/` | **0** | Under Studio CSM | **DELETE CANDIDATE** (empty) |
| `ComplianceServiceManager.Operations.Tests/` | **0** | Under Studio CSM | **DELETE CANDIDATE** (empty) |
| `ComplianceServiceManager.Tests/` | **0** | Under Studio CSM | **DELETE CANDIDATE** (empty) |

### Root `Aovias.*` shells

| Path | Files | Content | Canonical | Disposition |
|------|-------|---------|-----------|-------------|
| `Aovias.Configuration/` | **1** | `Secrets/SecretsSettings.cs` — **C# source** (options model; no secret values in file) | **No** `SecretsSettings.cs` under `AOVIAS.Studio/Aovias.Configuration/` on disk; **not** in `git ls-files`; Studio code **references** `SecretsSettings` type | **REVIEW REQUIRED** — may be **only on-disk definition** of class; **do not delete** until tracked source location confirmed |
| `Aovias.Runtime.Agent/` | **1** | `src/.../logs/runtime-agent/runtime-agent-20260626.log` | Logs under Studio agent path (gitignored pattern) | **DELETE CANDIDATE** (generated log) |
| `Aovias.Bootstrap/` | **15** | `bootstrap-reports/*.log` demo transcripts | `AOVIAS.Studio/Aovias.Bootstrap/` tracked (**97** paths) | **ARCHIVE** or **DELETE CANDIDATE** after **BATCH-REVIEW-4** |
| `Aovias.CommandCenter`, `Deployment*`, `DesignSystem`, `Orchestration`, `Publishing`, `Release`, `ReleaseValidation`, `Runtime.Contracts` | **0** | Empty / artifact-only shells | `AOVIAS.Studio/...` or Infrastructure | **DELETE CANDIDATE** (empty dirs) — **except** review `SecretsSettings` first |

### Root `tools/`

| Path | Files | Canonical | Disposition |
|------|-------|-----------|-------------|
| `tools/` | **0** | `AOVIAS.Infrastructure/tools/` (**4** tracked) | **DELETE CANDIDATE** (empty shell after GEN-3) |

---

## 2.E — Bootstrap reports and other exceptions

| Path | Content | Purpose | Disposition |
|------|---------|---------|-------------|
| `Aovias.Bootstrap/bootstrap-reports/` | **15** × `*-demo-*-transcript.log` | Local bootstrap demo transcripts | **ARCHIVE** if history matters; else **DELETE CANDIDATE** after REVIEW-4 |
| `preflight-docker-probe/` | Empty | Manifest **O-8** unresolved | **REVIEW REQUIRED** — **DELETE CANDIDATE** (empty) or keep for future probe |
| `Aovias.Runtime.Contracts/` | Empty `Deployment/` subtree | Pre-W3 ghost | **DELETE CANDIDATE** (empty) |

---

## 2.F — Protected items verification

| Protected item | Verification | Result |
|----------------|--------------|--------|
| Tracked W1–W5 roots | `git ls-files` counts: Infra **33**, Studio **1191**, Command **4525**, Website **2274** | **PASS** |
| `AOVIAS Website/` siblings | **67** tracked paths | **PASS** |
| `AOVIAS.AccredaOne/README.md` | Present, tracked | **PASS** |
| Root `AOVIAS_PHASE*` + JSON/CSV | **65** files present | **PASS** (unchanged) |
| Reference + 1I-JA reports | Present | **PASS** |
| Canonical markers | `AOVIAS.Command/ComplaincePlatform/.dockerignore`, `AOVIAS.Command/WebApp/angular.json`, `AOVIAS.Website/apps/website/package.json` | **PASS** |

---

## 3. Paths requiring human review before deletion

| Priority | Path | Reason |
|----------|------|--------|
| **P0** | `Aovias.Configuration/Secrets/SecretsSettings.cs` | Referenced by tracked Studio code; **no** tracked counterpart file found; deleting root shell may remove **only** class definition on disk |
| **P1** | `AOVIAS Website/AOVIAS Web/deployment/environments/.env.*.example` (3 files) | Templates not present at same paths in `AOVIAS.Website/`; may need merge into tracked tree or archive |
| **P1** | `AOVIAS Website/AOVIAS Web/docs/release/AOVIAS-Release-Specification-v1.0.md` | Doc not found under canonical website tree |
| **P1** | `WebApp/.planning/release/**` (**29** files) | Release/certification history — archive vs discard |
| **P2** | `ComplaincePlatform/release/**`, `ComplaincePlatform/scripts/release/*.ps1` | Compare with Command canonical release/scripts |
| **P2** | `ComplaincePlatform/.planning/**` (**210** files) | Local planning — archive optional |
| **P2** | `AOVIAS Website/AOVIAS Web/*.zip` (2 files) | Large release archives — archive off-repo? |
| **P3** | `preflight-docker-probe/` | O-8 contract decision |

---

## 4. Approval checklist (grouped by safe cleanup batch)

Use after explicit human sign-off (maps to 1I-IZ §6).

### Pre-flight

- [ ] `main` @ `bc1365d`, tracked tree clean, **8228** paths  
- [ ] **P0** `SecretsSettings.cs` resolved (track, move to Studio, or confirm redundant)  
- [ ] No `git clean -fdx` at repo root  

### After P0/P1 review

- [ ] **BATCH-REVIEW-1** + **BATCH-TREE-1** — entire `ComplaincePlatform/` (**977** files)  
- [ ] **BATCH-REVIEW-2** + **BATCH-TREE-2** — entire `WebApp/` (**124** files; **archive `.planning`** first if desired)  
- [ ] **BATCH-REVIEW-3** — 8 files under `AOVIAS Website/AOVIAS Web/`  
- [ ] **BATCH-REVIEW-4** — `Aovias.Bootstrap/bootstrap-reports/`  
- [ ] **BATCH-TREE-3** — empty root `ComplianceServiceManager*` dirs  
- [ ] **BATCH-TREE-4** — empty root `Aovias.*` shells (**after P0**)  
- [ ] **BATCH-TREE-5** — remove `AOVIAS Website/AOVIAS Web/` only (keep parent **67** tracked)  
- [ ] **BATCH-MISC-1** — `preflight-docker-probe/`, root `tools/` empty shell  

### Default deny

- [ ] Do **not** delete tracked `AOVIAS Website/` Guidelines/assets  
- [ ] Do **not** delete `AOVIAS_PHASE*` reports without archive policy  
- [ ] Do **not** delete `AOVIAS.AccredaOne/`  

---

## 5. Disposition summary counts

| Disposition | Approx. scope |
|-------------|----------------|
| **DELETE CANDIDATE** | Root CP logs/artifacts/IDE (~900+ files), WebApp IDE (~95), empty shells, generated website ts/next files, agent log |
| **ARCHIVE** | Website zips + release spec, WebApp `.planning/release/`, CP `release/` + SBOM, bootstrap transcripts |
| **REVIEW REQUIRED** | `SecretsSettings.cs`, env example templates (3), CP scripts/release, preflight O-8 |
| **KEEP** (protected) | All **8228** tracked paths, **67** website siblings, **65** phase reports |

---

## 6. Artifact

- **Created (untracked):** `AOVIAS_PHASE1IJB_LEFTOVER_CONTENT_REVIEW.md` — **not staged**

No files were moved, archived, deleted, or edited except this report.
