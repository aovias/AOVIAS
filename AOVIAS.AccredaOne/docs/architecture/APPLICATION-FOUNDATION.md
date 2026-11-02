# Application foundation — DEV-03 increment 1

Date: 2026-11-02

This note describes the engineering foundation inside `application/`. It does not approve Phase 1, pass G1, or authorize production deployment.

## What exists

| Surface | Role |
| --- | --- |
| `application/frontend` | Next.js application shell: header, sidebar, workspace, and loading / empty / error presentation |
| `application/backend` | One ASP.NET Core API process with health, readiness, a versioned foundation route, problem responses, and development OpenAPI |
| `tests/AccredaOne.Api.Tests` | Integration tests for those API routes |

PostgreSQL 18 remains the approved database **direction**. This increment does **not** open a database connection.

## API shape

Unversioned probes:

- `GET /health` — process is up
- `GET /ready` — configuration is present; database check is `not_configured`

Versioned application routes use a path prefix:

- `GET /api/v1/foundation`

`v1` is the only supported version. It is configured as `AccredaOne:ApiVersion`. A different version returns problem details. No versioning package is added until more than one supported version exists.

OpenAPI is mapped only when the host environment is Development (`/openapi/v1.json`). It is not a production surface. `Microsoft.OpenApi` is pinned to **2.7.6** so the development document generator is not left on the vulnerable 2.0.0 transitive version (GHSA-v5pm-xwqc-g5wc). This API emits its own document; it does not parse uploaded OpenAPI files.

Errors use `application/problem+json`.

## Configuration

The `AccredaOne` configuration section supplies the service name, API version, and phase label.

Environment variables follow ASP.NET Core conventions, for example `AccredaOne__ApiVersion`. No connection string is read. Do not commit secrets.

Structured logs use the JSON console formatter configured in `appsettings.json`.

## Frontend

The shell is a local layout for a Hospital Quality Manager / Accreditation Lead. Copy on the page is synthetic and is labeled as such. It does not state accreditation requirements or citations.

## Explicitly not in this increment

- Authentication and Central Identity
- Evidence upload, readiness scoring, accreditation decisions, or cross-framework equivalence
- Payment or subscription flows
- Public deployment or VPS changes
- Real accreditation source content

## PostCSS

The temporary development risk acceptance still applies. See [../security/DEV-03-POSTCSS-TEMPORARY-RISK-ACCEPTANCE.md](../security/DEV-03-POSTCSS-TEMPORARY-RISK-ACCEPTANCE.md). This increment does not upgrade Next.js, add an npm override, or treat the audit finding as closed.

## Decisions left open

- D3 reconciliation and service topology
- When PostgreSQL is actually connected
- How API versions will be retired once a second version exists
- Product navigation beyond this single workspace shell
