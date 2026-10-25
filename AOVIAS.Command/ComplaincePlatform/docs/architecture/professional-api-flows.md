# Professional API flows (M6.3)

Canonical professional credentials live in **ProfessionalService**. Person identity stays in PersonService; employment participation in WorkforceService.

## Create professional profile

```mermaid
sequenceDiagram
    participant Web as WebApp
    participant GW as Gateway
    participant PS as ProfessionalService
    participant WF as WorkforceService

    Web->>GW: POST /api/v1/professional-profiles
    GW->>PS: forward JWT
    PS->>WF: GET /internal/employment/{employmentId}
    PS->>PS: Enforce one profile per PersonId
    PS->>PS: Create PRO number + timeline + outbox
    PS-->>GW: ApiResponse profileId
```

## Grant clinical privilege

```mermaid
sequenceDiagram
    participant Web as WebApp
    participant GW as Gateway
    participant PS as ProfessionalService
    participant OS as OrganizationService

    Web->>GW: POST /api/v1/professional-profiles/{id}/privileges
    GW->>PS: proxy
    PS->>OS: GET /internal/master-departments/{id}
    PS->>PS: PrivilegeGranted outbox + timeline
```

## Renewal dashboard

`GET /api/v1/professional-profiles/renewals?withinDays=90` aggregates expiring registrations, licenses, and certifications.

## Related

- [PROFESSIONAL_DOMAIN_MODEL.md](./PROFESSIONAL_DOMAIN_MODEL.md)
- [WORKFORCE_DOMAIN_MODEL.md](./WORKFORCE_DOMAIN_MODEL.md)
