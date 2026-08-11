# Deployment Guide — AOVIAS Command v1.0.0

**Audience:** Platform engineers, hospital IT  
**Version:** 1.0.0

## 1. Packaging

1. Build package: `.\scripts\build-praman-aovpkg.ps1 -Version 1.0.0`
2. SBOM: `.\scripts\release\generate-sbom.ps1`
3. Checksums: `.\scripts\release\generate-checksums.ps1`
4. Publish through AOVIAS Package Builder / Deployment Center as documented in Phase 6.

Docker images follow `aovias/command-{service}:1.0.0` (gateway, identity, organization, compliance, governance, AI orchestrator, etc.).

## 2. Prerequisites

- SQL Server (Express or higher) with catalogs for each service
- .NET runtime matching service TFMs
- RabbitMQ for Gateway messaging infrastructure (domain services remain broker-agnostic via Outbox)
- Optional: Redis, OpenTelemetry collector, Azure Key Vault

## 3. Compose / commercial stack

Use `docker-compose.commercial.yml` (and env files derived from `.env.example`) for multi-service bring-up. Apply EF migrations for Organization (licensing), Compliance, Identity, Governance, AIOrchestrator, and other service catalogs before opening traffic.

## 4. Upgrade path

1. Backup all databases (see Phase 6 backup docs / `scripts/ops` backup guidance).
2. Run `.\scripts\ops\upgrade.ps1` with dry-run first when available.
3. Apply migrations, then roll containers/services to `1.0.0` tags.
4. Smoke: license status, Gateway health, sample journey read, correlation header propagation.

## 5. License activation in deployed environments

- Online activate against Gateway `/api/v1/licenses/activate`
- Offline: signed payload flow from licensing platform docs
- Schedule heartbeat from operators or platform job

## 6. Post-deploy verification

| Check | Expectation |
|-------|-------------|
| Gateway `/health` (or equivalent) | Healthy |
| License status | Active / Grace with known end date |
| Entitlement check | Matches sold SKU |
| Outbox processor | Processing without poison loop |
| Correlation IDs | Present in logs end-to-end |

## 7. Rollback

Restore DB backups taken immediately before upgrade; redeploy previous image tags; re-validate license heartbeat. Do not “fix forward” migrations without a tested reverse plan.
