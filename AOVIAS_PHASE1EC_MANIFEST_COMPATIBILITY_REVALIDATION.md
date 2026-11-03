# AOVIAS Phase 1E-C — Manifest Compatibility Revalidation

**Phase:** 1E-C (minimal manifest edit + read-only validation)  
**Date:** 2026-10-25  
**Worktree:** `V:\DevelopmentRepo\AOVIAS` only  
**No staging, commit, merge, branch operations, or worktree sync performed.**

---

## 1. Baseline (Phase A)

| Item | Actual |
|------|--------|
| Branch | `main` |
| HEAD | `5a1526d99a68241d40ccba759d8fc62586f4af86` |
| Material change vs Phase 1E-B preflight | **No** — same two modified resolvers, same untracked foundation tree, same preservation artifacts |

**Preservation:** Website ZIPs, Lighthouse dirs, Phase reports, Cursor probe, Claude `Properties/` — untouched by this task.

---

## 2. Manifest field corrected

**Field:** `compatibilityConstraints[0]` in `repository-layout.json`

### Old wording

```text
Production RootPathResolver and PlatformRootResolver are not wired to this file in Phase 1C-A.
```

### New wording

```text
Publishing PlatformRootResolver and CSM RootPathResolver.ResolvePlatformRoot use RepositoryLayoutProductionAdapter with the Current profile and discovery.current markers (Publishing: aovias-config plus ComplaincePlatform or Aovias.Runtime.Agent; CSM platform root: aovias-config and Aovias.Runtime.Agent). When repository-layout.json is missing, invalid, or markers do not match, legacy directory walks apply. Other path resolvers and tooling are not manifest-driven.
```

**Scope of edit:** Single array element only. No other manifest keys, schema, discovery profiles, paths, code, or tests changed.

---

## 3. Source and test evidence

### Production wiring (reviewed)

| Component | Verified behavior |
|-----------|-------------------|
| `PlatformRootResolver.Resolve` | After optional `configuredRoot`, calls `RepositoryLayoutProductionAdapter.ResolvePublishingPlatformRoot` |
| `RootPathResolver.ResolvePlatformRoot` | Calls `RepositoryLayoutProductionAdapter.ResolveCsmPlatformRoot` only |
| `RootPathResolver` (other methods) | Still legacy upward walks for `ComplaincePlatform` / `WebApp` — **not** manifest-driven |
| `RepositoryLayoutProductionAdapter` | Manifest load + `RepositoryLayoutProfile.Current` only for production resolution; `ResolvePublishingPlatformRoot` / `ResolveCsmPlatformRoot` fall back to `LegacyPublishingPlatformRootDiscovery` / `LegacyCsmPlatformRootDiscovery` |
| `discovery.current` in manifest | Matches marker text in new constraint string |

### Tests reviewed (not exhaustive list)

- `RepositoryLayoutProductionAdapterTests` — manifest missing/invalid, parity with legacy publishing, Current-only production profile
- `RepositoryLayoutCsmResolverContractTests` — CSM markers, Target profile not permitted in production adapter
- `RepositoryLayoutContractTests` — loader validation, Current vs Target in tests, integration repo marker match

---

## 4. Validation executed this phase

| Step | Command / action | Result |
|------|------------------|--------|
| JSON validity | `Get-Content repository-layout.json -Raw \| ConvertFrom-Json` (PowerShell) | **OK** |
| Layout tests | `dotnet test Aovias.Configuration\tests\Aovias.Configuration.Tests\Aovias.Configuration.Tests.csproj --filter "FullyQualifiedName~Layout" --no-restore` | **Passed 29/29** |
| Whitespace check | `git -C V:\DevelopmentRepo\AOVIAS diff --check` | **Exit 0** (tracked resolver diffs only; manifest remains untracked) |

---

## 5. Changed-file list (this task only)

| Path | Change |
|------|--------|
| `repository-layout.json` | **Content edited** (`compatibilityConstraints[0]` only) |

**Pre-existing changes not introduced by 1E-C (unchanged):**

- `M` `Aovias.Publishing/src/Aovias.Publishing.Core/Discovery/PlatformRootResolver.cs`
- `M` `ComplianceServiceManager/Services/RootPathResolver.cs`
- Untracked foundation files per Phase 1E-B inventory (15-path bundle)

---

## 6. Operations not performed

- No `git add`, commit, merge, rebase, reset, clean, or force
- No branch create/switch
- No Cursor/Claude ff-only
- No staging of foundation bundle or this report

---

## 7. Final status

**READY FOR PHASE 1E-B PREFLIGHT REVALIDATION**

The next integration run must independently re-run the full Phase 1E-B preflight (manifest note, 15-path inventory, focused tests, policy) before requesting human staging/commit authorization.
