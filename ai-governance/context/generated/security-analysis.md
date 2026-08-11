# PRAMAN Security & Compliance Analysis Report

**Issue:** PRA-8 — Map Codebase (Security Review)  
**Analyst:** Security Reviewer Persona  
**Date:** 2026-05-07  
**Scope:** Full codebase — ComplaincePlatform (backend services) + WebApp (Angular frontend)  
**Constitution:** PRAMAN-Architecture-Constitution.md v1.0 — Section 5 applied  

---

## Executive Summary

The PRAMAN codebase implements a multi-service healthcare compliance platform with a well-structured gateway, shared RBAC infrastructure, and domain-separated services. However, a cluster of **3 Critical** and **7 High** severity vulnerabilities were identified. Several risks directly violate the PRAMAN Architecture Constitution Section 5.1 (Mandatory Security Rules). The most severe issue is unauthenticated internal API exposure allowing privilege escalation and tenant data manipulation. These issues must be remediated before any production deployment.

**Risk distribution:**
| Severity | Count |
|---|---|
| Critical | 3 |
| High | 7 |
| Medium | 7 |
| Low | 5 |

---

## Section 1 — Architecture Security Assessment

### 1.1 Service Topology

| Service | Auth Mechanism | Internal Exposure | Notes |
|---|---|---|---|
| Gateway | JWT Bearer + RBAC + Scope | None (entry point) | YARP reverse proxy with auth middleware |
| IdentityService | JWT Bearer | `/internal/*` routes — **unauthenticated** | Critical: AllowAnonymous |
| ComplianceService | JWT Bearer + Scope | `/internal/*` routes — InternalOnly API key | Policy present but key may be unset |
| OrganizationService | JWT Bearer | `/internal/*` routes — **unauthenticated** | Critical: AllowAnonymous |
| AIOrchestratorService | JWT Bearer + AiScope | None visible | Tenant header spoofing risk |
| KnowledgeBase | JWT Bearer (assumed) | None found | Partial review |
| AuditService | JWT Bearer (assumed) | None found | Partial review |

### 1.2 RBAC Architecture Assessment

- Canonical roles are well-defined in `Shared.Infrastructure.Auth.ApplicationRoles`.
- `OrganizationAccessHandler` correctly validates `organization_id` JWT claim against route `orgId`.
- `SUPER_ADMIN` correctly bypasses org isolation (by design) — but `SUPER_ADMIN` can be self-assigned via public registration (see VULN-003).
- Scope-based authorization (`ComplianceScope`, `OrganizationScope`, etc.) is implemented in Gateway and ComplianceService.
- **Gap:** Internal service endpoints in IdentityService and OrganizationService bypass all RBAC entirely.

---

## Section 2 — Vulnerability Findings

---

### VULN-001 — `InternalUserController` Unauthenticated Access
**Severity:** CRITICAL  
**Classification:** Auth bypass + Privilege escalation  
**File:** `IdentityService/Controllers/Internal/InternalUserController.cs`

**Evidence:**
```csharp
[ApiController]
[Route("internal")]
[AllowAnonymous] // Internal endpoints - can be secured with API key in production
public class InternalUserController : ControllerBase
```

The controller exposes three unauthenticated endpoints:
- `POST /internal/users/register` — Creates any user with any valid role including `SUPER_ADMIN`
- `DELETE /internal/users/{id}` — Deletes any user by ID
- `POST /internal/token/generate` — Generates a valid signed JWT for any userId, email, organizationId, and hospitalId supplied by the caller

**Impact:** Any network-reachable attacker can:
1. Register an account with `SUPER_ADMIN` role bypassing the normal public registration flow.
2. Generate a forged-but-cryptographically-valid JWT token for any user and any organization.
3. Delete any user from the platform, including administrators.

**Remediation:**
1. Apply `[Authorize(Policy = "InternalOnly")]` or network-level isolation (service mesh mTLS, private subnet) immediately.
2. Implement the same `X-Internal-Api-Key` pattern used by `ComplianceService/InternalJourneysController`.
3. If these endpoints must remain, document the required network topology and add runtime assertion that the caller IP is within the internal service network.
4. At minimum, never expose this service port externally. Add network policy to Docker Compose and Kubernetes to block external access to the `internal/` path prefix.

**Verification:**
```bash
curl -X POST http://<identityservice>:7135/internal/users/register \
  -H "Content-Type: application/json" \
  -d '{"email":"attacker@evil.com","password":"P@ssw0rd!","role":"SUPER_ADMIN"}'
# Expected (currently): 200 OK with userId — MUST return 401/403
```

---

### VULN-002 — `InternalOrganizationController` Unauthenticated Access
**Severity:** CRITICAL  
**Classification:** Auth bypass + Tenant data manipulation  
**File:** `OrganizationService/Controllers/Internal/InternalOrganizationController.cs`

**Evidence:**
```csharp
[ApiController]
[Route("internal")]
[AllowAnonymous] // Internal endpoints - can be secured with API key in production
public class InternalOrganizationController : ControllerBase
```

Exposes unauthenticated endpoints:
- `POST /internal/organizations` — Create organizations
- `DELETE /internal/organizations/{id}` — Delete any organization
- `POST /internal/hospitals` — Create hospitals
- `DELETE /internal/hospitals/{id}` — Delete any hospital
- `POST /internal/organization-members` — Add members to any org with any role
- `PUT /internal/organization-members/{id}` — Update any member
- `GET /internal/users/{userId}/organization-hospital` — Read tenant membership for any user
- `GET /internal/organizations/{orgId}/hospitals` — Enumerate all hospitals for any org

**Impact:** Full read/write access to all tenant data, organization hierarchy, and membership structures without authentication. Combined with VULN-001, an attacker can create a rogue organization, add themselves as admin, and access all healthcare compliance data.

**Remediation:** Same as VULN-001. Apply internal API key authentication. Treat the same network isolation requirement.

---

### VULN-003 — Self-Registration as `SUPER_ADMIN` via Public API
**Severity:** CRITICAL  
**Classification:** Privilege escalation  
**File:** `IdentityService/Controllers/V1/AuthController.cs` lines 114–126

**Evidence:**
```csharp
var roleToAssign = string.IsNullOrWhiteSpace(request.Role)
    ? ApplicationRoles.USER
    : RoleLegacyMap.ToCanonical(request.Role);

if (!ApplicationRoles.IsAssignableRole(roleToAssign))
{
    return BadRequest(...);
}
await _userManager.AddToRoleAsync(user, roleToAssign);
```

`ApplicationRoles.AssignableRoles` includes `SUPER_ADMIN`:
```csharp
public static readonly IReadOnlySet<string> AssignableRoles = new HashSet<string>(StringComparer.Ordinal)
{
    USER, ORG_ADMIN, GROUP_ADMIN, SUPER_ADMIN, REGULATORY_ADMIN, ...
```

Any anonymous caller can POST to `POST /api/v1/auth/register` with `"role": "SUPER_ADMIN"` and receive a full platform super-admin account. `SUPER_ADMIN` bypasses all organization isolation checks.

**Impact:** Immediate platform takeover. SUPER_ADMIN can read/write all organizations and hospitals.

**Remediation:**
1. Remove `SUPER_ADMIN` and `REGULATORY_ADMIN` from `AssignableRoles`. These privileged roles must only be assigned by existing super admins via a protected admin API.
2. Public registration should only assign `USER` or `ORG_ADMIN` (for the first user in an organization onboarding flow).
3. Add an integration test asserting that SUPER_ADMIN cannot be self-assigned.

**Verification:**
```bash
curl -X POST http://<gateway>/api/v1/identity/register \
  -d '{"email":"test@evil.com","password":"Secure1!","role":"SUPER_ADMIN"}'
# Expected (currently): 200 OK — MUST return 400 Bad Request
```

---

### VULN-004 — Hardcoded JWT Secret Key in `appsettings.json`
**Severity:** HIGH  
**Classification:** Secret exposure  
**Files:** `IdentityService/appsettings.json`, `Gateway/appsettings.json`, `ComplianceService/appsettings.json`

**Evidence:**
```json
"JwtSettings": {
  "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLongForHS256Algorithm",
  ...
}
```

The exact same placeholder key is committed in all service `appsettings.json` files. If any deployment environment uses these files without overrides, all JWTs can be forged using the known key.

**Impact:** Complete authentication bypass — any attacker with knowledge of this key can generate valid JWT tokens for any user/role.

**Remediation:**
1. Remove `SecretKey` from all committed `appsettings.json` files entirely. Use `appsettings.Development.json` for development only, and mark it in `.gitignore`.
2. For production, inject via environment variables (`JwtSettings__SecretKey`) or a secrets manager (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault).
3. Rotate the key immediately in any environment where these files were used.
4. Add `StartupConfigurationValidation` guard that throws if the secret key contains "YourSuper" prefix.

---

### VULN-005 — Tenant Isolation Bypass via Untrusted `X-Hospital-Id` Header
**Severity:** HIGH  
**Classification:** Tenant escape  
**File:** `AIOrchestratorService/Controllers/V1/AiController.cs` lines 305–313, 325–331

**Evidence:**
```csharp
private void NormalizeRequest(AIRequest request)
{
    if (request.HospitalId == Guid.Empty &&
        Guid.TryParse(Request.Headers["X-Hospital-Id"], out var hospitalId))
    {
        request.HospitalId = hospitalId;
    }
    ...
}

private string GetTenantKey()
{
    if (Guid.TryParse(Request.Headers["X-Hospital-Id"], out var hospitalId))
    {
        return hospitalId.ToString("N");
    }
    return "unknown-tenant";
}
```

Any authenticated user with a valid `ai.api` scope can supply an arbitrary `X-Hospital-Id` header. The AI controller uses this as the authoritative hospital/tenant identifier for all AI operations, workflow execution, and metrics tracking.

**Impact:** An authenticated user from Organization A can access AI context, start workflows, and trigger autonomous actions against Organization B's hospital data by supplying Organization B's hospitalId in the header.

**Remediation:**
1. Remove header fallback. `HospitalId` must be resolved exclusively from the JWT claim `hospital_id`.
2. Add explicit assertion: `if (request.HospitalId != jwtHospitalId) return Forbid()`.
3. The `GetTenantKey()` method must also use the JWT claim, not the header.

---

### VULN-006 — Exception Message Leakage to Clients
**Severity:** HIGH  
**Classification:** Information disclosure  
**Files:**
- `Gateway/Middleware/ExceptionHandlingMiddleware.cs` line 63
- `ComplianceService/Middleware/ExceptionHandlingMiddleware.cs` lines 67–68
- `IdentityService/Controllers/Internal/InternalUserController.cs` lines 117–120

**Evidence (Gateway middleware):**
```csharp
var errors = new List<ApiError>
{
    new ApiError
    {
        Message = exception.Message  // Leaked unconditionally in all environments
    }
};
```

**Evidence (InternalUserController):**
```csharp
return StatusCode(..., new {
    error = "An unexpected error occurred during user registration.",
    details = ex.Message,           // Leaked
    type = ex.GetType().Name        // Leaked
});
```

**Impact:** Exception messages may contain database connection strings, table names, file paths, internal service URLs, SQL fragments, or other infrastructure-revealing details. This violates Section 5.2 of the PRAMAN Constitution: "MUST NEVER expose internal stack traces in public responses."

**Remediation:**
1. Gateway and ComplianceService middleware: replace `exception.Message` with a generic "An internal error occurred." and only include message detail when `IHostEnvironment.IsDevelopment()`.
2. InternalUserController: remove `details` and `type` fields from error responses entirely.
3. Shared.Infrastructure `GlobalExceptionMiddleware` already implements the `isDevelopment` check correctly — standardize all middleware to this pattern.

---

### VULN-007 — Evidence Upload Missing User Attribution and Content Integrity
**Severity:** HIGH  
**Classification:** Audit gap + Healthcare compliance risk  
**File:** `ComplianceService/Controllers/V1/EvidenceController.cs` lines 90–101

**Evidence:**
```csharp
var evidence = new Evidence
{
    ...
    Hash = string.Empty,            // No content integrity hash
    UploadedBy = string.Empty,      // No user attribution
    ...
};
```

**Impact:** Healthcare compliance frameworks (NABH, NABL) require evidence chain-of-custody. Empty `UploadedBy` means audit logs cannot identify who submitted compliance evidence. Empty `Hash` means tampered evidence cannot be detected. This is a direct PRAMAN Constitution Section 1.2 violation (compliance-centric, traceability).

**Remediation:**
1. `UploadedBy` must be populated from the authenticated user's JWT sub claim.
2. `Hash` must be computed on the file content before storage (SHA-256 minimum).
3. Add a migration to make `UploadedBy` non-nullable.
4. Add an audit log entry for every evidence creation event.

---

### VULN-008 — `FileUrl` Client-Supplied with No Validation (SSRF / Path Traversal Risk)
**Severity:** HIGH  
**Classification:** Input validation gap  
**File:** `ComplianceService/Controllers/V1/EvidenceController.cs` lines 96–97, and `CreateEvidenceRequest`

**Evidence:**
```csharp
public record CreateEvidenceRequest
{
    public string? EvidenceType { get; init; }
    public string? FileUrl { get; init; }   // No validation
    public string? Metadata { get; init; }
}

evidence.FileUrl = request.FileUrl ?? string.Empty;
```

The `FileUrl` field is stored directly from client input without:
- URL scheme whitelist (rejects `file://`, `ftp://`, internal `http://`)
- Hostname allowlist (prevents SSRF via internal service URLs)
- Path traversal detection
- MIME type or extension validation

**Impact:**
- SSRF: attacker stores `http://identityservice:7135/internal/users/register` as a FileUrl; downstream rendering components may trigger internal calls.
- Stored XSS: if `FileUrl` is rendered as an `<a href>` or `<iframe src>` in the frontend, `javascript:` URLs are possible.
- Path traversal: if `FileUrl` is ever used to access local file storage.

**Remediation:**
1. Add `[Required]` and URL format validation to `CreateEvidenceRequest.FileUrl`.
2. Implement allowlist validation: only permit HTTPS URLs from configured storage domains (Azure Blob, S3, or configured local storage prefix).
3. Reject `file://`, `ftp://`, `javascript:`, and internal service hostnames.

---

### VULN-009 — Refresh Token Stored in In-Memory Cache
**Severity:** HIGH  
**Classification:** Token security  
**File:** `IdentityService/Services/RefreshTokenService.cs`

**Evidence:**
```csharp
public class RefreshTokenService : IRefreshTokenService
{
    private readonly IMemoryCache _cache;
    // Scaling: memory cache is per process and is cleared on restart. Multiple IdentityService
    // replicas do not share entries
```

**Impact:**
1. On service restart, all refresh tokens are invalidated — poor availability.
2. In multi-replica deployments, token rotation is not shared. A rotated token from replica A may still be accepted by replica B, enabling replay attacks.
3. Revoked tokens (logout) are not revoked in other replicas.
4. No token family tracking — if a refresh token is stolen and used, the original owner's subsequent use won't detect theft.

**Remediation:**
1. Replace with distributed cache (Redis) or a persistent database table with index on token hash.
2. Implement token family/rotation detection: each use rotates the token; reuse of a consumed token invalidates the entire family.
3. Store token as SHA-256 hash, not plaintext.

---

### VULN-010 — JWT Tokens Stored in `localStorage` (XSS Token Theft)
**Severity:** HIGH  
**Classification:** Token exposure  
**File:** `WebApp/src/app/core/auth/token.service.ts`

**Evidence:**
```typescript
setAccessToken(token: string): void {
    localStorage.setItem(this.ACCESS_TOKEN_KEY, token);
}
setRefreshToken(token: string): void {
    localStorage.setItem(this.REFRESH_TOKEN_KEY, token);
}
```

Both the access token and refresh token are stored in `localStorage`, which is accessible to any JavaScript running on the page (including injected scripts via XSS).

**Impact:** Any XSS vulnerability (now or in the future) allows complete account takeover by extracting both tokens. The refresh token with 7-day expiry is particularly dangerous as it enables persistent access.

**Remediation:**
1. Access token: can remain in memory (application state) for an SPA.
2. Refresh token: should be stored in an `HttpOnly; Secure; SameSite=Strict` cookie, inaccessible to JavaScript.
3. Implement token refresh via a dedicated `/auth/refresh` endpoint that reads the cookie server-side.
4. If localStorage cannot be avoided immediately, implement Content Security Policy (CSP) headers to reduce XSS surface.

---

## Section 3 — Medium Risk Findings

### M-001 — RabbitMQ Default Credentials
**File:** `ComplianceService/appsettings.json`
```json
"UserName": "guest",
"Password": "guest"
```
Default RabbitMQ credentials committed. Any network-reachable attacker can inject or consume compliance events from the message bus.  
**Remediation:** Inject via environment variables or secrets manager. Rotate immediately in any non-development environment.

---

### M-002 — Email Verification Bypassed (`EmailConfirmed = true`)
**Files:** `AuthController.cs` line 87, `InternalUserController.cs` line 66
```csharp
EmailConfirmed = true // For development - in production, require email confirmation
```
Email confirmation is disabled in all environments. Attackers can register accounts with arbitrary email addresses without ownership verification, enabling account enumeration or pre-registration attacks.  
**Remediation:** Implement email confirmation flow for the public `Register` endpoint. Gate login on `EmailConfirmed == true` except for internally-registered accounts.

---

### M-003 — `AllowedHosts: "*"` in All appsettings
**Files:** All `appsettings.json` files  
Setting `AllowedHosts` to `"*"` accepts any `Host` header value, enabling Host header injection attacks that can affect password reset links, logging, CORS decisions, and reverse proxy routing.  
**Remediation:** Set `AllowedHosts` to the explicit DNS names for each environment.

---

### M-004 — Refresh Token Endpoint Has No Rate Limiting
**File:** `IdentityService/Controllers/V1/AuthController.cs` line 268–269
```csharp
[HttpPost("refresh")]
[AllowAnonymous]
// No [EnableRateLimiting("LoginPolicy")]
```
The `/api/v1/auth/refresh` endpoint is not rate-limited. An attacker can brute-force refresh token values at high speed.  
**Remediation:** Apply the same `[EnableRateLimiting("LoginPolicy")]` attribute applied to `/login` and `/register`.

---

### M-005 — `InternalOnly` Policy Fallback Allows All Localhost Access
**File:** `ComplianceService/Infrastructure/Extensions/ServiceCollectionExtensions.cs` lines 134–140
```csharp
if (string.IsNullOrEmpty(expectedKey))
{
    var isLocalhost = request.Host.Host.Equals("localhost", ...) || ...;
    return isLocalhost;
}
```
When `InternalApi:ApiKey` is not configured (the common case in development), any request with `Host: localhost` bypasses the InternalOnly check. If the configuration key is missing from production, the fallback could be exploited.  
**Remediation:** Remove the localhost fallback. If the API key is not configured, always return `false`. Log a startup warning when the key is absent.

---

### M-006 — Weak Refresh Token Generation
**File:** `IdentityService/Services/TokenService.cs` line 84
```csharp
public string GenerateRefreshToken()
{
    return Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + Guid.NewGuid().ToString()));
}
```
`Guid.NewGuid()` uses `System.Random`-derived generation in some environments and produces predictable patterns. The result is base64 of ASCII text, not random bytes.  
**Remediation:** Use `RandomNumberGenerator.GetBytes(32)` for cryptographically secure token generation:
```csharp
var bytes = new byte[32];
RandomNumberGenerator.Fill(bytes);
return Convert.ToBase64String(bytes);
```

---

### M-007 — AI Context Bundle Insufficient Access Scoping
**File:** `ComplianceService/Controllers/Internal/AIContextController.cs`  
The internal AI context bundle endpoint (`POST /api/v{v}/internal/ai/context-bundle`) is accessible to any authenticated user with `ComplianceScope`. This bundles potentially sensitive compliance data (task states, framework details, scoring) for AI consumption. No role restriction (e.g., `MANAGER` or above) is enforced.  
**Remediation:** Add a role requirement (`[Authorize(Roles = ApplicationRoles.Authorization.RegulatoryOverviewReaders)]`) to all internal AI context endpoints.

---

## Section 4 — Low Risk Findings

### L-001 — Login/Lockout Events Not Written to Audit Table
Security-sensitive authentication events (failed login, account lockout, password change) are only logged via `ILogger`. They are not written to the `AuditLog` database table visible to compliance reviewers. PRAMAN Constitution Section 5.1 requires audit logging for security-sensitive actions.  
**Remediation:** Emit audit log records for: login success, login failure (with reason), account lockout, password reset, role change, and token revocation.

---

### L-002 — Hangfire Dashboard Uses Blocking `.GetAwaiter().GetResult()`
**File:** `ComplianceService/Infrastructure/Filters/HangfireAuthorizationFilter.cs` line 28
```csharp
var authorizationResult = authorizationService.AuthorizeAsync(...).GetAwaiter().GetResult();
```
Blocking async calls on ASP.NET Core thread pool threads can cause thread-pool starvation under load.  
**Remediation:** Hangfire dashboard does not support async authorization. Implement a synchronous check using custom policy (checking JWT claims directly) to avoid async blocking.

---

### L-003 — Scaffold Template Controllers Present in Production Services
**Files:** `AuditService/Controllers/WeatherForecastController.cs`, `IdentityService/Controllers/WeatherForecastController.cs`  
Default ASP.NET template `WeatherForecast` controllers are present in production service assemblies, revealing framework version and service structure.  
**Remediation:** Delete these files. Add a CI check that fails if `WeatherForecastController` is found in production source.

---

### L-004 — Temporary Debug File in ComplianceService
**File:** `ComplianceService/TEMP_RegulationEnrollmentTest.cs`  
A file named `TEMP_*` is committed to the ComplianceService source. Temporary test/debug files in production code create ambiguity and may contain unsafe or incomplete code paths.  
**Remediation:** Remove file. Add a pre-commit hook or CI check rejecting files matching `TEMP_*` pattern in production service directories.

---

### L-005 — CORS for IdentityService Hardcoded in Program.cs
**File:** `IdentityService/Program.cs` lines 85–91
```csharp
policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
```
CORS origins are hardcoded rather than config-driven, preventing environment-specific overrides for staging/production.  
**Remediation:** Move to `CorsSettings:AllowedOrigins` configuration (same pattern used by Gateway and ComplianceService).

---

## Section 5 — Compliance Risk Assessment

| Healthcare Compliance Area | Risk | Finding |
|---|---|---|
| Evidence chain-of-custody | HIGH | VULN-007: No UploadedBy, no Hash |
| Audit trail completeness | HIGH | L-001: Security events missing from audit table |
| Data integrity | HIGH | VULN-007: No evidence hash verification |
| Access control audit | CRITICAL | VULN-001/002: Unauthenticated internal endpoints bypass all access logs |
| Multi-tenant isolation | CRITICAL | VULN-003/005: SUPER_ADMIN self-assign, header-based tenant spoofing |
| Secret management | HIGH | VULN-004: Hardcoded JWT key |
| Message bus integrity | MEDIUM | M-001: Default RabbitMQ credentials |

PRAMAN Constitution violations:
- **Section 5.1 (Mandatory)**: VULN-001, VULN-002, VULN-003, VULN-007 — RBAC enforcement absent; audit logging missing; server-side permission checks bypassed
- **Section 5.2 (Forbidden)**: VULN-004 — secrets exposed in committed code; VULN-006 — internal stack details exposed
- **Section 3.1 (CQRS/DTO)**: VULN-008 — DTO input not validated at controller boundary

---

## Section 6 — Security Technical Debt Register

| Item | Location | Debt Type | Priority |
|---|---|---|---|
| Internal endpoints have no auth (AllowAnonymous) | IdentityService, OrganizationService | Architecture | P0 — Block deployment |
| SUPER_ADMIN self-registration | AuthController | RBAC logic | P0 — Block deployment |
| JWT secret in committed config | All services | Secret management | P0 — Rotate + fix |
| Refresh token in-memory | RefreshTokenService | Scalability + Security | P1 |
| localStorage token storage | WebApp TokenService | Frontend security | P1 |
| Exception message leakage | Gateway, ComplianceService middleware | Information disclosure | P1 |
| Evidence UploadedBy empty | EvidenceController | Compliance | P1 |
| FileUrl not validated | EvidenceController | Input validation | P1 |
| X-Hospital-Id header trust | AIOrchestratorService | Tenant isolation | P1 |
| RabbitMQ default credentials | appsettings.json | Secret management | P2 |
| Email confirmation disabled | AuthController | Identity hygiene | P2 |
| Refresh no rate limit | AuthController | Abuse prevention | P2 |
| Weak refresh token generation | TokenService | Crypto | P2 |
| Login failures not in audit table | All services | Compliance | P2 |
| WeatherForecast scaffold controllers | AuditService, IdentityService | Hygiene | P3 |
| TEMP file in production | ComplianceService | Hygiene | P3 |

---

## Section 7 — Prioritized Remediation Recommendations

### P0 — Block Deployment (Must fix before any production release)

1. **Secure or network-isolate `InternalUserController` and `InternalOrganizationController`** — Add `X-Internal-Api-Key` enforcement. Remove `[AllowAnonymous]` immediately.
2. **Remove SUPER_ADMIN from `AssignableRoles`** — Only allow `USER` role from the public register endpoint.
3. **Remove JWT SecretKey from all committed appsettings files** — Inject via environment or secrets manager. Validate at startup.

### P1 — High Priority (Sprint 1 remediation)

4. Fix exception message leakage in Gateway and ComplianceService middleware.
5. Populate `UploadedBy` from JWT claim in `EvidenceController`.
6. Validate `FileUrl` against allowed URL schemes and hostnames.
7. Replace `X-Hospital-Id` header trust in AiController with JWT claim.
8. Migrate refresh tokens to Redis/DB with token rotation detection.
9. Move refresh tokens to `HttpOnly` cookies in the Angular frontend.
10. Compute and store SHA-256 hash on evidence file content.

### P2 — Medium Priority (Sprint 2)

11. Replace RabbitMQ `guest:guest` with secret-managed credentials.
12. Implement email confirmation flow for public registration.
13. Set `AllowedHosts` explicitly in all appsettings for each environment.
14. Add rate limiting to refresh token endpoint.
15. Replace `Guid.NewGuid()` refresh token with `RandomNumberGenerator.GetBytes(32)`.
16. Add security events (login failure, lockout) to the AuditLog table.

### P3 — Hygiene (Sprint 3)

17. Delete WeatherForecast scaffold controllers.
18. Delete `TEMP_RegulationEnrollmentTest.cs`.
19. Move IdentityService CORS config to appsettings.
20. Remove localhost fallback from `InternalOnly` policy.

---

## Section 8 — Verification Checklist

For each P0 item, the following tests must pass before closure:

- [ ] `POST /internal/users/register` returns 401/403 without valid internal API key
- [ ] `POST /internal/token/generate` returns 401/403 without valid internal API key
- [ ] `POST /api/v1/auth/register` with `role: SUPER_ADMIN` returns 400 Bad Request
- [ ] JWT token with known hardcoded key is rejected by all services after rotation
- [ ] `POST /api/ai/v1/ai/read/explain` with spoofed `X-Hospital-Id` header is rejected or uses JWT hospital claim
- [ ] Exception responses in Gateway contain no `exception.Message` detail
- [ ] Evidence record `UploadedBy` is non-empty after creation
- [ ] `FileUrl` with `file://` or `http://internalservice/` is rejected with 400

---

*Report generated by PRAMAN Security Reviewer Persona. This document must be reviewed by the Engineering Lead before remediation work begins. P0 items should be escalated to CTO if not scheduled within current sprint.*
