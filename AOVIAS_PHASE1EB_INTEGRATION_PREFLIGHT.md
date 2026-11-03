# AOVIAS Phase 1E-B — Integration Preflight (Phase A)

**Phase:** 1E-B Phase A only (read-only; no staging, commit, merge, or ff-only)  
**Audit date:** 2026-10-25  
**Expected baseline HEAD:** `5a1526d99a68241d40ccba759d8fc62586f4af86`  
**Runtime probe:** Not executed.

---

## 1. Worktree baseline and preservation

| Worktree | Path | Branch (actual) | HEAD (actual) | Matches expected? |
|----------|------|-----------------|---------------|-------------------|
| Main | `V:\DevelopmentRepo\AOVIAS` | `main` | `5a1526d99a68241d40ccba759d8fc62586f4af86` | Yes |
| Cursor | `V:\DevelopmentRepo\AOVIAS.Cursor` | `agent/cursor/active` | `5a1526d99a68241d40ccba759d8fc62586f4af86` | Yes |
| Claude | `V:\DevelopmentRepo\AOVIAS.Claude` | `agent/claude/active` | `5a1526d99a68241d40ccba759d8fc62586f4af86` | Yes |

**Upstream:** Not revalidated (PowerShell `@{upstream}` quoting avoided); no integration action taken in this phase.

**Main tracked changes (foundation-related only):**

- `M` `Aovias.Publishing/src/Aovias.Publishing.Core/Discovery/PlatformRootResolver.cs`
- `M` `ComplianceServiceManager/Services/RootPathResolver.cs`

**Main untracked (foundation bundle):** `repository-layout.json`, `Aovias.Configuration/Layout/` (8 files), `Aovias.Configuration/tests/.../Layout/` (3 files), `AOVIAS.AccredaOne/README.md`.

**Preservation (verified on disk):**

| Artifact | Status |
|----------|--------|
| Cursor `scripts/dev/_phase3h_runtime_probe.py` | Present, untracked |
| Claude CommandCenter `Properties/` (incl. launchSettings) | Present, untracked |
| Main website ZIPs | Untracked in `git status` |
| Main Lighthouse `.lighthouseci-*` dirs | Untracked |
| Phase 0/1 reports on Main | Untracked; not part of foundation bundle |

**Agent worktrees:** No foundation files on disk (no manifest, no `Layout/`). Expected until post-commit ff-only.

**Index:** Empty staged set (`git diff --cached --stat` produced no output).

---

## 2. Repository policy determination

From `AGENTS.md`:

- **Human** integrates on `V:\DevelopmentRepo\AOVIAS` / `main` (merge, validation).
- **Agents** must not use Main as a development worktree; product work normally lands via **agent task branch → human merge → ff-only** agent worktrees.
- **Direct commit on `main`:** Permitted for **human integration**, not as default agent behavior without explicit assignment.

**Permitted integration paths (after gates pass and explicit approval):**

- **Path A (human):** Stage and commit on `main` at `V:\DevelopmentRepo\AOVIAS`.
- **Path B (agent):** Copy approved paths into `V:\DevelopmentRepo\AOVIAS.Cursor` on `agent/cursor/<task-slug>`, commit, human `merge --no-ff` into `main`, then ff-only Cursor/Claude (Phase D, separately authorized).

Phase A did **not** create or switch branches.

---

## 3. Foundation path inventory (reconciled)

### 3.1 Nomenclature vs counts

| Label | Count | Notes |
|-------|-------|-------|
| Prompt / 1E-A “16-path bundle” | **16** (claimed) | Arithmetic does not match file list. |
| `AOVIAS_PHASE1EA_STAGING_PROPOSAL.md` §1.1 “14 paths” | **13** listed | Header typo: lists 8 layout + 3 tests + 2 resolvers = **13**. |
| **Actual distinct foundation files on disk** | **15** | 8 + 3 + 1 manifest + 2 resolvers + 1 README. |

**Reconciliation:** Every path in the staging proposal is present; **no extra** files under `Layout/` or `Layout/` tests (exactly 8 and 3). **No missing** foundation file. The mismatch is **documentation arithmetic only** (16 vs 15 vs “14”), not a missing or extra source file.

### 3.2 Exact approved foundation paths (15 files)

| # | Path | Main status | In bundle |
|---|------|-------------|-----------|
| 1 | `Aovias.Configuration/Layout/LegacyCsmPlatformRootDiscovery.cs` | `??` | Yes |
| 2 | `Aovias.Configuration/Layout/LegacyPublishingPlatformRootDiscovery.cs` | `??` | Yes |
| 3 | `Aovias.Configuration/Layout/RepositoryLayoutLoader.cs` | `??` | Yes |
| 4 | `Aovias.Configuration/Layout/RepositoryLayoutManifestProvider.cs` | `??` | Yes |
| 5 | `Aovias.Configuration/Layout/RepositoryLayoutModels.cs` | `??` | Yes |
| 6 | `Aovias.Configuration/Layout/RepositoryLayoutPathResolver.cs` | `??` | Yes |
| 7 | `Aovias.Configuration/Layout/RepositoryLayoutProductionAdapter.cs` | `??` | Yes |
| 8 | `Aovias.Configuration/Layout/RepositoryLayoutSchema.cs` | `??` | Yes |
| 9 | `Aovias.Configuration/tests/Aovias.Configuration.Tests/Layout/RepositoryLayoutContractTests.cs` | `??` | Yes |
| 10 | `Aovias.Configuration/tests/Aovias.Configuration.Tests/Layout/RepositoryLayoutCsmResolverContractTests.cs` | `??` | Yes |
| 11 | `Aovias.Configuration/tests/Aovias.Configuration.Tests/Layout/RepositoryLayoutProductionAdapterTests.cs` | `??` | Yes |
| 12 | `repository-layout.json` | `??` | Yes |
| 13 | `Aovias.Publishing/src/Aovias.Publishing.Core/Discovery/PlatformRootResolver.cs` | `M` | Yes |
| 14 | `ComplianceServiceManager/Services/RootPathResolver.cs` | `M` | Yes |
| 15 | `AOVIAS.AccredaOne/README.md` | `??` | Yes |

---

## 4. Manifest compatibility note verification

**Claim under review:** `compatibilityConstraints[0]` was corrected to reflect adapter integration.

**Actual content (2026-10-25):**

```text
"Production RootPathResolver and PlatformRootResolver are not wired to this file in Phase 1C-A."
```

**Code reality on Main working tree:**

- `PlatformRootResolver.Resolve` → `RepositoryLayoutProductionAdapter.ResolvePublishingPlatformRoot`
- `RootPathResolver.ResolvePlatformRoot` → `RepositoryLayoutProductionAdapter.ResolveCsmPlatformRoot`

**Result:** The compatibility note is **still stale** and **contradicts** integrated resolvers. This is an implementation/manifest **documentation inconsistency** (not a runtime test failure).

**Other manifest checks (pass):**

- `schemaVersion`: `1.0.0` ↔ `RepositoryLayoutSchema`
- `discovery.current.platformRoot`: `requiredAll` [`aovias-config`], `requiredAny` [`ComplaincePlatform`, `Aovias.Runtime.Agent`] ↔ legacy publishing + loader validation
- `discovery.current.csmPlatformRoot`: `requiredAll` [`aovias-config`, `Aovias.Runtime.Agent`] ↔ legacy CSM
- Production adapter uses **Current** profile only in resolution entry points; Target used in contract tests only
- No secrets in manifest (grep)

---

## 5. Resolver and scope verification

**Resolver diffs:** Only platform-root delegation + `using Aovias.Configuration.Layout`; Publishing **configuredRoot** precedence unchanged; CSM other methods unchanged.

**AccredaOne README:** Scaffold only; no live API claim.

**KnowledgeBase:** No moves; `ComplaincePlatform/KnowledgeBase` unchanged on disk.

**Out of scope confirmed absent from foundation paths:** Dockerfiles, Compose, CI workflows, solution files, dependency/csproj edits, W1 `aovias-config` move, generated `bin/`/`obj/` staging.

**Untracked review note:** Ordinary `git diff` covers only the two `M` files; untracked foundation files were verified by direct file inspection and existence/status table above.

---

## 6. Fresh validation (this run)

| # | Command | Result |
|---|---------|--------|
| 1 | `dotnet test V:\DevelopmentRepo\AOVIAS\Aovias.Configuration\tests\Aovias.Configuration.Tests\Aovias.Configuration.Tests.csproj --filter "FullyQualifiedName~Layout" --no-restore` | **Passed 29/29** |
| 2 | `dotnet test V:\DevelopmentRepo\AOVIAS\Aovias.Publishing\tests\Aovias.Publishing.Core.Tests\Aovias.Publishing.Core.Tests.csproj --filter "FullyQualifiedName~PublishingProjectPaths"` | **Passed 2/2** |
| 3 | `dotnet build V:\DevelopmentRepo\AOVIAS\Aovias.Publishing\src\Aovias.Publishing.Core\Aovias.Publishing.Core.csproj -c Release` | **Succeeded** (0 errors, 0 warnings) |

No dependency install; no unrelated baseline suites.

---

## 7. `git diff --check`

On tracked modifications only:

```text
git -C V:\DevelopmentRepo\AOVIAS diff --check
```

**Exit code 0** — no whitespace/conflict marker issues reported on tracked diff.

---

## 8. Explicitly excluded from foundation staging

- All Phase 0/1 markdown reports (including this preflight report until separately approved)
- Website ZIPs, Lighthouse directories
- Cursor runtime probe, Claude `launchSettings.json`
- Any path outside the 15-file table in §3.2

---

## 9. Blockers

| ID | Severity | Issue |
|----|----------|-------|
| B-1 | **Blocking** | `repository-layout.json` `compatibilityConstraints[0]` still states resolvers are not wired; contradicts code. Reported correction **not** present on disk. |
| B-2 | **Informational** | “16-path” label vs **15** actual files; proposal §1.1 “14 paths” vs **13** listed. File set is complete; fix labels or update manifest in a scoped edit before commit. |

Per Phase 1E-B §3: stop before staging when manifest and source are inconsistent. **No staging or commit in Phase A.**

---

## 10. Phase A status

**BLOCKED — IMPLEMENTATION OR TEST FAILURE**

(Blocking reason: manifest/code inconsistency B-1; tests and Release build passed.)

---

## 11. Required before Phase B / Phase C

1. **Human decision:** Update `compatibilityConstraints[0]` (and optionally proposal path counts) in a **separately reviewed** edit, **or** explicitly accept committing with stale manifest text (not recommended; contradicts §3 gate).
2. Re-run Phase A manifest check or full preflight refresh after fix.
3. **Separate explicit authorization** for: (a) staging the 15 paths individually, (b) one atomic commit, (c) integration path (Main human commit vs agent task branch), (d) Phase D ff-only separately.

**Phase D:** Not authorized in this document.
