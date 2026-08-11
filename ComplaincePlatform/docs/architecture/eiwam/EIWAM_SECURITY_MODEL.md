# EIWAM Security Model

## Authentication

- **JWT (HS256)** issued by IdentityService; validated at Gateway and each microservice.
- **Internal API key** (`X-Internal-Api-Key`) for service-to-service calls (Integration → Person/Workforce).
- Correlation ID propagated on all requests (`X-Correlation-ID`).

## Authorization (Access Governance)

- Permissions seeded per module: `Integration.*`, `Directory.*`, `Intelligence.*`, `AI.*`, etc.
- Controllers use `[RequirePermission("...")]` attribute.
- `IntegrationAuthorizationService` evaluates AG for connector/sync operations.
- Field-level masks on directory contact/credential fields.

## Integration credentials

- `ConnectorCredential.EncryptedPayload` stored with **AES-GCM** (`CredentialProtector`).
- Encryption key from `Integration:CredentialEncryptionKey` configuration (Key Vault in production).
- Secrets never returned in API responses.

## Webhook security

| Direction | Mechanism |
|-----------|-----------|
| Inbound | HMAC-SHA256 (`X-Praman-Signature`), subscription secret hash stored |
| Outbound | Signed payload, retry with exponential backoff, DLQ |

## Audit

- `IntegrationAuditLog` — immutable per-operation record with traceId, duration, record counts.
- Compliance and Audit services maintain separate evidence trails.
- AI operations logged in AIOrchestrator audit tables.

## Data boundaries

- Integration cannot bypass journey locking rules — writes go to Person/Workforce APIs which enforce their own rules.
- Gateway never exposes EF entities; DTOs only.
- No PII in integration outbox payloads beyond IDs.

## Threat mitigations

| Threat | Control |
|--------|---------|
| Stolen JWT | Short expiry, AG re-evaluation per request |
| Webhook replay | Signature verification + idempotency keys (future) |
| Credential leak | Encryption at rest, no secret in logs |
| Privilege escalation | AG central evaluation, no controller-side role checks |

## Compliance alignment

- HIPAA-aligned audit logging with correlation IDs
- Role-based operator access for integration management
- Separation of duties: `Integration.Connector.Manage` vs `Integration.Sync.Execute`
