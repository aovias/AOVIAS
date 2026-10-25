# PRAMAN Communication Gap Analysis — EIWAM Phase 8

| Field | Value |
|-------|-------|
| **Phase** | EIWAM Phase 8 — Communication & Collaboration |
| **Prior work** | M6 Identity, Phases 1–7 EIWAM, Compliance event catalog, OutboxRelayWorker |
| **Status** | Investigation complete — implementation authorized |
| **Bounded context** | CommunicationService (new greenfield service) |

## Executive summary

PRAMAN has **event plumbing and escalation business logic** that is notification-ready, but **no end-to-end communication framework**. Phase 8 introduces **CommunicationService** as the single platform capability for outbound messaging. Business modules publish communication requests or outbox events; the framework handles templates, preferences, availability, channel selection, delivery, and audit.

**Do not embed notification logic in Compliance, Governance, or Person.**

---

## Investigation scope

| Area | Locations reviewed |
|------|-------------------|
| Email | `Shared.Infrastructure/Email/IEmailService.cs`, `ConsoleEmailService.cs`, Identity registration/password |
| Outbox | Compliance, Governance, Identity, Person, Workforce, Professional outbox processors |
| Event relay | `OutboxRelayWorker/OutboxRelayBackgroundService.cs` → Azure Service Bus `compliance-events` |
| Compliance events | `ComplianceEventType`, `ComplianceEventEmitter` |
| Escalation | `ComplianceService/EscalationService.cs`, `GovernanceService/EscalationResolverService` |
| Person contacts | `PersonService/Domain/Entities/PersonEntities.cs`, `NotificationDefaults` JSON |
| Invitations | `OrganizationMemberInvitation`, `InvitationAppService` |
| Angular | `header.component.ts` mock bell, `NotificationService` (toast only) |
| Gateway | No `/notifications` routes |
| AI consumer | `ProactiveSuggestionConsumer` (compliance events → AI, not users) |

---

## Already implemented

| Capability | Implementation |
|------------|----------------|
| Email interface | `IEmailService` + console stub used by Identity auth |
| Transactional outbox | All M6 services have `OutboxMessage` tables |
| Compliance event catalog | 40+ `ComplianceEventType` values via `ComplianceEventEmitter` |
| Service Bus relay | `OutboxRelayWorker` relays Compliance outbox to `compliance-events` |
| Escalation state | Compliance deficiency escalation + Governance escalation chains |
| Person contacts | Email/Phone via `PersonContact` |
| Login/auth emails | Identity registration, verify, reset password |
| WebApp toasts | `NotificationService` MatSnackBar wrapper |
| Hangfire jobs | Compliance scheduled jobs (state only, no user alerts) |
| Gateway proxy pattern | MediatR + `IProxyService` (Phases 6–7) |

---

## Reusable (extend, do not duplicate)

| Asset | Reuse pattern |
|-------|---------------|
| `OutboxMessage` pattern | Same shape in CommunicationService |
| `IEmailService` | Evolve into CommunicationService email channel adapter |
| `ComplianceEventType` + relay | CommunicationService Service Bus consumer |
| `PersonContact` | Resolve email/phone; no copy in Communication DB |
| `OrganizationMemberInvitation` | Wire send via CommunicationService |
| Escalation target resolution | Governance/Workforce clients for recipient IDs |
| Gateway MediatR proxy | Mirror Governance/Security controllers |
| WebApp `BaseApiService` | New `communication-api.service.ts` |

---

## Needs enhancement

| Area | Current | Phase 8 target |
|------|---------|----------------|
| **Email** | Console stub | Provider-independent adapter + queue + retry |
| **Person prefs** | Opaque `NotificationDefaults` JSON | Typed `NotificationPreference` in CommunicationService |
| **Governance outbox** | Log-only processor | Add relay to Service Bus for communication consumer |
| **Invitations** | DB + API; email not wired | Template + send on create |
| **Document reminders** | `ReminderCount` columns | `ReminderRule` + scheduler |
| **Header bell** | Hardcoded mock | Real in-app inbox API |
| **Identity emails** | Direct `IEmailService` | Phase 8.1 route through Communication (optional defer) |

---

## Missing

| Capability | Phase 8 deliverable |
|------------|---------------------|
| CommunicationService microservice | New bounded context |
| Shared.Communication SDK | `ICommunicationClient` for modules |
| In-app notifications | `InAppNotification` + inbox API |
| SMS / WhatsApp / Push / Teams / Slack | Provider abstractions (stubs + email/in-app v1) |
| Message templates | Versioned, localized `CommunicationTemplate` |
| Delivery tracking | `DeliveryRecord` lifecycle |
| Preference engine | Per-person + per-module prefs, quiet hours |
| Availability | Working hours, vacation, timezone |
| Reminder rules | Configurable scheduler |
| Escalation delivery rules | Event → template → channel mapping |
| Communication timeline | Unified audit stream |
| Gateway `/notifications/*` | Full proxy |
| Angular notification center | Feature module at `/app/notifications` |

---

## Technical debt

| Item | Risk | Phase 8 action |
|------|------|----------------|
| Direct email from Identity | Bypasses framework | Document; migrate in 8.1 |
| Mock header notifications | Misleading UX | Wire to real API |
| Governance outbox not relayed | Events lost for delivery | Add relay topic |
| RabbitMQ documented but unused | Confusion | Service Bus + HTTP for v1 |
| `NotificationDefaults` untyped JSON | Schema drift | Communication owns typed prefs |

---

## Definition of Done

- [ ] CommunicationService deployed with M8 migration
- [ ] Modules use `ICommunicationClient` or outbox — no direct SMTP
- [ ] Email + in-app channels operational
- [ ] Templates, preferences, reminders, availability APIs
- [ ] Compliance Service Bus consumer for high-value events
- [ ] Gateway + Angular notification center
- [ ] Delivery audit + outbox domain events
- [ ] Unit + integration tests

---

## Boundary rules

- **CommunicationService** owns delivery, templates, preferences, availability, delivery records.
- **PersonService** owns contact data only.
- **Compliance/Governance** own escalation **state**; Communication owns escalation **delivery**.
- **Gateway** proxies only; no DbContext dependencies on Communication entities.
