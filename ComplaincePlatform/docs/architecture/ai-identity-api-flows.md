# AI Identity API Flows

Phase 9 establishes AI as a governed digital actor in PRAMAN.

## Profile-scoped execution

```mermaid
sequenceDiagram
    participant User
    participant GW as Gateway
    participant AG as AccessGovernance
    participant AI as AIOrchestrator
    participant Audit as AIExecutionAudit

    User->>GW: POST /ai/query + JWT
    GW->>AG: Evaluate AI.Query.Read
    AG-->>GW: Permit
    GW->>AI: Forward + X-AI-Profile-Id
    AI->>AI: Resolve AIAssistantProfile
    AI->>AI: AutomationPolicyResolver
    AI->>AI: PromptProfileService merge layers
    AI->>AI: LLM + tools
    AI->>Audit: Record execution
```

## Approval workflow

```mermaid
sequenceDiagram
    participant AI as AIOrchestrator
    participant Policy as AutomationPolicy
    participant Comm as CommunicationService
    participant User

    AI->>Policy: ResolveMode (confidence)
    Policy-->>AI: HumanApprovalRequired
    AI->>AI: Create AIApprovalRequest
    AI->>Comm: AIApprovalRequested notification
    User->>AI: POST /ai/approvals/{id}/grant
    AI->>AI: Resume tool execution
```

## Copilot bridge

Compliance `CopilotPersona` enums resolve to `AIAssistantProfile` via `GET /internal/ai/profiles/by-persona/{key}`. Persona catalog responses include `aiProfileId`.

## Gateway routes

| Route | Permission |
|-------|------------|
| GET/POST `/ai/profiles` | AI.Profile.Manage (write) |
| GET/POST `/ai/policies` | AI.Policy.Manage (write) |
| GET/POST `/ai/prompts` | AI.Prompt.Manage (write) |
| POST `/ai/prompts/evaluate` | authenticated |
| GET `/ai/executions` | authenticated |
| GET `/ai/analytics` | AI.Analytics.View |
| GET/POST `/ai/approvals` | authenticated |
| POST `/ai/query` | AI.Query.Read |

## Boundary

- **AccessGovernance** owns permission evaluation.
- **GovernanceService** owns approval chain state; AI references chain IDs.
- **Compliance Copilot** remains in ComplianceService — bridged, not migrated.
