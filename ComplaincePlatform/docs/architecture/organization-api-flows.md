# Organization API Flows (EIWAM Phase 4)

| Field | Value |
|-------|-------|
| **Status** | Implemented — Organization & Membership Evolution |
| **Service** | `OrganizationService` |
| **Gateway** | `ProxyToOrganizationServiceAsync` |

## Platform position

```
Identity → Person → Workforce → Professional → AccessGovernance → Governance
                    ↑ Employment hospital/dept assignments (Workforce owns)
OrganizationService → membership + org structure (canonical)
```

---

## 1. Signup / onboarding membership

```mermaid
sequenceDiagram
    participant GW as Gateway
    participant OS as OrganizationService
    participant ID as IdentityService

    GW->>OS: POST /internal/organizations
    GW->>OS: POST /internal/hospitals
    GW->>ID: register user
    GW->>OS: POST /internal/organization-members
    Note over OS: Legacy path sets Active, IsPrimary, EffectiveFrom defaults
```

---

## 2. Enterprise membership create

```mermaid
sequenceDiagram
    participant UI as WebApp
    participant GW as Gateway
    participant OS as OrganizationService

    UI->>GW: POST /api/v1/membership
    GW->>OS: POST /api/v1/membership
    OS->>OS: Validate primary uniqueness, effective dates
    OS->>OS: MembershipHistory + Outbox + Timeline
```

---

## 3. Invitation accept

```mermaid
sequenceDiagram
    participant UI as WebApp
    participant OS as OrganizationService

    UI->>OS: POST /api/v1/invitations
    UI->>OS: POST /api/v1/invitations/{id}/accept
    OS->>OS: Mark invitation Accepted
    OS->>OS: Create MembershipCreated
```

---

## 4. Organization tree

`GET /api/v1/organizations/tree?organizationId=` returns Organization → Hospitals → OrganizationalUnits.

---

## 5. Auth context (JWT)

`GET /internal/users/{userId}/auth-context?organizationId=` resolves **primary active** membership (not first CreatedAt).

---

## Workforce boundary

Department/hospital **assignments** for employed staff: query WorkforceService `/api/v1/employment` — not duplicated in OrganizationService.

See [`WORKFORCE_DOMAIN_MODEL.md`](./WORKFORCE_DOMAIN_MODEL.md).
