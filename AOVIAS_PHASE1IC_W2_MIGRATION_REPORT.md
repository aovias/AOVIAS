# AOVIAS Phase 1I-C — W2 Controlled Physical Migration Report

**Phase:** 1I-C (W2 execution — no commit/merge/sync)  
**Date:** 2025-10-25  
**Worktree:** `V:\DevelopmentRepo\AOVIAS.Cursor`  
**Branch:** `agent/cursor/phase1ic-w2-migration`  
**Baseline HEAD:** `7758e30ce0f8c406fe8f292006895f67fb31cafd`

---

## Final status

**W2 COMPLETE — REVIEW REQUIRED**

Physical moves and required reference updates are implemented on the Cursor task branch only. No commit, push, merge, Docker build, publish, preflight, or worktree synchronization was performed.

---

## 0. Safety gate

| Worktree | Branch | HEAD | Tracked tree |
|----------|--------|------|--------------|
| Main | `main` | `7758e30ce0f8c406fe8f292006895f67fb31cafd` | Clean (untracked phase reports/ZIPs/Lighthouse preserved) |
| Cursor | `agent/cursor/phase1ic-w2-migration` (from `agent/cursor/active`) | `7758e30…` | W2 changes only |
| Claude | `agent/claude/active` | `7758e30…` | Clean (untracked `Properties/launchSettings.json` preserved) |

**Sources read:** `AGENTS.md`, `repository-layout.json`, `AOVIAS_PHASE1B_MIGRATION_CONTRACT.md`, `AOVIAS_PHASE1IA_TARGET_PHYSICAL_LAYOUT_REPORT.md`, `AOVIAS_PHASE1IB_ARCHITECTURE_DECISIONS_REPORT.md` (§8 recommendations treated as human-approved per phase authorization).

**Manifest alignment:** W2 MOVE rows **M-I-02**, **M-I-03**, **M-I-04** are unambiguous and consistent across 1I-A, 1I-B, and `repository-layout.json` target paths. **M-I-01** KEEP (config). **M-I-05** DEFER (whole `tools/` tree). No conflict detected.

**Concurrent agent scope:** No tracked changes on Main or Claude; implementation isolated to Cursor task branch.

---

## 1. Readiness decision (pre-move)

**W2 READY — AUTHORIZED SCOPE UNAMBIGUOUS**

| Manifest ID | Class | Source | Destination |
|-------------|-------|--------|-------------|
| M-I-01 | KEEP | `AOVIAS.Infrastructure/aovias-config/` | *(unchanged)* |
| M-I-02 | MOVE | `Aovias.ReleaseValidation/` | `AOVIAS.Infrastructure/Aovias.ReleaseValidation/` |
| M-I-03 | MOVE | `tools/PublishAllServices/` | `AOVIAS.Infrastructure/tools/PublishAllServices/` |
| M-I-04 | MOVE | `tools/PublishRuntimeAgent/` | `AOVIAS.Infrastructure/tools/PublishRuntimeAgent/` |
| M-I-05 | DEFER | `tools/` *(entire tree)* | — |

---

## 2. W2 inventory (pre-move evidence)

### M-I-02 — `Aovias.ReleaseValidation`

| Field | Value |
|-------|--------|
| Type | Node/TypeScript harness (not in any `.slnx`) |
| Tracked | Yes (24 files under tree) |
| Solution membership | None |
| Project references | N/A (npm/Playwright) |
| Path assumptions | `service-registry.ts`: `PLATFORM_ROOT = 'ComplaincePlatform'` (repo-root-relative service paths — **unchanged** by RV folder move) |
| Manifest | `products.infrastructure.paths.releaseValidation` |
| `discovery.current` | Does not list ReleaseValidation |
| Tests | Node/npm tests **NOT RUN** (would require `npm ci` — prohibited) |
| Rollback | `git mv` reverse + manifest `current` restore |

### M-I-03 / M-I-04 — Publish CLIs

| Field | Value |
|-------|--------|
| Type | `net10.0` executables |
| Tracked | Yes |
| Solution | `Aovias.Platform.slnx` |
| Project references | `Aovias.Configuration`, `Aovias.Publishing.SDK` (relative `..` paths — **required update** after move) |
| Platform root | `RepositoryLayoutProductionAdapter.ResolvePublishingPlatformRoot(AppContext.BaseDirectory)` — manifest walk-up; **no** hard-coded `..` count in Program.cs |
| Consumers | Docs/runbooks; human/CI `dotnet run --project …` |
| Manifest | `products.infrastructure.paths.publishTools` |
| Rollback | Revert slnx, csproj refs, manifest |

### Dependencies **not** in W2 scope (unchanged)

- `discovery.current` platform/scan/Docker markers (Studio/Command — W3/W4)
- `Aovias.DeploymentValidation`, Runtime Agent, Publishing stack
- CI workflows (no `tools/Publish` references in repo `.github`)

---

## 3. INV-2 — `deployment.metadata.json`

| Metric | Result |
|--------|--------|
| Files found (glob) | 21 |
| Excluded `_perf_build_out` | 2 (`Gateway`, `ComplianceService` under `ComplaincePlatform/_perf_build_out/`) |
| Canonical count | **19** (18 Command/Agent/WebApp services + 1 Agent root — see table) |

**Reconciliation with 1I-B (~18):** Count differs by **+1** because **WebApp** metadata is included in the canonical set (21 − 2 perf = **19**). No W2 move changes any metadata path. All consumers remain under `ComplaincePlatform/`, `WebApp/`, or `Aovias.Runtime.Agent/`.

| Path | Owner | W2 scope | Path changes after W2 | Primary consumer |
|------|-------|----------|----------------------|------------------|
| `ComplaincePlatform/*/deployment.metadata.json` (16 services + KnowledgeBase) | Command | No | No | Publishing discover/publish |
| `WebApp/deployment.metadata.json` | Command | No | No | Publishing |
| `Aovias.Runtime.Agent/deployment.metadata.json` | Studio | No | No | Publishing / Agent publish |

**Discrepancy:** None blocking W2. Formal sign-off: canonical list is **19** production paths with `_perf_build_out` excluded.

---

## 4. INV-1 — Scoped inventory (W2)

| Item | Solutions | Referenced by |
|------|-----------|---------------|
| `PublishAllServices.csproj` | `Aovias.Platform.slnx` | SDK → Publishing.Core graph |
| `PublishRuntimeAgent.csproj` | `Aovias.Platform.slnx` | Same |
| `Aovias.ReleaseValidation` (npm) | *(none)* | Standalone harness |

**Pre-W3 gate (unchanged):** Full 87×9 project-to-solution matrix still **mandatory before W3**.

---

## 5. Execution summary

### 5.1 Moves (`git mv`)

| Source | Destination | Status |
|--------|-------------|--------|
| `Aovias.ReleaseValidation/` | `AOVIAS.Infrastructure/Aovias.ReleaseValidation/` | Done |
| `tools/PublishAllServices/` | `AOVIAS.Infrastructure/tools/PublishAllServices/` | Done |
| `tools/PublishRuntimeAgent/` | `AOVIAS.Infrastructure/tools/PublishRuntimeAgent/` | Done |

**Verification:** Root `Aovias.ReleaseValidation/` and `tools/PublishAllServices/` **absent**. Each destination exists **once**.

**Note:** Empty untracked directory `tools/` may remain on disk; no tracked files under root `tools/` after move. **M-I-05** not executed; root `tools/` not removed.

### 5.2 Reference and configuration changes

| File | Change | Rationale |
|------|--------|-----------|
| `AOVIAS.Infrastructure/tools/PublishAllServices/PublishAllServices.csproj` | ProjectReference `..\..\` → `..\..\..\` | Restore repo-root-relative paths after deeper nesting |
| `AOVIAS.Infrastructure/tools/PublishRuntimeAgent/PublishRuntimeAgent.csproj` | Same | Same |
| `Aovias.Platform.slnx` | Project paths → `AOVIAS.Infrastructure/tools/...` | Solution load |
| `repository-layout.json` | `releaseValidation.current`, `publishTools.current` → post-W2 paths (match target) | Contract matches disk; **Target profile not enabled in production** |
| `docs/milestones/G1-EXECUTION-RUNBOOK.md` | `dotnet run --project` paths | Operational docs |
| `docs/milestones/ROADMAP.md` | Tool path mention | Traceability |
| `docs/milestones/G1-EXECUTION-REPORT.md` | Tool path mention | Traceability |
| `docs/Architecture/ADR-014-…md` | TESTID-CONTRACT path | Cross-doc link |

**Not changed (by design):** `discovery.current` / `discovery.target`, O-5 entry in `unresolvedDecisions`, `service-registry.ts` PLATFORM_ROOT, phase reports at repo root, `preflight-docker-probe/`, `AOVIAS.Shared`, Studio/Command/Website trees.

---

## 6. Validation

| Check | Command / method | Result |
|-------|------------------|--------|
| PublishAllServices Release build | `dotnet build AOVIAS.Infrastructure/tools/PublishAllServices/PublishAllServices.csproj -c Release` | **PASS** |
| PublishRuntimeAgent Release build | `dotnet build AOVIAS.Infrastructure/tools/PublishRuntimeAgent/PublishRuntimeAgent.csproj -c Release` | **PASS** |
| Layout contract tests | `dotnet test Aovias.Configuration.Tests -c Release --filter FullyQualifiedName~Layout` | **PASS** (39/39) |
| PublishingProjectPaths tests | `dotnet test Aovias.Publishing.Core.Tests -c Release --filter FullyQualifiedName~PublishingProjectPaths` | **PASS** (2/2) |
| Manifest JSON | `ConvertFrom-Json` on `repository-layout.json` | **PASS** |
| Post-W2 path existence | PowerShell `Test-Path` | **PASS** |
| `--discover` / preflight / Docker | — | **NOT RUN** — separate authorization (phase prohibition) |
| ReleaseValidation npm tests | — | **NOT RUN** — would require `npm ci` |

**Target profile:** `RepositoryLayoutProductionAdapter` still permits **Current** only; no production wiring change.

---

## 7. Stale-reference scan

| Category | Finding |
|----------|---------|
| Tracked source (`.cs`, `.csproj`, `.slnx`, `.json`, CI) | **No** stale `tools/Publish*` or root `Aovias.ReleaseValidation/` paths |
| Tracked docs updated | G1 runbook/report, ROADMAP, ADR-014 |
| Intentional / historical | Untracked `AOVIAS_PHASE*.md` on Main/Cursor; `AOVIAS_REPOSITORY_RESTRUCTURING_ASSESSMENT.md` (planning doc — pre-W2 layout narrative) |
| `repository-layout.json` | O-5 topic string still says `tools/Publish*` (decision id label — not a filesystem path) |
| Negative/historical fixtures | Layout tests unchanged |

---

## 8. Git diff summary

- **Renames:** 27 files (ReleaseValidation tree + 2 Publish tool projects/sources)
- **Modified:** 8 files (2 csproj, slnx, manifest, 4 docs)
- **Out of scope:** No changes under Studio, Command, Website, Docker, Publishing Core logic, or CI workflows

**Untracked preserved:** Cursor `scripts/dev/_phase3h_runtime_probe.py`, prior W1 phase reports, Main untracked artifacts untouched.

---

## 9. Deviations, blockers, residual risks

| Item | Severity | Notes |
|------|----------|-------|
| Publish CLI runtime after move | Low | Builds pass; discover/preflight not re-run (prohibited) |
| Empty root `tools/` directory | Low | Cosmetic; M-I-05 deferred |
| INV-2 count 19 vs ~18 | Info | WebApp metadata included in canonical set |
| Historical docs / phase reports | Low | Still mention old paths until human archive/update |

---

## 10. Authorization boundary confirmation

| Action | Performed? |
|--------|------------|
| Git commit | **No** |
| Push | **No** |
| Merge to Main | **No** |
| Worktree sync | **No** |
| W3–W5 | **No** |
| Docker / publish / preflight | **No** |

---

## 11. Recommended next step (not executed)

1. Human review diff on `agent/cursor/phase1ic-w2-migration` in Cursor worktree.
2. Separate authorization: **commit** W2 on task branch, merge to `main`, validate on Main, sync agent `active` baselines.
3. Before **W3:** complete **INV-1** full project-to-solution matrix (pre-W3 gate).
4. Optional: `--discover`-only smoke on moved PublishAllServices path after merge authorization.

---

**Report path:** `AOVIAS_PHASE1IC_W2_MIGRATION_REPORT.md` (untracked, Cursor worktree root)
