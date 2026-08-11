# Enterprise Security API Flows (EIWAM Phase 7)

IdentityService remains the single owner of authentication, sessions, devices, policies, and security telemetry. Gateway exposes `/api/v1/security/*` as a transparent proxy.

## Adaptive login

```mermaid
sequenceDiagram
    participant UI as WebApp
    participant GW as Gateway
    participant ID as IdentityService
    participant Risk as RiskScoringService
    participant Adapt as AdaptiveAuthenticationService

    UI->>GW: POST /auth/login
    GW->>ID: POST /api/v1/auth/login
    ID->>ID: Validate credentials
    ID->>Adapt: EvaluateLoginAsync(context)
    Adapt->>Risk: CalculateAndPersistAsync(userId)
    Risk-->>Adapt: UserRiskScoreDto
    alt Critical risk
        Adapt-->>ID: Block
        ID-->>UI: 403 Forbidden
    else High risk or unknown device
        Adapt-->>ID: StepUpMfa
        ID-->>UI: RequiresMfa challenge
    else Allow
        ID->>ID: IssueLoginTokensAsync
        ID->>ID: ThreatDetection + SecurityEvent
        ID-->>UI: Tokens
    end
```

## Threat detection on login

```mermaid
sequenceDiagram
    participant Auth as AuthenticationService
    participant Threat as ThreatDetectionService
    participant Events as SecurityEventService
    participant DB as IdentityDbContext

    Auth->>Threat: EvaluateLoginAsync(userId, ip, geo, sessionId)
    Threat->>DB: Load recent sessions / prior login
    alt Concurrent distant IPs
        Threat->>Events: ConcurrentLogin event
        Threat->>DB: Mark session suspicious
    else Impossible travel heuristic
        Threat->>Events: ImpossibleTravel + ThreatDetected outbox
        Threat->>DB: Create SecurityIncident
    end
    Threat->>DB: Recalculate risk score
```

## Device revoke

```mermaid
sequenceDiagram
    participant UI as WebApp
    participant GW as Gateway
    participant ID as SecurityController
    participant Dev as TrustedDeviceService

    UI->>GW: POST /security/device/revoke
    GW->>ID: POST /api/v1/security/device/revoke
    ID->>Dev: RevokeAsync(userId, deviceRecordId)
    Dev->>Dev: SecurityEvent + DeviceRevoked outbox
    Dev-->>UI: Success
```

## Refresh token reuse

```mermaid
sequenceDiagram
    participant Client
    participant Session as SessionService
    participant Threat as ThreatDetectionService

    Client->>Session: RotateRefreshTokenAsync(reused token)
    Session->>Session: Revoke token family
    Session->>Threat: EvaluateRefreshReuseAsync
    Threat->>Threat: SessionCompromised + AccountCompromised outbox
    Threat->>Threat: SecurityIncident Open
    Session-->>Client: 401 / invalid refresh
```

## Boundary note

- **Identity security** (this document): authentication, sessions, device trust, risk, threats, security events.
- **Access governance** (see `governance-api-flows.md`): roles, permissions, delegations, authorization evaluation.

Both are consumed via Gateway; domain services do not call each other directly for user lifecycle data.
