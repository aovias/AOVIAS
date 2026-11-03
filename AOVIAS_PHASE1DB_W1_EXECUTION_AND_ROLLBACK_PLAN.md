# AOVIAS Phase 1D-B — W1 Execution and Rollback Plan

**Date:** 2026-10-25  
**Move:** `aovias-config/` → `AOVIAS.Infrastructure/aovias-config/`  
**Authority:** Plan only — **execution requires separate human authorization**

---

## 1. Pre-migration baseline and preservation

| Step | Action | Owner |
|------|--------|-------|
| P0 | Verify Main/Cursor/Claude @ `main`/`agent/*/active` HEAD aligned | Human |
| P1 | Preserve probe, Claude Properties, website ZIPs, Lighthouse, local gitignored `aovias-config/storage` (copy if needed) | Human |
| P2 | Record `git status`, `git rev-parse HEAD` on all three worktrees | Human |
| P3 | **Integrator:** Commit or stash Phase 1C/1D **layout contract** work so `repository-layout.json` is **tracked** on `main` | Human (prerequisite) |
| P4 | Run **historical** baseline note: PublishingEngine **PASS**, Platform **FAIL** NU1903 (Phase 0B) — do not block W1 on Platform slnx | Evidence |
| P5 | Run focused tests (fresh at execution time): `dotnet test ...Configuration.Tests... --filter RepositoryLayout` | Agent/Human |

**Failure at P3:** Stop — manifest distribution risk (see readiness report).

---

## 2. Source paths to move (single `git mv` tree)

```text
aovias-config/  →  AOVIAS.Infrastructure/aovias-config/
```

Create empty parent if needed:

```text
AOVIAS.Infrastructure/   (new directory, may contain only aovias-config initially)
```

**Do not move:** `AOVIAS.AccredaOne/`, Command, Studio, Website, `ComplaincePlatform`, `WebApp`.

**Gitignored content:** `secrets/`, `storage/`, `logs/`, `certificates/` under old path — **manually relocate** or re-point `AOVIAS_CONFIG_ROOT` after move; never `git clean`.

---

## 3. Resolver and discovery changes (same PR/merge)

| Order | Change |
|-------|--------|
| 1 | `repository-layout.json`: set `discovery.current.*.requiredAll` entries from `aovias-config` → `AOVIAS.Infrastructure/aovias-config`; align `products[infrastructure].paths.aoviasConfig.current` |
| 2 | `LegacyPublishingPlatformRootDiscovery` + `LegacyCsmPlatformRootDiscovery`: accept **either** old or new relative config path until transition end (recommended) |
| 3 | `RepositoryLayoutPathResolver` / adapter tests updated for new current paths |
| 4 | `tools/PublishAllServices/Program.cs`, `tools/PublishRuntimeAgent/Program.cs`: config root combine path |
| 5 | `SolutionPublishingPreflightService` platform walk (if still hard-coded) |
| 6 | Dev samples: `ComplianceServiceManager/appsettings.json`, Bootstrap defaults (repo-relative, not `V1.0`) where applicable |

**Not in W1 PR:** `SourceDiscoveryBoundary`, `GetScanRoots`, `RuntimeAgentDockerContextStager`, Command Compose.

---

## 4. Dockerfile, Compose, CI, solutions

| Item | W1 change |
|------|-----------|
| Runtime Agent Dockerfile | **None** (no aovias-config COPY) |
| Compose files | **None** |
| CI workflows | **None** |
| `.slnx` / `.csproj` | **None** unless new path embedded (none found for config tree) |

---

## 5. Tests before and after move

### Before move (on authorized branch)

```text
dotnet test Aovias.Configuration/tests/Aovias.Configuration.Tests/Aovias.Configuration.Tests.csproj -c Release --filter FullyQualifiedName~RepositoryLayout
dotnet test Aovias.Publishing/tests/Aovias.Publishing.Core.Tests/Aovias.Publishing.Core.Tests.csproj -c Release --filter FullyQualifiedName~PublishingProjectPathsTests
```

### After move (same branch, before merge to main)

- Re-run **all** `RepositoryLayout*` tests (including dual-marker / new path fixtures).
- `dotnet build Aovias.Publishing/src/Aovias.Publishing.Core/Aovias.Publishing.Core.csproj -c Release`
- Optional: `dotnet run` PublishAllServices `--discover` from repo root (requires valid config tree).
- **Not required for W1 gate:** full Platform slnx, CSM WPF, website npm.

---

## 6. Deployment / runtime validation (post-merge on main)

| Check | Expected |
|-------|----------|
| `PlatformRootResolver.Resolve(null)` from Publishing bin dir | Returns repo root |
| `RootPathResolver.ResolvePlatformRoot()` from CSM | Same repo root |
| `AOVIAS_CONFIG_ROOT` unset | Publish CLI finds new config path |
| Existing VPS deployments | Still use `/opt/...` via env — **unchanged** |

---

## 7. Success and failure conditions

| Outcome | Condition |
|---------|-----------|
| **Success** | Tree at new path; tracked templates present; layout tests green; publishing root resolves; no duplicate root-level `aovias-config/` |
| **Failure** | Any resolver returns wrong root; tests fail; Publish CLI cannot find config; partial move (old path gone, new path broken) |

---

## 8. Rollback (non-destructive)

1. **Revert integration merge** on `V:\DevelopmentRepo\AOVIAS` (single merge commit preferred).
2. `git -C AOVIAS.Cursor merge --ff-only main` and same for Claude after main restored.
3. Restore gitignored local storage from P1 backup if relocated.
4. Re-run layout tests — must match pre-W1 parity.
5. **Forbidden:** `git reset --hard` / `git clean -fd` on agent worktrees without human approval.

---

## 9. Single commit / merge unit

**Must land atomically in one integration merge:**

- `git mv` of entire tracked `aovias-config` tree
- All Blocker/High code + manifest + test updates from dependency closure doc

**Intermediate broken states forbidden** on `main` (e.g. move-only without resolver updates).

---

## 10. Worktree synchronization (AGENTS.md)

| Step | Action |
|------|--------|
| 1 | Human merges W1 branch to `main` on `V:\DevelopmentRepo\AOVIAS` |
| 2 | Validate `main` |
| 3 | `git -C AOVIAS.Cursor merge --ff-only main` (preserve untracked probe) |
| 4 | `git -C AOVIAS.Claude merge --ff-only main` (preserve Properties) |
| 5 | Confirm each worktree has **tracked** `repository-layout.json` and new config path |

---

## 11. Ordered execution checklist (human/agent at authorization)

```text
[ ] P3 manifest tracked on main
[ ] Branch agent/cursor/w1-aovias-config (example) from main
[ ] Implement resolver + manifest + tool path updates
[ ] Run pre-move tests
[ ] git mv aovias-config AOVIAS.Infrastructure/aovias-config
[ ] Run post-move tests + Publishing.Core build
[ ] Commit; human review
[ ] Merge --no-ff to main; validate; ff agent worktrees
[ ] Post-merge smoke (Resolve platform root)
```

---

*Execution plan only. Phase 1D-B does not perform these steps.*
