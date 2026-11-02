# AOVIAS Phase 1I-Q — Independent W3 Migration Review

**Phase:** 1I-Q (read-only review — no repairs)  
**Date:** 2026-10-25  
**Reviewed branch:** `agent/cursor/phase1ip-w3-studio-migration` @ `V:\DevelopmentRepo\AOVIAS.Cursor`  
**Diff base:** `b9e2de634b86d04f54b3a1d97ad6bfc44fa646f1`

---

## Final status

**`W3 REVIEW PASS WITH NON-BLOCKING FINDINGS`**

The approved **physical relocation and manifest contract** appear satisfied on static evidence. One **HIGH** test-contract defect should be corrected **before** scoped build/test validation. This review does **not** authorize validation, commit, merge, push, or worktree sync.

---

## 1. Baseline and branch evidence

| Worktree | Branch | HEAD | Tracked delta vs expectation |
|----------|--------|------|------------------------------|
| Main | `main` | `b9e2de634b86d04f54b3a1d97ad6bfc44fa646f1` | **Match** |
| Claude | `agent/claude/active` | `b9e2de634b86d04f54b3a1d97ad6bfc44fa646f1` | **Match** |
| Cursor | `agent/cursor/phase1ip-w3-studio-migration` | `b9e2de634b86d04f54b3a1d97ad6bfc44fa646f1` | **Match** (no commit; tree dirty) |

**Cursor working tree:** Large **rename/move** set under `AOVIAS.Studio/` plus **20** content-modified paths (see §8). **No staged changes** observed in sample status. **Untracked** pre-existing phase reports + `AOVIAS_PHASE1IP_W3_IMPLEMENTATION_REPORT.md` + this report.

**Sources reviewed:**

| Document | Location | Status |
|----------|----------|--------|
| `AGENTS.md` | Cursor repo | Read |
| `AOVIAS_PHASE1IL_W3_APPROVAL_RECORD.md` | **Main** root (§11 Phase 1I-O) | Read |
| `AOVIAS_PHASE1IK_W3_SCOPE_APPROVAL_PACKAGE.md` | Main (referenced) | Not re-opened line-by-line; cross-checked via 1I-P report |
| INV-1 report | Main | Referenced for counts |
| `AOVIAS_PHASE1IP_W3_IMPLEMENTATION_REPORT.md` | Cursor | Read; **cross-checked against diff** |
| `repository-layout.json` | Cursor branch | Inspected |
| Branch diff | `git diff b9e2de6…` | Inspected |

**Note:** Approval record **§11.3** still records **K-17 DEFERRED**; W3 implementation on this branch relies on **separate Phase 1I-P human authorization**, not an updated §11 entry (see finding F-02).

---

## 2. Manifest verification (M-S-01 … M-S-17)

| ID | Approved destination | Diff / disk evidence | Result |
|----|----------------------|----------------------|--------|
| **M-S-01** | `AOVIAS.Studio/Aovias.Platform.slnx` | Root slnx absent; Studio path present; rename in diff | **PASS** |
| **M-S-02** | `AOVIAS.Studio/Aovias.PublishingEngine.slnx` | Co-moved | **PASS** |
| **M-S-03** | `AOVIAS.Studio/ComplianceServiceManager/` | Co-moved | **PASS** |
| **M-S-04** | `AOVIAS.Studio/ComplianceServiceManager.Tests/` | Co-moved | **PASS** |
| **M-S-05** | `AOVIAS.Studio/ComplianceServiceManager.Operations/` | Co-moved | **PASS** |
| **M-S-06** | `AOVIAS.Studio/ComplianceServiceManager.Operations.Tests/` | Co-moved | **PASS** |
| **M-S-07** | `AOVIAS.Studio/Aovias.Configuration/` | Co-moved | **PASS** |
| **M-S-08** | `AOVIAS.Studio/Aovias.Deployment.Contracts/` | Co-moved | **PASS** |
| **M-S-09** | `AOVIAS.Studio/Aovias.CommandCenter/` | Co-moved | **PASS** |
| **M-S-10** | `AOVIAS.Studio/Aovias.Runtime.Agent/` (+ Contracts in-tree) | Co-moved; Contracts csproj still under Agent `src/` | **PASS** |
| **M-S-11** | `AOVIAS.Studio/Aovias.Publishing/` | Co-moved | **PASS** |
| **M-S-12** | `AOVIAS.Studio/Aovias.Bootstrap/` | Co-moved; `.sln` present | **PASS** |
| **M-S-13** | `AOVIAS.Studio/Aovias.Release/` | Co-moved | **PASS** |
| **M-S-14** | `AOVIAS.Studio/Aovias.Orchestration/` | Co-moved | **PASS** |
| **M-S-15** | `AOVIAS.Studio/Aovias.DesignSystem/` | Co-moved | **PASS** |
| **M-S-16** | `AOVIAS.Studio/Aovias.DeploymentValidation/` (O-3) | Co-moved | **PASS** |
| **M-S-17** | Nested area `*.slnx` under Studio | **8** `.slnx` under `AOVIAS.Studio/` (2 root + 6 nested); 9th is Command/W4 | **PASS** |

**W4/W5 exclusion:** `git diff b9e2de6… -- ComplaincePlatform WebApp "AOVIAS Website"` → **no paths**. **PASS**

**Orphans:** `ComplianceServiceManager.Tests` **not** listed in any reviewed `.slnx`. Platform slnx has **38** `<Project Path=` entries; PublishingEngine **14**. **PASS** (no orphan cleanup scope creep).

**Git rename integrity:** ~1197 paths in diff stat (mostly **R**enames with history). No evidence of duplicate root + Studio copies for product trees. **PASS**

---

## 3. Solutions and project references

| Check | Evidence | Result |
|-------|----------|--------|
| Platform **38** projects | Count in `AOVIAS.Studio/Aovias.Platform.slnx` | **PASS** |
| PublishingEngine **14** | Count in `AOVIAS.Studio/Aovias.PublishingEngine.slnx` | **PASS** |
| Infrastructure tools in Platform slnx | `../AOVIAS.Infrastructure/tools/PublishAllServices` and `PublishRuntimeAgent` | **PASS** |
| Internal Studio-relative project paths | Sibling paths under Studio (e.g. `Aovias.Configuration/…`) unchanged semantics | **PASS** |
| `Aovias.Bootstrap.sln` | `..\Aovias.Configuration`, `..\Aovias.Release\…` valid under Studio | **PASS** |
| Publish tools **K-13** | `..\..\..\AOVIAS.Studio\Aovias.Configuration\…` and Publishing SDK paths; files exist on disk | **PASS** |
| CSM → **Runtime.Contracts** | `..\Aovias.Runtime.Agent\src\Aovias.Runtime.Contracts\…` (sibling under Studio) | **PASS** |
| Stale `..\..\..\Aovias.Configuration` in `.csproj` | Grep in Cursor: **no matches** | **PASS** |
| `CompliancePlatform.slnx` | Remains under `ComplaincePlatform/`; not in W3 diff | **PASS** |

**NOT VERIFIED (requires build):** All 38+14 project paths compile and load in CLI — deferred to validation phase.

---

## 4. Docker / O-3 static review

**Strategy (verified):** Repository-root build context; Dockerfile uses **`COPY AOVIAS.Studio/…`**; `WORKDIR /src/AOVIAS.Studio/Aovias.Runtime.Agent`.

| Element | Contract | Evidence | Result |
|---------|----------|----------|--------|
| Dockerfile COPY prefixes | Match disk under repo root | Lines 13–29 in `AOVIAS.Studio/Aovias.Runtime.Agent/Dockerfile` | **PASS** |
| Stager `CopyRootPattern` | Parse COPY source prefixes | `^COPY\s+([^\s]+)/` | **PASS** |
| Stager fallback roots | Seven Studio-prefixed folders | `RuntimeAgentDockerContextStager.cs` L11–19 | **PASS** |
| `TryResolvePlatformRoot` | `AOVIAS.Studio/Aovias.Configuration` + Agent `src` | L40–41 | **PASS** |
| ValidateDeploymentPathPolicy paths | Under `AOVIAS.Studio/Aovias.Runtime.Agent/…` | L92+ in stager | **PASS** |
| `discovery.current.runtimeAgentDockerCopyRoots` | Seven Studio paths | `repository-layout.json` L201–208 | **PASS** |
| Alignment Dockerfile ↔ manifest ↔ fallback | Same seven product roots | Static compare | **PASS** |

**NOT VERIFIED:** Docker daemon build, stager `Stage()` at runtime (K-16). Regex may yield both `…/Aovias.Runtime.Agent` and `…/Aovias.Runtime.Agent/src` entries from Dockerfile — static analysis suggests staging remains consistent; **runtime stager test** partially covers (see F-01).

---

## 5. Layout and discovery contract

| Check | Evidence | Result |
|-------|----------|--------|
| Studio `paths.*.current` | All `AOVIAS.Studio/…`; `migrationStatus`: `studio-at-target` | **PASS** |
| `solutions[]` current paths | Studio slnx paths updated | **PASS** |
| `discovery.current` Agent/CSM/scan roots | Studio Agent path; Command roots unchanged | **PASS** |
| W1/W2 Infrastructure paths | `AOVIAS.Infrastructure/aovias-config`, tools, ReleaseValidation unchanged in diff filter | **PASS** |
| Target profile production | `IsPermittedProductionProfile(Target)` still false in adapter tests (unchanged pattern) | **PASS** |
| Legacy fallback markers | `RepositoryLayoutPlatformRootDiscovery` accepts Studio Agent **or** legacy root Agent | **PASS** |
| Scripts | `scripts/verify-build.sh`, `verify-tests.sh` → `AOVIAS.Studio/Aovias.Platform.slnx` | **PASS** |
| Discoverer scan root | `SolutionDeployableProjectDiscoverer` → `AOVIAS.Studio/Aovias.Runtime.Agent` | **PASS** |
| Integration layout test expectation | `RepositoryLayoutContractTests` scan root assertion updated to Studio path | **PASS** |

**LOW:** Embedded JSON fixtures in layout unit tests still include **legacy** `current` profile samples for Target/current comparison — intentional for profile tests, not disk layout.

---

## 6. Diff integrity

| Check | Result |
|-------|--------|
| `git diff --check b9e2de6…` | **PASS** (no reported conflict markers / whitespace errors) |
| W4/W5 paths in diff | **None** |
| `bin/` / `obj/` in diff | **None found** in name scan |
| Secrets / credentials | **No** `.pfx`, `.env`, `.key` in diff names |
| Staged index | **None** observed |
| Diff scale | **1197 files changed**, **124 insertions**, **105 deletions** (moves dominate) |

**Content-modified tracked files (non-renamed content edits):**

- `AOVIAS.Infrastructure/tools/PublishAllServices/PublishAllServices.csproj`
- `AOVIAS.Infrastructure/tools/PublishRuntimeAgent/PublishRuntimeAgent.csproj`
- `repository-layout.json`
- `docs/milestones/G1-EXECUTION-RUNBOOK.md`
- `scripts/verify-build.sh`, `scripts/verify-tests.sh`

Additional edits live **inside moved trees** (Dockerfile, stager, resolvers, tests, Platform slnx tool paths, etc.) — consistent with approved batch.

---

## 7. K-16 boundary (observed)

**Not executed:** `--discover`, full preflight, Docker build/run, npm ReleaseValidation, **any** `dotnet build` / `dotnet test`.

---

## 8. Proposed focused validation commands — **NOT RUN**

To be authorized in a **separate** validation phase:

```powershell
dotnet build "AOVIAS.Studio/Aovias.Platform.slnx" -c Release
dotnet build "AOVIAS.Studio/Aovias.PublishingEngine.slnx" -c Release
dotnet test "AOVIAS.Studio/Aovias.Configuration/tests/Aovias.Configuration.Tests/Aovias.Configuration.Tests.csproj" -c Release --filter FullyQualifiedName~Layout
dotnet test "AOVIAS.Studio/Aovias.Publishing/tests/Aovias.Publishing.Core.Tests/Aovias.Publishing.Core.Tests.csproj" -c Release --filter FullyQualifiedName~PublishingProjectPaths
dotnet test "AOVIAS.Studio/Aovias.Publishing/tests/Aovias.Publishing.Core.Tests/Aovias.Publishing.Core.Tests.csproj" -c Release --filter FullyQualifiedName~RuntimeAgentDockerContextStager
dotnet build "AOVIAS.Infrastructure/tools/PublishAllServices/PublishAllServices.csproj" -c Release
dotnet build "AOVIAS.Infrastructure/tools/PublishRuntimeAgent/PublishRuntimeAgent.csproj" -c Release
```

**Likely scope:** Cross-platform Studio solutions, layout contract tests, publishing path + stager tests, Infrastructure publish tool compile. **Excluded** until separately authorized: discover, preflight, Docker, npm.

---

## 9. Findings

| ID | Severity | Location | Expected | Actual | Impact | Corrective action |
|----|----------|----------|----------|--------|--------|-------------------|
| **F-01** | **HIGH** | `AOVIAS.Studio/Aovias.Publishing/tests/…/RuntimeAgentDockerContextStagerTests.cs` L15–23 | Parsed folder for `COPY …/Aovias.Runtime.Agent/src/` should match assertion | Sample dockerfile only has `…/src/` COPY; parser yields **`AOVIAS.Studio/Aovias.Runtime.Agent/src`**, not **`AOVIAS.Studio/Aovias.Runtime.Agent`** | **`ResolveRequiredMonorepoFolders_includes_deployment_validation_from_dockerfile` likely fails** when run | Adjust assertion to `…/src` **or** normalize parser to collapse `/src` suffix **before** validation phase |
| **F-02** | **MEDIUM** | Main `AOVIAS_PHASE1IL_W3_APPROVAL_RECORD.md` §11.3 | K-17 state after 1I-P | Still **K-17 DEFERRED** | Audit trail lag; implementation authorized via separate prompt | Optional: Phase 1I-R record update (not required for technical correctness) |
| **F-03** | **LOW** | `AOVIAS.Studio/Aovias.Platform.slnx` header comments | Post-W3 entry-point paths | Comments still say root `Aovias.Platform.slnx` | Doc confusion only | Update comments in a small follow-up (optional) |
| **F-04** | **LOW** | Milestone docs outside G1 runbook | Studio slnx paths | Some historical docs unchanged | Operators may use old paths | Triage in doc pass (W3 non-blocker) |
| **F-05** | **NOT VERIFIED** | Whole branch | Green compile/test | No build executed (K-16 / review rules) | Unknown CS/NU errors | Run §8 commands after authorization |

---

## 10. Readiness recommendation

| Gate | Recommendation |
|------|----------------|
| **Migration contract (static)** | **Satisfied** — approve proceeding to **separate validation authorization** |
| **Before validation** | Fix **F-01** (recommended — avoids known test failure) |
| **Commit / merge / sync** | **Not authorized** by this review |

---

## 11. Git status summary

- **Branch:** `agent/cursor/phase1ip-w3-studio-migration`
- **HEAD:** `b9e2de634b86d04f54b3a1d97ad6bfc44fa646f1` (unchanged)
- **Staged:** none
- **Committed:** none
- **New untracked review:** this file

**No files were modified during Phase 1I-Q.**
