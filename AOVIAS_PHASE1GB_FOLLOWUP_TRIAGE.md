# AOVIAS Phase 1G-B — Post-W1 Follow-up Triage and Remediation Plan

**Date:** 2026-10-25  
**Source audit:** `AOVIAS_PHASE1GA_POST_W1_RUNTIME_DEPLOYMENT_AUDIT.md` (G1–G4)  
**Scope:** Read-only triage; **no** implementation, Git writes, or workflow execution.

---

## 1. Worktree baseline and preservation

| Worktree | Branch | HEAD | Index |
|----------|--------|------|-------|
| Main | `main` | `202dd525e8661ca0c3db68b40ab3a888140b79cf` | Clean |
| Cursor | `agent/cursor/active` | `202dd525…` | Clean |
| Claude | `agent/claude/active` | `202dd525…` | Clean |

**Preserved (untracked):** Cursor probe `scripts/dev/_phase3h_runtime_probe.py`; Claude `Properties/launchSettings.json`; Phase reports on Main/Cursor. Probe **not executed**.

**Phase 1G-A report:** Present on Main at `AOVIAS_PHASE1GA_POST_W1_RUNTIME_DEPLOYMENT_AUDIT.md`.

---

## 2. G1 — Documentation triage

### 2.1 Classification key

| Class | Action |
|-------|--------|
| **H** | Historical / sprint snapshot — retain; optional one-line “as of date” note only |
| **S** | Describes **current** operator behavior — update if factually stale |
| **M** | Migration-era Phase report — retain; add supersession pointer, do not rewrite history |
| **C** | Compatibility / VPS convention (`/opt/aovias-config`) — clarify vs repo layout |

### 2.2 Operator-facing / current-layout docs (priority fixes)

| Path | Line / section | Class | Stale? | Smallest proposed correction |
|------|------------------|-------|--------|------------------------------|
| `Aovias.Runtime.Agent/DEPLOYMENT.md` | L60: `aovias-config/templates/publishing/docker-compose.runtime-agent.yml` | **S** | **Yes** — templates tracked under `AOVIAS.Infrastructure/aovias-config/templates/` | Replace path with `AOVIAS.Infrastructure/aovias-config/templates/publishing/...` (or “infra config templates tree”) |
| `ComplianceServiceManager/docs/sprint5/VPS-Bootstrap-Guide.md` | L7 `/opt/aovias-config`; L19 `V:\DevelopmentRepo\V1.0\aovias-config` | **S/C** | **Partial** — VPS path OK; Windows example is obsolete dev layout | Keep `/opt/...`; replace Windows example with repo-relative `AOVIAS.Infrastructure/aovias-config` or `$AOVIAS_CONFIG_ROOT` |
| `Aovias.Runtime.Agent/.env.example` | L15 `AOVIAS_HOST_CONFIG_ROOT=/path/to/aovias-config` | **C** | Neutral placeholder | Optional: “host path to config root (not repo-relative)” — no path change required |
| `Aovias.Runtime.Agent/.dockerignore` | L12 `aovias-config/storage/` | **S** | **Low** — ignore pattern for staged contexts | Add `AOVIAS.Infrastructure/aovias-config/storage/` alongside legacy line |

### 2.3 Milestone / guidelines docs (`docs/milestones/`)

| Path | Example reference | Class | Stale? | Correction |
|------|-------------------|-------|--------|------------|
| `M1.2-IMPLEMENTATION-REPORT.md` | L51 `aovias-config/templates/publishing/...` | **H** | For **2026-07** sprint context | Add footnote: “Repo layout post–Phase 1F-W1: templates under `AOVIAS.Infrastructure/aovias-config/`.” |
| `M1.4-IMPLEMENTATION-REPORT.md` | `aovias-config/profiles/qa/...` | **H** | Profiles live under **config root** on disk, not repo root | Footnote: config root may be infra path; profiles are not repo-root-relative. |
| `MILESTONE-1-DETERMINISTIC-PUBLISHING.md` | D10 storage under `aovias-config/storage/...` | **H** | Path prefix depends on config root | Footnote only |
| `ROADMAP_STATUS_REVIEW.md`, `CURRENT-STATE-ASSESSMENT-2026-07-12.md` | Packages under `aovias-config/storage/...` | **H** | Point-in-time evidence | **Do not rewrite**; optional index link to 1G-A |
| `IMPLEMENTATION_GUIDELINES.md` | L46 exclude stale `aovias-config` snapshots | **H/C** | Still valid rule | None |
| `MILESTONE-2-INFRASTRUCTURE-CONTRACT-ARCHITECTURE.md` | L310 `aovias-config/scripts/provision-deployment-storage.sh` | **H** | Script may not exist at repo root post-W1 | Verify script location before edit; likely **H** unless script moved |

### 2.4 Phase reports (untracked on Main)

| Path | Issue | Class | Correction |
|------|-------|-------|------------|
| `AOVIAS_PHASE1CA_LAYOUT_CONTRACT.md` | L54 “root `aovias-config`” as Current | **M** | Superseded by W1 — add header: “Pre-W1; see `202dd525` + 1G-A.” |
| `AOVIAS_PHASE1EB_INTEGRATION_PREFLIGHT.md` | L114–115 markers list root `aovias-config` | **M** | Same supersession note |
| `AOVIAS_PHASE1FA_W1_MIGRATION_REVALIDATION.md` | Pre-merge state narrative | **M** | Keep; already historical |
| `AOVIAS_PHASE1B_*`, `AOVIAS_PHASE1DB_*` | W1 planning | **M** | Keep; mark “executed in 1F-E” |
| `AOVIAS_PHASE0_BASELINE_VALIDATION.md` | L166 templates at `aovias-config/` | **M** | Supersession note only |

**Recommendation (G1):** One **new** short index doc (e.g. `docs/development/REPOSITORY-LAYOUT-W1.md`) linking manifest Current profile + infra config path — **avoid** mass-editing milestone PDFs/reports. Patch **S** items (`DEPLOYMENT.md`, VPS guide, `.dockerignore`) in a **docs-only** authorized task.

---

## 3. G2 — DeploymentValidation fixture triage

**Note:** `DeploymentValidationTests.cs` hosts class **`EnvironmentNeutralRulesTests`** (not a separate file).

### 3.1 Fixture-by-fixture assessment

| Location | Symbol / test | Role of `V1.0` / `aovias-config` | Verdict | Proposed change (when authorized) |
|----------|---------------|-----------------------------------|---------|-----------------------------------|
| `DeploymentValidationTests.cs` L10, L18, L49 | `DeveloperConfigRoot`, `FindViolations_detects_developer_paths`, `detects_windows_drive_paths` | **Negative fixtures** — must trigger neutrality violations | **Keep as-is** (intentionally “bad” path) | Optional: rename constant to `ExampleDeveloperLeakPath`; **do not** change to infra path (would weaken test) |
| `BootstrapValidationTests.cs` L30 | `PatchValidationRuntimePaths_rewrites_host_specific_deployment_settings` | Input JSON with host-specific storage root | **Synthetic leak input** — not asserting repo layout | Replace string with `V:/Example/build/aovias-config/storage/local/deployments` **or** `C:/temp/...` — **cosmetic**; behavior unchanged |
| `EnvTemplateValidationRulesTests.cs` L22 | `AOVIAS_HOST_CONFIG_ROOT=/path/to/aovias-config` | Placeholder detection | **Intentional** generic placeholder | **No change** |
| `RegressionTests.cs` L14–15, L80 | `Neutrality_rejects_developer_repo...`, `Resolver_rejects_profile...` | Leak detection (`V1.0/config`) | **Intentional** | **No change** |
| `DeploymentValidationStages.cs` L538–541 | Bootstrap stage output scan | **Production guard** — fail if agent prints V1.0 path | **Intentional** | **No change** |
| `EnvironmentNeutralPortabilityRegressionTests.cs` (Publishing) L227 | JSON configRoot leak | Publishing neutrality regression | **Intentional** negative | **No change** (outside DeploymentValidation project but related) |

### 3.2 Production trace

- **`EnvironmentNeutralRules` / `PackageArtifactScanner`:** Scan **content** for developer paths; no filesystem dependency on `V1.0`.
- **`TemporaryDeploymentWorkspace.PatchValidationRuntimePaths`:** Rewrites profile JSON to `/opt/aovias/runtime/*` — test proves rewrite, not discovery.
- **`DeploymentValidationStages` bootstrap stage:** Parses container stdout; V1.0 substring is **forbidden output**, aligned with tests.

### 3.3 G2 remediation plan (authorized phase)

| Priority | Action | Regression coverage |
|----------|--------|---------------------|
| P2 (optional) | Cosmetic input path in `BootstrapValidationTests` only | Re-run `dotnet test Aovias.DeploymentValidation.Tests.csproj --filter PatchValidationRuntimePaths` |
| — | **Do not** replace `V1.0/aovias-config` in violation-detection tests | Existing tests **are** the coverage |

**Conclusion:** Phase 1G-A **G2** is largely **misclassified severity** — fixtures are **negative tests**, not stale layout assumptions. Only Bootstrap test input is a optional clarity edit.

---

## 4. G3 — Legacy `..` fallback review

### 4.1 Implementations (confirmed)

| Component | Fallback when walk fails | Lines |
|-----------|--------------------------|-------|
| `LegacyPublishingPlatformRootDiscovery.Resolve` | `Path.GetFullPath(Combine(baseDirectory, "..", x5))` | `LegacyPublishingPlatformRootDiscovery.cs` L24 |
| `LegacyCsmPlatformRootDiscovery.Resolve` | `Path.GetFullPath(Combine(baseDirectory, "..", x4))` | `LegacyCsmPlatformRootDiscovery.cs` L23 |
| `tools/PublishAllServices/Program.cs` `ResolvePlatformRoot` | 4× `..` from `AppContext.BaseDirectory` | L246 |
| `tools/PublishRuntimeAgent/Program.cs` `ResolvePlatformRoot` | 4× `..` | L95 |
| `Aovias.Bootstrap.Host/Program.cs` `ResolveLocalPlatformRoot` | Returns `AppContext.BaseDirectory` (no `..`) | L450 |

**Why it exists:** Pre-layout / pre-manifest heuristic when upward walk finds no marker directories (legacy dev layouts, running from `bin` without repo checkout).

**Callers:** `RepositoryLayoutProductionAdapter` (after failed manifest + failed legacy walk still returns legacy result); Publishing `PlatformRootResolver`; CSM `RootPathResolver.ResolvePlatformRoot`; CLI tools (duplicate walks, not adapter).

### 4.2 Security and determinism

| Question | Finding | Evidence type |
|----------|---------|---------------|
| Escape repository boundary? | **No confirmed CVE** — `Path.GetFullPath` resolves within filesystem; no `..` above drive root | **Theoretical** only if symlinks/junctions point outside — **not tested** |
| Wrong product root? | **Confirmed risk** — fixed `..` depth may land on non-repo directory without markers | **V-C** + test `Csm_does_not_resolve_when_neither_marker_present` expects **non-empty** legacy result |
| Multiple candidates | Parent walk is **deterministic** (single chain); manifest resolver **nearest ancestor wins** | **V-T** `TryResolveRepositoryRoot_prefers_nearest_ancestor_when_multiple_match` |
| Enumeration order | Walk uses `DirectoryInfo.Parent`, not directory listing | **V-C** |
| Symlinks / malformed paths | Not covered | **NOT COVERED BY TESTS** |

**Confirmed vs theoretical:** Mis-resolution when markers absent is **confirmed behavior** (by design fallback), not proven “safe” for publish/deploy. **Not** a confirmed authentication bypass or path traversal exploit.

### 4.3 Existing tests

| Case | Covered? |
|------|----------|
| Successful legacy / manifest resolve | **Yes** — layout 34 tests, W1 transition |
| Neither marker / empty manifest area | **Partial** — CSM asserts non-empty fallback; **no** assert on path quality |
| Tool `ResolvePlatformRoot` when run from repo | **NOT COVERED** — tools duplicate logic vs adapter |
| Fallback path lacks ComplaincePlatform + config | **NOT COVERED BY TESTS** |

### 4.4 Smallest safe hardening (proposal only)

1. **Single helper** (future authorized change): `PlatformRootDiscoveryFallback` returning `null` or throwing `InvalidOperationException` when walk fails — used by legacy classes and tools **or** tools call `RepositoryLayoutProductionAdapter` instead of local walks.
2. **Publishing/CSM:** Prefer **fail-fast** in CLI tools; services may still need documented default for dev (Bootstrap already uses `AppContext.BaseDirectory`).
3. **Tests to add:**
   - `LegacyPublishingPlatformRootDiscovery_returns_non_markered_path_when_no_match` — document current path; after hardening, expect throw or empty.
   - Integration: `dotnet run ... PublishAllServices --discover` from repo root **must** discover >0 projects (positive); from `%TEMP%` **must** fail fast (negative).

**Do not** introduce new security policy beyond fail-fast when platform root cannot be validated against dual-marker or manifest rules.

---

## 5. G4 — Controlled end-to-end validation plan

**Goal:** Prove post-W1 publishing/deploy paths without contaminating production config storage.

### 5.1 Safe in audit / CI-style (low side effect)

| Step | Entry point | Command (from repo root `V:\DevelopmentRepo\AOVIAS`) | Outputs | Side effects | Pass criteria |
|------|-------------|------------------------------------------------------|---------|--------------|---------------|
| V1 | Layout + publishing unit tests | `dotnet test Aovias.Configuration.Tests.csproj --filter FullyQualifiedName~Layout` | Test results | `bin/`/`obj/` | All pass (baseline: 34) |
| V2 | Publishing paths | `dotnet test Aovias.Publishing.Core.Tests.csproj --filter FullyQualifiedName~PublishingProjectPathsTests` | Test results | bin/obj | 2/2 pass |
| V3 | Config templates | `dotnet test ...Publishing.Core.Tests.csproj --filter ConfigTemplateGenerator` | Test results | bin/obj | 6/6 pass |
| V4 | DeploymentValidation unit | `dotnet test Aovias.DeploymentValidation.Tests.csproj` | Test results | bin/obj | All pass |
| V5 | Publishing.Core Release | `dotnet build Aovias.Publishing.Core.csproj -c Release` | DLL | bin/obj | 0 errors |
| V6 | Docker stager | `dotnet test ...RuntimeAgentDockerContextStagerTests` | Temp staging dir under `%TEMP%` | Temp dirs | Pass; cleanup in test `finally` |

**Authorization:** Can run under **Phase 1G-C “validation only”** without separate publish auth.

### 5.2 Requires isolated config root or human authorization

| Step | Workflow | Prerequisites | Command / entry | Output locations | Side effects | Cleanup | Pass criteria |
|------|----------|---------------|-----------------|------------------|--------------|---------|---------------|
| V7 | Publish discover-only | Built SDK; repo markers present | `dotnet run --project tools/PublishAllServices/PublishAllServices.csproj -- --discover` | Console only | Minimal | None | Non-zero deployable count; paths under `ComplaincePlatform` / Runtime Agent |
| V8 | Publish preflight-only | V7; writable **temp** output dir via env/settings | `--preflight-only` + explicit `Publishing:OutputFolder` under `%TEMP%\aovias-preflight-*` | Temp preflight artifacts | May write logs/temp | Delete temp tree | Preflight reports pass |
| V9 | Full publish CLI | **Separate auth** — writes packages | Default output `{configRoot}/storage/local/packages` | **Gitignored** infra config storage | **Overwrites/adds packages** | Manual delete under test config root | Valid `.aovdeploy` produced |
| V10 | `build-praman-aovpkg.ps1` | **Separate auth** | `ComplaincePlatform/scripts/build-praman-aovpkg.ps1` | `ComplaincePlatform/.publish`, `configRoot/storage/local/packages/praman` | **dotnet publish** + folder delete L29 | Remove `.publish` test output | Script completes exit 0 |
| V11 | DeploymentValidation DRV/bootstrap | Docker daemon; images; **Separate auth** | Test suite `Reliability/*` or manual DRV | Workspace under test profile | Containers, networks | Test harness cleanup | Stages pass; no V1.0 in bootstrap output (L538 guard) |
| V12 | Runtime Agent `docker build` | Docker; staged context from V6 pattern | `docker build -f Aovias.Runtime.Agent/Dockerfile` with context = staged monorepo copy | Local Docker image | Image layers | `docker rmi` | Build success; no missing COPY roots |
| V13 | Template consumption | Published package or generator test | Covered by V3 + optional V9 inspect | Package zip contents | V9 dependent | — | Templates reference infra paths in generated compose |

**Isolated temp config root pattern (recommended for V8–V10):**

1. Create `%TEMP%\aovias-e2e-{guid}\AOVIAS.Infrastructure\aovias-config\` with minimal `profiles/local` copied from repo **or** symlink junction to repo infra path (Windows: document junction risk).
2. Set `AOVIAS_CONFIG_ROOT` / in-memory bootstrap to temp root.
3. Never point at operator `storage/secrets` without backup.

**Runtime probe:** **Out of scope** — do not run `_phase3h_runtime_probe.py`.

---

## 6. Prioritized remediation backlog

| Rank | ID | Item | Owner worktree | Est. scope |
|------|-----|------|----------------|------------|
| **P0** | G1-S | Fix `Aovias.Runtime.Agent/DEPLOYMENT.md` template path | Cursor task branch | 1–2 lines |
| **P1** | G4 | Execute **V1–V6** validation bundle; record in execution report | Main or Cursor | Tests only |
| **P2** | G1-S | VPS Bootstrap guide Windows example + `.dockerignore` infra line | Cursor | Small docs |
| **P3** | G3 | Unify tool `ResolvePlatformRoot` with adapter; fail-fast fallback | Cursor | Small code + tests |
| **P4** | G1-M | Optional supersession headers on Phase reports (or single index doc) | Main untracked or docs commit | Docs |
| **P5** | G2 | Optional BootstrapValidationTests cosmetic path | Cursor | 1 line |
| **P6** | G4 | **V7–V13** with explicit human authorization per row | Human + agent | Separate phase |

---

## 7. Unresolved assumptions

- Whether any machine still has gitignored `aovias-config/storage` at **legacy** path (not visible in these worktrees).
- Whether `aovias-config/scripts/provision-deployment-storage.sh` exists post-W1 (milestone T8 reference not verified in this triage).
- Docker / DRV availability on validation runner (not probed here).
- Symlink-based repo layouts on developer machines (G3 theoretical).

---

## 8. Final status

**READY FOR SEPARATELY AUTHORIZED REMEDIATION**

Follow-up scope is clear; worktrees aligned at `202dd525`. No safety or preservation blocker. Implementation should be split: **docs (G1-S)**, **G3 hardening + tests**, **G4 validation phases** — each with explicit authorization per `AGENTS.md`.

---

## 9. Git writes in Phase 1G-B

**None** (this report is a new untracked deliverable on Main).
