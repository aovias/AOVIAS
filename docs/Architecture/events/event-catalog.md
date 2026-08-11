# Event Catalog

[← Event Architecture](../12-Event-Architecture.md)

---

## Outbox / Domain Events (ComplianceService)

| Type | Aggregate | When emitted | Downstream |
|------|-----------|--------------|------------|
| `DeficiencyIdentifiedDomainEvent` | Deficiency | New deficiency from evaluation | Projections, analytics inputs |
| `DeficiencyStatusChangedDomainEvent` | Deficiency | Status transition | SLA, Copilot rules (reads) |
| `DeficiencyClosedDomainEvent` | Deficiency | Closure | Readiness KPIs |
| `ObligationStatusUpdatedDomainEvent` | Obligation | Obligation progress | Journey, maturity |
| Compliance lifecycle events | Journey/Program | Enrollment, approval, rejection, expiry, renewal | State machine, snapshots |

**Transport:** `OutboxMessage` → `OutboxRelayWorker` → Azure Service Bus `compliance-events`.

---

## Copilot Activity Events (Application-level)

| EventType | Publisher | Payload highlights |
|-----------|-----------|-------------------|
| `WorkspaceViewed` | Client POST | persona |
| `RecommendationViewed` | Client POST | entityId, entityType |
| `PersonaChanged` | Client POST | persona |
| `RecommendationGenerated` | Server (partial) | — |
| `DraftCreated` | Server (partial) | TD-H02 gap |
| `ProposalApproved` | Server (partial) | TD-H02 gap |
| `ProposalExecuted` | Server (partial) | — |

**Storage:** `CopilotActivityEvents` table — not outbox relay.

---

## Hangfire / Scheduled (Not domain events)

| Job ID | Schedule |
|--------|----------|
| `nightly-compliance-snapshot-recalculation` | 2 AM UTC |
| `compliance-expiry-job` | 1 AM UTC |
| `compliance-escalation-job` | 3 AM UTC |
| `analytics-daily-snapshot` | 4 AM UTC |

---

## Idempotency Notes

- Outbox relay may retry; consumers must dedupe by event id.
- Copilot activity POST should tolerate duplicate client calls (**future** idempotency key).
