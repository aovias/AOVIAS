# AOVIAS Phase 1I-A — Target Physical Repository Structure and Relocation Manifest

**Phase:** 1I-A (design and inventory only)  
**Date:** 2025-10-25  
**Baseline HEAD:** `7758e30ce0f8c406fe8f292006895f67fb31cafd`

**Confirmation:** No repository files were moved, modified, staged, or committed. No Git operations, builds, publishing, Docker, deployment, or cleanup were performed.

---

## Final status

**ARCHITECTURAL DECISIONS REQUIRED**

A **contract-backed target layout** exists in `repository-layout.json` (`discovery.target`, product `paths.target`, `solutions[].path.target`) and Phase 1B migration contract waves. **W1 configuration templates are already at target.** Broader product-tree moves remain **proposals** until unresolved items **O-3 through O-8**, **INV-1/INV-2**, phase-report disposition, and **Target-profile resolver activation** are approved and sequenced.

---

## 1. Safety snapshot

| Worktree | Branch | HEAD | Tracked tree |
|----------|--------|------|--------------|
| Main | `main` | `7758e30ce0f8c406fe8f292006895f67fb31cafd` | Clean |
| Cursor | `agent/cursor/active` | `7758e30…` | Clean |
| Claude | `agent/claude/active` | `7758e30…` | Clean |

**Preserved (not altered this phase):**

- Untracked phase reports, website ZIPs, Lighthouse dirs on Main.
- Cursor `scripts/dev/_phase3h_runtime_probe.py` (untracked).
- Claude `Aovias.CommandCenter/.../Properties/launchSettings.json` (untracked).
- Temp config: `C:\Users\HP\AppData\Local\Temp\aovias-phase1hd-82ddf8d648044c1193fcf493bccdd99f`.
- Ignored operational data (e.g. `AOVIAS.Infrastructure/aovias-config/storage/`, `WebApp/node_modules/`) — **not relocated or deleted**.

**External worktrees:** `V:\DevelopmentRepo\AOVIAS.Cursor` and `AOVIAS.Claude` remain **outside** the Main repo tree per `AGENTS.md` and `repository-layout.json` `worktreePathsUnchangedByProductMoves`.

---

## 2. Authority and source reconciliation

| Source | Role | Authority level |
|--------|------|-----------------|
| **`repository-layout.json`** | Machine-readable **current** vs **target** paths, discovery markers, ownership IDs O-1–O-2, unresolved O-3–O-8 | **Primary contract** for planned physical layout (target = proposal until waves execute) |
| **`AOVIAS_PHASE1B_MIGRATION_CONTRACT.md`** | Wave model (W0–W6), blocker classes, no combined Command+Studio move | **Planning contract** (supplements manifest) |
| **`AOVIAS_PHASE1HA_REPOSITORY_LAYOUT_REALITY_REPORT.md`** | W1 verified; **no broader move authorized** at audit time | **Evidence** — current disk truth |
| **`AOVIAS_PHASE1HB_RESIDUAL_RISK_ASSESSMENT.md`** | Product moves high risk until CLI/DRV proof | **Risk gate** for sequencing |
| **`AOVIAS_REPOSITORY_RESTRUCTURING_ASSESSMENT.md`** | Four-product narrative; path-sensitive system inventory (pre-W1 snapshot in places) | **Historical assessment** — reconcile with manifest, not sole authority |
| **Phase 1A/1B inventories** | Path dependency audit, csproj/Compose inventory | **Dependency evidence** |
| **On-disk layout at HEAD** | Flat monorepo + `AOVIAS.Infrastructure/aovias-config` (W1) | **Current physical truth** — not the desired final layout |

**What defines the intended broader layout:** The **target** blocks in `repository-layout.json` plus Phase 1B wave table. **No separate “approved final tree” PDF** exists beyond these; several buckets are **provisional** (manifest `unresolvedDecisions`).

**Missing architectural decisions (must not invent):**

- **O-3:** `Aovias.DeploymentValidation` → Studio vs Infrastructure.
- **O-4:** `Aovias.Runtime.Contracts` placement (today under `Aovias.Runtime.Agent/`).
- **O-5:** `tools/Publish*` → full `tools/` vs Infrastructure-only publish CLIs.
- **O-6:** When/how to populate `AOVIAS.Shared`.
- **O-7:** Website flatten `AOVIAS Website/AOVIAS Web` → `AOVIAS.Website` vs keep nested name.
- **O-8:** `preflight-docker-probe/` at repo root (tracked folder present at HEAD).
- **INV-1 / INV-2:** Full solution membership matrix; deployment.metadata.json inventory.
- **Target-profile wiring:** Production uses **Current** discovery only; physical moves require **Target** resolver/Docker/stager updates in same wave (`repository-layout.json` `discovery.target`).
- **Phase report files:** Dozens of untracked `AOVIAS_PHASE*.md` at root — archive policy undecided.

---

## 3. Proposed target directory tree (physical)

Legend: **Confirmed** = manifest target + 1B wave; **Recommendation** = needs explicit approval.

```text
V:\DevelopmentRepo\AOVIAS\                    # Integration repo root (unchanged path on disk)
├── AGENTS.md                                 # KEEP AT ROOT (governance)
├── repository-layout.json                    # KEEP AT ROOT (contract)
├── .gitignore, .gitattributes                # KEEP AT ROOT
├── ai-governance/                            # KEEP AT ROOT or → docs/governance (RECOMMENDATION)
├── docs/                                     # KEEP IN PLACE (expand for ADRs, ops guides)
├── scripts/                                  # KEEP AT ROOT (repo-wide dev/verify; path refs must update)
├── AOVIAS_PHASE*.md (untracked)              # REQUIRES DECISION (archive vs docs/migration/)
├── AOVIAS_REPOSITORY_RESTRUCTURING_ASSESSMENT.md  # KEEP or → docs/
│
├── AOVIAS.Command/                           # NEW product root (CONFIRMED target)
│   ├── CompliancePlatform/                   # MOVE from ComplaincePlatform/
│   └── WebApp/                               # MOVE from WebApp/
│
├── AOVIAS.Studio/                            # NEW product root (CONFIRMED target)
│   ├── Aovias.Platform.slnx                  # MOVE
│   ├── Aovias.PublishingEngine.slnx          # MOVE
│   ├── ComplianceServiceManager/             # MOVE (+ Operations* siblings)
│   ├── Aovias.Configuration/                 # MOVE
│   ├── Aovias.Deployment.Contracts/         # MOVE
│   ├── Aovias.CommandCenter/                 # MOVE (O-1)
│   ├── Aovias.Runtime.Agent/                 # MOVE (+ nested Runtime.Contracts O-4)
│   ├── Aovias.Publishing/                    # MOVE
│   ├── Aovias.Bootstrap/                     # MOVE
│   ├── Aovias.Release/                       # MOVE
│   ├── Aovias.Orchestration/                 # MOVE
│   ├── Aovias.DesignSystem/                  # MOVE
│   └── Aovias.DeploymentValidation/           # MOVE (target provisional O-3)
│
├── AOVIAS.Infrastructure/                    # EXISTS (CONFIRMED)
│   ├── aovias-config/                        # KEEP IN PLACE (W1 complete — templates only in Git)
│   ├── Aovias.ReleaseValidation/             # MOVE from root (CONFIRMED target)
│   └── tools/                                # MOVE from root tools/ (O-5 partial: Publish* at minimum)
│
├── AOVIAS.Website/                           # MOVE/rename from AOVIAS Website/AOVIAS Web (O-7)
├── AOVIAS.AccredaOne/                        # KEEP IN PLACE (already at target path)
└── preflight-docker-probe/                   # REQUIRES ARCHITECTURAL DECISION (O-8)
```

**Must remain addressable at repo root for tooling (until paths updated):** Solution entry points during transition may require **root redirects** or **dual-path CI** — long term, slnx move under product roots per manifest.

**Do not move:** Git worktrees on `V:\DevelopmentRepo\`; ignored `aovias-config/storage` legacy paths on operator machines; Phase 1H-D temp directory.

---

## 4. Top-level category mapping

| # | Category | Target location | Current members (representative) | Notes |
|---|----------|-----------------|----------------------------------|-------|
| 1 | Product / applications | `AOVIAS.Command/*`, `AOVIAS.Studio/*`, `AOVIAS.Website`, `AOVIAS.AccredaOne` | `ComplaincePlatform`, `WebApp`, `Aovias.*`, `ComplianceServiceManager*`, website nested tree | **Confirmed** manifest targets |
| 2 | Shared libraries / contracts | Under Studio (future `AOVIAS.Shared` empty) | `Aovias.Configuration`, `Aovias.Deployment.Contracts`, `ComplaincePlatform/Shared.*`, `Aovias.Runtime.Contracts` | **O-2**, **O-4**, **O-6** |
| 3 | Infrastructure / deployment | `AOVIAS.Infrastructure/` | `aovias-config/templates`, `Aovias.ReleaseValidation`, publish tools | **W1 done** for config templates |
| 4 | Tests / QA | Co-located with products + `ComplianceServiceManager*.Tests` | `**/tests/**`, `ComplaincePlatform/**/*Tests*` | Move **with** parent tree (same batch) |
| 5 | Build / dev / release tooling | `scripts/`, `AOVIAS.Infrastructure/tools/` | `scripts/dev/*.ps1`, `tools/PublishAllServices`, `tools/PublishRuntimeAgent` | **O-5** for tools scope |
| 6 | Documentation / phase history | `docs/` + **TBD** for `AOVIAS_PHASE*.md` | `docs/`, root assessments | **Decision required** |
| 7 | Repo metadata / build config | Root + per-tree `Directory.Build.props` | No root `Directory.Build.props`; Command has `Directory.Packages.props` | **No global root props** today |
| 8 | Local / generated / operational | **Never commit / never casual move** | `bin/`, `obj/`, `node_modules/`, `AOVIAS.Infrastructure/aovias-config/storage/`, `.publish`, Lighthouse, ZIPs | Gitignored; migrate ops data by **host procedure**, not `git mv` |

---

## 5. Physical relocation manifest

**W1 (complete — no further Git move for templates):**

| ID | Class | Current | Target | Status |
|----|-------|---------|--------|--------|
| M-W1-01 | Templates (5 files) | ~~`aovias-config/templates/*`~~ | `AOVIAS.Infrastructure/aovias-config/templates/*` | **KEEP IN PLACE** (done `a3188c9`) |
| M-W1-02 | Legacy root config tree | `aovias-config/` | *(absent)* | **Do not recreate** |

### Command wave (manifest W4 / 1B)

| ID | Class | Current source | Proposed destination | Tracked | Owner | Risk | Prerequisites |
|----|-------|----------------|----------------------|---------|-------|------|----------------|
| M-C-01 | MOVE | `ComplaincePlatform/` | `AOVIAS.Command/CompliancePlatform/` | Yes (~3157+ paths) | Command | **Critical** | Update `discovery.target`, scan roots, Compose contexts, `ComplaincePlatform/.github/workflows/platform-integrity.yml`, Command slnx path, `build-praman-aovpkg.ps1` `$repoRoot` segments |
| M-C-02 | MOVE | `WebApp/` | `AOVIAS.Command/WebApp/` | Yes | Command | **High** | Angular CI cwd, publishing portal discovery, CSM `RootPathResolver` WebApp segment |
| M-C-03 | MOVE | `ComplaincePlatform/CompliancePlatform.slnx` | `AOVIAS.Command/CompliancePlatform/CompliancePlatform.slnx` | Yes | Command | High | IDE/CI entry paths |

**Spelling:** Folder remains **`ComplaincePlatform`** under Command until separate rename approved (`repository-layout.json` policy).

### Studio wave (manifest W3 / 1B)

| ID | Class | Current source | Proposed destination | Tracked | Owner | Risk | Prerequisites |
|----|-------|----------------|----------------------|---------|-------|------|----------------|
| M-S-01 | MOVE | `Aovias.Platform.slnx` | `AOVIAS.Studio/Aovias.Platform.slnx` | Yes | Studio | **Critical** | All slnx project paths; Platform CI |
| M-S-02 | MOVE | `Aovias.PublishingEngine.slnx` | `AOVIAS.Studio/Aovias.PublishingEngine.slnx` | Yes | Studio | High | Publishing CI |
| M-S-03 | MOVE | `ComplianceServiceManager/` | `AOVIAS.Studio/ComplianceServiceManager/` | Yes | Studio | **Critical** | WPF launch paths, CSM slnx, NuGet.Config |
| M-S-04 | MOVE | `ComplianceServiceManager.Tests/` | `AOVIAS.Studio/ComplianceServiceManager.Tests/` | Yes | Studio | Medium | Test csproj refs |
| M-S-05 | MOVE | `ComplianceServiceManager.Operations/` | `AOVIAS.Studio/ComplianceServiceManager.Operations/` | Yes | Studio | Medium | Same |
| M-S-06 | MOVE | `ComplianceServiceManager.Operations.Tests/` | `AOVIAS.Studio/ComplianceServiceManager.Operations.Tests/` | Yes | Studio | Medium | Same |
| M-S-07 | MOVE | `Aovias.Configuration/` | `AOVIAS.Studio/Aovias.Configuration/` | Yes | Studio (O-2a) | **Critical** | Layout tests, all consumers |
| M-S-08 | MOVE | `Aovias.Deployment.Contracts/` | `AOVIAS.Studio/Aovias.Deployment.Contracts/` | Yes | Studio (O-2b) | High | DRV, Publishing |
| M-S-09 | MOVE | `Aovias.CommandCenter/` | `AOVIAS.Studio/Aovias.CommandCenter/` | Yes | Studio (O-1) | High | API/Infrastructure refs |
| M-S-10 | MOVE | `Aovias.Runtime.Agent/` | `AOVIAS.Studio/Aovias.Runtime.Agent/` | Yes | Studio | **Critical** | Dockerfile COPY, stager, CSM markers |
| M-S-11 | MOVE | `Aovias.Publishing/` | `AOVIAS.Studio/Aovias.Publishing/` | Yes | Studio | **Critical** | Publish CLI, preflight, tests |
| M-S-12 | MOVE | `Aovias.Bootstrap/` | `AOVIAS.Studio/Aovias.Bootstrap/` | Yes | Studio | High | VPS docs, deploy CLI |
| M-S-13 | MOVE | `Aovias.Release/` | `AOVIAS.Studio/Aovias.Release/` | Yes | Studio | High | ReleaseManager, packages |
| M-S-14 | MOVE | `Aovias.Orchestration/` | `AOVIAS.Studio/Aovias.Orchestration/` | Yes | Studio | High | Dockerfile partial COPY |
| M-S-15 | MOVE | `Aovias.DesignSystem/` | `AOVIAS.Studio/Aovias.DesignSystem/` | Yes | Studio | Medium | UI consumers |
| M-S-16 | MOVE | `Aovias.DeploymentValidation/` | `AOVIAS.Studio/Aovias.DeploymentValidation/` **or** `AOVIAS.Infrastructure/...` | Yes | **O-3** | **Critical** | 309+ tests, Dockerfile COPY list |
| M-S-17 | MOVE | Nested slnx under each product folder | Under `AOVIAS.Studio/...` per manifest `solutions[]` | Yes | Studio | High | Nine slnx files total at HEAD |

### Infrastructure wave (post-W1)

| ID | Class | Current source | Proposed destination | Tracked | Owner | Risk | Prerequisites |
|----|-------|----------------|----------------------|---------|-------|------|----------------|
| M-I-01 | KEEP IN PLACE | `AOVIAS.Infrastructure/aovias-config/` | *(unchanged)* | Yes (5 templates) | Infrastructure | Low | W1 contract preserved |
| M-I-02 | MOVE | `Aovias.ReleaseValidation/` | `AOVIAS.Infrastructure/Aovias.ReleaseValidation/` | Yes | Infrastructure | Medium | Node tooling paths |
| M-I-03 | MOVE | `tools/PublishAllServices/` | `AOVIAS.Infrastructure/tools/PublishAllServices/` | Yes | Infrastructure (O-5) | High | Publish CLI cwd, csproj refs |
| M-I-04 | MOVE | `tools/PublishRuntimeAgent/` | `AOVIAS.Infrastructure/tools/PublishRuntimeAgent/` | Yes | Infrastructure (O-5) | High | Same |
| M-I-05 | DEFER / DECISION | `tools/` *(entire tree)* | `AOVIAS.Infrastructure/tools/` | Yes | O-5 | Medium | Confirm no non-infra tools before whole-tree move |

### Website wave

| ID | Class | Current source | Proposed destination | Tracked | Owner | Risk | Prerequisites |
|----|-------|----------------|----------------------|---------|-------|------|----------------|
| M-W-01 | MOVE | `AOVIAS Website/AOVIAS Web/` | `AOVIAS.Website/` | Yes | Website (O-7) | **High** | Nested space in path; website `.github/workflows/ci.yml`; ZIP/Lighthouse untracked sibs |

### AccredaOne

| ID | Class | Current | Target | Status |
|----|-------|---------|--------|--------|
| M-A-01 | KEEP IN PLACE | `AOVIAS.AccredaOne/` | `AOVIAS.AccredaOne/` | Already at manifest target |

### Runtime.Contracts (unresolved)

| ID | Class | Current source | Proposed destination | Status |
|----|-------|----------------|----------------------|--------|
| M-R-01 | REQUIRES ARCHITECTURAL DECISION | `Aovias.Runtime.Agent/src/Aovias.Runtime.Contracts/` (and related) | TBD (O-4) | Move with Agent unless split approved |

### Root governance / docs / scripts

| ID | Class | Path | Disposition | Status |
|----|-------|------|-------------|--------|
| M-G-01 | KEEP AT ROOT | `AGENTS.md` | Stay | **Confirmed** |
| M-G-02 | KEEP AT ROOT | `repository-layout.json` | Stay; update after each wave | **Confirmed** |
| M-G-03 | KEEP AT ROOT | `scripts/` | Stay; fix relative paths in agents | **Recommendation** |
| M-G-04 | KEEP IN PLACE | `docs/` | Stay | **Confirmed** |
| M-G-05 | REQUIRES DECISION | `ai-governance/` | Root vs `docs/` | **Recommendation** |
| M-G-06 | REQUIRES DECISION | `preflight-docker-probe/` | Delete, gitignore, or relocate (O-8) | **Unresolved** |
| M-G-07 | DEFER | Untracked `AOVIAS_PHASE*.md`, ZIPs, Lighthouse | No move in product waves | **Preserve** until archive policy |
| M-G-08 | KEEP AT ROOT | `ACCREDACONE_KNOWLEDGEBASE_READINESS_ASSESSMENT.md`, restructuring assessment | Optional → `docs/` | **Recommendation** |

**Reference updates (all MOVE rows):** ProjectReference relative paths, `repository-layout.json` **current** fields (post-wave), `discovery.current` markers, `Legacy*` + adapter tests, `RuntimeAgentDockerContextStager` + `Aovias.Runtime.Agent/Dockerfile` COPY paths, `SolutionDeployableProjectDiscoverer.GetScanRoots`, `ComplaincePlatform/scripts/build-praman-aovpkg.ps1`, `scripts/verify-build.sh`, website and Command CI workflows.

**Rollback:** `git revert` of merge commit per wave; **do not** `git clean` ignored storage; restore worktrees from pre-wave SHA; keep W1 templates at infra path on rollback of later waves unless reverting W1 itself.

---

## 6. Dependency and tooling impact map

| Impact area | Evidence (HEAD) | If Studio/Command moves without update |
|-------------|-----------------|----------------------------------------|
| **Layout discovery (production)** | `RepositoryLayoutProductionAdapter` + **Current** markers (`repository-layout.json` L188–209) | **Break** until **Current** updated or Target profile activated |
| **Publishing scan roots** | `SolutionDeployableProjectDiscoverer.GetScanRoots` → `ComplaincePlatform`, `Aovias.Runtime.Agent` | **Break** deployable discovery |
| **Runtime Agent Docker** | `Aovias.Runtime.Agent/Dockerfile` COPY `Aovias.Configuration/`, … | **Break** image build |
| **Docker context stager** | `RuntimeAgentDockerContextStager` fallback folder list; regex on Dockerfile | **Break** staged builds |
| **Publish CLIs** | `tools/PublishAllServices/Program.cs` platform root resolution | **Break** if cwd/refs wrong |
| **CSM** | `RootPathResolver`, `appsettings.json` `ConfigRoot` infra-relative | Partial break on **product** paths, not config root |
| **MSBuild** | Per-project `Directory.Build.props` under Orchestration, Release, Publishing, Bootstrap, Runtime.Agent, CommandCenter | **Update** relative imports after move |
| **Central packages** | `ComplaincePlatform/Directory.Packages.props` | Stays with Command tree |
| **NuGet** | `ComplianceServiceManager/NuGet.Config` only at product level | Moves with CSM |
| **CI** | `ComplaincePlatform/.github/workflows/platform-integrity.yml`; `AOVIAS Website/AOVIAS Web/.github/workflows/ci.yml` | **Update** `working-directory` |
| **Compose** | Under `ComplaincePlatform/docker-compose*.yml` (per 1B inventory) | **Update** build contexts |
| **Tests** | Layout 39 tests; Publishing paths; 309 DeploymentValidation; walk-up tests to infra config | **Update** expected paths |
| **IDE / launchSettings** | Untracked Claude `Properties/launchSettings.json` | Human merge — not in manifest |
| **Worktrees** | Fixed paths `AOVIAS`, `.Cursor`, `.Claude` | **Unchanged** by design |

**Uncertain (flag for later phase):** Full Compose edge list (INV inventory gap); Playwright/QA paths; every `deployment.metadata.json` path (INV-2).

---

## 7. Migration strategy (batches — not executed)

| Batch | Scope | Same-batch code/config updates | Pre/post gates | Separate auth for publish/Docker? |
|-------|--------|--------------------------------|----------------|-----------------------------------|
| **W0** | Docs, manifest **current** pointers, optional phase-report index | `repository-layout.json`, compatibilityConstraints text | Layout tests green | No |
| **W1** | Config templates → infra | *(done)* | 1H-A verified | No |
| **W2** | `Aovias.ReleaseValidation` + `tools/Publish*` → `AOVIAS.Infrastructure/` | Tool csproj paths; script invocations | Publish layout tests; `--discover` | **Yes** if validating CLI |
| **W3** | **Studio tree** (M-S-01–M-S-17) | **Target** discovery wiring OR bulk **current** path update; Dockerfile; stager; slnx; all ProjectReferences | Platform/Publishing slnx build; 1H-C test bundle; Docker stager tests | **Yes** (preflight/publish) |
| **W4** | **Command tree** (M-C-01–M-C-03) | Compose; Command CI; praman script; scan roots | Command slnx build; integration tests | **Yes** (praman script) |
| **W5** | Website (M-W-01) | Website CI; monorepo scripts | Website CI | Per website pipeline |
| **W6** | AccredaOne expansion | Product-specific | AccredaOne scaffold tests | TBD |

**Rules (from 1B):** One product bucket per wave; **no** Command+Studio in same wave; resolver/manifest updates **in the same authorized commit set** as `git mv`; human integration on `main` only; three worktrees synced after merge.

**Rollback per batch:** Revert merge commit; paths in manifest restored to previous **current**; ignored user data untouched; agent worktrees reset to validated active baseline only after human confirmation.

---

## 8. Unresolved questions for human approval

1. **O-3:** Final home for `Aovias.DeploymentValidation` (Studio vs Infrastructure).
2. **O-4:** `Aovias.Runtime.Contracts` — stay under Agent vs promote to Shared.
3. **O-5:** Move entire `tools/` or only Publish* CLIs.
4. **O-7:** Flatten website path and handle space in `AOVIAS Website`.
5. **O-8:** Disposition of `preflight-docker-probe/` at repo root.
6. **Target profile:** Schedule for production use of `discovery.target` vs editing **current** only.
7. **Phase reports:** Centralize under `docs/migration/` vs leave untracked at root.
8. **ComplaincePlatform rename:** Deferred indefinitely or scheduled post-move?
9. **INV-1/INV-2:** Complete inventories before W3/W4 execution.

---

## 9. Relation to completed W1 and Phase 1H work

- **Do not** recreate root `aovias-config/` or move templates back (1H-A, 1G-F).
- **Do not** authorize physical moves until **1I-B+** human approval of this manifest and open **O-*** decisions.
- Post-1H-D ignored `storage/` and `node_modules` changes are **operational**, not input to `git mv` planning.

---

## 10. Deliverable

| Report | Path |
|--------|------|
| Target layout and relocation manifest | `AOVIAS_PHASE1IA_TARGET_PHYSICAL_LAYOUT_REPORT.md` (untracked on Main) |

**Final status:** **ARCHITECTURAL DECISIONS REQUIRED**

Once **O-3–O-8** and phase-report policy are decided, status can advance to **TARGET STRUCTURE READY FOR HUMAN APPROVAL** for wave-by-wave execution authorization.
