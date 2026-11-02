# AOVIAS Phase 1F-B — W1 Implementation Report (Reviewable Diff)

**Date:** 2026-10-25  
**Worktree:** `V:\DevelopmentRepo\AOVIAS.Cursor`  
**Branch:** `agent/cursor/phase1fb-w1-layout-migration`  
**Base commit:** `cc3df70f182a7da00d62372b178b2e038753bc1e`  
**Main / Claude:** Not modified (per scope).  
**Staging / commit:** **Not performed** (Phase F).

---

## 1. Phase A — Preflight

| Worktree | Branch | HEAD |
|----------|--------|------|
| Main | `main` | `cc3df70…` |
| Cursor (pre-branch) | `agent/cursor/active` | `cc3df70…` |
| Claude | `agent/claude/active` | `cc3df70…` |

- **Operational dirs** (`storage`, `secrets`, `logs`, `certificates`): **Absent** under `aovias-config/` in Main, Cursor, and Claude before move.
- **Probe:** `scripts/dev/_phase3h_runtime_probe.py` untracked; not executed.
- **Task branch created:** `agent/cursor/phase1fb-w1-layout-migration`.

---

## 2. Physical move (Git)

```
aovias-config/templates/**  →  AOVIAS.Infrastructure/aovias-config/templates/**
```

Five tracked template files renamed (Git `R` records). Parent `AOVIAS.Infrastructure/` created as part of move.

---

## 3. Dependency → change mapping

| Dependency | Change | Rationale |
|------------|--------|-----------|
| `RepositoryLayoutAoviasConfigPaths.cs` | **Added** | Single source for infrastructure vs legacy config paths and dual-marker checks |
| `LegacyPublishingPlatformRootDiscovery.cs` | Dual-marker via helper | W1 + rollback transition |
| `LegacyCsmPlatformRootDiscovery.cs` | Dual-marker via helper | CSM distinct markers preserved |
| `repository-layout.json` | **Current** markers → `AOVIAS.Infrastructure/aovias-config`; product path; constraint text | Manifest-first resolution after move |
| `tools/PublishAllServices/Program.cs` | ConfigRoot, output path, platform walk | Publishing CLI paths |
| `tools/PublishRuntimeAgent/Program.cs` | ConfigRoot, platform walk | Same |
| `SolutionPublishingPreflightService.cs` | Dockerfile discovery walk | Preflight platform boundary |
| `ComplaincePlatform/scripts/build-praman-aovpkg.ps1` | Package root under infra or legacy | Package build script |
| `Aovias.Bootstrap.Host/Program.cs` | Layout-aware default ConfigRoot | Remove hardcoded V1.0 default |
| `ComplianceServiceManager/appsettings.json` | Example ConfigRoot path | Dev configuration |
| `Aovias.Runtime.Agent.Api/appsettings.Development.json` | Example ConfigRoot | Dev configuration |
| `.gitignore` | Infrastructure operational paths | Gitignore parity |
| Layout + W1 tests | Fixtures/manifest JSON + **new** `RepositoryLayoutW1TransitionTests.cs` | Post-W1 and dual-marker coverage |
| Publishing tests (3 files) | Repo-relative config paths | Align with moved tree |
| `LoggingSettingsBinderTests.cs` | Relative path to infra config | Test discovers moved tree |
| `DeploymentValidationStages.cs` | Message clarity only | Still flags legacy V1.0 paths in output |

**Unchanged (audit confirmed):** `GetScanRoots`, `SourceDiscoveryBoundary`, `RuntimeAgentDockerContextStager`, Runtime Agent Dockerfile, Compose, `.github` CI.

---

## 4. Manifest and resolver behavior (post-change)

| Aspect | Behavior |
|--------|----------|
| Publishing markers (Current) | `AOVIAS.Infrastructure/aovias-config` + (`ComplaincePlatform` OR `Aovias.Runtime.Agent`) |
| CSM markers (Current) | `AOVIAS.Infrastructure/aovias-config` + `Aovias.Runtime.Agent` |
| Legacy fallback | Accepts **infrastructure** or **legacy root** config directory |
| Config root path helper | Prefers infrastructure when both exist |
| Target profile | Unchanged (future studio/command nesting); not used in production APIs |
| Obsolete root | Integration test asserts root-level `aovias-config/` absent after W1 on Cursor tree |

---

## 5. Test and build evidence (Cursor, this run)

| Command | Exit | Result |
|---------|------|--------|
| `dotnet test ...Aovias.Configuration.Tests.csproj --filter "FullyQualifiedName~Layout"` | 0 | **34/34 passed** (includes 5 W1 transition tests) |
| `dotnet test ...Publishing.Core.Tests.csproj --filter "FullyQualifiedName~PublishingProjectPathsTests"` | 0 | **2/2 passed** |
| `dotnet test ...Publishing.Core.Tests.csproj --filter "FullyQualifiedName~ConfigTemplateGenerator"` | 0 | **6/6 passed** |
| `dotnet build ...Aovias.Publishing.Core.csproj -c Release` | 0 | **Succeeded** |
| `git diff --check` | 0 | Clean |

---

## 6. Operational data

- Pre-move and post-move recheck: operational directories **still absent** on Cursor; no backup required on this machine.
- **Execution environments with data** must follow Phase 1F-A backup/restore procedure before merge.

---

## 7. Rollback (human / merge revert)

1. Revert W1 merge commit on `main`.
2. `git mv` templates back to root `aovias-config/` (or restore from revert).
3. Restore gitignored operational dirs from backup to root `aovias-config/` if they were migrated manually.
4. ff-only agent worktrees after main rollback.
5. Verify layout tests and Publishing path tests at foundation or rollback SHA.

---

## 8. Moved / changed file inventory

**Renames (5):**  
`aovias-config/templates/...` → `AOVIAS.Infrastructure/aovias-config/templates/...`

**Added (2):**  
`Aovias.Configuration/Layout/RepositoryLayoutAoviasConfigPaths.cs`  
`Aovias.Configuration/tests/.../RepositoryLayoutW1TransitionTests.cs`

**Modified (19):** per `git diff --stat` on Cursor branch (includes `.gitignore`, manifest, legacy, tools, preflight, bootstrap, appsettings, tests).

**Untracked (not part of W1 set):** `scripts/dev/_phase3h_runtime_probe.py`, this report.

---

## 9. Unresolved risks

- INV-2 `deployment.metadata.json` inventory still open.
- Machines with populated gitignored config trees need manual migration with backup.
- `DeploymentValidationTests` / `BootstrapValidationTests` still use **V1.0** strings intentionally to test legacy-path detection.
- Documentation under `docs/milestones/` still mentions old paths (not updated in this scope).

---

## 10. Final status

**READY FOR HUMAN W1 DIFF REVIEW**

Await human review, then **separate** authorization for staging, commit on task branch, and merge to Main. Do not merge automatically.
