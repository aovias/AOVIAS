# DEV-05 — Automated checks (proposal)

Date: 2026-11-02

**Status:** Proposal only — **not implemented**. Does not authorize Phase 1, CI deployment, or product work.

## 1. Published baseline

| Item | Value |
| --- | --- |
| Canonical repository | `V:\DevelopmentRepo\AOVIAS` |
| Canonical branch | `main` |
| Published `origin/main` | **`749e1afd385f1f4d5686e8b794bc665a6c1c7c0b`** |
| DEV-04 merge commit | **`749e1af`** (`merge: AccredaOne DEV-04 persistence foundation`) |
| DEV-04 task commit | **`83a44ff`** (ancestor of `main`) |
| AccredaOne path | `AOVIAS.AccredaOne/` |
| SDK pin | .NET **10.0.103** (`AOVIAS.AccredaOne/global.json`) |
| Node line | **24** (`AOVIAS.AccredaOne/.nvmrc`) |

## 2. Existing quality checks (manual / local)

Documented in [SETUP.md](SETUP.md) and delivery reports:

| Area | Commands | Current expectation |
| --- | --- | --- |
| Frontend | `npm ci`, `npm run lint`, `npm run build` | Pass on published baseline |
| Backend | `dotnet restore`, `dotnet build`, `dotnet test tests/AccredaOne.Api.Tests/` | **20/20** tests pass |
| NuGet audit | `dotnet list package --vulnerable` (API project) | No vulnerable packages reported |
| npm audit | `npm audit` (frontend tree) | PostCSS/Next findings under DEV-03 temporary acceptance |
| Git whitespace | `git diff --check` | Pass on AccredaOne changes |

## 3. Existing automation inventory (repository)

| Location | Purpose | AccredaOne scope |
| --- | --- | --- |
| `AOVIAS.Website/.github/workflows/ci.yml` | Website Next.js verify + Docker | **No** — `working-directory: AOVIAS.Website` only |
| `AOVIAS.Command/ComplaincePlatform/.github/workflows/platform-integrity.yml` | Command platform integrity | **No** |
| Root `.github/workflows/` | — | **None** for AccredaOne |
| AccredaOne `application/` | Engineering foundation | **No CI workflow today** |

Do not duplicate Website or Command workflows. Add AccredaOne-specific automation under a scoped workflow path.

## 4. Identified gaps

- No GitHub Actions (or other CI) runs AccredaOne frontend/backend checks on `main` or pull requests.
- No automated enforcement of `global.json` SDK pin in CI.
- No documented CI policy for accepted npm audit findings (PostCSS) vs failing on new highs.
- No automated `git diff --check` on PRs touching AccredaOne.
- TypeScript is validated via `next build` only; no separate `tsc --noEmit` step today.
- No path filter to skip unrelated monorepo areas when only AccredaOne changes (cost control).

## 5. Proposed CI workflow (GitHub Actions)

**File (proposed):** `.github/workflows/accredaone-quality.yml` at repository root (monorepo checkout).

**Triggers:**

- `pull_request` — paths `AOVIAS.AccredaOne/**`, workflow file, root pins used by AccredaOne (`AOVIAS.AccredaOne/global.json`, `.nvmrc` if referenced).
- `push` to `main` — same path filters.

**Concurrency:** `group: accredaone-quality-${{ github.ref }}`, `cancel-in-progress: true`.

**Permissions:** `contents: read` only (no packages, deployments, or secrets for DEV-05).

### Job: `accredaone-verify` (single job initially)

| Step | Action |
| --- | --- |
| Checkout | `actions/checkout@v4` |
| .NET | `actions/setup-dotnet@v4` with `global-json-path: AOVIAS.AccredaOne/global.json` |
| Node | `actions/setup-node@v4`, `node-version-file: AOVIAS.AccredaOne/.nvmrc`, cache `npm` with `cache-dependency-path: AOVIAS.AccredaOne/application/frontend/package-lock.json` |
| Backend | `dotnet restore/build/test` on API + test projects |
| Frontend | `npm ci`, `npm run lint`, `npm run build` in `application/frontend` |
| NuGet audit | `dotnet list package --vulnerable` — **fail** on reported vulnerable packages |
| npm audit | Run `npm audit --omit=dev` (or full audit with documented allowlist file) — **fail on new** unaccepted highs; document PostCSS exception reference |
| Optional | `git diff --check` against merge base for AccredaOne paths on PRs |

**Working directory pattern:** Follow Website workflow: `defaults.run.working-directory` per step group, or explicit `working-directory` on each run step.

**Not in DEV-05:** GHCR builds, VPS deploy, PostgreSQL service containers, Testcontainers, production secrets.

## 6. Test strategy

| Layer | Today | DEV-05 recommendation |
| --- | --- | --- |
| API integration | xUnit + `WebApplicationFactory` (**20** tests) | Run full suite in CI; no live PostgreSQL |
| Persistence disconnected | Configuration, DI, `/ready` tests | Keep mandatory in CI |
| Health/live DB | Not enabled by default | **Defer** live DB tests until connectivity authorized |
| Frontend | Lint + production build | Keep; add explicit job failure output |
| Unit | Minimal | No artificial coverage targets; add unit tests only when logic warrants extraction |

Future (post-authorization): optional job with PostgreSQL service container — **explicitly out of DEV-05**.

## 7. Security and dependency policy

- **NuGet:** Fail CI on `dotnet list package --vulnerable` findings for AccredaOne backend projects.
- **npm:** Report audit output in CI logs. Maintain [../security/DEV-03-POSTCSS-TEMPORARY-RISK-ACCEPTANCE.md](../security/DEV-03-POSTCSS-TEMPORARY-RISK-ACCEPTANCE.md) (review **2026-12-02**). Do not use blanket `npm audit fix --force` or silent `.npmrc` audit suppressions without PO record.
- **Secrets:** No credentials in workflow YAML; continue `.gitignore` patterns for `.env*`.
- **Maintenance:** Track deferred **Npgsql 10.0.3 / EF Core 10.0.4+** alignment separately from DEV-05.

## 8. Workflow triggers and permissions

| Trigger | Scope |
| --- | --- |
| PR → `main` | Path-filtered AccredaOne + workflow |
| Push `main` | Same filters |

`permissions: contents: read` at workflow level. No `id-token`, `packages: write`, or deployment environments.

## 9. Acceptance criteria (for future DEV-05 implementation)

- Workflow runs on AccredaOne changes and passes on published baseline commit.
- Frontend and backend steps match local SETUP commands.
- **20/20** backend tests pass in CI.
- NuGet vulnerability step passes with current packages.
- npm audit step documents known PostCSS acceptance without hiding new critical issues (policy to be finalized with PO).
- No PostgreSQL service, deploy, or product features added.
- Documentation updated in SETUP and a DEV-05 implementation report when complete.

## 10. Risks and tradeoffs

| Risk | Mitigation |
| --- | --- |
| Monorepo path filters miss cross-cutting pin changes | Include `AOVIAS.AccredaOne/global.json` and `.nvmrc` in paths |
| npm audit noise | Document accepted findings; fail on delta vs baseline file (optional follow-up) |
| CI minutes / duplicate Website jobs | Path filters; single combined AccredaOne job |
| SDK pin drift on runners | `setup-dotnet` with `global.json` |
| False confidence | CI validates engineering checks only — not G1 or Phase 1 |

## 11. Explicit exclusions (DEV-05)

- Product features, schema, migrations, live DB connectivity
- Authentication, Central Identity, accreditation content
- Public/production deployment, VPS, Docker image publish
- Changes to Website or Command CI except shared workflow file location at repo root
- ADR approval status changes

## 12. Required future decisions

- Exact npm audit fail vs warn policy for accepted PostCSS finding
- Whether to add root-level workflow vs `AOVIAS.AccredaOne/.github/workflows/` (GitHub only runs default branch workflows from default branch location — prefer **root** `.github/workflows/accredaone-quality.yml`)
- PR required checks configuration in GitHub repo settings (human)
- Optional: `tsc --noEmit` as separate step vs build-only

## 13. Recommended implementation sequence

1. Add path-filtered `accredaone-quality.yml` with backend restore/build/test only (fastest proof).
2. Add frontend `npm ci` / lint / build steps.
3. Add NuGet vulnerable package check.
4. Add npm audit step with documented handling for PostCSS acceptance.
5. Add PR `git diff --check` (optional).
6. Document commands in SETUP; add **DEV-05 implementation report** when done.
7. Human enables branch protection / required checks if desired.

**Governance:** G1 **NOT PASSED**. Phase 1 **NOT AUTHORIZED**. This proposal does not implement DEV-05.
