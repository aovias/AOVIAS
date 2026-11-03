# AOVIAS Phase 1H-B — Residual Risk Assessment and Runtime-Proof Plan

**Phase:** 1H-B (read-only synthesis)  
**Date:** 2025-10-25  
**Baseline commit (all worktrees):** `7758e30ce0f8c406fe8f292006895f67fb31cafd`

**Confirmation:** No source files were modified, staged, or committed. No publishing, package, deployment, Docker, or synchronization operations were performed. Only this untracked report was created on Main.

---

## Final status

**NO CRITICAL RESIDUAL RISK IDENTIFIED — VALIDATION GAPS REMAIN**

W1 configuration migration and Phase 1G-D discovery hardening are **test-proven** for layout/resolver contracts on this baseline. **Critical** mis-resolution via silent `..` fallbacks was **remediated** in merge `7758e30`. Remaining items are **validation gaps** (CLI/Docker/DRV not executed post-merge), **machine-local hypotheses** (legacy gitignored ops data), or **deferred** maintainability/docs debt—not confirmed production defects on the integrated worktrees.

---

## 1. Worktree safety snapshot

| Worktree | Path | Branch | HEAD | Tracked tree |
|----------|------|--------|------|--------------|
| Main | `V:\DevelopmentRepo\AOVIAS` | `main` | `7758e30ce0f8c406fe8f292006895f67fb31cafd` | Clean |
| Cursor | `V:\DevelopmentRepo\AOVIAS.Cursor` | `agent/cursor/active` | `7758e30…` | Clean |
| Claude | `V:\DevelopmentRepo\AOVIAS.Claude` | `agent/claude/active` | `7758e30…` | Clean |

**Preserved artifacts (spot-check):**

- Cursor: `scripts/dev/_phase3h_runtime_probe.py` — **present** (not executed, excluded from all proposed steps below).
- Claude: `Aovias.CommandCenter/.../Properties/launchSettings.json` — **present**.
- Main/Cursor: Phase reports, ZIPs, Lighthouse outputs — **untracked**, unchanged pattern.

**Gate:** Expected state matched. No repair or sync performed.

---

## 2. Evidence sources inspected

| Document | Path | Role |
|----------|------|------|
| Phase 1H-A layout reality | Main: `AOVIAS_PHASE1HA_REPOSITORY_LAYOUT_REALITY_REPORT.md` | W1 filesystem/Git/manifest; no broader reorg |
| Phase 1G-A runtime/deployment audit | Main: `AOVIAS_PHASE1GA_POST_W1_RUNTIME_DEPLOYMENT_AUDIT.md` | Scenario matrices, workflow table (partially pre–1G-D for G3) |
| Phase 1G-B follow-up triage | Main: `AOVIAS_PHASE1GB_FOLLOWUP_TRIAGE.md` | G1–G4 backlog, V1–V13 validation plan |
| Phase 1G-D hardening report | Cursor: `AOVIAS_PHASE1GD_DISCOVERY_HARDENING_REPORT.md` | Fail-fast legacy, tool/Bootstrap alignment |
| Phase 1G-D-R code review | Cursor: `AOVIAS_PHASE1GDR_CODE_REVIEW_REPORT.md` | M1–M5, P1–P14 |
| Phase 1G-D-F minor triage | Cursor: `AOVIAS_PHASE1GDF_MINOR_FINDINGS_TRIAGE_REPORT.md` | Accept/defer M1–M5 |
| Phase 1G-F merge validation | Main: `AOVIAS_PHASE1GF_MERGE_AND_VALIDATION_REPORT.md` | Post-merge test counts; NU1903 note |
| Layout manifest | `repository-layout.json` | Current vs target discovery |
| Production adapter | `Aovias.Configuration/Layout/RepositoryLayoutProductionAdapter.cs` | Manifest-first + legacy |
| Config paths | `Aovias.Configuration/Layout/RepositoryLayoutAoviasConfigPaths.cs` | Infra vs legacy config root |
| Legacy discovery | `LegacyPublishingPlatformRootDiscovery.cs`, `LegacyCsmPlatformRootDiscovery.cs`, `RepositoryLayoutPlatformRootDiscovery.cs` | Markers, fail-fast (post–1G-D) |
| Publishing preflight | `Aovias.Publishing/.../SolutionPublishingPreflightService.cs` (`FindDockerfile` ~L821–847) | Dockerfile discovery walk |
| Config template / Docker staging | `Aovias.Publishing/.../Generation/Generators.cs` (`ConfigTemplateGenerator`), `RuntimeAgentDockerContextStager.cs` | Template gen + monorepo stage |
| Bootstrap | `Aovias.Bootstrap/.../Program.cs` (`ResolveLocalPlatformRoot`, default `ConfigRoot`) | Publishing markers + `ResolveConfigRootDirectory` |
| Praman script | `ComplaincePlatform/scripts/build-praman-aovpkg.ps1` | Infra/legacy config root selection |

**Note on 1G-A vs 7758e30:** 1G-A §G3 documents pre–1G-D `..` fallbacks. Those paths were **removed** in the integrated merge; residual risk register treats G3 as **closed** for silent fallback, with remaining gap = **tooling not executed at CLI level** (see R-08).

---

## 3. Prioritized residual-risk register

Severity: **Critical / High / Medium / Low / Informational**  
Evidence: **Runtime-proven / Test-proven / Static-only / Unverified**

| ID | Description | Components / references | Severity | Evidence | Impact / likelihood | Classification | Smallest next action | Constraints |
|----|-------------|-------------------------|----------|----------|---------------------|----------------|----------------------|-------------|
| **R-01** | Config root resolution after W1 | `RepositoryLayoutAoviasConfigPaths.ResolveConfigRootDirectory`; CSM/Agent `appsettings*`; publish CLIs | **Low** | **Test-proven** (W1 transition + 1G-F 39 layout tests); **Static-only** for live CLI | Wrong templates/packages if config root wrong — **low** on full monorepo | **No action required** (contract met) | Optional: re-run layout filter at `7758e30` (Stage A) | None |
| **R-02** | Legacy root `aovias-config/` coexistence | Dual-marker in `ConfigDirectoryExistsAtRepositoryRoot`; manifest `requiredAll` infra only | **Low** | **Test-proven** (legacy-only + both-exist-prefers-infra); **Static-only** on worktrees (no legacy dir) | Rollback machines may keep legacy ops tree — **hypothesis** | **Validation gap** (other machines) | Inventory gitignored storage on deploy hosts before deleting legacy path | Do not delete ops data without backup |
| **R-03** | Partial-marker directories (e.g. app without config) | `RepositoryLayoutPlatformRootDiscovery.Matches*` | **Low** | **Static-only**; M3 deferred | Walk continues; eventual throw — **low** mis-root risk post–1G-D | **Deferred improvement** | Add 1–2 unit tests (M3) when authorized | Test-only change |
| **R-04** | Manifest missing/invalid → legacy path | `RepositoryLayoutProductionAdapter.TryResolve*` → legacy `Resolve` | **Medium** | **Test-proven** (missing/invalid manifest tests); **Unverified** for every entrypoint | Mis-invocation from non-repo cwd now **throws** instead of silent `..` — **reduced** vs pre–1G-D | **Validation gap** (negative CLI) | Stage A: negative test already exists; Stage B: `PublishAllServices --discover` from `%TEMP%` must fail | Stage B needs auth |
| **R-05** | Publishing preflight Dockerfile discovery | `SolutionPublishingPreflightService.FindDockerfile` | **Low** | **Static-only**; uses config dual-marker + `ComplaincePlatform`, not adapter | Works on current root layout; **not** manifest-driven | **Validation gap** | Stage B: `--preflight-only` with temp output dir | Writes temp artifacts |
| **R-06** | Config template generation paths | `ConfigTemplateGenerator`; `ConfigTemplateGeneratorTests` | **Low** | **Test-proven** (6/6 at 1G-F) | Generated packages could embed wrong paths — **not observed** in tests | **No action required** | Re-run ConfigTemplateGenerator filter (Stage A) | None |
| **R-07** | Docker context staging / COPY roots | `RuntimeAgentDockerContextStager` (L35–50 walk: `Aovias.Configuration` + `Aovias.Runtime.Agent/src`); `Generators.cs` L349–363 | **Medium** | **Test-proven** (stager unit tests per 1G-A); **Unverified** full `docker build` | Stager **does not** use infra config marker; **valid today** at root layout; breaks if Studio nesting without stager update | **Validation gap** | Stage A: `RuntimeAgentDockerContextStagerTests`; Stage B: `docker build` (V12) | Stage B auth; Docker daemon |
| **R-08** | Publish CLI / tool execution post–1G-D | `tools/PublishAllServices/Program.cs`, `PublishRuntimeAgent/Program.cs` | **Medium** | **Static-only** (adapter wired); **Unverified** `dotnet run` | Discover/publish could fail at runtime despite tests — **medium** until V7+ | **Validation gap** | Stage B: V7 discover-only, then V8 preflight-only | Separate authorization |
| **R-09** | Bootstrap default platform/config resolution | `Aovias.Bootstrap.Host/Program.cs` L431–438 | **Medium** | **Static-only**; M2 accepted design | Stricter fail-fast vs old `BaseDirectory` default — **compatibility** for invalid cwd, not defect | **Validation gap** | Stage B/C: bootstrap with explicit `--config-root`; negative test from non-repo dir | DRV/VPS auth for full bootstrap |
| **R-10** | Praman package script | `build-praman-aovpkg.ps1` L9–17 | **Low** | **Static-only**; M4 dead branch | Script throws if config/ComplaincePlatform missing — **good**; full publish **not run** | **Validation gap** | Stage B: V10 with isolated config root | **Destructive** (dotnet publish, `.publish` delete) |
| **R-11** | `PublishRuntimeAgent` uses publishing OR-markers | `PublishRuntimeAgent/Program.cs`; M1 | **Low** | **Static-only**; triage **acceptable** | Edge tree with only ComplaincePlatform could resolve then fail on project path — **not silent wrong publish** | **Deferred improvement** | Optional integration test from tool `bin/Release` | Low priority |
| **R-12** | `ResolveConfigRootDirectory` when neither config dir exists | `RepositoryLayoutAoviasConfigPaths.cs` L51 | **Low** | **Static-only** (1G-A §8.6) | Returns non-existent infra path; downstream fail — **explicit** failure | **Validation gap** | Covered indirectly by fail-fast platform tests; optional unit test | Test-only |
| **R-13** | NU1903 (SSH.NET / advisories) on Platform/CSM slnx | Documented Phase 0B/1A; 1G-F §7 | **Medium** | **Static-only** (historical reports); **Unverified** at `7758e30` | Blocks full solution restore/build for some workflows — **known pre-W1** | **Deferred technical debt** | Separate dependency upgrade task; not layout-related | Package upgrades need review |
| **R-14** | Test coverage vs production entrypoints | 1G-F ran subset; 1G-B V4/V6 not re-listed in 1G-F | **Medium** | **Test-proven** partial | DeploymentValidation + full stager re-run may surface regressions | **Validation gap** | Stage A: V4 + V6 (see §4) | Release builds only |
| **R-15** | Git worktree / artifact preservation | `AGENTS.md`; phase reports untracked | **Informational** | **Runtime-proven** (1G-H sync) | Accidental commit of reports/ZIPs — process risk | **No action required** | Keep reports untracked until human commits selectively | Never `git clean` without auth |
| **R-16** | Broader root-directory reorganization | `repository-layout.json` `target` paths; 1H-A conclusion | **Informational** | **Static-only** | Premature Command/Studio moves would **High** break resolvers, slnx, Docker COPY | **No action required** | Do **not** authorize product tree moves until target wave plan + INV-1 | W1 scope was templates only |
| **R-17** | Historical / milestone docs cite root `aovias-config` | 1G-B G1; many `AOVIAS_PHASE*.md` | **Low** | **Static-only** | Operator confusion — **not** runtime on integrated code | **Deferred technical debt** | Docs-only task (G1-S largely addressed in merged 1G-C for DEPLOYMENT/VPS/dockerignore) | Docs-only authorization |
| **R-18** | Legacy gitignored ops data at old path | `.gitignore` L79–86; 1G-A §9 | **Medium** | **Unverified** on these worktrees (absent) | Publishing could read empty/wrong storage if env points at legacy path — **machine-dependent** | **Validation gap** | Host inventory + migrate storage to infra path under `AOVIAS_CONFIG_ROOT` | Backup first |
| **R-19** | `RuntimeAgentDockerContextStager.TryResolvePlatformRoot` separate from layout adapter | `RuntimeAgentDockerContextStager.cs` L35–50 | **Low** | **Static-only** | Divergent discovery if markers ever disagree — **unlikely** on full repo | **Deferred improvement** | Align stager with adapter in a **future** refactor wave | Not W1 scope |
| **R-20** | End-to-end DRV / bootstrap / V1.0 path leak guard | `DeploymentValidationStages.cs` L538–539; 1G-B V11 | **Medium** | **Test-proven** (unit fixtures); **Unverified** DRV | Stale absolute paths in package output — guarded but not re-run | **Validation gap** | Stage C: V11 when Docker available | Operational auth |

**Confirmed defects on `7758e30` worktrees:** None identified from inspected evidence (no runtime failure observed; silent `..` platform root **removed**).

---

## 4. Staged runtime-proof plan

### Explicit exclusions

- **`scripts/dev/_phase3h_runtime_probe.py`:** **Excluded** from all stages unless a human authorizes a new, separately scoped probe after review.
- **No** mass doc edits, **no** repository moves, **no** `git clean`, **no** remote push in this plan.

---

### Stage A — Safe local validation (no publish/deploy artifacts)

May be authorized as a single “validation bundle” phase; **does not** write packages under config `storage/`.

| ID | Target | Command (repo root `V:\DevelopmentRepo\AOVIAS`) | Expected evidence | Outputs / side effects | Auth | Risks |
|----|--------|--------------------------------------------------|-------------------|------------------------|------|-------|
| A1 | Layout + 1G-D hardening | `dotnet test Aovias.Configuration/tests/Aovias.Configuration.Tests/Aovias.Configuration.Tests.csproj --filter "FullyQualifiedName~Layout"` | All pass (baseline **39/39** at 1G-F) | `bin/`/`obj/` | Low | None significant |
| A2 | Publishing project paths | `dotnet test Aovias.Publishing/tests/Aovias.Publishing.Core.Tests/Aovias.Publishing.Core.Tests.csproj --filter "FullyQualifiedName~PublishingProjectPathsTests"` | **2/2** pass | bin/obj | Low | None |
| A3 | Config templates | Same project, `--filter "FullyQualifiedName~ConfigTemplateGenerator"` | **6/6** pass | bin/obj | Low | None |
| A4 | DeploymentValidation unit suite | `dotnet test Aovias.DeploymentValidation/tests/Aovias.DeploymentValidation.Tests/Aovias.DeploymentValidation.Tests.csproj` | All pass | bin/obj | Low | None |
| A5 | Docker stager | `dotnet test Aovias.Publishing/tests/Aovias.Publishing.Core.Tests/Aovias.Publishing.Core.Tests.csproj --filter "FullyQualifiedName~RuntimeAgentDockerContextStager"` | Pass; temp dirs under `%TEMP%` | Temp (test cleanup) | Low | None |
| A6 | Publishing.Core Release build | `dotnet build Aovias.Publishing/src/Aovias.Publishing.Core/Aovias.Publishing.Core.csproj -c Release` | 0 errors (1G-F: 0 warnings) | bin/obj | Low | None |
| A7 | Read-only marker check | Manual or script: confirm no `<root>/aovias-config`; infra templates present (1H-A §7 commands) | Matches manifest `discovery.current` | None | Low | None |

**Stage A gap vs 1G-F:** 1G-F executed A1–A3, A6; **did not** record A4, A5, A7 at merge time.

---

### Stage B — Artifact-producing validation (**separate authorization each**)

| ID | Target | Command / entry | Expected evidence | Output locations | Risks | Cleanup |
|----|--------|-----------------|-------------------|------------------|-------|---------|
| B1 (V7) | Publish discover-only | `dotnet run --project tools/PublishAllServices/PublishAllServices.csproj -- --discover` | Non-zero deployable count; paths under `ComplaincePlatform` / Runtime Agent | Console | Low write | None |
| B2 (V8) | Preflight-only | `--preflight-only` + `Publishing:OutputFolder` under `%TEMP%\aovias-preflight-*` | Preflight checks pass | Temp logs/reports | Medium write | Delete temp tree |
| B3 (V9) | Full publish CLI | Default publish flow | Valid deploy artifacts | `{configRoot}/storage/local/packages` (**gitignored**) | **High** — overwrites packages | Manual delete under test root only |
| B4 (V10) | Praman script | `ComplaincePlatform/scripts/build-praman-aovpkg.ps1` | Exit 0 | `ComplaincePlatform/.publish`, `storage/.../praman` | **High** — dotnet publish | Remove test outputs |
| B5 (V12) | Runtime Agent image | `docker build` with staged context (stager pattern) | Build success | Local image | Medium | `docker rmi` |

**Recommended isolated config pattern (1G-B §5.2):** Point `AOVIAS_CONFIG_ROOT` at `%TEMP%\aovias-e2e-{guid}\...` with minimal profiles; never use operator secrets without backup.

---

### Stage C — Operational validation (**separate authorization**)

| ID | Target | Entry | Expected evidence | Risks |
|----|--------|-------|-------------------|-------|
| C1 (V11) | DRV / bootstrap reliability | `Aovias.DeploymentValidation` reliability tests or manual DRV | Stages pass; bootstrap output **must not** contain `V:/DevelopmentRepo/V1.0/aovias-config` (guard L538–539) | Containers, networks |
| C2 | VPS / production-like bootstrap | Bootstrap CLI `bootstrap` verb against test VPS | Agent install + config scaffold | Production-like side effects |
| C3 (V13) | Template consumption in published package | Inspect package from B3 | Templates reference correct config root / infra-relative paths | Depends on B3 |

---

## 5. Prioritization and recommendations

### Must fix before further rollout

**None identified** as confirmed defects on integrated `7758e30` worktrees.

### Must validate before broader rollout

1. **Complete Stage A** (especially **A4**, **A5** not recorded at 1G-F) — highest risk reduction per unit effort.  
2. **Stage B1 (discover-only)** — proves publish CLI + adapter at runtime without package writes.  
3. **Stage B2 (preflight-only)** with temp output — proves `FindDockerfile` + preflight chain.  
4. **Host inventory (R-18)** before telling operators to delete legacy `aovias-config/storage`.

### Deferred technical debt

- **R-13** NU1903 / Platform slnx restore (parallel track, not W1/1G blocker).  
- **R-03, R-11, M4** — minor tests/script cleanup.  
- **R-17** — historical docs / optional index (`docs/development/REPOSITORY-LAYOUT-W1.md`).  
- **R-19** — stager vs adapter unification (future layout wave only).

### No action required

- **R-01, R-06, R-15, R-16** — W1 verified; worktrees aligned; no broader move justified (1H-A).

---

### Smallest recommended next phase

**Proposed: Phase 1H-C (or 1I-A) — “Post-merge Stage A completion + discover-only proof”**

1. Run **A4, A5, A7** (and optionally refresh A1–A3, A6) on Main at `7758e30`; record results in an execution report.  
2. If green, **separate authorization** for **B1 (`--discover`)** only.

**Why before any refactor:** Confirms merge integration without writing gitignored packages or invoking Docker/DRV. Addresses the largest remaining **Unverified** bucket (R-08) with minimal blast radius. **Must precede** Command/Studio tree moves (R-16), which would invalidate stager, scan roots, and slnx paths without a dedicated migration wave.

---

## 6. Summary table (risk × action)

| Category | Count | Representative IDs |
|----------|-------|---------------------|
| Validation gap | 9 | R-04, R-05, R-07, R-08, R-09, R-10, R-14, R-18, R-20 |
| Deferred improvement / debt | 5 | R-03, R-11, R-13, R-17, R-19 |
| No action required | 4 | R-01, R-06, R-15, R-16 |
| Closed (was G3 pre–1G-D) | — | Silent `..` platform root (**remediated** in `7758e30`) |

---

## 7. Phase 1H-B completion

| Deliverable | Path |
|-------------|------|
| Residual risk assessment | `AOVIAS_PHASE1HB_RESIDUAL_RISK_ASSESSMENT.md` (untracked on Main) |

**Final status:** **NO CRITICAL RESIDUAL RISK IDENTIFIED — VALIDATION GAPS REMAIN**
