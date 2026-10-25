# EIWAM API Catalog (Gateway)

All external APIs route through Gateway unless noted. Responses use `ApiResponse<T>` envelope.

## Integration (`/api/v1/integration`)

| Method | Route | Permission |
|--------|-------|------------|
| GET | `/connectors` | Integration.Connector.Read |
| GET | `/connectors/providers` | Integration.Connector.Read |
| GET | `/connectors/{id}` | Integration.Connector.Read |
| POST | `/connectors` | Integration.Connector.Manage |
| PUT | `/connectors/{id}` | Integration.Connector.Manage |
| POST | `/connectors/{id}/validate` | Integration.Connector.Manage |
| GET | `/synchronizations` | Integration.Sync.Read |
| GET | `/synchronizations/{jobId}/runs` | Integration.Sync.Read |
| POST | `/synchronizations` | Integration.Sync.Execute |
| POST | `/synchronizations/run` | Integration.Sync.Execute |
| GET | `/mappings` | Integration.Mapping.Read |
| POST | `/mappings` | Integration.Mapping.Manage |
| GET | `/webhooks` | Integration.Webhook.Read |
| POST | `/webhooks` | Integration.Webhook.Manage |
| POST | `/webhooks/inbound/{subscriptionId}` | Anonymous (signature) |
| POST | `/imports` | Integration.Import.Execute |
| POST | `/imports/{id}/preview` | Integration.Import.Execute |
| POST | `/imports/{id}/execute` | Integration.Import.Execute |
| POST | `/exports` | Integration.Export.Execute |
| POST | `/exports/{id}/execute` | Integration.Export.Execute |
| GET | `/connector-health` | Integration.Monitor.Read |
| GET | `/audit` | Integration.Audit.Read |

## Enterprise Directory (`/api/v1/directory`)

| Method | Route | Permission |
|--------|-------|------------|
| GET | `/search` | Directory.Search.Read |
| GET | `/profile/{id}` | Directory.Profile.Read |
| GET | `/suggestions` | Directory.Search.Read |
| POST | `/nl-search` | Directory.NLSearch.Use |
| POST | `/rebuild` | Directory.Search.Read |

## Workforce Intelligence (`/api/v1/intelligence`)

| Method | Route | Permission |
|--------|-------|------------|
| GET | `/dashboard` | Intelligence.Dashboard.Read |
| GET | `/forecast` | Intelligence.Forecast.Read |
| GET | `/recommendations` | Intelligence.Recommendations.Read |

## Compliance (selected)

| Method | Route | Permission |
|--------|-------|------------|
| GET | `/journeys` | Compliance.Standard.View |
| POST | `/evidence` | Evidence.Document.Upload |
| POST | `/journeys/{id}/transition` | Role-gated in service |

## Platform health

| Endpoint | Service |
|----------|---------|
| `/health` | All services |
| `/readiness` | All services |
| `/platform/registration` | AOVIAS metadata |

## Versioning

All routes use `api/v{version}` with default v1.0. Gateway and backend version explorers aligned via `AddApiVersioning`.
