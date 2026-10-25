# Person API flows (M6.1)

Canonical human identity lives in **PersonService**. **IdentityService** holds authentication (`AppUser`) linked via `AppUser.PersonId`. The Gateway proxies public Person APIs; Identity calls internal Person endpoints during registration.

## Create person (admin)

```mermaid
sequenceDiagram
    participant Web as WebApp
    participant GW as Gateway
    participant PS as PersonService

    Web->>GW: POST /api/v1/persons
    GW->>PS: POST /api/v1/persons (JWT forwarded)
    PS->>PS: Duplicate email check, timeline, outbox
    PS-->>GW: ApiResponse personId + platformId
    GW-->>Web: ApiResponse
```

## Search directory

```mermaid
sequenceDiagram
    participant Web as WebApp
    participant GW as Gateway
    participant PS as PersonService

    Web->>GW: GET /api/v1/persons/search?q=
    GW->>PS: GET /api/v1/persons/search?q=
    PS-->>GW: Paginated PersonListItemDto
    GW-->>Web: ApiResponse
```

## Registration link (new user)

```mermaid
sequenceDiagram
    participant GW as Gateway
    participant ID as IdentityService
    participant PS as PersonService

    GW->>ID: POST register
    ID->>PS: POST /internal/persons (X-Internal-Api-Key)
    PS-->>ID: personId + platformId
    ID->>ID: Create AppUser with PersonId
    ID-->>GW: userId + personId
    Note over ID: JWT includes person_id claim when set
```

## Backfill (existing users)

On IdentityService startup, `PersonIdBackfillHostedService` creates minimal Person records for `AppUser` rows where `PersonId` is null (best effort, batch of 100).

## Internal vs public

| Surface | Auth | Routes |
|---------|------|--------|
| Public | JWT `[Authorize]` | `/api/v1/persons/*` via Gateway |
| Internal | `InternalOnly` + `X-Internal-Api-Key` | `/internal/persons` on PersonService |

## Related

- [PERSON_DOMAIN_MODEL.md](./PERSON_DOMAIN_MODEL.md)
- [user-module-gap-analysis.md](./user-module-gap-analysis.md)
