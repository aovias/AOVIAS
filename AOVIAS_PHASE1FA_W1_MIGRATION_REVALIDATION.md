# AOVIAS Phase 1F-A — W1 Migration Revalidation

**Audit date:** 2026-10-25  
**Proposed move:** `aovias-config/` → `AOVIAS.Infrastructure/aovias-config/`  
**Foundation commit (verified on all worktrees):** `cc3df70f182a7da00d62372b178b2e038753bc1e`  
**Scope:** Read-only investigation — **no** moves, edits, staging, commits, or sync.

---

## 1. Worktree baseline and preservation

| Worktree | Path | Branch | HEAD | Target is ancestor of HEAD | Index | Notable working tree |
|----------|------|--------|------|----------------------------|-------|----------------------|
| Main | `V:\DevelopmentRepo\AOVIAS` | `main` | `cc3df70…` | Yes (exit 0) | Empty | Untracked Phase reports, ZIPs, Lighthouse |
| Cursor | `V:\DevelopmentRepo\AOVIAS.Cursor` | `agent/cursor/active` | `cc3df70…` | Yes | Empty | `?? scripts/dev/_phase3h_runtime_probe.py` |
| Claude | `V:\DevelopmentRepo\AOVIAS.Claude` | `agent/claude/active` | `cc3df70…` | Yes | Empty | `?? …/Properties/` (launchSettings) |

**Preservation:** Probe and Claude `launchSettings.json` present and untracked. Runtime probe **not** executed. No safety discrepancy.

**Change since Phase 1D-B:** Layout foundation and `repository-layout.json` are **tracked** at `cc3df70` on all three worktrees (1D-B prerequisite **satisfied**).

---

## 2. Historical contract (Phase 1D-B)

Reviewed on Main (untracked copies unchanged):

- `AOVIAS_PHASE1DB_W1_DEPENDENCY_CLOSURE.md`
- `AOVIAS_PHASE1DB_W1_EXECUTION_AND_ROLLBACK_PLAN.md`
- `AOVIAS_PHASE1DB_READINESS_REPORT.md`

Treated as **historical evidence**. This report re-derives findings from **current** `cc3df70` source and filesystem.

---

## 3. Current on-disk state

| Item | Main | Cursor | Claude |
|------|------|--------|--------|
| `aovias-config/` directory | Yes | Yes | Yes |
| `AOVIAS.Infrastructure/` | **No** | **No** | **No** |
| Tracked files under `aovias-config/` | **5** (templates only) | Same (shared commit) | Same |
| `aovias-config/storage` | **Absent** | Absent | Absent |
| `aovias-config/secrets` | **Absent** | Absent | Absent |
| `aovias-config/logs` | **Absent** | Absent | Absent |
| `aovias-config/certificates` | **Absent** | Absent | Absent |

**Tracked templates (move with `git mv`):**

- `aovias-config/templates/nginx/runtime-agent.site.conf.template`
- `aovias-config/templates/publishing/Dockerfile.{angular,aspnet,static,worker}`

**Ownership:** `repository-layout.json` assigns product `infrastructure` / owner `AOVIAS.Infrastructure`; no `.csproj` or solution under `AOVIAS.Infrastructure/` today (parent created at W1 execution).

---

## 4. Layout contract behavior (code + tests at `cc3df70`)

| Concern | Verified behavior |
|---------|-------------------|
| **Publishing platform root** | `PlatformRootResolver` → `RepositoryLayoutProductionAdapter.ResolvePublishingPlatformRoot`; manifest **Current** `discovery.current.platformRoot`: `requiredAll` [`aovias-config`], `requiredAny` [`ComplaincePlatform`, `Aovias.Runtime.Agent`]; else `LegacyPublishingPlatformRootDiscovery` (root-level `aovias-config` only) |
| **CSM platform root** | `RootPathResolver.ResolvePlatformRoot` → `ResolveCsmPlatformRoot`; **Current** `csmPlatformRoot`: `requiredAll` [`aovias-config`, `Aovias.Runtime.Agent`]; else `LegacyCsmPlatformRootDiscovery` |
| **Production profile** | Adapter resolution uses **Current only**; Target profile in tests/JSON only |
| **Legacy fallback** | Missing/invalid manifest or marker mismatch → legacy walks (still **old path only**) |
| **Proposed destination in manifest** | **Target** profile already lists `AOVIAS.Infrastructure/aovias-config`; **Current** still `aovias-config` at repo root |
| **Post-W1 manifest edits required** | Update `discovery.current.*.requiredAll`, `products[infrastructure].paths.aoviasConfig.current`, and `compatibilityConstraints[0]` wording if marker paths change |

**Masking risk (still valid):** If physical move happens **without** updating **Current** manifest markers **and** legacy dual-marker support, manifest-first resolution fails markers and legacy looks only for **root** `aovias-config` → wrong fallback root while tools still combine `platformRoot + "aovias-config"`.

**Mitigation (required in W1 bundle):** Same-merge **dual-marker** legacy + manifest **Current** update + transition tests (already sketched in 1D-B; **not yet implemented**).

---

## 5. Dependency inventory (current repository)

Classification: **B** = breaks or wrong path if tree moves without coordinated change; **U** = update in W1 bundle; **N** = no W1 change expected; **D** = documentation / example only; **T** = test fixture.

### 5.1 Discovery and layout (runtime) — **U / B**

| Path | Symbol / key | Assumption | W1 treatment |
|------|--------------|------------|--------------|
| `Aovias.Configuration/Layout/LegacyPublishingPlatformRootDiscovery.cs` | `Resolve` ~L14 | `Path.Combine(root, "aovias-config")` at repo root | **U** — dual-marker or helper accepting `AOVIAS.Infrastructure/aovias-config` |
| `Aovias.Configuration/Layout/LegacyCsmPlatformRootDiscovery.cs` | `Resolve` ~L15 | Same | **U** |
| `Aovias.Configuration/Layout/RepositoryLayoutProductionAdapter.cs` | `TryResolve*` | Reads manifest **Current** markers | **U** via manifest JSON |
| `repository-layout.json` | `discovery.current`, `products`, `compatibilityConstraints[0]` | Current paths at root | **U** — flip `current` to infrastructure path; optional transition note |
| `Aovias.Publishing/.../PlatformRootResolver.cs` | `Resolve` | Delegates to adapter | **N** if adapter/legacy/manifest updated |
| `ComplianceServiceManager/Services/RootPathResolver.cs` | `ResolvePlatformRoot` | Delegates to adapter | **N** if adapter/legacy/manifest updated |

### 5.2 Publishing tools and preflight — **U / B**

| Path | Location | Assumption | W1 treatment |
|------|----------|------------|--------------|
| `tools/PublishAllServices/Program.cs` | L20, L53, L232 | `Path.Combine(platformRoot, "aovias-config")`; walk for root `aovias-config` | **U** — shared config-root helper |
| `tools/PublishRuntimeAgent/Program.cs` | L25, L86 | Same pattern | **U** |
| `Aovias.Publishing/.../SolutionPublishingPreflightService.cs` | `FindDockerfile` ~L835–836 | Break walk when root `aovias-config` + `ComplaincePlatform` | **U** — accept new config path or layout helper |

### 5.3 Scan roots, CSM source boundary, Docker stager — **N**

| Path | Finding |
|------|---------|
| `SolutionDeployableProjectDiscoverer.GetScanRoots` | `ComplaincePlatform`, `Aovias.Runtime.Agent` only — **no** `aovias-config` |
| `SourceDiscoveryBoundary` | **No** `aovias-config` reference (verified) |
| `RuntimeAgentDockerContextStager` | Platform root via `Aovias.Configuration` + `Aovias.Runtime.Agent`; **no** `aovias-config` COPY |
| `Aovias.Runtime.Agent/Dockerfile` | No `aovias-config` in tracked Dockerfile (per 1D-B; unchanged) |
| Repository Compose / `.github` workflows | **No** `aovias-config` matches in compose or `.github` grep |

### 5.4 Dev configuration and scripts — **U / D**

| Path | Key | W1 treatment |
|------|-----|--------------|
| `ComplianceServiceManager/appsettings.json` | `ConfigRoot` → legacy `V:/DevelopmentRepo/V1.0/aovias-config` | **U** — repo-relative example or env-only |
| `Aovias.Bootstrap/.../Program.cs` | Default `V:/DevelopmentRepo/V1.0/aovias-config` | **U** — layout-aware default + `AOVIAS_CONFIG_ROOT` |
| `Aovias.Runtime.Agent/.../appsettings.Development.json` | `ConfigRoot` | **U** |
| `ComplaincePlatform/scripts/build-praman-aovpkg.ps1` | `aovias-config\storage\local\packages\praman` | **U** |
| `Aovias.DeploymentValidation/Validators/DeploymentValidationStages.cs` | Detects legacy V1.0 path in output | **U** if new path should be allowed |
| `Aovias.Runtime.Agent/DEPLOYMENT.md` | Template path under `aovias-config/templates` | **D** |

### 5.5 Tests — **U**

| Area | Examples |
|------|----------|
| Layout contract tests | Fixtures create root `aovias-config`; some create `AOVIAS.Infrastructure/aovias-config` for target scenarios — extend **Current** post-W1 fixtures |
| `ConfigTemplateGeneratorTests.cs`, `RegressionTests.cs`, `EnvironmentNeutralPortabilityRegressionTests.cs` | `V:/DevelopmentRepo/V1.0/aovias-config` or relative `../../../../../../aovias-config` | **U** |
| `ContractDrivenPublishingTests.cs` | Excludes path segment `aovias-config` from scans | **U** — segment may still match under Infrastructure |
| `LoggingSettingsBinderTests.cs` | Relative walk to `aovias-config` | **U** |
| `DeploymentValidationTests.cs` | DeveloperConfigRoot constant | **U** |

### 5.6 Git ignore — **U**

| Path | Content |
|------|---------|
| `.gitignore` L79–82 | `aovias-config/{secrets,storage,logs,certificates}/` | **U** — add parallel rules for `AOVIAS.Infrastructure/aovias-config/...` or path-neutral pattern |

### 5.7 Tracked production `.cs` / `.ps1` / `.json` hit list (excl. tests/Layout)

```
Aovias.Bootstrap/src/Aovias.Bootstrap.Host/Program.cs
Aovias.DeploymentValidation/Validators/DeploymentValidationStages.cs
Aovias.Publishing/src/Aovias.Publishing.Core/Orchestration/SolutionPublishingPreflightService.cs
Aovias.Runtime.Agent/src/Aovias.Runtime.Agent.Api/appsettings.Development.json
ComplaincePlatform/scripts/build-praman-aovpkg.ps1
ComplianceServiceManager/appsettings.json
tools/PublishAllServices/Program.cs
tools/PublishRuntimeAgent/Program.cs
```

Plus **Layout** legacy files and **`repository-layout.json`** (manifest).

---

## 6. Comparison with Phase 1D-B inventory

| Topic | 1D-B | 1F-A (now) |
|-------|------|------------|
| Manifest tracked on all worktrees | **No** (blocker) | **Yes** at `cc3df70` |
| Resolvers wired to adapter | Untracked on Main | **Committed** |
| Core W1 code touch set | ~16 files + manifest + tests | **Same set** confirmed by fresh grep |
| `AOVIAS.Infrastructure/` exists | No | **Still no** |
| Dual-marker transition | Recommended | **Still required**, not implemented |
| CI/Compose W1 impact | Minimal | **Still minimal** |
| INV-2 `deployment.metadata.json` | Deferred | **Still not inventoried** |

---

## 7. Operational data preservation gate

### 7.1 Audit findings (this environment)

On **Main, Cursor, and Claude**, gitignored operational directories under `aovias-config/` **do not exist** (no files to inventory). **No local-only data at risk on these worktrees today.**

### 7.2 Required execution-time plan (any environment with data)

**Do not run during 1F-A.** Proposed operator procedure:

1. **Pre-migration inventory** — For each worktree copy of the repo, if `{repo}/aovias-config/{storage,secrets,logs,certificates}` exists, record relative paths, file counts, and total size (no secret contents).
2. **Backup** — Copy entire operational subtree to `{repo}/.w1-backup/aovias-config-YYYYMMDD-HHMM/` (or external volume). Verify backup file count/size matches inventory.
3. **Restore test** — On a scratch path, restore one non-secret subtree (e.g. `storage/local/packages` structure) and verify readability.
4. **Physical move** — After `git mv` of tracked tree, **move or copy** gitignored dirs from old path to `AOVIAS.Infrastructure/aovias-config/` (Git does not move ignored content).
5. **Permissions** — Preserve ACLs on Windows (robocopy `/COPY:DAT` or explicit icacls export/import) where used in deployment.
6. **Post-migration verification** — Publishing CLI default output path resolves; package roots referenced by `build-praman-aovpkg.ps1` exist; Bootstrap/CSM `ConfigRoot` or env points to new path.
7. **Rollback** — Revert merge commit; move operational dirs back to root `aovias-config/` from backup; restore `.gitignore` and resolver/manifest state from revert.

**Blocker rule:** On machines **with** non-empty operational dirs, W1 execution must **not** proceed without completed backup + verified restore test.

---

## 8. Proposed W1 atomic change set and order

**Branch policy:** Implement on `agent/cursor/w1-aovias-config` (or human-assigned task branch), single merge to `main`, ff-only agent worktrees — per `AGENTS.md`.

### 8.1 Order (same merge commit)

1. **Pre:** Backup operational dirs if present (human/operator).
2. **Code + manifest (before or with move):** Dual-marker legacy discovery; update `repository-layout.json` **Current** paths; shared `ConfigRoot` path helper (optional but reduces duplication); Publish tools; preflight walk; `.gitignore`; dev appsettings/examples; affected tests.
3. **Physical:** `mkdir AOVIAS.Infrastructure` (if needed); `git mv aovias-config AOVIAS.Infrastructure/aovias-config`.
4. **Post-move (same commit):** Move gitignored operational folders into new path if they existed.
5. **Validate:** Layout tests (29), Publishing path tests (2), Publish tool smoke, new dual-marker / post-move integration tests.

### 8.2 Earliest safe physical move

**After** legacy + manifest **Current** updates are in the **same commit** as `git mv`. Never move tree first.

### 8.3 Required tests (add or extend)

- New infrastructure path resolves (manifest + legacy).
- Old root path **absent** after cutover test; rollback restores old layout.
- Publishing vs CSM marker distinction unchanged.
- Missing manifest still safe (legacy dual-marker).
- `PublishAllServices` / `PublishRuntimeAgent` `ConfigRoot` and output folder at new location.
- Preflight runtime-agent Dockerfile discovery with new marker layout.
- Optional: Cursor worktree run after ff-only (manifest on disk).

### 8.4 Rollback conditions

- Any layout test failure; platform root resolves to fallback `..` path; ConfigRoot points at non-existent directory; duplicate root + infrastructure config trees.

---

## 9. Unresolved / not fully verified

| ID | Item |
|----|------|
| INV-2 | Full `deployment.metadata.json` path inventory |
| O-5 | Long-term `tools/Publish*` ownership under Infrastructure |
| Remote CI | No `.github` references found; remote pipelines not audited |
| `aovias-config/scripts/**` | No tracked scripts under config tree today (only templates) |
| Production VPS paths | Docs reference `/opt/aovias-config` — deployment-specific, not repo layout |

---

## 10. Final status

**READY FOR HUMAN W1 EXECUTION AUTHORIZATION**

Meaning: Post-foundation dependency closure is **complete enough** for a human **go/no-go** on W1 **execution** (task branch + atomic merge). It does **not** authorize running the move in this audit phase.

**Mandatory execution gates (human + implementer):**

1. Dual-marker + manifest **Current** update in **same commit** as `git mv`.
2. Operational backup/restore when gitignored dirs exist.
3. `.gitignore` path update for new operational location.
4. Focused test matrix green before merge to `main`.

---

## 11. Git operations in Phase 1F-A

**None** — no staging, commit, move, or synchronization.
