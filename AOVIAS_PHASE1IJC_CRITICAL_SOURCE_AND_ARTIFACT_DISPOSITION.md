# AOVIAS Phase 1I-JC — Critical Source Recovery & Cleanup Authorization

**Phase:** 1I-JC (investigation and disposition only — **no** moves, deletes, edits, staging, or commits)  
**Date:** 2026-10-25  
**Worktree:** `V:\DevelopmentRepo\AOVIAS`  
**Branch:** `main`  
**HEAD:** `bc1365dc9dc0149ef46f5b66a828501387eae9dc`  
**Input:** `AOVIAS_PHASE1IJB_LEFTOVER_CONTENT_REVIEW.md`

---

## Executive summary

| Area | Outcome |
|------|---------|
| **P0 `SecretsSettings.cs`** | **Required for compile** of `Aovias.Configuration`; **only on-disk copy** is at **wrong path** (root leftover). **Canonical path** is established; **tracking** blocked by `.gitignore` pattern unless policy is adjusted. **Preserve** root file until an authorized recovery commit. |
| **Website env examples** | **Not redundant** — documented in tracked `AOVIAS.Website` docs but **missing** from canonical tree; **integrate or archive** before deleting nested shell. |
| **Whole-tree cleanup** | **Not authorized in 1I-JC** (per phase rules). Only **batch proposals** below for a later path-enumerated phase. |

---

## 1. Baseline verification

| Check | Result |
|-------|--------|
| Worktree `V:\DevelopmentRepo\AOVIAS` | **PASS** |
| Branch `main` | **PASS** |
| HEAD `bc1365dc9dc0149ef46f5b66a828501387eae9dc` | **PASS** |
| Tracked working tree | **Clean** (**0** modified/staged tracked paths) |
| `AOVIAS_PHASE1IJB_LEFTOVER_CONTENT_REVIEW.md` | **Present** |
| Tracked files | **8228** (`git ls-files`) |

No baseline discrepancy. Destructive work was not performed.

---

## 2. P0 — `Aovias.Configuration/Secrets/SecretsSettings.cs`

### 2.1 File identity (no secret values)

| Attribute | Value |
|-----------|--------|
| **Path (current)** | `Aovias.Configuration/Secrets/SecretsSettings.cs` (repo root — **untracked**, pre-W3 shell) |
| **Namespace** | `Aovias.Configuration.Secrets` |
| **Type** | `public sealed class SecretsSettings` |
| **Section constant** | `SecretsSettings.SectionName` → `"Secrets"` |
| **Dependencies** | `System.ComponentModel.DataAnnotations` (`[Required]` on string properties) |
| **Properties** | `ApiKey`, `JwtSigningKey`, `DatabaseConnectionString`, `DockerRegistryPassword`, `CertificatePassword`, `EncryptionKey` — **schema only**; defaults are empty strings |

### 2.2 Tracked references (consuming assembly)

All references are in **`AOVIAS.Studio/Aovias.Configuration`** (and CSM UI):

| File | Usage |
|------|--------|
| `Extensions/ModularConfigurationExtensions.cs` | `using Aovias.Configuration.Secrets`; `AddOptions<SecretsSettings>()`, `ValidateSecrets(SecretsSettings, ...)` |
| `Extensions/ServiceCollectionExtensions.cs` | `using Aovias.Configuration.Secrets` |
| `Loading/ProfileConfigurationLoader.cs` | Maps `settings.secrets.json` → `SecretsSettings.SectionName` |
| `Features/ConfigurationDependencyGraph.cs` | Module graph node for `"Secrets"` |
| `tests/.../ModularConfigurationTests.cs` | Asserts options registration behavior |
| `ComplianceServiceManager/.../SettingsConfigurationViewModel.cs` | `IOptionsMonitor<SecretsSettings>` |

**Not equivalent** to tracked `Settings/SecuritySettings.cs` (`SectionName = "Security"`; different properties — JWT issuer/audience vs dedicated secrets module).

### 2.3 Intended canonical location (W1–W5 layout)

| Item | Recommendation |
|------|----------------|
| **Project / assembly** | `AOVIAS.Studio/Aovias.Configuration/Aovias.Configuration.csproj` |
| **Logical folder** | `Secrets/` sibling to `Settings/` (matches namespace `Aovias.Configuration.Secrets` and existing usings) |
| **Canonical path** | **`AOVIAS.Studio/Aovias.Configuration/Secrets/SecretsSettings.cs`** |

**Manifest alignment:** `repository-layout.json` places `Aovias.Configuration` under **`AOVIAS.Studio/Aovias.Configuration`**.

### 2.4 Tracked equivalent?

| Check | Result |
|-------|--------|
| `git ls-files` `**/SecretsSettings.cs` | **Empty** — **never tracked** on `main` at this path |
| `git log --all -- **/SecretsSettings.cs` | **No commits** |
| Other filename/namespace | **None** — no other type defines `SecretsSettings` |
| Root file included in `.csproj` | **No** — root `Aovias.Configuration/` is **outside** the Studio project tree |

### 2.5 Compile requirement (evidence)

`dotnet build AOVIAS.Studio/Aovias.Configuration/Aovias.Configuration.csproj` **after restore** fails with:

- `CS0234`: namespace `Secrets` does not exist in `Aovias.Configuration`
- `CS0246`: `SecretsSettings` not found

**Conclusion:** The **type definition must exist under the Studio project** for the tracked project to compile. The root leftover file is **not** part of the build; it is a **misplaced local copy** that happens to be the **only** definition on disk in this worktree.

### 2.6 `.gitignore` conflict (tracking blocker)

`git check-ignore -v AOVIAS.Studio/Aovias.Configuration/Secrets/SecretsSettings.cs` → rule **`.gitignore:78:**/secrets/`** (matches on Windows case-insinsitively).

**Implication:** Even after moving content to the canonical path, **`git add` would ignore the file** unless:

- `.gitignore` is amended (e.g. negate `!AOVIAS.Studio/Aovias.Configuration/Secrets/*.cs` or rename folder to avoid `secrets/`), or  
- Source is relocated to a non-ignored path (e.g. `Settings/SecretsSettings.cs` with optional namespace change).

### 2.7 P0 disposition (1I-JC)

| Classification | **`REVIEW REQUIRED` → recovery action needed (separate authorized phase)** |
|----------------|-----------------------------------------------------------------------------|
| **Delete root file?** | **No** — preserve until canonical copy exists and builds |
| **Move/stage in 1I-JC?** | **Forbidden** by phase boundaries |
| **Human decision** | (1) Confirm `Secrets/` vs `Settings/` placement; (2) `.gitignore` exception vs folder rename; (3) **commit** recovered source to `main` via normal integration |

**Runtime note:** Consumption binds **`settings.secrets.json`** (local JSON, gitignored under `**/secrets/` dirs) — distinct from **this C# options class** (should be tracked source).

---

## 3. Environment and release artifacts

### 3.1 Website — `AOVIAS Website/AOVIAS Web/`

| Path | Size (approx.) | Role | Canonical counterpart | Unique? | Classification |
|------|----------------|------|------------------------|---------|----------------|
| `deployment/environments/.env.preview.example` | 450 B | Env **template** | **Missing** — docs cite `deployment/environments/.env.preview.example` (`r1.8-environment-configuration-guide.md`) but **`AOVIAS.Website/deployment/environments/` does not exist** on disk | **Yes** (path + template set) | **ARCHIVE** then **integrate** into `AOVIAS.Website/deployment/environments/` in authorized commit — **do not overwrite** `apps/website/.env.example` |
| `deployment/environments/.env.production.example` | 923 B | Env template | Same gap; compose references `../environments/.env.production` | **Yes** (broader key set vs single `.env.example` — structural diff only) | **ARCHIVE** / **integrate** |
| `deployment/environments/.env.staging.example` | 507 B | Env template | Same gap | **Yes** | **ARCHIVE** / **integrate** |
| `docs/release/AOVIAS-Release-Specification-v1.0.md` | ~26 KB | Documentation | **No** matching filename under `AOVIAS.Website/docs/` (glob) | **Likely unique** | **ARCHIVE** or copy into tracked docs if still authoritative |
| `AOVIAS.Website.v1.0.0.zip` | ~23 MB | Release **archive** | **None** in tracked tree | **Yes** | **ARCHIVE** off-repo optional; **DELETE CANDIDATE** after archive |
| `AOVIAS.Website.v1.1.0.zip` | ~24 MB | Release archive | **None** | **Yes** | Same |
| `apps/website/next-env.d.ts` | 262 B | **Generated** (Next) | Regenerates on build; listed in `AOVIAS.Website/.gitignore` | N/A | **DELETE CANDIDATE** (with nested shell cleanup later) |
| `apps/website/tsconfig.tsbuildinfo` | ~318 KB | **Generated** | Same | N/A | **DELETE CANDIDATE** |

**Env inspection:** Only **key names** were compared; **no values** printed. Leftover preview/staging templates are **subsets** of canonical `.env.example` key set; production template has **1** key not in canonical sample (structural difference only).

**Required?** Templates are **implied required** by tracked website documentation and compose layout, but **absent from canonical tree** — **repository gap**, not disposable without recovery plan.

### 3.2 Root `ComplaincePlatform/release/` and `scripts/release/`

| Path | Content | Canonical `AOVIAS.Command/ComplaincePlatform/` | Classification |
|------|---------|-----------------------------------------------|----------------|
| `ComplaincePlatform/release/1.0.0/*` | CHANGELOG, RELEASE_NOTES, checksums, SBOM JSON, manifest JSON/schema | **No** `release/1.0.0/` tree found at Command path (spot check) | **Historical / unique** — **ARCHIVE** before tree delete |
| `ComplaincePlatform/scripts/release/generate-checksums.ps1` | Script | **Not present** at `AOVIAS.Command/ComplaincePlatform/scripts/release/` | **Unique** — **REVIEW REQUIRED** (port to Command or archive) |
| `ComplaincePlatform/scripts/release/generate-sbom.ps1` | Script | **Not present** at canonical path | **Unique** — **REVIEW REQUIRED** |

**Reproducible:** Scripts may be re-runnable; SBOM/checksums are **point-in-time artifacts**.

### 3.3 `WebApp/.planning/release/` (29 files)

| Content | Canonical `AOVIAS.Command/WebApp/` | Classification |
|---------|-----------------------------------|----------------|
| RC/RC2 certification, go/no-go, runtime validation markdown | **Not** application source; **not** in tracked WebApp tree | **Historical** — **ARCHIVE** recommended; **DELETE CANDIDATE** after archive |

### 3.4 `Aovias.Bootstrap/bootstrap-reports/` (15 files)

| Content | Canonical `AOVIAS.Studio/Aovias.Bootstrap/` | Classification |
|---------|---------------------------------------------|----------------|
| Demo transcript `.log` files | Tracked bootstrap product **without** these logs | **Historical / local** — **ARCHIVE** or **DELETE CANDIDATE** after **BATCH-REVIEW-4** |

---

## 4. Cleanup authorization (1I-JC scope)

### 4.1 Explicitly **not** authorized in 1I-JC (per assignment)

- **BATCH-TREE-1** — entire root `ComplaincePlatform/`
- **BATCH-TREE-2** — entire root `WebApp/`
- **BATCH-TREE-5** — entire `AOVIAS Website/AOVIAS Web/`
- **BATCH-TREE-4** — any root `Aovias.*` removal **including** `Aovias.Configuration/` until P0 recovery is committed
- **BATCH-REVIEW-*** / **BATCH-TREE-*** sign-off — **not** executed; recommendations only

**No cleanup batches were executed in this phase.**

### 4.2 Batches **safe to propose** for a **future path-enumerated** phase

*(Proposal only — not approval. Human must check boxes in 1I-IZ §6 or equivalent.)*

| Batch / action | Proposal | Preconditions |
|----------------|----------|----------------|
| **P0-RECOVER** (new) | Add `SecretsSettings.cs` under Studio + `.gitignore`/path policy + **commit** | Human architecture decision on §2.6 |
| **BATCH-MISC-1** (partial) | Remove **empty** `preflight-docker-probe/`, root `tools/` shell | Confirm still 0 files; O-8 decision for probe |
| **BATCH-TREE-3** | Remove **empty** root `ComplianceServiceManager*` dirs | Already 0 files post–GEN-1 |
| **Path-specific** | `Aovias.Runtime.Agent/.../runtime-agent-20260626.log` | Confirm log only — **DELETE CANDIDATE** |
| **Path-specific** | Empty root `Aovias.*` dirs **except** `Aovias.Configuration/` | After P0 recovery |
| **WEB-INTEGRATE** (new) | Copy (not overwrite) env examples + release spec into **`AOVIAS.Website/`** tracked paths | Separate commit; then **BATCH-REVIEW-3** |
| **ARCHIVE-OFFREPO** (new) | ZIPs, CP `release/1.0.0`, WebApp `.planning/release/`, bootstrap logs | Before **TREE-1/2/5** |

### 4.3 Batches **not** safe to propose until reviews complete

| Batch | Blocker |
|-------|---------|
| **BATCH-TREE-1** | Unique `release/` + scripts; 977 non-artifact paths |
| **BATCH-TREE-2** | 29 planning docs + 95 IDE files — archive decision |
| **BATCH-TREE-5** | Env templates + release spec + zips (**§3.1**) |
| **BATCH-TREE-4** (full) | **P0** root `SecretsSettings.cs` |

**BATCH-GEN-1…5:** Already executed in **1I-JA** — not repeated.

---

## 5. Human decisions required

| ID | Decision |
|----|----------|
| **D-P0-1** | Approve canonical path: `AOVIAS.Studio/Aovias.Configuration/Secrets/SecretsSettings.cs` vs merge into `Settings/` |
| **D-P0-2** | Approve `.gitignore` exception or folder rename so **source** is trackable while **secret JSON dirs** stay ignored |
| **D-P0-3** | Authorize **recovery commit** on `main` (human integration) — not agent cleanup |
| **D-WEB-1** | Restore `deployment/environments/.env.*.example` under **`AOVIAS.Website/`** vs archive-only |
| **D-WEB-2** | Disposition of **ZIP** release archives |
| **D-CP-1** | Port or discard root-only **release scripts** and **1.0.0** release artifacts |
| **D-WA-1** | Archive **WebApp** RC certification folder or discard |
| **D-BOOT-1** | Bootstrap demo transcripts — archive or discard |
| **D-O8** | `preflight-docker-probe/` empty dir |

---

## 6. Compilation and runtime risks

| Risk | Severity | Notes |
|------|----------|-------|
| **`Aovias.Configuration` does not build** on clean tree | **High** | Proven CS0234/CS0246 without `SecretsSettings.cs` in project |
| **Deleting root `Aovias.Configuration/` before recovery** | **High** | Removes only on-disk type definition in this worktree |
| **Website prod compose** references missing `deployment/environments/.env.production` | **Medium** | Documented layout incomplete in tracked tree |
| **CSM / agent consumers** of `IOptionsMonitor<SecretsSettings>` | **Medium** | Fail at runtime/DI if assembly builds from an outdated package without secrets module |
| **Ignoring `Secrets/` via gitignore** | **High** for recovery | Risk of repeated “lost” source if not policy-fixed |

---

## 7. Final status (phase)

**Investigation complete.** P0 is **resolved for intent** (canonical location + compile dependency + gitignore blocker); **recovery is not done** — root file **preserved**.

**Cleanup authorization for 1I-JC:** **None executed** — whole-tree and ambiguous shells remain **blocked** until **P0-RECOVER** and **WEB/CP/WA** human decisions.

---

## 8. Artifact

- **Created (untracked):** `AOVIAS_PHASE1IJC_CRITICAL_SOURCE_AND_ARTIFACT_DISPOSITION.md` — **not staged**

No files were moved, copied, archived, deleted, or edited except this report.
