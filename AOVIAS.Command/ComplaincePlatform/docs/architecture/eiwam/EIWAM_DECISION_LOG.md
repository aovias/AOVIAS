# EIWAM Decision Log

Architecture Decision Records (ADRs) for the EIWAM platform foundation.

---

## ADR-001: Microservices per bounded context

**Status:** Accepted

**Context:** Healthcare compliance requires independent scaling and team ownership.

**Decision:** One deployable service per bounded context with dedicated database.

**Consequences:** HTTP + event integration required; no shared EF DbContext.

---

## ADR-002: Gateway as sole BFF

**Status:** Accepted

**Context:** Angular WebApp needs single origin and auth termination.

**Decision:** All operator APIs route through Gateway with MediatR proxy handlers.

**Consequences:** Gateway must register HttpClient per backend service.

---

## ADR-003: Outbox pattern for messaging

**Status:** Accepted

**Context:** Domain services must remain broker-agnostic.

**Decision:** `OutboxMessage` + local processor + `OutboxRelayWorker` to Azure Service Bus.

**Consequences:** Eventual consistency; relay worker is required infrastructure.

---

## ADR-004: Access Governance centralization

**Status:** Accepted

**Context:** Permissions scattered across services caused drift.

**Decision:** Seed permissions in AccessGovernanceService; evaluate via `IAuthorizationEvaluator`.

**Consequences:** New modules require permission seeding (e.g. `Integration.*`).

---

## ADR-005: ComplianceJourney as sole lifecycle owner

**Status:** Accepted

**Context:** Duplicate status enums caused invalid state transitions.

**Decision:** `ComplianceLifecycleStateMachine` only; transitions via `ComplianceJourneyService`.

**Consequences:** Background jobs and integrations must call transition APIs.

---

## ADR-006: Enterprise Directory vs Workforce Intelligence split

**Status:** Accepted (Phase 11)

**Context:** Search/discovery mixed with analytics created coupling.

**Decision:** Directory owns governed search; Intelligence owns analytics/forecasts.

**Consequences:** No search APIs in Intelligence; no KPI dashboards in Directory.

---

## ADR-007: Dedicated IntegrationService (Phase 12)

**Status:** Accepted

**Context:** External sync scattered; no connector framework existed.

**Decision:** New `IntegrationService` on port 7218; never owns domain aggregates.

**Consequences:** HTTP writes to Person/Workforce/Org only; mapping stored in DB.

---

## ADR-008: Provider tier strategy for connectors

**Status:** Accepted

**Context:** 20+ connector types needed; cannot implement all at once.

**Decision:** Tier 1 full, Tier 2 reference (LDAP/Azure AD), Tier 3 validate-only stubs.

**Consequences:** Extension guide required; stubs registered in DI with metadata.

---

## ADR-009: Credential encryption at application layer

**Status:** Accepted

**Context:** Connector secrets stored in SQL Server.

**Decision:** AES-GCM via `CredentialProtector`; key from configuration/Key Vault.

**Consequences:** Key rotation requires re-encryption strategy (future).

---

## ADR-010: EIWAM architecture freeze

**Status:** Accepted

**Context:** Phase 12 completes the 12-phase EIWAM roadmap.

**Decision:** Document canonical architecture in `docs/architecture/eiwam/`; treat as stable foundation.

**Consequences:** Future changes require new ADRs; major shifts need platform review.

---

## ADR-011: Azure Service Bus over RabbitMQ in domain services

**Status:** Accepted

**Context:** Gateway historically used RabbitMQ; workers use Service Bus.

**Decision:** Domain services use outbox only; relay worker publishes to Service Bus topics.

**Consequences:** RabbitMQ limited to Gateway/messaging infrastructure per architecture rules.
