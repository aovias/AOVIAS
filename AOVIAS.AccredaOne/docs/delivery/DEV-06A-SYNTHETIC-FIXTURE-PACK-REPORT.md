# DEV-06A — Synthetic fixture pack and boundary verification

Date: 2026-11-02

**Authorization:** Product Owner approved DEV-06A on 2026-11-02 (ACR-DEV-06A-01 scope).

**Status:** Implemented on task branch `agent/cursor/accredaone-dev06a-synthetic-fixtures` — **not merged or pushed**.

**Baseline:** canonical `main` @ **`ec3fee7ee3a2e9611f42a604aeb86bcf038c0ddc`**

This increment is **test-only**. No production API, UI, database, or deployment changes.

## Fixture inventory

| Category | Count | Notes |
| --- | ---: | --- |
| Eligible fictional frameworks | 2 | NOAF, MCQF |
| Ineligible fictional framework | 1 | LCF (`disabled`) |
| Source instruments | 2 | One per eligible framework |
| Current requirements | 8 | Four per eligible framework |
| NOAF version records | 3 | historical, current, unverified publication candidate |
| AccredaOne synthetic explanations | 2 | One per eligible framework |
| Query scenarios | 4 | explicit NOAF, explicit MCQF, no selection, ambiguous/unsupported |

Manifest: `tests/AccredaOne.Api.Tests/Fixtures/Synthetic/manifest.json`

All records use `classification: synthetic` and fictional provenance strings. No real accreditation publishers, clause IDs, or source URLs.

## Files added (test project)

| Path | Role |
| --- | --- |
| `Fixtures/Synthetic/*.json` | Deterministic fixture pack |
| `Fixtures/Synthetic/SyntheticFixtureModels.cs` | Test-only models |
| `Fixtures/Synthetic/SyntheticFixtureLoader.cs` | Load + validate |
| `Fixtures/Synthetic/SyntheticFixtureBoundaryVerifier.cs` | Boundary/query resolution (tests) |
| `Fixtures/Synthetic/SyntheticFixtureBoundaryTests.cs` | Automated boundary tests |
| `AccredaOne.Api.Tests.csproj` | Copy JSON fixtures to output |

## Validation evidence

```powershell
dotnet test AOVIAS.AccredaOne/tests/AccredaOne.Api.Tests/AccredaOne.Api.Tests.csproj -c Release
```

Result: **31/31 passed** (includes prior 20 API tests + 11 DEV-06A tests).

`git diff --check`: pass before commit.

CI: No workflow changes; existing AccredaOne quality workflow runs `dotnet test` on backend job — compatible.

## Acceptance criteria mapping

| Criterion | Met |
| --- | --- |
| Two eligible fictional frameworks + disabled exclusion fixture | Yes |
| One source + four current requirements per eligible framework | Yes |
| NOAF historical + unverified candidate versions | Yes |
| Separate explanations + four query scenarios | Yes |
| Synthetic markers and fictional provenance | Yes |
| Deterministic loader validation | Yes |
| Isolation, grouping, provenance, separation, versions, eligibility, query bounds | Yes |
| No production/schema/DB/network/AI changes | Yes |

## Risks and deviations

- ACR-DEV-06A-01 text not present in repository; implementation follows PO task specification.
- Boundary verifier lives in test assembly only (no production abstraction).

## Governance (unchanged)

G1 **NOT PASSED**. Phase 1 **NOT AUTHORIZED**. Live PostgreSQL not activated. PostCSS exception unchanged. DEV-06B+ not started.
