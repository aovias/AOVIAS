# Governance Consumer Playbook — EIWAM Phase 6

| Field | Value |
|-------|-------|
| **Audience** | ComplianceService, AuditService, Risk modules |
| **SDK** | `Shared.Governance` |
| **Canonical service** | `GovernanceService` |

## When to use governance vs access control

| Need | Use |
|------|-----|
| Can user perform action? | `Shared.Authorization` + AccessGovernance |
| Who owns outcome? | `Shared.Governance` + GovernanceService |
| Who must approve? | `IApprovalWorkflowEvaluator` |
| Who receives escalation? | `IEscalationResolver` |

Never store long-term ownership in domain tables when governance assignment is appropriate.

---

## Registration

```csharp
builder.Services.AddTransient<InternalApiKeyForwardingHandler>();
builder.Services.AddPramanGovernance(builder.Configuration);
```

`appsettings.json`:

```json
"GovernanceService": {
  "BaseUrl": "https://localhost:7210"
}
```

---

## Owner lookup

```csharp
var owners = await _governanceClient.GetOwnersAsync(new OwnerLookupRequest
{
    ResourceType = "Department",
    ResourceId = departmentId,
    ResponsibilityTypeCode = "ProcessOwner"
}, ct);
```

Returns active `GovernanceOwnerDto` entries (assignee type/id, priority).

---

## Approval evaluation

```csharp
var decision = await _approvalEvaluator.EvaluateAsync(new ApprovalEvaluateRequest
{
    ApprovalChainId = chainId,
    ApprovedSteps = "1,2",
    Context = new Dictionary<string, string> { ["requiresLegal"] = "true" }
}, ct);
```

`Completed` + `Approved` when all applicable steps satisfied.

---

## Escalation resolution

```csharp
var resolution = await _escalationResolver.ResolveAsync(new EscalationResolveRequest
{
    EscalationChainId = chainId,
    CurrentLevel = 0,
    CurrentEmploymentId = employmentId,
    HoursSinceOpen = 48,
    Severity = "High"
}, ct);
```

Supports `DelayHours`, severity conditions, and `ManagerChain` targets via WorkforceService.

---

## Compliance pilot (Phase 6)

`DepartmentOwnershipAppService.ListAsync` enriches rows from governance when local primary owner is empty:

- Resource type: `Department`
- Responsibility type: `ProcessOwner`
- Failures are logged; local data remains authoritative for writes

Incremental migration path documented in [GOVERNANCE_GAP_ANALYSIS.md](./GOVERNANCE_GAP_ANALYSIS.md).

---

## Audit module (future)

Audit findings use governed-resource refs (`AuditFinding`, `CorrectiveAction`) — no embedded owner fields in AuditService today. Register `AddPramanGovernance()` when audit workflows need approval/escalation chains.

---

## Internal API requirements

- `X-Internal-Api-Key` forwarded via `InternalApiKeyForwardingHandler`
- Correlation ID propagated via `CorrelationForwardingHandler`

Endpoints:

| Endpoint | Purpose |
|----------|---------|
| `GET /internal/governance/owners` | Owner lookup |
| `GET /internal/governance/timeline` | Event history |
| `POST /internal/governance/approval/evaluate` | Approval engine |
| `POST /internal/governance/escalation/resolve` | Escalation engine |
