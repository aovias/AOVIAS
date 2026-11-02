# AOVIAS Phase 1F-C — W1 Diff Review (Read-Only)

**Review date:** 2026-10-25  
**Worktree reviewed:** `V:\DevelopmentRepo\AOVIAS.Cursor`  
**Branch:** `agent/cursor/phase1fb-w1-layout-migration`  
**Foundation commit:** `cc3df70f182a7da00d62372b178b2e038753bc1e`  
**Branch HEAD (Git):** `cc3df70…` (no W1 commit yet — **entire W1 change set is uncommitted working tree + index-empty renames in status**)  
**No edits, staging, commit, merge, or sync performed in this phase.**

---

## 1. Worktree baselines and preservation

| Worktree | Branch | HEAD | W1 changes? | Index |
|----------|--------|------|-------------|-------|
| Main | `main` | `cc3df70…` | **No** — root `aovias-config/` still present; no `AOVIAS.Infrastructure/` | Empty |
| Cursor | `agent/cursor/phase1fb-w1-layout-migration` | `cc3df70…` | **Yes** — uncommitted diff | Empty (changes in working tree) |
| Claude | `agent/claude/active` | `cc3df70…` | **No** | Empty |

**Preservation**

| Artifact | Status |
|----------|--------|
| Cursor `scripts/dev/_phase3h_runtime_probe.py` | Untracked, present |
| Claude `Properties/launchSettings.json` | Untracked, present |
| Operational dirs under config tree | **Absent** (storage/secrets/logs/certificates) on Cursor infra path before and after move |

---

## 2. Complete change inventory (vs Phase 1F-A / 1F-B)

### 2.1 Intended W1 set (26 path entries in status)

| Category | Paths |
|----------|--------|
| **Renames (5)** | Templates: `aovias-config/templates/**` → `AOVIAS.Infrastructure/aovias-config/templates/**` |
| **Added (2)** | `RepositoryLayoutAoviasConfigPaths.cs`, `RepositoryLayoutW1TransitionTests.cs` |
| **Modified (19)** | `.gitignore`, `repository-layout.json`, legacy discovery (2), tools (2), preflight (1), bootstrap (1), appsettings (2), `build-praman-aovpkg.ps1`, DeploymentValidationStages (1), layout tests (3), other tests (4) |

### 2.2 Untracked, not part of W1 code commit (exclude unless human opts in)

- `AOVIAS_PHASE1FB_W1_IMPLEMENTATION_REPORT.md`
- `scripts/dev/_phase3h_runtime_probe.py` (pre-existing preservation)

### 2.3 Five template verification

`git ls-files` on Cursor lists **exactly five** paths under `AOVIAS.Infrastructure/aovias-config/templates/` — one each for nginx + four publishing Dockerfiles. No duplicate tracked copies at old root (rename `R` in status).

### 2.4 Confirmed unchanged vs `cc3df70` (scope boundary)

No diff on:

- `SolutionDeployableProjectDiscoverer.cs` (`GetScanRoots`)
- `SourceDiscoveryBoundary.cs`
- `RuntimeAgentDockerContextStager.cs`
- `Aovias.Runtime.Agent/Dockerfile`
- `PlatformRootResolver.cs`
- `RepositoryLayoutProductionAdapter.cs` (production adapter logic file unchanged)

---

## 3. Per-path rationale (audit mapping)

| Path | Why changed | 1F-A dependency |
|------|-------------|-----------------|
| `RepositoryLayoutAoviasConfigPaths.cs` | Central dual-marker + config-root resolution | W1 path helper (new) |
| `LegacyPublishingPlatformRootDiscovery.cs` | `ConfigDirectoryExistsAtRepositoryRoot` | Legacy dual-marker |
| `LegacyCsmPlatformRootDiscovery.cs` | Same | CSM + config markers |
| `repository-layout.json` | Current markers → infrastructure path; constraint text | Manifest Current update |
| `tools/PublishAllServices/Program.cs` | ConfigRoot, output path, platform walk | Publish CLI |
| `tools/PublishRuntimeAgent/Program.cs` | ConfigRoot, platform walk | Publish CLI |
| `SolutionPublishingPreflightService.cs` | Dockerfile discovery walk | Preflight |
| `build-praman-aovpkg.ps1` | Package storage root | Command script |
| `Aovias.Bootstrap.Host/Program.cs` | Layout-aware default ConfigRoot | Bootstrap |
| `ComplianceServiceManager/appsettings.json` | Example ConfigRoot | Dev config |
| `appsettings.Development.json` (Runtime Agent) | Example ConfigRoot | Dev config |
| `.gitignore` | Infrastructure operational ignores | Gitignore |
| Layout / W1 / Publishing / Logging tests | Post-W1 fixtures and coverage | Test closure |
| `DeploymentValidationStages.cs` | Wording only (still flags V1.0 paths) | Staging validator |

---

## 4. Root-selection matrix (code + test evidence)

| Infrastructure config | Legacy root config | Platform markers | Expected behavior | Evidence |
|----------------------|-------------------|------------------|-------------------|----------|
| Present | Absent | Valid (Publishing: + ComplaincePlatform or Agent; CSM: + Agent) | Repo root resolves; manifest Current matches | `Legacy_publishing_resolves_when_only_infrastructure_config_exists`; `Integration_manifest_current_markers_match_post_w1_repository_layout` |
| Absent | Present | Valid | Legacy dual-marker resolves (rollback) | `Legacy_publishing_resolves_when_only_legacy_root_config_exists`; CSM legacy test with legacy layout |
| Present | Present | Valid | Same **repository root**; **ConfigRoot** prefers infrastructure | `ResolveConfigRootDirectory_prefers_infrastructure_when_both_exist` |
| Present | Present | Valid | Manifest-first uses Current (infra marker); legacy walk also succeeds | Adapter unchanged: manifest `requiredAll` infra only; legacy OR for config dir |
| Infra absent | Absent | Any | Legacy walk fails → `..` fallback segments (unchanged) | No new test; pre-existing fallback in legacy classes |
| Manifest missing/invalid | Either config present | Valid markers | `TryLoadForProduction` false → legacy dual-marker | Existing adapter tests (missing manifest) + W1 legacy tests |

**Determinism**

- **Platform repository root:** Single directory per match; walk is upward, nearest ancestor wins (`TryResolveRepositoryRoot_prefers_nearest_ancestor_when_multiple_match` updated for infra nested fixture).
- **Config root path:** Explicit precedence in `ResolveConfigRootDirectory`: infrastructure **before** legacy; if neither exists, returns infrastructure combine (path default for tooling — may not exist on disk).
- **No filesystem enumeration order dependency** for root selection (boolean OR on two fixed paths).

**Publishing vs CSM distinction (unchanged)**

- Publishing legacy: config + (**ComplaincePlatform** OR **Agent**).
- CSM legacy: config + **Agent** (both required).
- Manifest Current: same marker sets with infrastructure config path.
- Existing CSM contract tests (e.g. agent missing) remain applicable via unchanged adapter + updated fixtures.

---

## 5. Current / Target profile review

| Check | Result |
|-------|--------|
| Production uses Current only | `RepositoryLayoutProductionAdapter` **unchanged**; still passes `RepositoryLayoutProfile.Current` only |
| Current describes post-W1 layout | `discovery.current.*.requiredAll` uses `AOVIAS.Infrastructure/aovias-config` |
| Target in production | **Not used** by adapter entry points |
| On-disk layout on Cursor | Matches Current markers; root `aovias-config/` **absent** (integration test asserts) |
| `compatibilityConstraints[0]` | Matches implementation (infra markers + dual-marker legacy note) |
| Target profile | Still describes **future** Command/Studio nesting — intentional; differs from Current |

---

## 6. Dual-marker semantics (exact)

| Layer | Meaning |
|-------|---------|
| **Manifest Current** | Single config marker path: `AOVIAS.Infrastructure/aovias-config` (not dual at manifest level) |
| **Legacy fallback** | `ConfigDirectoryExistsAtRepositoryRoot` = infrastructure dir **OR** legacy root `aovias-config` |
| **ConfigRoot combine** | Infrastructure preferred when both directories exist |

Rollback without reverting manifest: Current markers would **not** match legacy-only tree → manifest path fails → legacy dual-marker still resolves **platform root**; ConfigRoot helpers prefer legacy when infra absent.

---

## 7. Remaining `aovias-config` references (classification)

| Reference | Classification |
|-----------|----------------|
| `RepositoryLayoutAoviasConfigPaths.LegacyRootRelativeDirectory` | **Required compatibility** |
| W1 transition tests creating legacy dirs | **Test fixture** |
| `build-praman-aovpkg.ps1` legacy branch | **Required compatibility** |
| `.gitignore` root `aovias-config/*` | **Required compatibility** (rollback) |
| `DeploymentValidationTests` / `BootstrapValidationTests` V1.0 strings | **Test fixture** (legacy path detection) |
| `EnvironmentNeutralPortabilityRegressionTests` V1.0 JSON | **Test fixture** |
| `ContractDrivenPublishingTests` comment/filter segment `aovias-config` | **Intentional** — still matches `.../aovias-config/...` under Infrastructure |
| `docs/milestones/*` (not modified) | **Historical documentation — follow-up** |
| `LegacyCsmPlatformRootDiscovery` XML comment | **Stale comment** (low risk) |

No unexplained production code references to **root-only** `Path.Combine(..., "aovias-config")` remain outside compatibility helpers, tests, and gitignore (verified via grep + diff scope).

---

## 8. Rollback procedure review (not executed)

1. Revert W1 merge commit on `main` (or reset task branch).
2. `git mv` templates back to `aovias-config/` at repo root.
3. Restore manifest Current markers and code changes via revert.
4. Move gitignored operational data from infra path back to root config if migrated manually.
5. Re-run layout (34) + Publishing path (2) tests on Main.
6. ff-only agent worktrees after Main validated.

Adequate for tracked-template W1; operational data steps mandatory when storage dirs exist.

---

## 9. Independent validation (this review run)

| Command | Exit | Result |
|---------|------|--------|
| `dotnet test ...Configuration.Tests.csproj --filter "FullyQualifiedName~Layout" --no-restore` | 0 | **34/34 passed** |
| `dotnet test ...PublishingProjectPathsTests` | 0 | **2/2 passed** |
| `dotnet test ...ConfigTemplateGenerator` | 0 | **6/6 passed** |
| `dotnet build ...Publishing.Core.csproj -c Release --no-restore` | 0 | **Succeeded** |
| `git diff --check` (1F-B / prior) | 0 | Clean on tracked diff |

Generated `bin/`/`obj/` from tests — **not** part of intended commit.

---

## 10. Unresolved risks (non-blocking for merge authorization)

| Risk | Severity |
|------|----------|
| W1 exists only as **uncommitted** diff — commit required before merge | Process |
| INV-2 deployment.metadata.json not inventoried | Low |
| Milestone docs still mention old paths | Docs follow-up |
| No dedicated test for “Current manifest + legacy-only disk” combined with Publish CLI | Medium — mitigated by legacy fallback behavior analysis |
| `ResolveConfigRootDirectory` returns infra path when neither dir exists | Pre-existing pattern; may affect greenfield paths |

---

## 11. Final status

**READY FOR HUMAN COMMIT AND MERGE AUTHORIZATION**

Human must separately authorize:

1. **Staging and commit** on `agent/cursor/phase1fb-w1-layout-migration` (W1 paths only; exclude probe and optionally exclude Phase reports).
2. **Merge to Main** and post-merge validation on Main + ff-only Cursor/Claude.

W1 is **not complete** until committed, merged, and validated on Main.

---

## 12. Git write confirmation

No staging, commit, merge, rebase, reset, or synchronization in Phase 1F-C.
