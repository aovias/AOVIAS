# Troubleshooting Guide — AOVIAS Command v1.0.0

## Quick triage

1. Capture **correlation ID** from response headers or UI error panel.
2. Confirm **license status** (Active / Grace / Expired) and entitlements.
3. Check Gateway and target service **health**.
4. Inspect service logs for the same correlation ID.
5. For writes: verify journey status (evidence locked after Submitted).

## Common issues

### Cannot activate license

- Clock skew between nodes and license server
- Wrong organization ID
- Offline payload signature mismatch
- See Phase 6 licensing APIs and admin guide

### 403 / entitlement denied

- Route requires `X-Require-Entitlement` code not on key
- Seat not assigned to user
- Grace expired → Expired status

### Evidence update rejected

- Journey status ≥ Submitted (expected lock)
- Wrong role for mutation
- Validation failure → `ApiResponseCode.VALIDATION_FAILED`

### Gateway 502 / timeout

- Downstream service down or migration pending
- Circuit breaker open (Polly)—check dependency health
- Connection string / SQL Express instance wrong

### Demo seed fails

- Prerequisites in `DemoDataSeeder/README.md` not met (account/org/hospital GUIDs)
- Migrations not applied; seeder reports unsupported schema
- Always `dry-run` before `seed`

### Outbox backlog

- Outbox processor/relay not running
- Poison message—inspect payload; do not truncate casually in production

## Useful commands

```powershell
# Demo seeder
dotnet run --project .\DemoDataSeeder\DemoDataSeeder.csproj -- dry-run
dotnet run --project .\DemoDataSeeder\DemoDataSeeder.csproj -- verify

# Upgrade dry-run (ops)
.\scripts\ops\upgrade.ps1
```

## When to escalate

Provide: VERSION (`1.0.0`), environment, organization ID, correlation IDs, timestamps (UTC), and whether PHI was involved (never paste PHI into tickets).
