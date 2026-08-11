# Testing Guide

[← README](./README.md)

---

## Frontend (WebApp)

| Aspect | Detail |
|--------|--------|
| Runner | **Vitest** (`ng test`, builder `@angular/build:unit-test`) |
| Config | `tsconfig.spec.json`, `vitest` globals |
| Location | `*.spec.ts` alongside components/services |

**Coverage today:** ~17 spec files — journey, tasks, gap-analysis, dashboard, shared models. **Not** comprehensive.

**Expectations for new code:**

- Store logic: unit tests with mocked `HttpClient`.
- Pure pipes/utilities: direct tests.
- Components: TestBed smoke tests for critical paths.

**Not used:** Karma (not configured), Jest (not configured).

---

## Backend

| Project | Focus |
|---------|-------|
| `ComplianceService.Tests` | Domain, services |
| `Gateway.UnitTests` | Handlers, proxy |
| `Knowledgebase.Tests` | KB logic |
| `AIOrchestratorService.Tests` | AI workflows |
| `Shared.Infrastructure.Tests` | Shared helpers |

Run:

```bash
dotnet test ComplaincePlatform/ComplianceService.Tests
dotnet test ComplaincePlatform/Gateway.UnitTests
```

---

## Integration / E2E

- Postman environments in `ComplaincePlatform/postman-environments/`.
- Manual QA via Angular + Gateway for sprint sign-off.
- **No** Playwright/Cypress suite in repo (planned enhancement).

---

## Copilot Testing

- API: Postman or curl via Gateway with JWT + hospital header.
- UI: tab walkthrough (Overview → Analytics).
- Rule engine: `POST /copilot/recommendations/refresh` with seeded hospital data.

---

## QA Checklist (Release)

1. Auth login/refresh/logout.
2. Hospital/framework context switch.
3. Journey OE wizard happy path.
4. Evidence upload + review queue.
5. Finding create + promote.
6. Copilot refresh + proposal approve flow (if data exists).
7. Analytics Hub loads without 500s.
