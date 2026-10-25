# 01 — Enterprise Security

**Phase:** 7.1  
**Sprint:** 1  
**Date:** 2026-07-26

## Delivered

| Capability | Implementation |
|------------|----------------|
| Security headers | `Gateway/Middleware/SecurityHeadersMiddleware.cs` — CSP, XFO, nosniff, Referrer, Permissions-Policy |
| Secrets / Key Vault | `AddAoviasKeyVault` + `ProductionSecretsValidator` fail-fast (non-Dev) |
| JWT key rotation | Dual-key validation via `JwtSettings:PreviousSecretKey` + current `SecretKey` |
| Admin MFA | Default `RequireMfaForAdminRoles=true` on new security policy; existing login gate in `AuthenticationService` |
| Privilege audit export | `GET /api/v1/privilege-audit/export` (AccessGovernance) |
| Encryption at rest | Ops: SQL TDE / disk encryption (documented); app-level `CredentialProtector` unchanged |
| Tenant security | Phase 6 middleware retained; regression via Gateway unit tests |

## JWT rotation runbook

1. Set `JwtSettings:PreviousSecretKey` = current secret.
2. Set `JwtSettings:SecretKey` = new secret (Identity issuer must use new key for new tokens).
3. Deploy Gateway (accepts both keys).
4. After all tokens expire / sessions rotate, clear `PreviousSecretKey`.

## Key Vault

Set `KeyVault:VaultUri` (e.g. `https://{name}.vault.azure.net/`). Optionally `KeyVault:RequiredInProduction=true` to fail boot without vault.

## Exit

Headers on Gateway responses; Production secrets validated; admin MFA enforceable; JWT rotation without full outage.
