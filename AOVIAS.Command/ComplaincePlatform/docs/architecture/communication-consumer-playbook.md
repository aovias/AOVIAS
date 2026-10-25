# Communication Consumer Playbook

How business modules request communications **without** coupling to email, SMS, or in-app delivery.

## Principles

1. **Modules publish; CommunicationService delivers.**
2. Domain services stay broker-agnostic (Outbox + HTTP client only).
3. Use **dedupe keys** for idempotent event handling.
4. Mark **Critical** only when availability/quiet-hours must be bypassed.

## Option A — Imperative HTTP (`ICommunicationClient`)

Register in `Program.cs` / service extensions:

```csharp
builder.Services.AddPramanCommunication(builder.Configuration);
```

Configure:

```json
"CommunicationService": {
  "BaseUrl": "https://localhost:7215"
}
```

Send:

```csharp
await communicationClient.SendAsync(new CommunicationRequestDto
{
    ModuleCode = "Organization",
    EventType = "InvitationCreated",
    RecipientEmail = invite.Email,
    TemplateCode = "member-invitation",
    Variables = new Dictionary<string, string> { ["inviteLink"] = link },
    DedupeKey = $"InvitationCreated:{invite.Id}"
}, ct);
```

**When to use:** Known recipient at call site (invitations, governance escalation, admin actions).

## Option B — Event subscription (Azure Service Bus)

1. Compliance (or other publisher) emits JSON to topic `compliance-events`.
2. `ComplianceEventConsumer` in CommunicationService matches `EventSubscriptionRule` rows (seeded at bootstrap).
3. Consumer extracts `personId` / `assigneeUserId` from payload and calls `CommunicationRequestService`.

**When to use:** High-volume lifecycle events (`TaskAssigned`, `SlaWarning`, `SlaBreached`) where the publisher should not know channel details.

Configure CommunicationService:

```json
"AzureServiceBus": {
  "ConnectionString": "<connection>",
  "ComplianceEventsTopic": "compliance-events",
  "SubscriptionName": "communication-service"
}
```

If connection string is empty, the consumer is disabled (safe for local dev). Prefer:

1. **HTTP `ICommunicationClient.SendAsync`** (Compliance / Organization path), or  
2. **`POST /internal/communication/events`** for subscription-style enqueue without Service Bus.

See also `docs/commercial-readiness/phase-2/05-notification-platform.md`.

## Templates

Bootstrap seeder registers v1 templates:

| Code | Module | Typical event |
|------|--------|---------------|
| `task-assigned` | Compliance | TaskAssigned |
| `sla-warning` | Compliance | SlaWarning |
| `sla-breached` | Compliance | SlaBreached |
| `governance-escalation` | Governance | EscalationTriggered |
| `member-invitation` | Organization | InvitationCreated |

Admins can extend via `POST /api/v1/templates` (published versions used by `TemplateRenderer`).

## Recipient resolution order

1. Explicit `RecipientEmail` / `RecipientPhone` on request
2. `PersonId` → PersonService / Identity contact API
3. Fail fast if no routable address (logged; no partial send)

## Deduplication

Set `DedupeKey` to a stable business key, e.g. `TaskAssigned:{taskId}:{personId}`. Duplicate requests within the retention window return the existing `OutboundMessageId`.

## Non-goals (v1)

- Modules must **not** call `ConsoleEmailService` or SMTP directly.
- Do **not** publish to RabbitMQ from ComplianceService — use Outbox/Service Bus as configured.
- Do **not** store duplicate preference JSON in PersonService for new features.

## Checklist for new module integration

- [ ] Add `Shared.Communication` reference
- [ ] Call `AddPramanCommunication(configuration)`
- [ ] Add `CommunicationService:BaseUrl` to appsettings
- [ ] Define template code + bootstrap or admin template
- [ ] Choose HTTP vs event subscription
- [ ] Provide dedupe key and variables
- [ ] Verify delivery via Communication timeline / delivery dashboard

## Smoke verification

```bash
dotnet test CommunicationService.Tests
dotnet run --project CommunicationService
# POST /internal/communication/send with internal API key
# GET /api/v1/notifications/unread-count via Gateway with user JWT
```

Load smoke: enqueue 1000 outbound messages with distinct dedupe keys; confirm outbound processor drains queue and inbox pagination remains sub-second with `(PersonId, CreatedAt)` index.
