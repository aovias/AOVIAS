# Phase 2 — Notification Platform (Sprint 4)

**Sprint:** 4  
**Status:** Delivered  
**Principle:** Modules publish intent; CommunicationService owns delivery, preferences, templates, and digests. Gateway owns SignalR fan-out to the WebApp.

## Architecture

```text
ComplianceService (IComplianceNotificationPublisher)
  → HTTP ICommunicationClient  (AddPramanCommunication)
    → CommunicationService POST /internal/communication/send
      → OutboundMessage + DeliveryOrchestrator
        → InAppChannelProvider → InAppNotifications
        → Gateway POST /internal/notifications/broadcast
          → NotificationHub group person:{personId}
            → WebApp NotificationRealtimeService (@microsoft/signalr)
```

### Dev path when Service Bus is unavailable

| Path | When to use |
|------|-------------|
| **HTTP `ICommunicationClient.SendAsync`** | Preferred for Compliance (and Organization). Works with empty `ServiceBus:ConnectionString`. |
| **HTTP enqueue** `POST /internal/communication/events` | Event-subscription style without Azure Service Bus (Dev). |
| **Azure Service Bus** `compliance-events` | Optional; `ComplianceEventConsumerHostedService` enables only when `ServiceBus:ConnectionString` is set. |

If Service Bus is missing, the consumer logs a warning and stays disabled — modules must use HTTP (above). Documented also in `docs/architecture/communication-consumer-playbook.md`.

### Console fallback

Compliance registers:

- **Communication** publisher when `CommunicationService:BaseUrl` **or** `Communication:BaseUrl` is set.
- **`ConsoleComplianceNotificationPublisher`** when neither BaseUrl is configured.

## Templates & variables (seeded)

| Template code | Event types | Variables |
|---------------|-------------|-----------|
| `task-assigned` | TaskAssigned, OeOwnershipAssigned | `taskTitle`, `assigneeName`, `dueDate`, `message` |
| `capa-overdue` | CapaOverdue | `capaNumber`, `ownerName`, `dueDate`, `message` |
| `audit-scheduled` | AuditScheduled | `auditName`, `scheduledDate`, `location`, `message` |
| `evidence-approved` | EvidenceApproved | `evidenceId`, `frameworkId`, `message` |
| `evidence-rejected` | EvidenceRejected | `evidenceId`, `status`, `message` |
| `framework-activated` | FrameworkActivated | `name`, `version`, `message` |

Compliance Phase-1 dotted keys (`evidence.approved`, `framework.activated`, …) are normalized to PascalCase before template mapping.

## Preferences

`NotificationPreference` enforces:

- **Channel enabled** — disabled channels are skipped (critical + `EscalationOverride` may still send).
- **Quiet hours** — non-critical messages deferred (`AvailabilityEngine.IsQuietHoursAsync`).
- **Digest / DailySummary / WeeklySummary** — deferred; flushed by `DigestNotificationHostedService` (15‑minute hosted job).

## Gateway SignalR

- Hub path: `/hubs/notifications`
- Client method: `notificationReceived`
- JWT via query `access_token` (WebSocket-friendly)
- Internal broadcast: `POST /internal/notifications/broadcast` (`X-Internal-Api-Key`)

## WebApp

- `NotificationRealtimeService` uses `@microsoft/signalr` against Gateway hub.
- Notification bell + Notification Center refresh on `notificationReceived`.
- `proxy.conf.json` proxies `/hubs` with WebSockets to Gateway.

## Configuration

```json
// ComplianceService
"CommunicationService": { "BaseUrl": "https://localhost:7215" },
"Communication": { "BaseUrl": "https://localhost:7215" }

// CommunicationService
"Gateway": { "BaseUrl": "http://localhost:5212" },
"ServiceBus": { "ConnectionString": "" }
```

## Verification

```bash
dotnet test ComplianceService.Tests --filter ComplianceNotificationPublisherSelection
dotnet build CompliancePlatform.slnx -o _build_verify_phase0\P2S4
```

Smoke expectations:

1. Missing BaseUrl → console publisher selected.
2. BaseUrl present → Communication publisher selected; HTTP enqueue reaches CommunicationService.
3. In-app create → Gateway broadcast → WebApp hub handler refreshes inbox.
