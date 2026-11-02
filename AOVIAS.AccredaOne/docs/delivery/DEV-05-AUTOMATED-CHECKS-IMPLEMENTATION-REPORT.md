# DEV-05 — Automated checks implementation report

Date: 2026-11-02

**Status:** Implemented on task branch `agent/cursor/accredaone-dev05-automated-checks` (not merged to `main` unless separately authorized).

**Baseline:** `c667e34d8414dba5699356cb655e0d9d41eb619e`

This increment does not authorize Phase 1, production deployment, or live PostgreSQL connectivity.

## 1. Workflow location and purpose

| Item | Value |
| --- | --- |
| File | `.github/workflows/accredaone-quality.yml` (repository root) |
| Name | AccredaOne quality |
| Purpose | Path-filtered PR/`main` validation and manual runs for AccredaOne engineering checks |

## 2. Triggers

| Trigger | Behavior |
| --- | --- |
| `pull_request` → `main` | Runs when PR changes match path filters |
| `push` → `main` | Runs when push changes match path filters |
| `workflow_dispatch` | Maintainer manual run (full workflow) |

## 3. Path filters

Included paths:

- `AOVIAS.AccredaOne/**` (covers `global.json`, `.nvmrc`, application, tests, scripts, docs)
- `.github/workflows/accredaone-quality.yml`

Excluded by design: `AOVIAS.Website/**`, `AOVIAS.Command/**`, and other monorepo areas unless they also touch AccredaOne paths.

**Note:** GitHub applies path filters at workflow trigger time. AccredaOne-only PRs receive validation; unrelated PRs do not spawn this workflow.

## 4. Jobs

### `accredaone-backend`

- .NET SDK from `AOVIAS.AccredaOne/global.json` (**10.0.103**)
- `dotnet restore/build/test` on `tests/AccredaOne.Api.Tests` in **Release**
- `nuget-audit-gate.mjs` on `application/backend/AccredaOne.Api.csproj`

### `accredaone-frontend`

- Node from `AOVIAS.AccredaOne/.nvmrc` (**24.x**)
- `npm ci`, `npm run lint`, `npm run build`
- npm audit reporting + `npm-audit-gate.mjs` with PostCSS exception policy

## 5. NuGet audit policy

- Command: `dotnet list package --vulnerable --include-transitive --format json`
- Gate: **fail** on **High** or **Critical** vulnerable packages
- Moderate/low: logged for review; does not fail the gate
- Current baseline: **0** vulnerable packages on API project

Remediation: upgrade or pin packages; do not suppress NU1903 without PO/security record.

## 6. npm audit policy

- Policy file: `AOVIAS.AccredaOne/scripts/ci/npm-audit-policy.json`
- Exception document: [../security/DEV-03-POSTCSS-TEMPORARY-RISK-ACCEPTANCE.md](../security/DEV-03-POSTCSS-TEMPORARY-RISK-ACCEPTANCE.md)
- Review date: **2026-12-02**
- Allowed GHSA ids (PostCSS chain only): GHSA-qx2v-qp2m-jg93, GHSA-6g55-p6wh-862q, GHSA-fxqj-rqcc-2cmp, GHSA-r28c-9q8g-f849
- **Fail** on any other **high** or **critical** advisory
- **Fail** when exception is expired and allowed high advisories remain
- **Moderate** findings: reported, visible in logs; do not fail unless elevated to high/critical
- Malformed audit JSON: **fail closed**

The workflow captures audit JSON with a non-fatal npm exit code (npm returns non-zero when vulnerabilities exist). The **gate script** determines pass/fail — no `|| true` on the gate step.

## 7. Permissions and security

```yaml
permissions:
  contents: read
```

- No `pull_request_target`, secrets, deployments, or package write
- Actions pinned to commit SHAs with version comments (checkout, setup-dotnet, setup-node)
- No untrusted PR inputs interpolated into shell beyond GitHub's built-in env vars for paths

## 8. Local commands (match CI)

From repository root:

```powershell
# Backend (Release)
dotnet restore AOVIAS.AccredaOne/tests/AccredaOne.Api.Tests/AccredaOne.Api.Tests.csproj
dotnet build AOVIAS.AccredaOne/tests/AccredaOne.Api.Tests/AccredaOne.Api.Tests.csproj -c Release
dotnet test AOVIAS.AccredaOne/tests/AccredaOne.Api.Tests/AccredaOne.Api.Tests.csproj -c Release --no-build
node AOVIAS.AccredaOne/scripts/ci/nuget-audit-gate.mjs AOVIAS.AccredaOne/application/backend/AccredaOne.Api.csproj

# Frontend
cd AOVIAS.AccredaOne/application/frontend
npm ci
npm run lint
npm run build
npm audit --json > $env:TEMP\accredaone-npm-audit.json
node ../../scripts/ci/npm-audit-gate.mjs $env:TEMP\accredaone-npm-audit.json ../../scripts/ci/npm-audit-policy.json

# Audit gate unit tests
node --test AOVIAS.AccredaOne/scripts/ci/npm-audit-gate.test.mjs
```

## 9. Failure diagnosis

| Failure | Likely cause |
| --- | --- |
| SDK version | Install .NET **10.0.103**; verify `global.json` |
| Node version | Use Node **24.x** |
| Tests | Run `dotnet test` locally with `-c Release` |
| NuGet gate | High/critical advisory on package — upgrade/pin |
| npm gate | New high/critical GHSA or expired PostCSS exception |
| Path filter | Workflow did not run — confirm AccredaOne paths changed |

## 10. Local acceptance results (task branch)

| Check | Result |
| --- | --- |
| `node --test` audit gate fixtures | **8/8** pass |
| NuGet gate (API) | Pass (0 vulnerable) |
| npm gate (current lockfile) | Pass (2 allowed high PostCSS advisories) |
| Release `dotnet test` | **20/20** pass |
| Frontend lint/build | Pass (pre-DEV-05 baseline) |

**GitHub Actions:** Not executed in this task; validate on first PR/`main` push after merge.

## 11. Known limitations

- No branch-protection or required-check configuration (human GitHub setting)
- npm audit devDependency moderates (ESLint/minimatch chain) visible but not gated
- NuGet gate uses JSON severity; moderate/low logged only
- No PostgreSQL service container or integration tests against live DB
- No `git diff --check` job (deferred)

## 12. Deferred capabilities

- GHCR/Docker/VPS deploy pipelines
- npm audit baseline file diff for moderates
- Separate TypeScript `tsc --noEmit` job
- Required status checks and PR templates

## Governance

G1 **NOT PASSED**. Phase 1 **NOT AUTHORIZED**. ADR statuses unchanged.
