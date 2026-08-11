# Access Governance API Flows — EIWAM Phase 5

## End-to-end authorization (login → evaluate → API enforce)

```mermaid
sequenceDiagram
    participant User as User
    participant UI as Angular_WebApp
    participant Id as IdentityService
    participant GW as Gateway
    participant CS as ComplianceService
    participant SDK as Shared.Authorization
    participant AG as AccessGovernanceService

    User->>UI: Login
    UI->>Id: Authenticate
    Id-->>UI: JWT with context claims only
    User->>UI: Action requiring permission
    UI->>GW: API request + Bearer token
    GW->>CS: Proxy request + JWT
    CS->>SDK: PermissionAuthorizationHandler
    SDK->>AG: POST /internal/authorization/evaluate
    AG->>AG: Deny policies, delegations, RBAC, RLS, permit policies
    AG-->>SDK: AuthorizationDecision + trace
    alt Allowed
        SDK-->>CS: Succeed requirement
        CS-->>GW: 200 ApiResponse
    else Denied
        SDK-->>CS: 403 Forbidden
    end
    GW-->>UI: Response
```

## Evaluate authorization (direct)

```mermaid
sequenceDiagram
    participant UI as WebApp_or_Service
    participant GW as Gateway
    participant AG as AccessGovernanceService
    participant WF as WorkforceService
    participant PRO as ProfessionalService
    participant ORG as OrganizationService

    UI->>GW: POST /api/v1/authorization/evaluate
    GW->>AG: Proxy evaluate
    AG->>WF: GET /internal/employment/{id} optional
    AG->>PRO: GET /internal/professional/{id} optional
    AG->>ORG: GET /internal/users/{userId}/auth-context optional
    AG->>AG: Evaluation pipeline + AccessEvaluation audit
    AG-->>GW: AuthorizationDecision with evaluationTrace
    GW-->>UI: ApiResponse
```

## Assign role (Phase 5 dedicated route)

1. `POST /api/v1/role-assignments` — assign role with scope and effective dates
2. `GET /api/v1/role-assignments?userId=` — list assignments
3. `POST /api/v1/role-assignments/{id}/revoke` — revoke assignment
4. Outbox event: `RoleAssigned` / `RoleRevoked`

Legacy route remains: `POST /api/v1/roles/users/{userId}/assign/{roleId}`

## Policy designer

1. `POST /api/v1/policies` — create draft with rules and conditions
2. `GET /api/v1/policies/{id}` — load for editing
3. `PUT /api/v1/policies/{id}` — update draft
4. `POST /api/v1/policies/{id}/publish` — publish (immutable)
5. `POST /api/v1/policies/{id}/archive` — archive

Policy operators (lookup `PolicyOperator`): Equals, NotEquals, In, Contains, GreaterThan, Exists

## Module consumption (ComplianceService pilot)

Register in service `Program.cs`:

```csharp
builder.Services.AddTransient<InternalApiKeyForwardingHandler>();
builder.Services.AddPramanAuthorization(builder.Configuration);
```

Apply on controller action:

```csharp
[RequirePermission(CompliancePermissionCodes.EvidenceVerify)]
```

See [module-authorization-adoption-playbook.md](./module-authorization-adoption-playbook.md)

## Internal SDK entry

Services use `Shared.Authorization.IAuthorizationEvaluator` → `POST /internal/authorization/evaluate`

## JWT context claims (Identity)

- `person_id`, `employment_id`, `professional_profile_id`
- `organization_id`, `hospital_id`, `active_department_id`

Permissions are **not** embedded in JWT.
