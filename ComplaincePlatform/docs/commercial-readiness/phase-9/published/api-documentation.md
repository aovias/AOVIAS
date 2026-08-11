# API Documentation Index — AOVIAS Command v1.0.0

**Audience:** Integrators, partner developers, hospital IT

AOVIAS does not publish a single static OpenAPI dump in this commercial pack. Discover live contracts from **Gateway Swagger/OpenAPI** in each environment, and follow the standardized response envelope.

## Entry point: Gateway

All external clients should call **Gateway** only. Backend service ports are internal.

Typical local patterns:

| Item | Pattern |
|------|---------|
| Base URL | `https://{gateway-host}/` or `http://localhost:{gateway-port}/` |
| API prefix | `/api/v1/...` |
| Swagger UI | `/swagger` (when enabled for the environment) |
| OpenAPI JSON | `/swagger/v1/swagger.json` (ASP.NET default; confirm per deploy) |

Enable Swagger only on non-production or behind operator auth per hospital policy.

## Response envelope

Success and failure bodies use `ApiResponse<T>` from Shared.Infrastructure:

- Factory: `ApiResponseFactory.Success` / `Failure`
- Codes: `ApiResponseCode` + `ApiResponseCodes.Registry` (HTTP status mapping)
- Controllers return `ActionResult<ApiResponse<T>>` unless raw proxying

Do not invent ad-hoc `{ error: ... }` shapes in new clients.

## Cross-cutting headers

| Header | Purpose |
|--------|---------|
| `Authorization` | Bearer token from Identity |
| `X-Correlation-ID` / `X-Correlation-Id` | End-to-end tracing (propagate always) |
| `X-Require-Entitlement` | Soft gate feature code (when used by route) |

## Capability map (Gateway → domains)

| Area | Example Gateway routes | Notes |
|------|------------------------|-------|
| Licensing | `/api/v1/licenses/*` | Issue, activate, heartbeat, seats, entitlements |
| Onboarding | Onboarding controller surfaces | `ApiResponse` + HOSPITAL_ONBOARDED codes |
| Compliance | Proxied compliance APIs | Journeys, evidence, CAPA—lifecycle via services |
| Governance | Proxied governance APIs | Risk, documents, incidents (entitlement-gated) |
| AI | Proxied AI orchestrator APIs | Requires `ai.assist` / `ace.insights` |

Exact route lists evolve; use Swagger + Postman collection `CompliancePlatform.postman_collection.json` as working references.

## Client guidelines

- Prefer the Angular interceptor guideline (`ANGULAR_API_INTERCEPTOR_GUIDELINE.md`) for frontend auth/correlation.
- Follow `API_COMMUNICATION_GUIDELINES.md` for Gateway communication.
- Never call domain DbContexts or RabbitMQ from external clients.

## Related published guides

- [architecture-guide.md](./architecture-guide.md)
- [administrator-guide.md](./administrator-guide.md)
- [deployment-guide.md](./deployment-guide.md)
