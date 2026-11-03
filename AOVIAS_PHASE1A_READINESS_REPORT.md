# AOVIAS Phase 1A — Readiness Report

**Date:** 2026-10-25  
**Phase:** 1A — Migration-safe layout manifest and path-dependency audit  
**Baseline HEAD:** `5a1526d99a68241d40ccba759d8fc62586f4af86`

---

## 1. Objectives completed

| Objective | Status | Deliverable |
|-----------|--------|-------------|
| Revalidate worktrees and HEADs | **Done** | §2 below |
| Workstream A — ownership inventory | **Done** | `AOVIAS_PHASE1A_LAYOUT_MANIFEST.md` §3–5 |
| Workstream B — path-dependency audit | **Done** | `AOVIAS_PHASE1A_PATH_DEPENDENCY_AUDIT.md` |
| Workstream C — build baseline mapping | **Done** | §4 below (historical Phase 0B) |
| Workstream D — migration manifest | **Done** | `AOVIAS_PHASE1A_LAYOUT_MANIFEST.md` §6–12 |

**Constraints honored:** No moves, edits, commits, builds, npm installs, or probe execution in Phase 1A.

---

## 2. Baseline verification (fresh — 2026-10-25)

| Check | Result |
|-------|--------|
| Main @ `main` | **Verified** `5a1526d99a68241d40ccba759d8fc62586f4af86` |
| Cursor @ `agent/cursor/active` | **Verified** same HEAD |
| Claude @ `agent/claude/active` | **Verified** same HEAD |
| Main tracked modifications | **None** |
| Cursor untracked probe | **Present** `scripts/dev/_phase3h_runtime_probe.py` |
| Claude untracked Properties | **Present** under CommandCenter API |
| Main untracked artifacts | Website ZIPs, `.lighthouseci-r1-*`, Phase 0/0D/1A reports (as created) |
| Phase 0C preservation dir | **Exists** |
| `AGENTS.md` / worktree policy | Read; two-worktree model unchanged |

**Note:** Agent preflight scripts require a **fully clean** porcelain status; current intentional untracked files will **fail preflight** until policy exempts them or files are moved outside worktrees — documented, not fixed in 1A.

---

## 3. Inventory coverage and gaps

### Covered (verified at HEAD)

- Top-level directories and major product areas (Command platform tree, WebApp, Studio stack, Website monorepo, `aovias-config`, docs, scripts, tools, governance).
- Nine `*.slnx` entry points and ownership alignment with Phase 0 assessment.
- Command vs Studio **MSBuild decoupling** (no `ComplaincePlatform` → `Aovias.*` ProjectReference).
- Mandated resolver/script/Docker/CI files per mission brief.
- Extended search hits for `DevelopmentRepo`, legacy `V1.0` paths, and monorepo folder names.

### Gaps (non-blocking for human review)

- Exhaustive line audit of every `docker-compose*.yml` build context.
- Full enumeration of every `.csproj` (Platform slnx is authoritative for Studio cross-platform set).
- Fresh build/test re-run (explicitly out of scope for 1A).
- AccredaOne — no implementation paths (by design).

---

## 4. Build and test baseline (labeling)

**All rows below are HISTORICAL — reproduced from `AOVIAS_PHASE0_BASELINE_VALIDATION.md` (executed 2026-10-25 on integrator machine). Not re-run in Phase 1A.**

| Target | Result | Label |
|--------|--------|-------|
| `ComplaincePlatform/CompliancePlatform.slnx` | PASS | Historical |
| `Aovias.PublishingEngine.slnx` | PASS | Historical |
| `Aovias.CommandCenter/Aovias.CommandCenter.slnx` | PASS | Historical |
| `WebApp/` npm build | PASS (warnings) | Historical |
| `Aovias.Platform.slnx` restore/build | FAIL (NU1903 SSH.NET, SignalR.Common) | Historical |
| `ComplianceServiceManager.slnx` | FAIL (NU1903) | Historical |
| Website npm typecheck/lint/build | FAIL (`@aovias/*` workspace) | Historical |
| `Aovias.Platform.slnx` dotnet test | NOT RUN | Historical |
| Runtime Agent Docker build | NOT RUN | Historical |
| `scripts/verify-build.sh` | NOT RUN | Historical |
| Command platform-integrity integration job | NOT RUN | Historical |

**Blocked by external deps (documented):** SQL Server/RabbitMQ for full Command integration tests; Windows Desktop workload for CSM (attempted; NU1903 hit first).

---

## 5. Highest-risk path dependencies

1. **`PlatformRootResolver` + `RootPathResolver`** — dual logic; sibling-folder heuristics for `ComplaincePlatform`, `WebApp`, `aovias-config`, `Aovias.Runtime.Agent`.
2. **`SolutionDeployableProjectDiscoverer.GetScanRoots`** — hard-coded `ComplaincePlatform` path segment.
3. **`RuntimeAgentDockerContextStager` + `Aovias.Runtime.Agent/Dockerfile`** — top-level `Aovias.*` COPY list.
4. **`Aovias.Platform.slnx` / `CompliancePlatform.slnx`** — hundreds of relative project paths.
5. **`DevelopmentWorkspaceRegistry` + dev scripts** — worktree paths (stable) vs discovery roots (break on product move).
6. **Legacy `V:\DevelopmentRepo\V1.0\...` in Bootstrap defaults and tests** — pre-dates current layout; migration must not inherit wrong defaults.

---

## 6. Ownership decisions still required (before Phase 1B)

| ID | Topic |
|----|--------|
| O-1 | `Aovias.CommandCenter` — Studio fleet API vs Infrastructure ops vs rename for DCC disambiguation |
| O-2 | Whether/when to hoist `Aovias.Configuration` and `Aovias.Deployment.Contracts` to `AOVIAS.Shared` |
| O-3 | `Aovias.DeploymentValidation` product bucket |
| O-4 | `Aovias.Runtime.Contracts` placement |
| O-5 | `tools/Publish*` ownership |
| O-6 | Minimal vs empty `AOVIAS.Shared` in wave 1 |
| O-7 | Website folder flatten vs keep `AOVIAS Website/AOVIAS Web` |
| O-8 | `preflight-docker-probe/` disposition |

KnowledgeBase: **remain Command-owned**; AccredaOne remains **scaffold + boundary design** only.

---

## 7. Deliverable paths

| File | Purpose |
|------|---------|
| `V:\DevelopmentRepo\AOVIAS\AOVIAS_PHASE1A_LAYOUT_MANIFEST.md` | Mapping, waves, resolvers, rollback, artifacts |
| `V:\DevelopmentRepo\AOVIAS\AOVIAS_PHASE1A_PATH_DEPENDENCY_AUDIT.md` | File-level findings and remediation order |
| `V:\DevelopmentRepo\AOVIAS\AOVIAS_PHASE1A_READINESS_REPORT.md` | This report |

Prior reports **preserved** (not overwritten): Phase 0/0C/0D, restructuring assessment, anomaly report.

---

## 8. Smallest safe next step

1. **Human review** of this readiness report, layout manifest, and path audit.  
2. **Resolve O-1 through O-8** (at minimum O-1 CommandCenter and O-2 Shared vs Studio for Configuration/Deployment.Contracts).  
3. **Optional:** Commit Phase 0/1A documentation on `main` when approved.  
4. **Do not start Phase 1B** (resolver implementation or `git mv` waves) until explicit authorization.  
5. Parallel track (separate from layout): NU1903 and website npm baseline — still **historical FAILs**; not remediated in 1A.

---

## 9. Readiness verdict

The repository is **not** ready for physical directory moves. Phase 1A deliverables are complete for **human review** of the migration plan; unresolved ownership items block **implementation** (Phase 1B+).

### **READY FOR HUMAN REVIEW**

---

*Phase 1A complete. No Phase 1B work authorized by this document.*
