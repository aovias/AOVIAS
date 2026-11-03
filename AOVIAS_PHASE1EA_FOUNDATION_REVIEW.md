# AOVIAS Phase 1E-A — Layout Foundation Review

**Phase:** 1E-A (review and commit preparation only)  
**Date:** 2026-10-25  
**Baseline HEAD (all worktrees):** `5a1526d99a68241d40ccba759d8fc62586f4af86`  
**Runtime probe:** Not executed.

---

## 1. Safety gate

| Worktree | Path | Branch | HEAD | Preservation / status |
|----------|------|--------|------|------------------------|
| Main (integration) | `V:\DevelopmentRepo\AOVIAS` | `main` | `5a1526d99a68241d40ccba759d8fc62586f4af86` | Foundation work **uncommitted** (see §2). Website ZIPs, Lighthouse dirs, Phase 0–1D reports untracked. |
| Cursor | `V:\DevelopmentRepo\AOVIAS.Cursor` | `agent/cursor/active` | `5a1526d…` | Clean tracked tree. `?? scripts/dev/_phase3h_runtime_probe.py` only. **No** `repository-layout.json` or `Layout/` on disk. |
| Claude | `V:\DevelopmentRepo\AOVIAS.Claude` | `agent/claude/active` | `5a1526d…` | Clean tracked tree. `?? Aovias.CommandCenter/.../Properties/` only. **No** manifest or `Layout/` on disk. |

**Preservation:** Expected artifacts present. No unexpected branch or HEAD divergence.

**Important:** Untracked foundation files exist **only** in the Main worktree working directory. Agent worktrees share Git objects but do **not** see Main-only untracked files until commit + fast-forward.

---

## 2. Foundation file inventory

Legend: **T** = tracked at HEAD, **M** = tracked, modified, **U** = untracked. Agent columns reflect disk at audit time (HEAD clean).

| Path | Main | Cursor | Claude | Class | In foundation commit? | Notes |
|------|------|--------|--------|-------|----------------------|-------|
| `repository-layout.json` | U | absent | absent | Manifest | **Yes** | Schema `1.0.0`; not in Git index today. |
| `Aovias.Configuration/Layout/LegacyCsmPlatformRootDiscovery.cs` | U | absent | absent | Foundation source | **Yes** | CSM legacy walk (`aovias-config` **and** `Aovias.Runtime.Agent`). |
| `Aovias.Configuration/Layout/LegacyPublishingPlatformRootDiscovery.cs` | U | absent | absent | Foundation source | **Yes** | Publishing legacy walk (`aovias-config` + any of Command/Agent markers). |
| `Aovias.Configuration/Layout/RepositoryLayoutLoader.cs` | U | absent | absent | Foundation source | **Yes** | Load/validate JSON; supports only `1.0.0`. |
| `Aovias.Configuration/Layout/RepositoryLayoutManifestProvider.cs` | U | absent | absent | Foundation source | **Yes** | Upward search for manifest; production cache. |
| `Aovias.Configuration/Layout/RepositoryLayoutModels.cs` | U | absent | absent | Foundation source | **Yes** | DTOs for manifest. |
| `Aovias.Configuration/Layout/RepositoryLayoutPathResolver.cs` | U | absent | absent | Foundation source | **Yes** | Marker walks; `CombineUnderRepositoryRoot` boundary checks. |
| `Aovias.Configuration/Layout/RepositoryLayoutProductionAdapter.cs` | U | absent | absent | Foundation source | **Yes** | Production entry; **Current profile only** for resolution. |
| `Aovias.Configuration/Layout/RepositoryLayoutSchema.cs` | U | absent | absent | Foundation source | **Yes** | Supported schema list. |
| `Aovias.Configuration/tests/.../Layout/RepositoryLayoutContractTests.cs` | U | absent | absent | Test | **Yes** | Contract + target-profile **test-only** use. |
| `Aovias.Configuration/tests/.../Layout/RepositoryLayoutProductionAdapterTests.cs` | U | absent | absent | Test | **Yes** | Manifest missing/invalid; parity with legacy publishing. |
| `Aovias.Configuration/tests/.../Layout/RepositoryLayoutCsmResolverContractTests.cs` | U | absent | absent | Test | **Yes** | CSM marker policy + production profile gate. |
| `Aovias.Publishing/.../PlatformRootResolver.cs` | M | T@HEAD | T@HEAD | Production wiring | **Yes** | Delegates to adapter; **configuredRoot** precedence unchanged. |
| `ComplianceServiceManager/Services/RootPathResolver.cs` | M | T@HEAD | T@HEAD | Production wiring | **Yes** | Only `ResolvePlatformRoot()` wired; other CSM walks unchanged. |
| `AOVIAS.AccredaOne/README.md` | U | absent | absent | Product scaffold | **Yes** (optional bundle) | Additive; no code. |
| `Aovias.Configuration/Aovias.Configuration.csproj` | T | T | T | Project file | **No change** | SDK includes `Layout/**/*.cs` by default (`tests/**` excluded). |
| `Aovias.Configuration/tests/.../Aovias.Configuration.Tests.csproj` | T | T | T | Test project | **No change** | SDK includes new test files under project tree. |
| `Aovias.Publishing/.../PublishingProjectPathsTests.cs` | T | T | T | Test | **No change** | Exercises wired `PlatformRootResolver` (environment-gated skips). |

### Phase 1C/1D reports (Main, all untracked)

| Report | Role |
|--------|------|
| `AOVIAS_PHASE1CA_LAYOUT_CONTRACT.md`, `AOVIAS_PHASE1CA_TEST_REPORT.md` | 1C-A evidence |
| `AOVIAS_PHASE1CB_*`, `AOVIAS_PHASE1CC_*` | 1C-B/C contracts and implementation |
| `AOVIAS_PHASE1DA_ACCREDAONE_SCAFFOLD_REPORT.md` | 1D-A scaffold |
| `AOVIAS_PHASE1DB_*` | 1D-B W1 readiness (depends on foundation being tracked) |
| `AOVIAS_PHASE1A_*`, `AOVIAS_PHASE1B_*` | Pre-foundation planning inventory |
| `AOVIAS_PHASE0*`, `AOVIAS_GIT_PATH_ANOMALY_REPORT.md` | Baseline / normalization (not layout code) |

Staging treatment for reports: see `AOVIAS_PHASE1EA_STAGING_PROPOSAL.md`.

---

## 3. Implementation coherence

### Schema vs code

- `repository-layout.json` declares `"schemaVersion": "1.0.0"`.
- `RepositoryLayoutSchema.SupportedSchemaVersions` = `["1.0.0"]`.
- `RepositoryLayoutLoader.Validate` enforces discovery shape, non-empty marker sets, and rejects `..` / absolute segments in path fields.
- **Review concern (non-blocking):** `compatibilityConstraints[0]` in the manifest still states production resolvers are *not* wired (Phase 1C-A wording). Code and resolvers **are** wired on Main working tree. Update manifest text in a follow-up doc/manifest commit or accept stale narrative until W1.

### Publishing vs CSM marker policies

| Policy | Legacy | Manifest `discovery.current` |
|--------|--------|------------------------------|
| Publishing platform root | `aovias-config` + (`ComplaincePlatform` **or** `Aovias.Runtime.Agent`) | Same via `platformRoot` |
| CSM platform root | `aovias-config` **and** `Aovias.Runtime.Agent` | Same via `csmPlatformRoot` |

Production adapter uses `TryResolveRepositoryRoot` (publishing) vs `TryResolveCsmRepositoryRoot` (CSM) — distinct code paths.

### Production profile and target paths

- `RepositoryLayoutProductionAdapter.IsPermittedProductionProfile` returns true **only** for `Current`.
- Production resolution methods call manifest loaders with **Current** markers only; they never pass `Target` to path resolution.
- `Target` profile appears **only** in contract tests and in manifest JSON for future waves — not in production adapter entry points.

### Missing / invalid manifest

- `TryLoadForProduction` → false when file missing or validation fails → legacy discovery used (Publishing and CSM).
- Tests: `TryResolvePlatformRepositoryRoot_returns_false_when_manifest_missing`, invalid schema, malformed JSON, adapter parity with legacy on fixture trees.
- **Post-commit behavior:** Cursor/Claude worktrees will **lack** manifest until ff-only to commit; legacy fallback remains correct for **current** on-disk layout.

### Root boundary and traversal

- `RepositoryLayoutPathResolver.CombineUnderRepositoryRoot` rejects `..`, absolute segments, and paths escaping repository root.
- Loader validation rejects `..` in manifest segments.

### Configured-root precedence (Publishing)

`PlatformRootResolver.Resolve` still returns `Path.GetFullPath(configuredRoot)` when non-whitespace and directory exists — unchanged.

CSM `ResolvePlatformRoot` has no configured-root parameter (unchanged from baseline design).

### AccredaOne and KnowledgeBase

- README states scaffold only, no API, no relocation of `ComplaincePlatform/KnowledgeBase`.
- Manifest records AccredaOne as `additive-scaffold`; `knowledgeBaseCurrentPath` remains `ComplaincePlatform/KnowledgeBase`.
- No tracked KnowledgeBase path changes in foundation bundle.

### Atomicity requirement

**Critical:** The two modified resolver files reference `Aovias.Configuration.Layout` types that exist only in **untracked** files on Main. Committing resolvers without the `Layout/` tree **breaks the build**. The foundation integration must stage resolver changes **together with** all `Layout/` sources and tests (and should include `repository-layout.json` so manifest-first path matches repo root in CI and after ff-only).

### W1 plan consistency

Phase 1D-B assumes foundation is **tracked on `main`** before W1. This review confirms that prerequisite is not yet met. No inconsistency in W1 dependency closure relative to code behavior.

### Unrelated artifacts

No foundation source includes probe, launchSettings, ZIP, or Lighthouse output. Resolver diff is limited to platform-root resolution wiring.

---

## 4. Fresh validation (2026-10-25)

Executed from `V:\DevelopmentRepo\AOVIAS` (Main worktree, with uncommitted foundation files present).

| Command | Outcome |
|---------|---------|
| `dotnet test Aovias.Configuration\tests\Aovias.Configuration.Tests\Aovias.Configuration.Tests.csproj --filter "FullyQualifiedName~Layout"` | **Passed:** 29/29 |
| `dotnet test Aovias.Publishing\tests\Aovias.Publishing.Core.Tests\Aovias.Publishing.Core.Tests.csproj --filter "FullyQualifiedName~PublishingProjectPaths"` | **Passed:** 2/2 |
| `dotnet build Aovias.Publishing\src\Aovias.Publishing.Core\Aovias.Publishing.Core.csproj` | **Succeeded** (0 errors) |

No dependency installation was performed. No unrelated baseline suites (Platform NU1903, website npm) were run.

---

## 5. Review concerns summary

| ID | Severity | Item |
|----|----------|------|
| R-1 | Medium (process) | Foundation physically exists only as Main untracked + modified files; agent worktrees cannot run layout tests against manifest until integration commit. |
| R-2 | Low (docs) | Stale `compatibilityConstraints` text in `repository-layout.json`. |
| R-3 | Low (W1) | After W1, legacy fallback could mask partial migration unless dual-marker strategy from 1D-B is implemented in W1 bundle (out of scope for 1E-A). |

None of R-1–R-3 block commit preparation.

---

## 6. Final status

**READY FOR HUMAN COMMIT APPROVAL**

Meaning: The foundation bundle is identified, coherence-reviewed, and narrowly tested on Main. Human may approve the staging list and integration path in `AOVIAS_PHASE1EA_STAGING_PROPOSAL.md`. This status does **not** authorize commit, W1, or staging.
