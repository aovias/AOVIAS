# AOVIAS Demo Runbook

**Version:** 1.0.0  
**Audience:** Sales engineers, CS, partner demos  
**Rule:** Demo on staging/local only—never seed production with demo data.

## 1. Goals

Deliver a 45–60 minute guided tour covering QM day, audit prep, CAPA, AI assistant, risk register, and management review using consistent demo data.

## 2. Prerequisites

- .NET 10 SDK  
- SQL Server Express `localhost\SQLEXPRESS` (or env overrides)  
- Service migrations applied  
- Existing demo account / org / hospital as documented in `DemoDataSeeder/README.md`:
  - Email default: `ekta.gupta@gmail.com` (override with `AOVIAS_SEED_EMAIL`)
  - Organization `2759FAD4-C895-4A5F-9E49-6DED5D378098`
  - Hospital `D9F5554A-BF83-4262-B386-A31587B0BA06`
- Gateway + required services running  
- License entitlements matching the story (Enterprise recommended for full tour)

## 3. Seed data steps

### 3.1 Platform / master seed (if used in your environment)

If your environment uses `seed-data/` scripts or master-data packages for frameworks, apply those **before** the demo seeder so journeys have standards to attach to. Confirm with engineering which seed-data packs are current for the build.

### 3.2 DemoDataSeeder

From repo root `ComplaincePlatform`:

```powershell
# Always dry-run first
dotnet run --project .\DemoDataSeeder\DemoDataSeeder.csproj -- dry-run

# Idempotent seed
dotnet run --project .\DemoDataSeeder\DemoDataSeeder.csproj -- seed

# Verify
dotnet run --project .\DemoDataSeeder\DemoDataSeeder.csproj -- verify
```

Notes from seeder README:

- Does **not** create users, change passwords, delete, or truncate  
- Uses Windows integrated auth by default  
- Writes `demo-seed-manifest.json` beside the build output  
- Optional connection overrides: `AOVIAS_*_CONNECTION`, `AOVIAS_SQL_SERVER`

### 3.3 Sanity before guests join

- [ ] Sign in as demo QM account  
- [ ] License status Active; entitlements include story modules  
- [ ] At least one journey with evidence  
- [ ] One open CAPA / deficiency path  
- [ ] Risk register entry visible (if `governance.risk`)  
- [ ] AI panel responds (if `ai.assist`)  

## 4. Demo flow (recommended order)

| # | Minutes | Scenario file | Outcome |
|---|---------|---------------|---------|
| 1 | 10 | [scenarios/qm-day.md](./scenarios/qm-day.md) | Role home credibility |
| 2 | 8 | [scenarios/audit-prep.md](./scenarios/audit-prep.md) | Auditor package story |
| 3 | 8 | [scenarios/capa.md](./scenarios/capa.md) | Deficiency → CAPA |
| 4 | 8 | [scenarios/ai-assistant.md](./scenarios/ai-assistant.md) | Advisory AI |
| 5 | 8 | [scenarios/risk-register.md](./scenarios/risk-register.md) | Governance |
| 6 | 8 | [scenarios/management-review.md](./scenarios/management-review.md) | Executive close |

Leave 5–10 minutes for Q&A / pricing redirect to sales@aovias.example.

## 5. Talking discipline

- Do not invent customer logos or fake SOC2 certificates  
- Say “advisory” every time AI appears  
- If a click fails, show troubleshooting honesty—switch to backup scenario  
- Never open production databases in a sales call  

## 6. Reset

Re-run `seed` (idempotent) or restore staging snapshot. Do not manually delete unrelated tenant data.

## 7. Handoff

After demo: send pricing one-pager, published docs link, and proposal template draft with discovered accreditation date.
