# Phase 12 — Security Verification Report

## Authentication

| Control | Status | Evidence |
|---------|--------|----------|
| JWT Bearer | ✅ | `AddIntegrationAuthentication` |
| Internal API key handler | ✅ | `InternalApiKeyForwardingHandler` on HttpClient |
| Anonymous inbound webhook | ✅ | `[AllowAnonymous]` on inbound route only |

## Authorization

| Control | Status | Evidence |
|---------|--------|----------|
| Integration.* permissions (12) | ✅ | Seeded in AG MasterDataSeeder |
| `[RequirePermission]` on controllers | ✅ | All secured endpoints |
| Gateway permission mirror | ✅ | `IntegrationController` |
| IntegrationAuthorizationService | ⚠️ | Registered but unused in application layer |
| Organization scoping in AG eval | ⚠️ | Service exists; not invoked per-request |

## Credentials & secrets

| Control | Status | Evidence |
|---------|--------|----------|
| AES-GCM encryption | ✅ | `CredentialProtector` |
| Secrets not in API responses | ✅ | DTOs exclude EncryptedPayload |
| Dev encryption key in appsettings | ⚠️ | Must use Key Vault in production |
| Webhook secret stored as hash | ✅ | SHA256 hash, HMAC for verify |

## Webhook security

| Control | Status | Gap |
|---------|--------|-----|
| HMAC-SHA256 inbound | ✅ | |
| Timestamp tolerance | ❌ | Plan requirement |
| Idempotency key | ❌ | Plan requirement |
| Outbound signed payload | ✅ | `X-Praman-Signature` header |

## PII & audit

| Control | Status |
|---------|--------|
| Integration audit log | ✅ Immutable-style append |
| TraceId on sync runs | ✅ |
| PII in outbox payloads | ⚠️ Minimal IDs only — acceptable |

## Validation

| Control | Status |
|---------|--------|
| Connector config validation | ✅ Via provider |
| Request DTO validation | ⚠️ No FluentValidation |
| SQL injection | ✅ EF parameterized |

## Security headers

- Delegated to shared `UsePramanObservability` / platform middleware — not Integration-specific.

## Issues

1. **PlatformWriteClient** swallows HTTP errors — sync may report success when downstream failed.
2. **Inbound webhook** returns non-ApiResponse — inconsistent error contract.
3. **Dev secrets** in `appsettings.json` — deployment must override.

## Verdict

Core authZ model is **correct and aligned with platform**. Webhook hardening and production secret management remain **open items**.
