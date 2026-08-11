# Microservices Catalog

[← README](./README.md)

| Service | Port (typical dev) | Responsibility | Maturity |
|---------|-------------------|----------------|----------|
| **Gateway** | 5000 | BFF, auth, proxy, MediatR | Production-path |
| **ComplianceService** | 5016 / 7087 | Core compliance + Copilot | Production-path |
| **IdentityService** | 7135 | Auth, JWT, IdentityServer (dev) | Production-path |
| **OrganizationService** | 7181 | Orgs, hospitals, members | Production-path |
| **AuditService** | varies | Legacy audit sessions | Partial overlap |
| **AIOrchestratorService** | 7097 | AI workflows, drafts API | Partial |
| **KnowledgeBase** | 5120 | Framework KB, assessments | Production-path |
| **OutboxRelayWorker** | — | Service Bus relay | Background worker |

---

## Gateway Responsibilities

- JWT validation, organization access middleware, idempotency middleware.
- **29+ controllers** proxying to backends.
- YARP routes for legacy paths (`/api/identity`, `/api/compliance`, `/api/ai`).

---

## ComplianceService Responsibilities

- Journeys, tasks, evidence, findings, deficiencies, SLA, CAPA, maturity, snapshots, analytics, **full Copilot stack**.
- Hangfire: nightly snapshots, expiry, escalation, analytics daily job.
- Internal APIs: obligations, assignment, deficiencies, SLA (service-to-service).

---

## Inter-Service Communication

| From | To | Mechanism |
|------|-----|-----------|
| Gateway | All | HttpClient (sync) |
| ComplianceService | Organization | HttpClient (hospital lists) |
| ComplianceService | KnowledgeBase | HttpClient (OE codes) |
| AIOrchestrator | Compliance | HttpClient |
| OutboxRelay | Service Bus | Async publish |

**No** synchronous chain deeper than Gateway → one downstream for user requests (target pattern).

---

## Shared Projects

| Project | Contents |
|---------|----------|
| `Shared.Infrastructure` | `ApiResponse`, auth, DTOs, middleware helpers |
| `Shared.Logging` | Serilog configuration |
| `Compliance.MasterData` | Seed/master definitions |

---

## Not in Default Docker Compose

Gateway, KnowledgeBase, OutboxRelayWorker — run locally or extend compose.

See `ComplaincePlatform/docker-compose.dev.yml`.
