# Phase 2 — Workflow Automation (Sprint 5)

**Sprint:** 5  
**Status:** Delivered  
**Principle:** Declarative, code-first rules react to `ComplianceEventType` via `IWorkflowRule` / `WorkflowRuleEngine`. Rules must not break journey/enroll or existing outbox projection processors.

## Architecture

```text
Domain action
  → IComplianceEventEmitter.EmitAsync(...)
      → OutboxMessage (ComplianceEvent.{EventType})
      → IWorkflowRuleEngine.ExecuteAsync(envelope)   // same request, deferred DI resolve
  → SaveChanges

InProcessOutboxProcessor
  → dashboard / snapshot projection (unchanged)
```

`ComplianceEventEmitter` resolves `IWorkflowRuleEngine` via `IServiceProvider` to avoid constructor cycles (`rules → FindingPromotion → emitter`).

## Rule catalog

| Rule | Event | Behavior |
|------|-------|----------|
| `EvidenceApprovedWorkflowRule` | `EvidenceApproved` (13) | Publish notification (assignee when resolvable); enqueue snapshot recalculation; invalidate dashboard summary cache. Complements existing `EvidenceReviewService` processors. |
| `AuditSessionClosedWorkflowRule` | `AuditSessionClosed` (43) | For each **Accepted** finding on the session without `PromotedDeficiencyId`: promote CAPA via `IFindingPromotionService`, `TryAutoAssignDeficiencyAsync` via AssignmentRouting, notify. |

## Close gate change

`AuditSessionCloseGate` treats `FindingStatus.Accepted` as closeable. Accepted findings without CAPA are auto-promoted by the `AuditSessionClosed` rule after close emits.

## Registration (`ComplianceService/Program.cs`)

- `IWorkflowRuleEngine` → `WorkflowRuleEngine`
- `IWorkflowRule` → `EvidenceApprovedWorkflowRule`, `AuditSessionClosedWorkflowRule`

## Events

| Event | Value | Emitter |
|-------|-------|---------|
| `AuditSessionClosed` | 43 | `AuditorWorkspaceQueryService.CloseSessionAsync` |

## Tests

- `WorkflowRuleEngineTests` — matching rule dispatch + failure isolation + outbox deserialize smoke
- `AuditSessionCloseGateTests` — Accepted without CAPA allowed for close

## Build verify

```bash
dotnet test ComplianceService.Tests --filter "FullyQualifiedName~WorkflowRuleEngineTests|FullyQualifiedName~BusinessCalendarCalculatorTests"
dotnet build CompliancePlatform.slnx -o _build_verify_phase0\P2S5
```
