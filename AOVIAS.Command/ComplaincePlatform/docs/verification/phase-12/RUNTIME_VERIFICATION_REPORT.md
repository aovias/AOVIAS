# Phase 12 — Runtime Verification Report

## IntegrationService startup

**Command:** `dotnet run --project IntegrationService/IntegrationService.csproj`

| Check | Result | Evidence |
|-------|--------|----------|
| Application starts | ✅ | `APPLICATION_STARTED IntegrationService started` |
| EF migration auto-apply | ✅ | No migration fatal error logged |
| Seed data | ✅ | `IntegrationSeedData.SeedAsync` runs |
| Port binding | ✅ | `https://localhost:7218;http://localhost:5218` |
| ServiceBus consumer | ⚠️ Disabled | Expected — empty connection string |
| Hosted services registered | ✅ | Scheduler, consumer, outbox, webhook monitor |

### Startup warnings

1. `SuccessRate` decimal precision EF warning — non-fatal
2. ServiceBus not configured — expected in local dev

## Dependency injection

| Service | Resolves |
|---------|----------|
| IConnectorRegistry | ✅ 25+ providers |
| ISynchronizationEngine | ✅ |
| IPlatformWriteClient | ✅ (Workforce base URL) |
| IntegrationDbContext | ✅ |

## Health endpoints

- Platform maps via `MapPramanAoviasPlatform()` — `/health`, `/readiness`, `/liveness` expected per AOVIAS catalog
- Health check DB registration: `AddDbContextCheck<IntegrationDbContext>`

## Gateway routing

- Not started in this audit session
- Compile-time proxy handlers verified

## Background workers

| Worker | Runtime behavior |
|--------|------------------|
| SyncSchedulerHostedService | Runs on interval (60s default) |
| IntegrationOutboxProcessor | Marks ProcessedOn every 5s |
| WebhookRetryHostedService | Logs DLQ count every 5 min |
| IntegrationEventConsumerHostedService | Skipped when no ServiceBus |

## OutboxRelayWorker

- Build verified; not started (requires ServiceBus connection string)

## Angular startup

- Not verified — full build fails pre-existing errors

## Runtime exceptions

- None observed during IntegrationService startup window

## Verdict

IntegrationService is **runtime-viable** for local development. Full stack verification (Gateway + AG + Workforce + Service Bus) requires integrated environment configuration.
