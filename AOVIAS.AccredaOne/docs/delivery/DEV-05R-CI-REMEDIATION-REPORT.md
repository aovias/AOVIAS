# DEV-05R — CI remediation report

Date: 2026-11-02

**Status:** Remediation **published** on canonical `main`.

| Item | Commit |
| --- | --- |
| Publication merge commit | **`8a80da1e7403dcaf9d745e438f06b348ff15b5d0`** |
| Remediation implementation commit | **`3e99e69fee5957228c55aff207988c60bd36385f`** |

**Initial failed remote run (historical):** [GitHub Actions run 41443110029](https://github.com/aovias/AOVIAS/actions/runs/41443110029) for merge commit **`070fb5b357d0efb86a3a4d6dc871bc4f4d1f5436`.

## Initial remote CI failure

Both jobs failed at **Set up job** before checkout or build steps executed. No backend restore/build/test or frontend npm steps ran on the runner.

## Backend diagnosis

| Field | Evidence |
| --- | --- |
| Job | `accredaone-backend` |
| First failing step | **Set up job** |
| Error | `Unable to resolve action actions/checkout@11bd71901bbe5b163ea8cb88a7dcfb1e1a2921cd` … `actions/setup-dotnet@67a357865f41c067b1772bd8ea6631c885250225` … `actions/setup-node@39370e3970a52506810543d2bf669be0fad14875` |
| Exit code | Job failure (action resolution) |
| Root cause | **Incorrect commit SHAs** pinned in workflow YAML (SHAs do not exist on GitHub). Secondary: invalid input `global-json-path` (correct input for setup-dotnet v4.1.0 is **`global-json-file`**) — not reached until actions resolve. |
| Fix | Replace SHAs with tag-resolved commits from `actions/*` repositories; set `global-json-file: AOVIAS.AccredaOne/global.json`. |

## Frontend diagnosis

| Field | Evidence |
| --- | --- |
| Job | `accredaone-frontend` |
| First failing step | **Set up job** |
| Error | `Unable to resolve action actions/checkout@11bd71901bbe5b163ea8cb88a7dcfb1e1a2921cd` … `actions/setup-node@39370e3970a52506810543d2bf669be0fad14875` |
| Exit code | Job failure (action resolution) |
| Root cause | Same **invalid action commit SHAs** as backend. |
| Fix | Correct checkout and setup-node SHAs (same tags v4.2.2 / v4.1.0). |

## Corrective changes

File: `.github/workflows/accredaone-quality.yml`

| Action | Tag | Correct commit SHA |
| --- | --- | --- |
| `actions/checkout` | v4.2.2 | `11bd71901bbe5b1630ceea73d27597364c9af683` |
| `actions/setup-dotnet` | v4.1.0 | `3e891b0cb619bf60e2c25674b222b8940e2c1c25` |
| `actions/setup-node` | v4.1.0 | `39370e3970a6d050c480ffad4ff0ed4d3fdee5af` |

Input rename: `global-json-path` → **`global-json-file`** per [setup-dotnet v4.1.0 action.yml](https://github.com/actions/setup-dotnet/blob/v4.1.0/action.yml).

No changes to audit policy, PostCSS exception, application code, or dependencies.

## Local validation evidence

Recorded during DEV-05R on remediation branch (see task final report matrix).

## Remote CI verification (post-publication)

| Field | Value |
| --- | --- |
| Workflow | AccredaOne quality |
| Run ID | **41444633431** |
| Run URL | https://github.com/aovias/AOVIAS/actions/runs/41444633431 |
| Trigger | Push to canonical `main` |
| Commit | **`8a80da1e7403dcaf9d745e438f06b348ff15b5d0`** |
| Backend · .NET build and tests | **SUCCESS** — restore, Release build, API tests, NuGet vulnerability audit gate passed |
| Frontend · lint and build | **SUCCESS** — `npm ci`, lint, production build, npm audit reporting, npm audit gate passed |

## Closure

| Item | Status |
| --- | --- |
| DEV-05R implementation | **COMPLETE** |
| Publication | **COMPLETE** |
| Remote CI verification | **PASS** |
| DEV-05 / DEV-05R programme record | **CLOSED** |

No application code, dependencies, audit policy, PostCSS exception, deployment, or governance scope changed during remediation.

## Governance

G1 **NOT PASSED**. Phase 1 **NOT AUTHORIZED**. PostCSS temporary acceptance unchanged (review **2026-12-02**). Live PostgreSQL connectivity not activated. Production deployment not authorized. DEV-06 not started.
