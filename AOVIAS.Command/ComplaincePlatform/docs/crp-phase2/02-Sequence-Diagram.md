# CRP Phase 2 — Sequence Diagram

```mermaid
sequenceDiagram
  participant Web as WebApp
  participant GW as Gateway SignupCommandHandler
  participant Org as OrganizationService
  participant Id as IdentityService
  participant Mail as IEmailService

  Web->>GW: POST /onboarding/signup
  GW->>Org: POST /internal/organizations
  GW->>Org: POST /internal/hospitals
  GW->>Org: POST /internal/organizations/{id}/ensure-defaults
  GW->>Id: POST /internal/users/register
  GW->>Org: POST /internal/organization-members
  GW->>Org: POST /internal/licenses/trial/get-or-issue
  GW->>Org: POST /internal/licenses/activate
  GW->>Org: POST /internal/licenses/seats/assign
  GW->>Id: POST /internal/token/generate
  GW->>Mail: WelcomeWithTrial (best-effort)
  GW-->>Web: SignupResponse + JWT
  Web->>Web: Persist tokens, sessionStorage trial snapshot
  Web->>Web: Navigate /onboarding
```

## Failure path

Any hard failure after stack pushes → `RunRollbackAsync` (LIFO): revoke license (if issued this run), release seat, delete user/hospital/org. Welcome email failure does **not** rollback.
