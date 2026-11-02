# DEV-04 — Persistence foundation report

Date: 2026-11-02

**Increment:** PostgreSQL 18 persistence integration foundation (configuration, DI, health design, tests, documentation).

**Governance:** G1 **NOT PASSED**. Phase 1 **NOT AUTHORIZED**. No change to programme gates.

## Scope delivered

- Npgsql EF Core provider **10.0.0** on .NET **10** / EF Core **10.0.0**
- `Persistence` configuration section with validation and environment variable binding
- `AddAccredaOnePersistence` registration boundary and minimal `AccredaOneDbContext`
- Readiness integration on `GET /ready` with `not_configured` / `disabled` / `healthy` / `unhealthy` semantics
- Bounded `PostgresConnectivityHealthCheck` (disabled by default)
- Automated tests (no live PostgreSQL)
- [../architecture/PERSISTENCE-FOUNDATION.md](../architecture/PERSISTENCE-FOUNDATION.md)

## Out of scope (confirmed)

- Database deployment, Docker, VPS, schema, migrations, seed data
- Live connectivity in default configuration
- Auth, product workflows, service decomposition

## Verification

Run from `AOVIAS.AccredaOne`:

```powershell
dotnet restore application/backend/AccredaOne.Api.csproj
dotnet build application/backend/AccredaOne.Api.csproj
dotnet test tests/AccredaOne.Api.Tests/AccredaOne.Api.Tests.csproj

cd application/frontend
npm ci
npm run lint
npm run build
```

Record command output in the integration review before merge to canonical `main`.

## Risks and follow-ups

- npm PostCSS advisory remains under DEV-03 temporary acceptance (review **2026-12-02**)
- Npgsql **10.0.3** / EF **10.0.4+** alignment deferred to avoid unrelated package churn
- Enabling live connectivity requires explicit PO authorization and infrastructure
