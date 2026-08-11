# PRAMAN Identity Authentication Flows

This document describes the M6.0 identity security flows implemented in `IdentityService`, proxied via `Gateway`, and consumed by the Angular `WebApp`.

## Token Storage (Known Limitation)

Access and refresh tokens are stored in browser `localStorage` (`token.service.ts`). This is acceptable for current development but carries XSS exfiltration risk. Production hardening should evaluate an httpOnly cookie + Gateway BFF pattern.

---

## Login Flow

```mermaid
sequenceDiagram
    participant WebApp
    participant Gateway
    participant Identity
    participant OrgService
    participant SQL

    WebApp->>Gateway: POST /api/v1/identity/login
    Gateway->>Identity: POST /api/v1/auth/login
    Identity->>SQL: Record LoginAttempt
    Identity->>Identity: CheckPasswordSignInAsync + lockout
    alt MFA enabled
        Identity->>SQL: Create MfaChallenge
        Identity-->>WebApp: requiresMfa + challengeToken
    else Password OK
        Identity->>SQL: Create UserSession + refresh token hash
        Identity->>OrgService: GET /internal/users/{id}/auth-context
        Identity-->>WebApp: JWT + refresh token
    end
```

**Account gates:** suspended/disabled/archived/pending verification, forced password change, password max age, admin MFA requirement.

---

## MFA Login Flow

```mermaid
sequenceDiagram
    participant WebApp
    participant Gateway
    participant Identity

    WebApp->>Gateway: POST /identity/login (email + password)
    Gateway->>Identity: login
    Identity-->>WebApp: requiresMfa + mfaChallengeToken
    WebApp->>Gateway: POST /identity/login/mfa
    Gateway->>Identity: login/mfa
    Identity->>Identity: Validate challenge + TOTP/recovery code
    Identity-->>WebApp: JWT + refresh token
```

---

## Refresh Flow

```mermaid
sequenceDiagram
    participant WebApp
    participant Gateway
    participant Identity
    participant SQL

    WebApp->>Gateway: POST /identity/refresh
    Gateway->>Identity: POST /auth/refresh
    Identity->>SQL: Atomic revoke old session + create rotated session
    Identity->>Identity: Issue new JWT (token_version + security_stamp)
    Identity-->>WebApp: new JWT + new refresh token
```

**Security:** Refresh tokens are SHA-256 hashed in SQL. Rotation uses atomic `ExecuteUpdate` to prevent concurrent double-refresh. Reuse of a revoked refresh token revokes the entire token family.

**Gateway JWT validation:** After refresh, API calls validate JWT signature, expiry, `jti` blacklist, `token_version`, and `security_stamp` against live user state.

---

## Logout / Logout All

```mermaid
sequenceDiagram
    participant WebApp
    participant Gateway
    participant Identity
    participant SQL

    WebApp->>Gateway: POST /identity/logout (+ Bearer JWT)
    Gateway->>Identity: logout
    Identity->>SQL: Revoke session + blacklist JWT jti
    Identity-->>WebApp: success

    Note over WebApp,SQL: Logout All
    WebApp->>Gateway: POST /identity/logout-all
    Identity->>SQL: Revoke all sessions
    Identity->>SQL: Increment TokenVersion + SecurityStamp
```

Single logout blacklists the current access token `jti`. Logout-all invalidates all refresh sessions and bumps `token_version` so existing JWTs fail Gateway validation.

---

## Password Reset

```mermaid
sequenceDiagram
    participant WebApp
    participant Identity
    participant Email

    WebApp->>Identity: POST /auth/forgot-password
    Identity->>Identity: Create PasswordResetToken (hashed)
    Identity->>Email: Send reset link
    WebApp->>Identity: POST /auth/reset-password
    Identity->>Identity: Change password + revoke all sessions + bump TokenVersion
```

---

## Session Lifecycle

| State | Meaning |
|-------|---------|
| Active | Valid refresh token; may issue JWTs |
| Revoked | Rotated or explicitly logged out |
| Expired | Past `ExpiresAt` |
| ReuseDetected | Family compromised after refresh reuse |

Concurrent session limit enforced on login; oldest sessions revoked when limit exceeded. Background cleanup marks expired sessions and purges old records.

---

## Internal Service Security

Service-to-service calls to `/internal/*` require header `X-Internal-Api-Key` when `InternalApi:ApiKey` is configured. In development without a key, only localhost callers are permitted.

---

## Incident Recovery

| Scenario | Action |
|----------|--------|
| Compromised refresh token | User logout-all; family reuse detection revokes chain |
| Compromised access token | Short TTL (60 min default); logout blacklists jti |
| Compromised admin account | Suspend via `/internal/users/{id}/suspend`; revokes all sessions |
| Password breach | Force password reset; all sessions invalidated on change |

---

## Deployment Notes

- Configure `JwtSettings:SecretKey` via environment or user secrets (not committed).
- Set `InternalApi:ApiKey` consistently on Gateway, Identity, and Organization services.
- Production Identity signing may use certificate mode (`appsettings.Production.json`).
