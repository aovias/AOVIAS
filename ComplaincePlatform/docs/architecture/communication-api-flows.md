# Communication API Flows

Phase 8 introduces **CommunicationService** as the shared delivery framework. Business modules publish communication **requests**; the framework resolves recipients, preferences, availability, templates, channels, and audit.

## Imperative send (HTTP)

```mermaid
sequenceDiagram
    participant Module as Business Module
    participant Comm as CommunicationService
    participant Person as Person/Identity
    participant Email as Email Provider
    participant DB as Communication DB

    Module->>Comm: POST /internal/communication/send
    Comm->>Comm: Dedupe check
    Comm->>Person: Resolve contact (if PersonId)
    Comm->>Comm: AvailabilityEngine + preferences
    Comm->>Comm: TemplateRenderer
    Comm->>DB: OutboundMessage + DeliveryRecords
    Comm->>Email: EmailChannelProvider
    Comm->>DB: InAppNotification + Timeline + Outbox
    Comm-->>Module: OutboundMessageId
```

**Callers:** Organization invitations (`InvitationCreated`), Governance escalations (`EscalationTriggered`), admin imperative send via Gateway.

## Event-driven send (Azure Service Bus)

```mermaid
sequenceDiagram
    participant Compliance as ComplianceService
    participant Bus as Azure Service Bus
    participant Comm as ComplianceEventConsumer
    participant CommSvc as CommunicationRequestService

    Compliance->>Bus: compliance-events (TaskAssigned, SLA*)
    Bus->>Comm: Message received
    Comm->>Comm: Match EventSubscriptionRule
    Comm->>CommSvc: SendAsync(module, eventType, personId)
    CommSvc->>CommSvc: Same delivery pipeline as HTTP
```

Compliance remains **broker-agnostic** — it publishes domain/outbox events; CommunicationService owns subscription rules and templates.

## User-facing inbox (Gateway → CommunicationService)

```mermaid
sequenceDiagram
    participant UI as Angular WebApp
    participant GW as Gateway
    participant Comm as CommunicationService

    UI->>GW: GET /api/v1/notifications/unread-count
    GW->>Comm: Proxy (JWT person_id)
    Comm-->>UI: Unread count

    UI->>GW: GET /api/v1/notifications?page=1
    GW->>Comm: Proxy
    Comm-->>UI: InAppNotification page
```

## Preference update

```mermaid
sequenceDiagram
    participant UI as Angular Preferences
    participant GW as Gateway
    participant Comm as CommunicationService

    UI->>GW: PUT /api/v1/preferences
    GW->>Comm: Update NotificationPreference
    Comm->>Comm: Append CommunicationEvent (timeline)
```

Quiet hours and channel toggles apply on the next delivery attempt unless `Critical=true`.

## Reminder scheduler

```mermaid
sequenceDiagram
    participant Job as ReminderSchedulerHostedService
    participant Comm as CommunicationRequestService

    Job->>Job: Poll active ReminderRules (cron)
    Job->>Comm: SendAsync(templateCode, module)
```

## Escalation (Governance)

Governance **state** (escalation chain level) stays in GovernanceService. On level advance, `EscalationResolverService` calls `ICommunicationClient` with template `governance-escalation` after resolving assignee email via Identity internal contact API.

## Gateway route map

| Gateway route | CommunicationService |
|---------------|---------------------|
| `GET /notifications` | In-app inbox |
| `GET /notifications/unread-count` | Badge count |
| `POST /notifications/{id}/read` | Mark read |
| `GET/PUT /preferences` | Notification preferences |
| `GET/POST /templates` | Template catalog |
| `GET /communication/history` | Unified timeline |
| `GET /communication/dashboard` | Delivery stats |
| `GET/POST /reminders` | Reminder rules |
| `GET/POST /campaigns` | Campaigns |
| `GET/PUT /availability` | Working hours / vacation |

Internal only: `POST /internal/communication/send` (internal API key).

## Boundary notes

- **Toast notifications** in the WebApp (`NotificationService`) remain ephemeral UX; the header bell uses CommunicationService inbox data.
- **PersonService** exposes contacts; typed preferences migrate to CommunicationService.
- **Governance escalation state** is not duplicated in CommunicationService — only delivery artifacts (OutboundMessage, DeliveryRecord, InAppNotification).
