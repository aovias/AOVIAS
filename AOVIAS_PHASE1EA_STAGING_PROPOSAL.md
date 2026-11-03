# AOVIAS Phase 1E-A — Foundation Staging Proposal

**Phase:** 1E-A (proposal only — no `git add`, no commit)  
**Date:** 2026-10-25  
**Baseline HEAD:** `5a1526d99a68241d40ccba759d8fc62586f4af86`

---

## 1. Proposed commit scope (atomic foundation)

Stage **all** of the following in **one** commit (or one merge commit). Partial staging of resolver files without `Layout/` is **forbidden**.

### 1.1 Required source and tests (14 paths)

```
Aovias.Configuration/Layout/LegacyCsmPlatformRootDiscovery.cs
Aovias.Configuration/Layout/LegacyPublishingPlatformRootDiscovery.cs
Aovias.Configuration/Layout/RepositoryLayoutLoader.cs
Aovias.Configuration/Layout/RepositoryLayoutManifestProvider.cs
Aovias.Configuration/Layout/RepositoryLayoutModels.cs
Aovias.Configuration/Layout/RepositoryLayoutPathResolver.cs
Aovias.Configuration/Layout/RepositoryLayoutProductionAdapter.cs
Aovias.Configuration/Layout/RepositoryLayoutSchema.cs
Aovias.Configuration/tests/Aovias.Configuration.Tests/Layout/RepositoryLayoutContractTests.cs
Aovias.Configuration/tests/Aovias.Configuration.Tests/Layout/RepositoryLayoutCsmResolverContractTests.cs
Aovias.Configuration/tests/Aovias.Configuration.Tests/Layout/RepositoryLayoutProductionAdapterTests.cs
Aovias.Publishing/src/Aovias.Publishing.Core/Discovery/PlatformRootResolver.cs
ComplianceServiceManager/Services/RootPathResolver.cs
```

### 1.2 Required layout manifest (1 path)

```
repository-layout.json
```

Must live at **repository root** in Git so `RepositoryLayoutManifestProvider` upward search from build/test output finds it after clone/ff-only.

### 1.3 AccredaOne scaffold (1 path)

```
AOVIAS.AccredaOne/README.md
```

Recommend **including in the same foundation commit** so manifest product entry and on-disk scaffold stay aligned. Safe to omit only if human wants a code-only commit; not recommended.

---

## 2. Reports — retain in repository history

Recommend **one or two documentation commits** after the foundation code commit (keeps code review diff focused). If a single merge is preferred, add these in the same PR but **after** the code paths in the diff narrative.

### 2.1 Strongly recommended (directly support foundation and W1 gate)

| File | Rationale |
|------|-----------|
| `AOVIAS_PHASE1CA_LAYOUT_CONTRACT.md` | Contract definition for manifest and loader. |
| `AOVIAS_PHASE1CA_TEST_REPORT.md` | Test evidence baseline (supersede counts with 1E-A fresh run). |
| `AOVIAS_PHASE1CB_COMPATIBILITY_CONTRACT.md` | Publishing integration contract. |
| `AOVIAS_PHASE1CB_IMPLEMENTATION_REPORT.md` | Publishing wiring evidence. |
| `AOVIAS_PHASE1CC_CSM_COMPATIBILITY_CONTRACT.md` | CSM marker distinction. |
| `AOVIAS_PHASE1CC_IMPLEMENTATION_REPORT.md` | CSM wiring evidence. |
| `AOVIAS_PHASE1DA_ACCREDAONE_SCAFFOLD_REPORT.md` | Scaffold scope boundary. |
| `AOVIAS_PHASE1DB_READINESS_REPORT.md` | W1 gate status. |
| `AOVIAS_PHASE1DB_W1_DEPENDENCY_CLOSURE.md` | W1 dependency audit. |
| `AOVIAS_PHASE1DB_W1_EXECUTION_AND_ROLLBACK_PLAN.md` | W1 execution plan. |
| `AOVIAS_PHASE1EA_FOUNDATION_REVIEW.md` | This phase review. |
| `AOVIAS_PHASE1EA_STAGING_PROPOSAL.md` | This staging plan. |

### 2.2 Recommended (migration program context)

| File | Rationale |
|------|-----------|
| `AOVIAS_PHASE1B_MIGRATION_CONTRACT.md` | Wave model and ownership references in manifest. |
| `AOVIAS_PHASE1B_OWNERSHIP_AND_EVIDENCE.md` | O-1/O-2 evidence. |
| `AOVIAS_PHASE1A_READINESS_REPORT.md` | Phase 1 entry decision record. |

### 2.3 Optional second batch (large / baseline; avoid bloating foundation PR)

| File | Rationale |
|------|-----------|
| `AOVIAS_PHASE1A_LAYOUT_MANIFEST.md`, `AOVIAS_PHASE1A_PATH_DEPENDENCY_AUDIT.md` | Pre-1C-A inventory; partially superseded by manifest file. |
| `AOVIAS_PHASE1B_COMPLETE_PATH_INVENTORY.md` | Large inventory snapshot at 1B time. |
| `AOVIAS_PHASE0_BASELINE_VALIDATION.md`, `AOVIAS_PHASE0_MIGRATION_READINESS.md` | Baseline build matrix (historical). |
| `AOVIAS_PHASE0C_WORKTREE_PRESERVATION.md`, `AOVIAS_PHASE0D_NORMALIZATION.md`, `AOVIAS_PHASE0D_NORMALIZATION_EXECUTION.md` | Git normalization audit trail. |
| `AOVIAS_GIT_PATH_ANOMALY_REPORT.md` | Resolved anomaly; archival value. |

---

## 3. Reports — prefer local audit artifacts (default: do not commit)

| Artifact | Reason |
|----------|--------|
| Lighthouse directories under `AOVIAS Website/.../.lighthouseci-*` | Generated CI/local output; not source of truth. |
| `AOVIAS Website/AOVIAS Web/AOVIAS.Website.v*.zip` | Release/build artifacts. |

These may remain untracked or be gitignored in a separate hygiene change (not part of 1E-A).

---

## 4. Must remain untouched (exclude from foundation staging)

| Item | Location / note |
|------|-----------------|
| Phase 3H runtime probe | `AOVIAS.Cursor`: `scripts/dev/_phase3h_runtime_probe.py` |
| Claude launch settings | `AOVIAS.Claude`: untracked `Properties/launchSettings.json` |
| Website ZIPs and Lighthouse output | Main untracked (§3) |
| All other tracked product trees | No drive-by edits |
| `aovias-config/` physical location | W1 not authorized |
| Agent worktree branches | No switch/create in 1E-A |

---

## 5. Proposed staging commands (human executes — not run in 1E-A)

Example **explicit path list** (no `git add .`):

```powershell
# From V:\DevelopmentRepo\AOVIAS on branch main — human only
git add repository-layout.json
git add Aovias.Configuration/Layout/
git add Aovias.Configuration/tests/Aovias.Configuration.Tests/Layout/
git add Aovias.Publishing/src/Aovias.Publishing.Core/Discovery/PlatformRootResolver.cs
git add ComplianceServiceManager/Services/RootPathResolver.cs
git add AOVIAS.AccredaOne/README.md
```

Then verify:

```powershell
git status
git diff --cached --stat
dotnet test Aovias.Configuration\tests\Aovias.Configuration.Tests\Aovias.Configuration.Tests.csproj --filter "FullyQualifiedName~Layout"
dotnet test Aovias.Publishing\tests\Aovias.Publishing.Core.Tests\Aovias.Publishing.Core.Tests.csproj --filter "FullyQualifiedName~PublishingProjectPaths"
dotnet build Aovias.Publishing\src\Aovias.Publishing.Core\Aovias.Publishing.Core.csproj
```

Suggested commit message (conventional):

```text
feat: add repository layout contract and wire platform root resolvers

Introduce repository-layout.json, layout loader/resolver/adapters, and
manifest-first platform root discovery for Publishing and CSM with legacy
fallback. Include AccredaOne scaffold README.
```

Documentation commits may use `docs: record Phase 1C–1E restructuring evidence`.

---

## 6. Integration and branch policy

Per `AGENTS.md`:

- Agents normally implement on `V:\DevelopmentRepo\AOVIAS.Cursor` or `.Claude` on task branches; **human integrates on Main**.
- Foundation changes currently exist **only** in the Main working tree (untracked + two modified tracked files).

### Recommended path A — human integration on Main (simplest)

1. Human runs staging and commit on `V:\DevelopmentRepo\AOVIAS` / `main` (allowed as integration owner).
2. Human validates tests on `main`.
3. Sync agent worktrees without destructive ops:

   ```powershell
   git -C "V:\DevelopmentRepo\AOVIAS.Cursor" fetch
   git -C "V:\DevelopmentRepo\AOVIAS.Cursor" merge --ff-only main
   git -C "V:\DevelopmentRepo\AOVIAS.Claude" fetch
   git -C "V:\DevelopmentRepo\AOVIAS.Claude" merge --ff-only main
   ```

4. Preserved untracked artifacts (probe, launchSettings, ZIPs, Lighthouse) remain in their worktrees; they were never staged.

### Recommended path B — agent task branch (policy-pure for product code)

Use when human wants Cursor-owned commit SHA before merge:

1. Human **does not** commit on Main first.
2. On Cursor worktree: create `agent/cursor/layout-foundation` from `main` at `5a1526d` (human or assigned agent **after** explicit task assignment).
3. **Copy** the 16 foundation paths from Main working tree into Cursor worktree (same machine; copy is not a Git move). Untracked files do not transfer automatically.
4. Agent runs tests, commits, reports SHA.
5. Human merges to `main` from `V:\DevelopmentRepo\AOVIAS` (`merge --no-ff` preferred).
6. Claude/Cursor ff-only to `main`.

Path B avoids leaving a long-lived dirty integration tree but requires a one-time file copy because work was prepared on Main.

### Before W1

After foundation is on `main`:

- All three worktrees should ff-only to the foundation commit so `repository-layout.json` exists everywhere tests run.
- W1 remains a **separate** authorized task (`agent/cursor/...` or human-directed) per `AOVIAS_PHASE1DB_W1_EXECUTION_AND_ROLLBACK_PLAN.md`.

---

## 7. Post-commit verification checklist

- [ ] `git ls-files repository-layout.json` non-empty on `main`.
- [ ] Layout tests 29/29 on Main and Cursor after ff-only.
- [ ] `PublishingProjectPathsTests` 2/2 on Main.
- [ ] Cursor still has only probe untracked; Claude still has only launchSettings untracked.
- [ ] No W1 directory moves in commit.

---

## 8. Final status

**READY FOR HUMAN COMMIT APPROVAL**

Human action: choose Path A or B, approve file list §1–§2, execute staging §5, then merge/sync §6.
