# AOVIAS Phase 1C-A — Test Report

**Date:** 2026-10-25  
**Baseline HEAD:** `5a1526d99a68241d40ccba759d8fc62586f4af86`

---

## 1. Tests executed (fresh)

**Command:**

```text
dotnet test V:\DevelopmentRepo\AOVIAS\Aovias.Configuration\tests\Aovias.Configuration.Tests\Aovias.Configuration.Tests.csproj -c Release --filter "FullyQualifiedName~RepositoryLayoutContractTests"
```

**Environment:** Integrator machine; .NET SDK available (test host reported `net10.0`).

**Outcome:** **PASS** — **11/11** tests, ~141 ms (after resolver nearest-ancestor fix in **contract** code only).

**Label:** **Freshly executed** in Phase 1C-A.

---

## 2. Test coverage map

| Requirement | Test(s) | Result |
|-------------|---------|--------|
| Manifest load + validation | `LoadFromFile_validates_repository_layout_json_at_repo_root`, `LoadFromJson_rejects_missing_schema_version`, `LoadFromJson_rejects_path_traversal_segments` | Pass |
| Current-layout root discovery | `TryResolveRepositoryRoot_finds_current_layout_from_repo_fixture`, integration repo marker test | Pass |
| Target-layout (fixture) | `TryResolveRepositoryRoot_finds_target_layout_from_synthetic_fixture`, `Target_profile_resolution_uses_injected_fixture_without_on_disk_target_tree` | Pass |
| Multiple candidates (nearest wins) | `TryResolveRepositoryRoot_prefers_nearest_ancestor_when_multiple_match` | Pass |
| Invalid / escape paths | `CombineUnderRepositoryRoot_rejects_escape_attempts` | Pass |
| Discovery roots (explicit root) | `GetDeployableProjectScanRoots_*`, `GetDefaultApplicationRoots_*` | Pass |
| Backward compatibility (contract vs legacy **names**) | Scan roots = `ComplaincePlatform` + `Aovias.Runtime.Agent`; app roots = `ComplaincePlatform` + `WebApp` | Pass |
| Integration repo current paths exist | `Current_layout_key_paths_exist_when_manifest_loaded_from_integration_repo` | Pass |

---

## 3. Blocked / not attempted

| Item | Reason | Smallest follow-up |
|------|--------|-------------------|
| Assert **`PlatformRootResolver.Resolve()`** against target layout | Production resolver **not modified** by design; no DI hook | Phase 1C-B: adapter or optional manifest parameter + golden tests |
| Assert **`RootPathResolver`** against target layout | Same | Same |
| **`RuntimeAgentDockerContextStager`** target COPY list | Production code unchanged | Wire stager to manifest `runtimeAgentDockerCopyRoots.target` in implementation phase |
| Full **`Aovias.Platform.slnx`** build | NU1903 historical baseline; out of scope | Separate remediation track |
| Publishing.Core.Tests path suite | Not required for 1C-A gate | Run selectively when wiring publishing |
| `npm ci` / website tests | Prohibited | N/A |

**Architecture note:** Target-layout behavior is proven on **`RepositoryLayoutPathResolver` + temp fixtures**, not on legacy resolvers. This is intentional for 1C-A.

---

## 4. Baseline failures

Phase 0 NU1903, website npm, and integration tests were **not re-run**. They **did not block** Configuration.Tests execution.

---

## 5. Production and directory layout confirmation

| Check | Status |
|-------|--------|
| Physical directory tree | **Unchanged** (no `git mv`) |
| `RootPathResolver`, `PlatformRootResolver`, discoverer, stager | **Unchanged** |
| Untracked artifacts (probe, Properties, ZIPs, Lighthouse) | **Unchanged** |
| Runtime probe executed | **No** |

---

## 6. Readiness verdict

### **READY FOR PHASE 1C-B REVIEW**

Meaning: Layout contract JSON, contract loader/resolver, and focused tests are in place for human review. **Does not authorize** physical migration or production resolver wiring.

---

## 7. Smallest safe next step

1. Human review of `repository-layout.json` and Phase 1C-A reports.  
2. Authorize **Phase 1C-B** scope: either **AccredaOne scaffold-only** (additive) and/or **production resolver adapter** design with golden tests against `RepositoryLayoutProfile.Current` first.  
3. Defer any **`aovias-config` move** until resolvers read the contract with dual-mode fallback.

---

*End of Phase 1C-A test report.*
