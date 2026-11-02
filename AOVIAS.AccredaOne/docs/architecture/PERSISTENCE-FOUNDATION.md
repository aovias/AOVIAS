# Persistence foundation — DEV-04

Date: 2026-11-02

This note documents the PostgreSQL persistence **integration foundation** in `application/backend`. It does not approve Phase 1, pass G1, authorize production deployment, or imply that a database is connected or ready.

Direction aligns with Confluence **ADR-005** (PostgreSQL and .NET persistence strategy) within that record's stated scope. This file is engineering documentation, not a new ADR approval.

## Provider selection

| Package | Version | Role |
| --- | --- | --- |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | **10.0.0** | EF Core provider for PostgreSQL |
| `Microsoft.EntityFrameworkCore` (transitive) | **10.0.0** | ORM aligned with .NET 10 |
| `Npgsql` (transitive) | **10.0.0** | ADO.NET driver |

**Rationale:** Stable **10.0.0** patch set matches the repository's .NET **10.0.103** SDK pin and existing ASP.NET Core **10.0.3** packages without introducing EF Core 11 previews. Npgsql EF Core 10 release notes and CI cover **PostgreSQL 18**; the provider is configured with `SetPostgresVersion(18, 0)` when a connection string is present.

**Not selected:** `Npgsql.EntityFrameworkCore.PostgreSQL` **10.0.3** requires EF Core **≥ 10.0.4**, which would force a broader Microsoft package bump unrelated to this increment. Revisit when the backend baseline moves to EF 10.0.4+.

## Configuration

Section name: **`Persistence`**.

| Property | Default | Purpose |
| --- | --- | --- |
| `ConnectionString` | *(unset)* | PostgreSQL connection string from configuration or environment only |
| `EnableConnectivity` | `false` | Opt-in for live connectivity checks; **must remain false** in this increment |
| `ConnectionTimeoutSeconds` | `15` | Bounded connection open timeout (1–120) |
| `CommandTimeoutSeconds` | `30` | Npgsql command timeout (1–120) |

**Environment variables** (ASP.NET Core `__` convention):

- `Persistence__ConnectionString`
- `Persistence__EnableConnectivity`
- `Persistence__ConnectionTimeoutSeconds`
- `Persistence__CommandTimeoutSeconds`

**Placeholder example only** (do not commit; do not use in production):

```text
Persistence__ConnectionString=Host=localhost;Port=5432;Database=accredaone;Username=app;Password=<from-secret-store>
Persistence__EnableConnectivity=false
```

**Validation (no database required):**

- Missing or empty connection string → valid; runtime status **`not_configured`**
- Malformed connection string → invalid; startup fails via options validation
- `EnableConnectivity=true` without a connection string → invalid
- Timeout values outside 1–120 → invalid
- Validation messages must not echo connection strings or passwords

## Dependency injection boundary

- Registration: `AddAccredaOnePersistence(IConfiguration)` in `Persistence/ServiceCollectionExtensions.cs`
- Options: `PersistenceOptions` + `PersistenceOptionsValidator` (`IValidateOptions`, `ValidateOnStart`)
- Minimal context: `AccredaOneDbContext` (no entities, no migrations)
- `Program.cs` only calls the extension method; it does not reference EF Core types

When a connection string is configured, EF Core registers Npgsql with PostgreSQL **18** as the target version. Resolving `AccredaOneDbContext` does **not** open a connection by itself.

No repositories, unit of work, or domain entities are introduced.

## Health and readiness

| Route | Semantics |
| --- | --- |
| `GET /health` | Process liveness; **unaffected** by database configuration |
| `GET /ready` | Includes `database` check status from `IPersistenceReadinessEvaluator` |

Database readiness statuses:

| Status | Meaning |
| --- | --- |
| `not_configured` | No connection string |
| `disabled` | Connection string present, `EnableConnectivity=false` (default) |
| `healthy` | Only when connectivity is explicitly enabled **and** the bounded health check succeeds |
| `unhealthy` | Connectivity enabled but check failed or timed out |

`PostgresConnectivityHealthCheck` uses ASP.NET Core health checks with a bounded timeout. It does **not** run live verification when connectivity is disabled or not configured. Responses do not include connection strings, credentials, or raw exception details.

**This increment:** default configuration keeps **`EnableConnectivity=false`**. The API must not be described as database-ready.

## Security

- No committed credentials or real connection strings
- Connection strings parsed with `NpgsqlConnectionStringBuilder` for validation only
- Pinned OpenAPI packages from DEV-03 unchanged
- PostCSS temporary acceptance unchanged ([../security/DEV-03-POSTCSS-TEMPORARY-RISK-ACCEPTANCE.md](../security/DEV-03-POSTCSS-TEMPORARY-RISK-ACCEPTANCE.md))

## Explicit limitations

- No PostgreSQL installation, Docker, VPS, or database creation
- No schema, migrations, seed data, or startup initialization
- No live connectivity in default configuration
- No product persistence workflows or accreditation content
- **G1 NOT PASSED**; **Phase 1 NOT AUTHORIZED**

## Future connection enablement

A later authorized increment must:

1. Provide a validated connection string via secure configuration
2. Set `Persistence__EnableConnectivity=true` explicitly
3. Run against an approved PostgreSQL 18 instance
4. Extend readiness and operational docs; still no automatic migrations unless separately approved

## Open decisions

- Exact EF Core / Npgsql patch alignment when moving the backend to Microsoft **10.0.4+**
- D3 service topology impact on where `DbContext` lives
- When and how migrations are introduced
- Compose / local PostgreSQL wiring (deferred)
