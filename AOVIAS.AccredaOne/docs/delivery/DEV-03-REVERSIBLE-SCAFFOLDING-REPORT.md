# DEV-03 — Reversible scaffolding report

Date: 2026-11-02

Branch: `A1-ProjectSetup`

Baseline commits: `447f78a` (DEV-01), `518a3b9` (DEV-02)

## Status

**SCAFFOLDING COMPLETE** (local build and test validation; **subject to** documented PostCSS dependency exception in `docs/security/`)

## Governance boundary

- **G1** NOT PASSED; **Phase 1** NOT AUTHORIZED; **first development slice** NOT APPROVED.
- **D1-01** and **D2** approved in Confluence; **D3** not approved; **ADR-011** formal approval pending.
- No authentication, Central Identity, database, domain entities, agent orchestration, search, or real accreditation content.
- No production architecture or microservice topology implied.

## Structure and rationale

| Path | Purpose |
| --- | --- |
| `application/frontend/` | Minimal Next.js App Router UI placeholder |
| `application/backend/` | Minimal ASP.NET Core Web API with `/health` |
| `tests/AccredaOne.Api.Tests/` | Single health-endpoint integration test |

Single frontend + single backend preserves flexibility while **D3** and service topology remain open.

## Technology and exact versions

### Frontend (`application/frontend`)

| Package | Version (installed) |
| --- | --- |
| next | 15.5.28 |
| react | 19.3.1 |
| react-dom | 19.3.1 |
| typescript | 5.9.3 |
| eslint-config-next | 15.5.28 |

Node: **v24.13.0** (workstation). npm: **11.6.2**.

**Historical note:** First local validation used **Next.js 15.1.0** before PO-approved maintenance upgraded **next** and **eslint-config-next** to **15.5.28** (React **19.3.1** unchanged).

**npm audit:** Production tree (`--omit=dev`) reports **PostCSS 8.4.31** (transitive of **next@15.5.28**) with **one high** and associated **moderate** advisories. Temporary development risk acceptance is recorded separately under `docs/security/`.

### Backend (`application/backend`)

| Component | Version |
| --- | --- |
| Target framework | net10.0 |
| SDK (repo pin) | 10.0.103 |
| NuGet packages | None beyond implicit ASP.NET Core 10 shared framework |

### Tests (`tests/AccredaOne.Api.Tests`)

| Package | Version |
| --- | --- |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.3 |
| xUnit | (template default via SDK) |

## Build and test commands

```powershell
# Repository root
dotnet build application/backend/AccredaOne.Api.csproj
dotnet test tests/AccredaOne.Api.Tests/AccredaOne.Api.Tests.csproj

cd application/frontend
npm run build
npm run dev
```

## Validation results (2026-11-02)

| Step | Result |
| --- | --- |
| `dotnet build` (backend) | **Succeeded** — 0 warnings, 0 errors |
| `dotnet test` (health endpoint) | **Passed** — 1/1 |
| `npm ci` / `npm run build` (frontend) | **Succeeded** — Next.js **15.5.28** production build |
| `npm run lint` (frontend) | **Succeeded** — no ESLint warnings or errors |

Development-only **minimatch / ESLint** advisories may appear in the full `npm audit` tree; they are separate from the accepted production-tree PostCSS findings.

## Decisions deliberately left open

- Final service boundaries and D3 reconciliation
- OpenAPI surface beyond future authorization
- Database and VPS Compose wiring (DEV-02 **PARTIAL**)
- CI/CD (GitHub Actions) and GHCR
- Frontend–backend integration and API contracts
- PostCSS remediation (Next.js 16 or overrides — not applied under current acceptance)

## Safe removal or replacement

To remove DEV-03 scaffolding:

1. Delete `application/frontend/`, `application/backend/`, and `tests/AccredaOne.Api.Tests/`.
2. Restore `application/README.md` to foundation-only wording if desired.
3. Remove DEV-03 sections from `docs/delivery/SETUP.md` if reverting documentation.
4. Run `git status` — ensure `node_modules/`, `bin/`, `obj/`, and `.next/` remain ignored and are not tracked.

No database migrations or VPS state exist for this scaffold.

## Files created or modified (summary)

**Created:** `application/backend/*`, `application/frontend/*` (create-next-app tree), `tests/AccredaOne.Api.Tests/*`, this report.

**Modified:** `application/README.md`, `docs/delivery/SETUP.md` (DEV-03 command section).

Generated artifacts (`node_modules/`, `bin/`, `obj/`, `.next/`) are gitignored and must not be committed.

## Confirmation

No product functionality, production architecture, accreditation source content, or VPS infrastructure changes were implemented.

---

## Addendum — PostCSS temporary risk acceptance (2026-11-02)

**Product Owner decision (2026-11-02):** **Temporary risk acceptance** for residual **PostCSS 8.4.31** advisories (transitive of **next@15.5.28**) during **controlled local development only**, with mandatory restrictions and periodic review by **2026-12-02**.

Full record:

- [../security/DEV-03-POSTCSS-TEMPORARY-RISK-ACCEPTANCE.md](../security/DEV-03-POSTCSS-TEMPORARY-RISK-ACCEPTANCE.md)

This is **risk acceptance, not remediation**; `npm audit` findings remain open. This is **not** production security clearance. **G1** NOT PASSED; **Phase 1** NOT AUTHORIZED; **DEV-02** **PARTIAL**.
