# Developer Guide

[← README](./README.md) · [Coding Standards](./20-Coding-Standards.md)

---

## Repository Layout

```text
V:\DevelopmentRepo\V1.0\
├── ComplaincePlatform/     # .NET microservices
├── WebApp/                 # Angular operator portal
└── docs/                   # Platform docs + this Architecture portal
```

---

## Prerequisites

- .NET 10 SDK
- Node 20+ for Angular
- SQL Server (local or Docker)
- (Optional) Azure Service Bus for outbox relay

---

## Running Locally

1. Start SQL Server.
2. Start Identity, Organization, Compliance, KnowledgeBase, Gateway (order flexible after SQL).
3. `cd WebApp && npx ng serve --port 4200`
4. Login via `/login` — Gateway must be reachable.

Apply migrations:

```bash
cd ComplaincePlatform/ComplianceService
dotnet ef database update
```

---

## Adding a New API (Backend)

1. Define contract in `Application/Contracts` or Copilot contracts.
2. Implement service in `Application/Services`.
3. Register in `Program.cs`.
4. Add controller under `Controllers/V1/`.
5. Add Gateway MediatR handler + controller route.
6. Add EF migration if schema changes: `dotnet ef migrations add Name`.
7. Document in [11-API-Architecture.md](./11-API-Architecture.md).

---

## Adding a New Entity

1. Domain entity + configuration (`Infrastructure/Persistence` or `EntityConfigurations`).
2. DbSet on `ComplianceDbContext`.
3. Repository interface + implementation.
4. Migration — **never** manual migration without snapshot update.

---

## Adding an Angular Feature Page

1. Create `features/{name}/` with `routes.ts`, page component.
2. Register lazy route in `app.routes.ts` under `/app`.
3. Add sidebar entry in `sidebar-nav.model.ts`.
4. Create `*ApiService` extending `BaseApiService`.
5. Use signals store if cross-route state needed.
6. Follow `WebApp/docs/architecture/PRAMAN_ANGULAR_ARCHITECTURE_RULES.md`.

---

## DDD Guidelines

- Controllers thin; business logic in application services.
- Domain entities enforce invariants (methods, not anemic setters everywhere).
- Cross-aggregate orchestration in services, not entities.

---

## CQRS Guidelines

- Gateway: separate query/command handlers where MediatR adopted.
- Compliance: read DTOs for APIs; snapshots for heavy reads.
- Avoid command logic in GET handlers.

---

## Testing Expectations

- Backend: `ComplianceService.Tests`, `Gateway.UnitTests`.
- Frontend: Vitest via `ng test` — add specs for new stores/services.
- E2E: **limited** — manual browser QA for major sprints.

---

## Documentation Expectations

- Sprint report in `ComplaincePlatform/docs/` on delivery.
- Update [05-Module-Readiness-Matrix.md](./05-Module-Readiness-Matrix.md) when module maturity changes.
- ADR in `decisions/` for architectural choices.

---

## Useful References

| Doc | Path |
|-----|------|
| API inventory | `ComplaincePlatform/docs/backend-api-inventory.md` |
| Angular rules | `WebApp/docs/architecture/PRAMAN_ANGULAR_ARCHITECTURE_RULES.md` |
| Design tokens | `WebApp/DESIGN_RULES.md` |
| Constitution | `docs/PRAMAN-Architecture-Constitution.md` |
