# AOVIAS demo-data seeder

Isolated .NET 10 console seeder for the existing local AOVIAS databases. It does not create users, change passwords, delete records, or truncate tables. The target account is resolved by email in `Identity`, then matched to its organization membership in `Organization`.

## Prerequisites

- .NET 10 SDK
- SQL Server Express at `localhost\SQLEXPRESS`
- Current service migrations already applied
- Existing account `ekta.gupta@gmail.com`
- Existing organization `2759FAD4-C895-4A5F-9E49-6DED5D378098`
- Existing hospital `D9F5554A-BF83-4262-B386-A31587B0BA06`

Windows integrated authentication is used by default. No database or application password is stored by this project.

## Commands

From `ComplaincePlatform`:

```powershell
dotnet run --project .\DemoDataSeeder\DemoDataSeeder.csproj -- dry-run
dotnet run --project .\DemoDataSeeder\DemoDataSeeder.csproj -- seed
dotnet run --project .\DemoDataSeeder\DemoDataSeeder.csproj -- verify
```

Always run `dry-run` first. `seed` is idempotent: records use deterministic GUIDs or stable natural keys, and only those records are inserted or refreshed. Unrelated records are preserved.

## Environment variables

- `AOVIAS_SQL_SERVER` — default `localhost\SQLEXPRESS`
- `AOVIAS_SEED_EMAIL` — default `ekta.gupta@gmail.com`
- `AOVIAS_IDENTITY_CONNECTION`
- `AOVIAS_ORGANIZATION_CONNECTION`
- `AOVIAS_PERSON_CONNECTION`
- `AOVIAS_WORKFORCE_CONNECTION`
- `AOVIAS_PROFESSIONAL_CONNECTION`
- `AOVIAS_GOVERNANCE_CONNECTION`
- `AOVIAS_COMPLIANCE_CONNECTION`

The per-database variables override the generated integrated-auth connection strings. If credentials are ever needed, inject the complete connection string through the corresponding environment variable; do not put it in source control.

Each run writes `demo-seed-manifest.json` beside the built executable with resolved context, counts, unsupported schema features, and errors.

## Safety and schema behavior

The seeder inspects `sys.tables` and `sys.columns` before writes and excludes absent, identity, computed, and rowversion columns. Optional aggregate families are reported as unsupported if their table or required schema is unavailable. Core identity, organization, people, workforce, and journey prerequisites fail fast. Databases are committed independently because the services use separate catalogs.
