# AOVIAS Phase 1E-D — Foundation Preflight Revalidation

**Timestamp:** 2026-10-25 (Phase 1E-D audit run)  
**Scope:** Verification and reporting only — **no Git write operations**  
**Expected baseline HEAD:** `5a1526d99a68241d40ccba759d8fc62586f4af86`  
**Runtime probe:** Not executed.

---

## 1. Repository policy (`AGENTS.md`)

- **Human** owns integration on `V:\DevelopmentRepo\AOVIAS` / `main`.
- **Agents** implement on `AOVIAS.Cursor` / `AOVIAS.Claude`; must not treat Main as default dev worktree.
- Foundation may integrate via **human commit on `main`** (Path A) or **agent task branch → human merge → ff-only** (Path B).
- This phase performed **no** staging, commit, merge, branch create/switch, reset, clean, stash, or worktree sync.

---

## 2. Worktree baseline (fresh inspection)

| Worktree | Path | Branch | HEAD | Working tree | Index (staged) |
|----------|------|--------|------|--------------|----------------|
| Main | `V:\DevelopmentRepo\AOVIAS` | `main` | `5a1526d99a68241d40ccba759d8fc62586f4af86` | 2× `M` resolvers; foundation untracked; reports/ZIPs/Lighthouse untracked | **Empty** (`git diff --cached --stat` empty) |
| Cursor | `V:\DevelopmentRepo\AOVIAS.Cursor` | `agent/cursor/active` | `5a1526d…` | `?? scripts/dev/_phase3h_runtime_probe.py` only | **Empty** |
| Claude | `V:\DevelopmentRepo\AOVIAS.Claude` | `agent/claude/active` | `5a1526d…` | `?? .../Properties/` only | **Empty** |

**Risk to unrelated work:** None identified. Pre-existing Main dirty state is limited to intended foundation + preserved artifacts.

---

## 3. Path inventory label correction

| Label | Correct count |
|-------|----------------|
| Informal “16-path bundle” | **Incorrect** — use **15** distinct files |
| `AOVIAS_PHASE1EA_STAGING_PROPOSAL.md` §1.1 “14 paths” | **Incorrect header** — lists **13** lines (8 layout + 3 tests + 2 resolvers) |
| **Authoritative foundation scope** | **15 paths** (13 + `repository-layout.json` + `AOVIAS.AccredaOne/README.md`) |

**Reconciliation:** All **15** paths exist on disk. **No missing** paths. **No extra** files under `Aovias.Configuration/Layout/` (8), test `Layout/` (3), or `AOVIAS.AccredaOne/` (README only). **No** foundation files staged in any worktree.

### 3.1 Exact 15-path list (each once)

1. `Aovias.Configuration/Layout/LegacyCsmPlatformRootDiscovery.cs`
2. `Aovias.Configuration/Layout/LegacyPublishingPlatformRootDiscovery.cs`
3. `Aovias.Configuration/Layout/RepositoryLayoutLoader.cs`
4. `Aovias.Configuration/Layout/RepositoryLayoutManifestProvider.cs`
5. `Aovias.Configuration/Layout/RepositoryLayoutModels.cs`
6. `Aovias.Configuration/Layout/RepositoryLayoutPathResolver.cs`
7. `Aovias.Configuration/Layout/RepositoryLayoutProductionAdapter.cs`
8. `Aovias.Configuration/Layout/RepositoryLayoutSchema.cs`
9. `Aovias.Configuration/tests/Aovias.Configuration.Tests/Layout/RepositoryLayoutContractTests.cs`
10. `Aovias.Configuration/tests/Aovias.Configuration.Tests/Layout/RepositoryLayoutCsmResolverContractTests.cs`
11. `Aovias.Configuration/tests/Aovias.Configuration.Tests/Layout/RepositoryLayoutProductionAdapterTests.cs`
12. `repository-layout.json`
13. `Aovias.Publishing/src/Aovias.Publishing.Core/Discovery/PlatformRootResolver.cs` (`M`)
14. `ComplianceServiceManager/Services/RootPathResolver.cs` (`M`)
15. `AOVIAS.AccredaOne/README.md`

---

## 4. Manifest vs production behavior

### 4.1 `compatibilityConstraints[0]` (Phase 1E-C text, revalidated)

```text
Publishing PlatformRootResolver and CSM RootPathResolver.ResolvePlatformRoot use RepositoryLayoutProductionAdapter with the Current profile and discovery.current markers (Publishing: aovias-config plus ComplaincePlatform or Aovias.Runtime.Agent; CSM platform root: aovias-config and Aovias.Runtime.Agent). When repository-layout.json is missing, invalid, or markers do not match, legacy directory walks apply. Other path resolvers and tooling are not manifest-driven.
```

| Claim in constraint | Code/test evidence |
|---------------------|-------------------|
| Publishing via `PlatformRootResolver` → adapter | `PlatformRootResolver.Resolve` → `ResolvePublishingPlatformRoot` |
| CSM via `ResolvePlatformRoot` only | `RootPathResolver.ResolvePlatformRoot` → `ResolveCsmPlatformRoot`; other CSM methods still legacy walks |
| Current profile + `discovery.current` markers | Adapter uses `RepositoryLayoutProfile.Current` only in production resolution paths; `IsPermittedProductionProfile(Target)` false in tests |
| Publishing markers | Manifest `discovery.current.platformRoot`; legacy `LegacyPublishingPlatformRootDiscovery` |
| CSM markers | Manifest `discovery.current.csmPlatformRoot`; legacy `LegacyCsmPlatformRootDiscovery` |
| Legacy fallback | `TryLoadForProduction` / marker mismatch → legacy classes; covered in adapter tests |
| Other consumers not manifest-driven | CSM CompliancePlatform/WebApp walks; Publishing `ResolveWebAppRoot` hardcoded segments; tools unchanged in bundle |

**JSON:** PowerShell `ConvertFrom-Json` on full file — **valid**.  
**Unrelated manifest fields:** Only `compatibilityConstraints[0]` differs from Phase 1C-A narrative; discovery blocks unchanged and match loader validation.

**Informational (non-blocking):** `compatibilityConstraints[3]` references Command csproj graph at **baseline HEAD** — still true at commit `5a1526d`; does not describe post-foundation wiring.

---

## 5. Fresh validation (Main, this run)

| # | Command | Exit | Result |
|---|---------|------|--------|
| 1 | `dotnet test V:\DevelopmentRepo\AOVIAS\Aovias.Configuration\tests\Aovias.Configuration.Tests\Aovias.Configuration.Tests.csproj --filter "FullyQualifiedName~Layout" --no-restore` | 0 | **Passed 29/29** |
| 2 | `dotnet test V:\DevelopmentRepo\AOVIAS\Aovias.Publishing\tests\Aovias.Publishing.Core.Tests\Aovias.Publishing.Core.Tests.csproj --filter "FullyQualifiedName~PublishingProjectPathsTests"` | 0 | **Passed 2/2** |
| 3 | `dotnet build V:\DevelopmentRepo\AOVIAS\Aovias.Publishing\src\Aovias.Publishing.Core\Aovias.Publishing.Core.csproj -c Release` | 0 | **Succeeded** (0 errors, 0 warnings) |
| 4 | `git -C V:\DevelopmentRepo\AOVIAS diff --check` | 0 | No conflict-marker/whitespace issues on **tracked** diffs |

---

## 6. Content review (15 paths)

| Check | Finding |
|-------|---------|
| `.csproj` / solution changes in bundle | **None** — project files not modified |
| Publishing vs CSM markers | **Distinct** in manifest, legacy classes, and tests |
| Production Target profile | **Not used** by `RepositoryLayoutProductionAdapter` resolution APIs |
| Legacy fallback | **Implemented** for missing/invalid/non-matching manifest |
| `configuredRoot` precedence (Publishing) | **Unchanged** — checked before adapter |
| AccredaOne README | **Scaffold only** — no API/runtime claims |
| KnowledgeBase | **Command-owned** path documented; no physical move |
| Docker / Compose / CI / unrelated products | **Not in** 15-path bundle |
| Preserved artifacts | Probe, launchSettings, ZIPs, Lighthouse, reports — **not modified** by foundation files |
| Resolver diffs | Platform-root delegation only (+ `using`) |
| Untrackeds | Layout stack + manifest + README reviewed via filesystem inventory and test compilation |

---

## 7. Preserved artifacts (confirmed)

| Artifact | Present |
|----------|---------|
| Cursor `_phase3h_runtime_probe.py` | Yes |
| Claude `launchSettings.json` | Yes |
| Main website ZIPs / Lighthouse dirs | Untracked in status |
| Phase 0–1E reports | Untracked on Main |

---

## 8. Recommended integration path (after human approval)

**Path A (human on Main):** Individual `git add` for the 15 paths → verify `git diff --cached` → re-run focused tests → one atomic commit on `main` → ff-only Cursor/Claude when separately authorized.

**Path B (agent task branch):** Copy 15 paths from Main into Cursor worktree (untracked do not auto-sync), commit on `agent/cursor/layout-foundation`, human merge to `main`.

---

## 9. Git write confirmation

**No** `git add`, **no** commit, **no** merge, **no** branch operations, **no** worktree synchronization in Phase 1E-D.

---

## 10. Final status

**READY FOR HUMAN STAGING AND COMMIT APPROVAL**

Human must explicitly authorize staging (15 individual paths), one atomic foundation commit, and integration path in a **separate** instruction. Prior prompts and this report are **not** Git-write authorization.
