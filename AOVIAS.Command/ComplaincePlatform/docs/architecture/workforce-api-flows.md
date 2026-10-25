# Workforce API flows (M6.2)

Canonical employment lifecycle lives in **WorkforceService**. **PersonService** holds identity; **OrganizationService** holds org topology references.

## Create employment

```mermaid
sequenceDiagram
    participant Web as WebApp
    participant GW as Gateway
    participant WF as WorkforceService
    participant PS as PersonService
    participant OS as OrganizationService

    Web->>GW: POST /api/v1/employment
    GW->>WF: forward JWT
    WF->>PS: GET /internal/persons/{personId}
    WF->>OS: GET /internal/hospitals/{hospitalId}
    WF->>WF: Create Employment EMP number + timeline + outbox
    WF-->>GW: ApiResponse employmentId
```

## Transfer / promote

```mermaid
sequenceDiagram
    participant Web as WebApp
    participant GW as Gateway
    participant WF as WorkforceService

    Web->>GW: POST /api/v1/employment/{id}/transfer
    GW->>WF: proxy
    WF->>WF: TransferRecord + assignments + history + EmploymentTransferred outbox
```

## Org chart

`GET /api/v1/employment/org-chart?organizationId=&hospitalId=` builds reporting tree from `ReportingRelationships`.

## Related

- [WORKFORCE_DOMAIN_MODEL.md](./WORKFORCE_DOMAIN_MODEL.md)
- [PERSON_DOMAIN_MODEL.md](./PERSON_DOMAIN_MODEL.md)
