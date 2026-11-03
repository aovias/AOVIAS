# AOVIAS Phase 1B — Migration Contract

**Date:** 2026-10-25  
**Version:** 1B (successor to Phase 1A manifest; **Phase 1A files preserved**)  
**Baseline HEAD:** `5a1526d99a68241d40ccba759d8fc62586f4af86`  
**Authority:** Planning and contract only — **no physical migration authorized**

**Related deliverables:**

- `AOVIAS_PHASE1B_OWNERSHIP_AND_EVIDENCE.md` (O-1, O-2)
- `AOVIAS_PHASE1B_COMPLETE_PATH_INVENTORY.md` (Compose, csproj, paths)
- Phase 1A audit trio (unchanged)

---

## 1. Contract principles

1. **Verified vs proposal:** Tables label **Verified** (HEAD evidence) vs **Proposal** (planned waves).
2. **Small waves:** Each wave has entry gate, exit gate, rollback, and test list.
3. **No combined ownership + mass move:** Studio tree move does not include Command in same wave.
4. **Spelling:** `ComplaincePlatform` folder name unchanged until separate approved rename.
5. **Worktrees:** `V:\DevelopmentRepo\AOVIAS{,.Cursor,.Claude}` paths **unchanged** by product-folder moves.
6. **Baseline failures:** NU1903, website npm, external integration deps — **out of scope** for migration waves unless explicitly re-baselined.

---

## 2. Approved ownership decisions (Phase 1B)

| ID | Asset | Owner | Target path | Evidence doc |
|----|-------|-------|-------------|--------------|
| O-1 | `Aovias.CommandCenter/` | **Studio** | `AOVIAS.Studio/Aovias.CommandCenter/` | Phase 1B ownership §2 |
| O-2 | `Aovias.Configuration/` | **Studio** | `AOVIAS.Studio/Aovias.Configuration/` | Phase 1B ownership §3 |
| O-2 | `Aovias.Deployment.Contracts/` | **Studio** | `AOVIAS.Studio/Aovias.Deployment.Contracts/` | Phase 1B ownership §3 |

### Provisional (not user-approved in 1B — carry from 1A)

| ID | Asset | Provisional owner | Target |
|----|-------|-------------------|--------|
| O-3 | `Aovias.DeploymentValidation/` | Studio | `AOVIAS.Studio/` or Infrastructure |
| O-4 | `Aovias.Runtime.Contracts` | Studio (under Agent) | `AOVIAS.Studio/Aovias.Runtime.Agent/...` |
| O-5 | `tools/Publish*` | Infrastructure | `AOVIAS.Infrastructure/tools/` |
| O-6 | `AOVIAS.Shared` | Empty until promotion criteria met | TBD |
| O-7 | Website folder | Website product | `AOVIAS.Website/` |
| O-8 | `preflight-docker-probe/` | Delete or gitignore | Not tracked at HEAD |

---

## 3. Current-to-target mapping (contract table)

Abbreviated; full csproj set in Phase 1B complete inventory.

| Current | Target | Owner | Wave |
|---------|--------|-------|------|
| `aovias-config/` | `AOVIAS.Infrastructure/aovias-config/` | Infrastructure | W1 |
| `Aovias.ReleaseValidation/` | `AOVIAS.Infrastructure/Aovias.ReleaseValidation/` | Infrastructure | W1 |
| `tools/PublishAllServices`, `tools/PublishRuntimeAgent` | `AOVIAS.Infrastructure/tools/...` | Infrastructure | W1b (optional split) |
| `Aovias.{Configuration,Deployment.Contracts,Bootstrap,Runtime.Agent,Publishing,Release,Orchestration,CommandCenter,DesignSystem}` + CSM* | `AOVIAS.Studio/<same-name>/` | Studio | W3 |
| `Aovias.Platform.slnx`, `Aovias.PublishingEngine.slnx` | `AOVIAS.Studio/` | Studio | W3 |
| `ComplaincePlatform/` | `AOVIAS.Command/CompliancePlatform/` | Command | W4 |
| `WebApp/` | `AOVIAS.Command/WebApp/` | Command | W4 |
| `AOVIAS Website/AOVIAS Web/` | `AOVIAS.Website/` | Website | W5 |
| *(none)* | `AOVIAS.AccredaOne/` scaffold | AccredaOne | W6 |
| `docs/`, `scripts/`, `ai-governance/`, `AGENTS.md` | Root | Governance | W0 docs update |

---

## 4. Path and dependency assumptions per move

| Move | Affected systems | Class |
|------|------------------|-------|
| `aovias-config` | `PlatformRootResolver`, `RootPathResolver.ResolvePlatformRoot`, Publish CLIs, Bootstrap `ConfigRoot` | **MIGRATION BLOCKER** without resolver update |
| Studio tree | Platform slnx, Dockerfile COPY, Docker stager, all Studio ProjectReferences | **MIGRATION BLOCKER** |
| Command tree | Compose context `.`, discoverer scan root, LogPathResolver, Command CI cwd | **MIGRATION BLOCKER** |
| Website tree | Compose `../..` contexts, website CI | **MUST UPDATE** |

**Contract rule:** No physical wave may ship **without** paired resolver/script updates in the **same authorized change set** (Phase 1C implementation).

---

## 5. Required resolver and discovery changes (implementation checklist — not executed in 1B)

| # | Change | Owner code |
|---|--------|------------|
| R1 | Introduce **`repository-layout.json`** (or equivalent) at repo root: `repoRoot`, `commandRoot`, `studioRoot`, `infrastructureRoot`, legacy aliases | New — Phase 1C |
| R2 | Refactor **`PlatformRootResolver`** to consult layout manifest + legacy fallbacks | Publishing |
| R3 | Refactor **`RootPathResolver`** similarly | CSM |
| R4 | Update **`SourceDiscoveryBoundary.GetDefaultApplicationRoots`** | CSM |
| R5 | Update **`SolutionDeployableProjectDiscoverer.GetScanRoots`** | Publishing |
| R6 | Update **`RuntimeAgentDockerContextStager`** folder list / COPY parser | Publishing |
| R7 | Update **`tools/Publish*`** local `ResolvePlatformRoot` | Tools |
| R8 | Replace Bootstrap default **`V1.0/aovias-config`** with layout-aware resolution | Bootstrap |

**Phase 1B:** No code changes.

---

## 6. Solution and project reference updates (by wave)

| Wave | Artifacts |
|------|-----------|
| W3 | `Aovias.Platform.slnx`, `Aovias.PublishingEngine.slnx`, area `*.slnx`, all Studio **ProjectReference** relative paths |
| W4 | `CompliancePlatform.slnx`, service **ProjectReference** paths (internal — mostly unchanged within Command tree) |
| W1 | Tool csproj paths to Configuration |
| W5 | None (.NET); npm workspace roots |

---

## 7. Docker, Compose, CI, scripts, documentation

| Artifact | Wave | Action |
|----------|------|--------|
| `Aovias.Runtime.Agent/Dockerfile` | W3 | COPY prefixes or build context = repo root with new paths |
| Command compose files | W4 | `context: .` remains Command platform root |
| Website compose (3 files) | W5 | Recompute `../..` from new compose location |
| `ComplaincePlatform/.github/workflows/platform-integrity.yml` | W4 | `working-directory` or path prefixes |
| `AOVIAS Website/.../ci.yml` | W5 | Monorepo root |
| `scripts/verify-build.sh`, `verify-tests.sh` | W3 | Platform slnx path |
| `scripts/dev/*.ps1` | W0/W3 | Defaults + documentation |
| `docs/development/WORKTREE-OPERATIONS.md` | W0 | Product paths note |
| Architecture docs with path examples | W0 | **DOCUMENTATION ONLY** batch |

---

## 8. Regression tests (historical baseline + required reruns)

**Historical (Phase 0B — not re-run in 1B):** Command slnx PASS, PublishingEngine PASS, CommandCenter PASS, WebApp PASS; Platform/CSM FAIL (NU1903); website npm FAIL.

### Before any physical wave (Phase 1C entry)

| Test | Purpose |
|------|---------|
| `git status` on three worktrees | Preservation |
| `dotnet build ComplaincePlatform/CompliancePlatform.slnx` | Command golden |
| `dotnet build Aovias.PublishingEngine.slnx` | Studio golden (known PASS) |
| New: **layout resolver unit tests** | Contract for R1–R8 |

### After W1 (Infrastructure)

| Test | Purpose |
|------|---------|
| Publish CLI dry-run or Configuration tests | `aovias-config` discovery |
| `Aovias.Configuration.Tests` | Settings paths |

### After W3 (Studio)

| Test | Purpose |
|------|---------|
| `Aovias.Publishing.Core.Tests` including `RuntimeAgentDockerContextStagerTests` | Docker staging |
| `dotnet build Aovias.PublishingEngine.slnx` | Studio |
| Optional: `docker build -f Aovias.Runtime.Agent/Dockerfile` | Agent image |
| CSM slnx on Windows | WPF (may still hit NU1903) |

### After W4 (Command)

| Test | Purpose |
|------|---------|
| `dotnet build CompliancePlatform.slnx` | Command |
| `npm run build` in `WebApp/` | Angular |
| `platform-integrity.yml` guardrails job (CI or local equivalent) | Governance tests |
| Publishing discoverer integration test | Scan roots find services |

### After W5 (Website)

| Test | Purpose |
|------|---------|
| Website CI scripts | When npm baseline fixed |

---

## 9. Rollback and validation

| Scenario | Rollback | Validation |
|----------|----------|------------|
| Failed wave on `main` | Revert merge commit on integration repo; agent worktrees `merge --ff-only main` after fix | HEAD equals known good SHA |
| Partial file move | **Forbidden** — use wave atomicity + Git mv commits | `git diff` empty vs expected |
| Resolver regression | Revert resolver commit; keep files in old paths via revert entire wave | Golden builds above |
| Operator LocalAppData registry | Manual reset of `development-workspaces.json` | CSM workspace picker |

**Exit validation for full program:** Cursor/Claude on `agent/*/active` @ `main`; documented build matrix re-run; preservation artifacts intact.

---

## 10. Untouched artifacts (contract)

| Artifact | Rule |
|----------|------|
| `scripts/dev/_phase3h_runtime_probe.py` | Do not delete/move without human decision |
| Claude untracked `launchSettings.json` | Same |
| Main website ZIPs, Lighthouse dirs | Same |
| Phase 0/1 reports | Preserve; new versioned reports only |
| `**/bin/`, `**/obj/`, `node_modules` | Never migrate — regenerate |
| Probe execution | **Forbidden** in migration phases |

---

## 11. Migration waves — entry and exit gates

### W0 — Governance and contract (non-physical)

| | |
|-|-|
| **Entry** | Phase 1B **READY FOR PHASE 1C HUMAN REVIEW** accepted |
| **Work** | Human approval; optional commit of Phase reports; layout JSON spec |
| **Exit** | Signed Phase 1C charter; O-3..O-8 defaults accepted or resolved |
| **Rollback** | N/A |

### W1 — Infrastructure paths (physical — **blocked until R1–R2 exist**)

| | |
|-|-|
| **Entry** | Resolver contract merged; W0 exit |
| **Moves** | `aovias-config` → `AOVIAS.Infrastructure/aovias-config/`; optionally `Aovias.ReleaseValidation`, `tools/Publish*` |
| **Exit** | Resolvers find config; Infrastructure tests pass |
| **Rollback** | Revert single merge |

### W3 — Studio tree (physical — **largest wave**)

| | |
|-|-|
| **Entry** | W1 exit (recommended) or explicit risk acceptance |
| **Moves** | All O-1/O-2 assets + remaining Studio projects + root slnx |
| **Exit** | PublishingEngine build; stager tests; Agent Docker optional |
| **Rollback** | Revert merge; full rebuild |

### W4 — Command tree

| | |
|-|-|
| **Entry** | W3 exit; discoverer updated |
| **Moves** | `ComplaincePlatform`, `WebApp` |
| **Exit** | CompliancePlatform slnx + WebApp build + guardrails tests |
| **Rollback** | Revert merge |

### W5 — Website

| | |
|-|-|
| **Entry** | npm baseline strategy accepted |
| **Moves** | Website monorepo folder |
| **Exit** | Website CI green |
| **Rollback** | Revert merge |

### W6 — AccredaOne scaffold

| | |
|-|-|
| **Entry** | Product charter |
| **Work** | Add `AOVIAS.AccredaOne/README.md` + boundary doc only |
| **Exit** | No runtime dependency |
| **Rollback** | Remove folder |

---

## 12. Smallest safe first **implementation** wave (recommendation)

### Assessment

**No sufficiently isolated physical `git mv` wave exists** without simultaneous resolver changes. Moving **`aovias-config` alone** breaks **`PlatformRootResolver`** and **`RootPathResolver`** (verified markers).

### Recommended first authorized work (Phase 1C — still not executed in 1B)

**Phase 1C-A — Resolver contract only (non-physical or additive-only files if explicitly authorized):**

1. Add **`repository-layout.json`** schema + read-only loader.
2. Add unit tests asserting resolution for **current** layout and **target** layout (dual-mode / feature flag).
3. **No** `git mv` until tests pass on `main`.

### Recommended first **physical** wave (after 1C-A merged)

**Phase 1C-B — AccredaOne scaffold (additive):**

| Source | Destination | Rationale |
|--------|-------------|-----------|
| *(new)* | `AOVIAS.AccredaOne/README.md` | Zero consumers; no resolver coupling; establishes product boundary folder |

**Alternative first physical wave (higher risk):** **W1 Infrastructure** (`aovias-config` move) **only together with** R1–R2 resolver merge in the same PR — **not** Studio or Command moves.

### W1 Infrastructure — if chosen as first move bundle

| Exact paths | Destination |
|-------------|-------------|
| `aovias-config/` | `AOVIAS.Infrastructure/aovias-config/` |

**Consumers:** All platform root walkers, Publish CLIs, Bootstrap config root, generated publishing templates.

**Prerequisites:** Phase 1C-A tests green; historical PublishingEngine PASS reconfirmed.

**Rollback:** Revert merge; restore root-level `aovias-config` path in layout JSON default.

**Success evidence:** `PlatformRootResolver.Resolve()` returns valid root; `Aovias.Configuration.Tests` pass; no secret values in logs.

---

## 13. Build/test discipline (Phase 1B)

All Phase 0 build results treated as **historical**. Phase 1B performed **read-only Git enumeration** only — no NU1903/npm remediation, no probe execution.

---

## 14. Readiness status (Phase 1B)

| Criterion | Result |
|-----------|--------|
| Safety gate | **Pass** |
| O-1 / O-2 evidence | **Complete** — no conflict flag |
| Compose / csproj inventory | **Complete** (87 csproj, 9 slnx + 1 sln, 5 compose) |
| Migration contract | **Complete** |
| Physical migration | **Not authorized** |

### **READY FOR PHASE 1C HUMAN REVIEW**

Meaning: Ownership planning decisions, closed inventory gaps, and migration contract are ready for human review. **Does not authorize file moves or code changes.**

---

## 15. Smallest safe next step

1. Human review of Phase 1B trio + accept O-1/O-2 for planning continuity.  
2. Resolve provisional **O-3..O-8** or accept defaults in §2.  
3. Authorize **Phase 1C-A** (layout manifest + resolver contract tests only).  
4. Defer any **`git mv`** until 1C-A exit gate passes.

---

*End of migration contract v1B.*
