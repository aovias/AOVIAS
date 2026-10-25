# Architecture Guide — AOVIAS Command v1.0.0

**Audience:** Solution architects, partner engineers, security reviewers

## High-level shape

AOVIAS Command is a multi-service .NET platform fronted by a **Gateway**. Clients call Gateway HTTP APIs; Gateway proxies to backend services via named `HttpClient`s / `IProxyService`. Domain services do **not** take RabbitMQ dependencies—cross-service messages use **Outbox + domain events**, with Gateway/messaging infrastructure owning the broker.

## Lifecycle SSOT

- Owner: `ComplianceJourney` + `ComplianceLifecycleState`
- Transitions: `ComplianceJourneyService.TransitionJourneyAsync(...)`
- Validity: `ComplianceLifecycleStateMachine` only
- Framework rules: `IFrameworkJourneyPolicy` / factory
- Controllers stay thin; no direct DbContext or status mutation

## Key services (illustrative)

| Service | Responsibility |
|---------|----------------|
| Gateway | Auth edge, MediatR orchestration, proxy, messaging infra |
| Identity / Organization / Person | Users, orgs, hospitals, licensing |
| Compliance | Journeys, evidence, CAPA, audit, scoring events |
| Governance | Documents, risk, incidents, committees |
| AIOrchestrator | Knowledge, assistant, ACE |
| Communication / Workforce / … | Supporting domains |

## Events & projections

- Scoring/snapshots: `ComplianceScoreUpdatedDomainEvent` → `ComplianceSnapshotProjector`
- Lifecycle: `ComplianceStateChangedDomainEvent`
- Aggregates raise domain events via `AggregateRoot`; never call HTTP/Rabbit/EF from domain entities

## Licensing

Organization Service stores license keys, seats, heartbeats, and entitlement JSON. Gateway exposes `/api/v1/licenses/*` and soft-gates with `LicenseEntitlementMiddleware` / `X-Require-Entitlement`.

## Security & ops highlights

- Correlation ID propagation (`X-Correlation-ID`)
- Security headers, optional Key Vault, Redis, OTel, Polly circuit breakers, maintenance mode (Phase 7)
- Standardized `ApiResponse<T>` envelopes via `ApiResponseFactory`

## Related repo docs

- `ARCHITECTURE_DOCUMENTATION.md`
- `docs/commercial-readiness/foundation/*`
- Phase 6/7 commercial-readiness reports
