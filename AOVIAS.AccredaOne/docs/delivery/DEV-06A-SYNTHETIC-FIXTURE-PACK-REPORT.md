# DEV-06A — Synthetic fixture pack and boundary verification

Date: 2026-11-02 (original implementation); **DEV-06A-R1** and **DEV-06A-R2** corrective passes 2026-11-02.

**Authorization:** Product Owner approved DEV-06A on 2026-11-02 (ACR-DEV-06A-01 scope).

**Status:** Implemented on task branch `agent/cursor/accredaone-dev06a-synthetic-fixtures` — **not merged, not pushed, not published or closed**.

**Baseline:** canonical `main` @ **`ec3fee7ee3a2e9611f42a604aeb86bcf038c0ddc`**

**Original implementation commit:** `20d9cbf50b8987f7e2113aaffe7efba0d4752731`

This increment is **test-only**. No production API, UI, database, or deployment changes.

## Independent review (pre-R1)

Verdict: **PASS WITH FINDINGS** (read-only review vs PO task; Confluence page not available in reviewer environment).

| Severity | Finding | R1 response |
| --- | --- | --- |
| High | Query resolver branched on fixture `expectedOutcome`; tests mirrored the same field (circular verification). | **R1:** Resolver derives outcomes from `scenarioType`, `frameworkCode`, and framework eligibility only. Tests assert from those inputs; fixture `expectedOutcome` is cross-checked as an oracle after independent resolution. |
| Medium | Loader gaps: disabled-framework attachments, scenario coherence, one-source/one-explanation per eligible framework. | **R2:** Loader enforces these rules with deterministic error messages. |
| Medium | Weak negative and version-behavior tests. | **R3 / R4:** Temp-copy negative tests; version applicability helpers and tests. |
| Low | Incomplete `classification` asserts on non-framework entities. | **R3:** Provenance test asserts synthetic classification on all entity types. |

**Confluence:** [DEV-06A specification](https://nishant11soni.atlassian.net/wiki/spaces/ACCREDAONE/pages/1310735/DEV-06A+Synthetic+Fixture+Pack+Boundary+Verification) was **not** byte-level verified in this repository; mapping follows PO authorization and local artifacts only.

## DEV-06A-R2 (D1 no-framework selection)

**Decision:** Queries without an explicit framework auto-discover across all **eligible** frameworks, with results grouped separately by framework code (LCF excluded).

| Change | Detail |
| --- | --- |
| Resolver | `no_framework_selection` → outcome `bounded_cross_framework_results` via `GroupEligibleRequirementsByFramework` |
| Fixture | `SYN-QRY-003` `expectedOutcome` updated from `clarification_required` |
| Loader | `no_framework_selection` must expect `bounded_cross_framework_results`; at most one verified `current` version per framework |
| Tests | Cross-framework grouping assertions; disabled version/explanation loader negatives |

## Fixture inventory (presence)

| Category | Count | Notes |
| --- | ---: | --- |
| Eligible fictional frameworks | 2 | NOAF, MCQF |
| Ineligible fictional framework | 1 | LCF (`disabled`) |
| Source instruments | 2 | One per eligible framework |
| Current requirements | 8 | Four per eligible framework |
| NOAF version records | 3 | historical, current, unverified publication candidate |
| AccredaOne synthetic explanations | 2 | One per eligible framework |
| Query scenarios | 4 | explicit NOAF, explicit MCQF, no selection (cross-framework grouped), ambiguous/unsupported |

Manifest: `tests/AccredaOne.Api.Tests/Fixtures/Synthetic/manifest.json`

All records use `classification: synthetic` and fictional provenance strings. No real accreditation publishers, clause IDs, or source URLs.

## What behavior is actually verified (post-R1)

| Area | Verified by |
| --- | --- |
| Fixture graph isolation and grouping | `SyntheticFixtureBoundaryVerifier` + boundary tests |
| Query bounds (eligible / disabled / unknown / no selection / ambiguous) | Resolver logic independent of `expectedOutcome`; **R2:** `no_framework_selection` → `bounded_cross_framework_results` grouped by eligible framework (D1); explicit/disabled/unknown unchanged |
| Loader integrity | Expanded `Validate` + `SyntheticFixtureLoaderValidationTests` on temp fixture copies |
| Version **fixture** integrity | Single `current` NOAF row; historical and unverified not applicable as current; framework/source refs on NOAF requirements |
| Fixture oracle consistency | Loader rejects `expectedOutcome` that disagrees with scenarioType/framework contract |

**Not verified (by design, DEV-06A scope):** Production query APIs, D4 version-resolution contracts, runtime database seeding, or Confluence AC text line-by-line.

## Files (test project)

| Path | Role |
| --- | --- |
| `Fixtures/Synthetic/*.json` | Deterministic fixture pack |
| `Fixtures/Synthetic/SyntheticFixtureModels.cs` | Test-only models |
| `Fixtures/Synthetic/SyntheticFixtureLoader.cs` | Load + validate (R2) |
| `Fixtures/Synthetic/SyntheticFixtureBoundaryVerifier.cs` | Boundary/query resolution (R1) |
| `Fixtures/Synthetic/SyntheticFixtureBoundaryTests.cs` | Boundary + version fixture tests |
| `Fixtures/Synthetic/SyntheticFixtureLoaderValidationTests.cs` | Negative loader tests (R3) |
| `AccredaOne.Api.Tests.csproj` | Copy JSON fixtures to output |

## Validation evidence (R1 closeout)

Commands (run from repository root):

```powershell
dotnet test AOVIAS.AccredaOne/tests/AccredaOne.Api.Tests/AccredaOne.Api.Tests.csproj -c Release --filter "FullyQualifiedName~SyntheticFixture"
dotnet test AOVIAS.AccredaOne/tests/AccredaOne.Api.Tests/AccredaOne.Api.Tests.csproj -c Release
git diff --check
```

**R1 run (uncommitted):**

| Command | Result |
| --- | --- |
| `dotnet test ... --filter "FullyQualifiedName~SyntheticFixture" -c Release` | **32/32 passed** (after R2) |
| `dotnet test ... -c Release` (full test project) | **52/52 passed** (20 existing API + 32 synthetic) |
| `git diff --check` | **pass** |

## Acceptance criteria mapping (evidence)

| Criterion | Status | Evidence |
| --- | --- | --- |
| AC-01 Test-only fixture pack | Met | JSON under test project only |
| AC-02 Two eligible fictional frameworks | Met | `frameworks.json`; eligibility tests |
| AC-03 Sources + current requirements per eligible FW | Met | Loader counts; isolation tests |
| AC-04 Historical / unverified / disabled fixtures | Met | `framework-versions.json`; LCF disabled |
| AC-05 Explanations + four query scenarios | Met | JSON + scenario tests |
| AC-06 Synthetic classification + provenance | Met | Loader rejects non-synthetic; boundary provenance test |
| AC-07 Deterministic loader validation | Met (R2) | Duplicate ID, refs, disabled attachments, scenario contract |
| AC-08 Boundary verification | Met with disclosed limits (R1) | Independent resolver; not production query service |
| AC-09–AC-12 Scope exclusions | Met | No production/DB/network/AI changes in diff |
| AC-13 Release output fixture copy | Met | `CopyToOutputDirectory`; loader resolves output path |
| AC-14 Documentation | Met (R5) | This report; Confluence not byte-verified |

## Remaining limitations

- Boundary verifier remains **test-assembly only**; no production abstraction.
- Version tests assert **fixture-level** current/historical/unverified markers, not end-to-end publication workflow.
- ACR-DEV-06A-01 full text not stored in repo.
- DEV-06A-R1 changes are **uncommitted** until explicit commit authorization after review.

## Governance (unchanged)

G1 **NOT PASSED**. Phase 1 **NOT AUTHORIZED**. Live PostgreSQL not activated. PostCSS exception unchanged. DEV-06B+ not started.
