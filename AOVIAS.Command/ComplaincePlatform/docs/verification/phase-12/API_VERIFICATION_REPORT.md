# Phase 12 — API Verification Report

## IntegrationService (direct)

| Endpoint | Method | Auth Permission | Status | Notes |
|----------|--------|-----------------|--------|-------|
| `/api/v1/connectors` | GET | Integration.Connector.Read | ✅ | `organizationId` query |
| `/api/v1/connectors/providers` | GET | Integration.Connector.Read | ✅ | |
| `/api/v1/connectors/{id}` | GET | Integration.Connector.Read | ✅ | 404 via ApiResponse |
| `/api/v1/connectors` | POST | Integration.Connector.Manage | ✅ | Validates via provider |
| `/api/v1/connectors/{id}` | PUT | Integration.Connector.Manage | ✅ | |
| `/api/v1/connectors/{id}/validate` | POST | Integration.Connector.Manage | ✅ | |
| `/api/v1/synchronizations` | GET | Integration.Sync.Read | ✅ | |
| `/api/v1/synchronizations/{jobId}/runs` | GET | Integration.Sync.Read | ✅ | |
| `/api/v1/synchronizations` | POST | Integration.Sync.Execute | ✅ | |
| `/api/v1/synchronizations/run` | POST | Integration.Sync.Execute | ✅ | `jobId` query |
| `/api/v1/mappings` | GET | Integration.Mapping.Read | ✅ | |
| `/api/v1/mappings` | POST | Integration.Mapping.Manage | ✅ | No PUT/DELETE |
| `/api/v1/webhooks` | GET | Integration.Webhook.Read | ✅ | |
| `/api/v1/webhooks` | POST | Integration.Webhook.Manage | ✅ | |
| `/api/v1/webhooks/inbound/{id}` | POST | Anonymous | ⚠️ | Raw Ok/Unauthorized, not ApiResponse |
| `/api/v1/imports` | POST | Integration.Import.Execute | ✅ | |
| `/api/v1/imports/{id}/preview` | POST | Integration.Import.Execute | ✅ | |
| `/api/v1/imports/{id}/execute` | POST | Integration.Import.Execute | ✅ | |
| `/api/v1/imports/{id}/rollback` | POST | — | ❌ | **Missing** |
| `/api/v1/exports` | POST | Integration.Export.Execute | ✅ | |
| `/api/v1/exports/{id}/execute` | POST | Integration.Export.Execute | ✅ | |
| `/api/v1/connector-health` | GET | Integration.Monitor.Read | ✅ | |
| `/api/v1/integration-audit` | GET | Integration.Audit.Read | ✅ | |

## Gateway proxy (`/api/v1/integration/*`)

| Gateway route | Proxies to | Status |
|---------------|------------|--------|
| `GET integration/connectors` | IntegrationService | ✅ |
| `POST integration/connectors` | IntegrationService | ✅ |
| `POST integration/synchronizations/run` | IntegrationService | ✅ |
| `GET integration/audit` | `/integration-audit` | ✅ Path mapping correct |
| `POST integration/webhooks/inbound/{id}` | No auth forward | ✅ |

## DTOs & validation

| Area | Status |
|------|--------|
| Request DTOs in `IntegrationContracts.cs` | ✅ Present |
| FluentValidation validators | ❌ Not registered |
| Input validation | ⚠️ Minimal (provider validate on create) |

## API versioning

- ✅ `[ApiVersion("1.0")]` on all controllers
- ✅ `AddIntegrationApiVersioning()` configured

## Swagger / OpenAPI

- ⚠️ `AddEndpointsApiExplorer()` only; no explicit Swagger UI configuration in `Program.cs`

## Error handling

- ✅ Global exception middleware via `UseGlobalExceptionHandling()`
- ✅ `ApiResponseFactory` for authenticated endpoints
- ⚠️ Inbound webhook inconsistent envelope

## Missing APIs (per plan)

1. `POST /imports/{id}/rollback` — RollbackImportCommand
2. Mapping profile update/delete
3. Webhook subscription update/delete
4. Sync job update/disable endpoints

## Broken / deprecated

- None identified in Phase 12 surface area.

## Consistency

- ✅ Aligns with Enterprise Directory proxy pattern
- ✅ Permission naming follows `Integration.{Feature}.{Action}` convention
