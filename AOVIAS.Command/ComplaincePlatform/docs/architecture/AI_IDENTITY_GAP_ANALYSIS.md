# PRAMAN AI Identity Gap Analysis — EIWAM Phase 9

| Field | Value |
|-------|-------|
| **Phase** | EIWAM Phase 9 — AI Identity |
| **Prior work** | Phases 0–8 EIWAM, AIOrchestrator D0/D1, Compliance Copilot Sprint 12–13 |
| **Status** | Investigation complete — implementation authorized |
| **Bounded context** | AIOrchestratorService (evolve in place) |

## Executive summary

PRAMAN has a **mature AI execution runtime** (orchestration, retrieval, LLM routing, tools, autonomy scaffold) but AI operates as an **ungoverned privileged subsystem** — not as an enterprise digital actor. Phase 9 establishes **governed AI identities** with profiles, Access Governance permissions, automation policies, versioned prompts, rich execution audit, and admin APIs.

**Do not create a parallel AI microservice. Extend AIOrchestratorService.**

**Copilot strategy:** Bridge in place — seed `AIAssistantProfile` from `CopilotPersona`; Compliance Copilot references profile IDs; no bounded-context migration in Phase 9.

---

## Investigation scope

| Area | Locations reviewed |
|------|-------------------|
| AI Orchestrator | `AIOrchestratorService/Application/AI/Orchestration/AIOrchestrationService.cs` |
| AI Providers | `Infrastructure/LLM/ProviderRoutingModelAdapter.cs`, Sarvam/Azure adapters |
| Prompt storage | `Application/AI/Prompting/PromptTemplateRegistry.cs` (in-memory) |
| AI permissions | `AIFeaturePolicy.cs`, JWT `ai.api` scope, Gateway `AIController` |
| AI audit | `Domain/AIAuditEntry.cs`, `AIAuditService.cs` |
| Event processing | `ProactiveSuggestionConsumer`, `AgentLifecycleEventPublisher` |
| Configuration | `Application/AI/Configuration/AIOptions.cs`, appsettings |
| Execution pipeline | Read/assist/action + `AgentOrchestrator` |
| Copilot parallel | `ComplianceService/Domain/Copilot/CopilotPersona.cs`, `ApprovalPolicyService` |
| Access Governance | `AccessGovernanceMasterDataSeeder`, `Shared.Authorization` |
| Angular | Sidebar AI, `/app/copilot`, no AI admin UI |

---

## Already implemented

| Capability | Implementation |
|------------|----------------|
| AI orchestration pipeline | Retrieve → classify → prompt → LLM → validate → present |
| Provider abstraction | `IAIModelAdapter`, routing, Sarvam/Azure |
| Basic audit | `AIAuditEntry` + `AIAuditService` |
| Feature flags | `AIFeatureFlagsOptions` + `AIFeaturePolicy` |
| Autonomy scaffold | `AgentOrchestrator`, workflow templates, D0 approval gates |
| Tool registry | `AIToolRegistry`, precondition validation |
| Prompt versioning (lite) | In-memory templates + A/B bucket |
| PRAMAN persona voice | ExplainCompliance system prompts |
| Gateway AI proxy | `Gateway/Controllers/AIController.cs` |
| Copilot personas | 7 `CopilotPersona` enums with experience registry |
| Access Governance engine | RBAC/ABAC evaluator + `Shared.Authorization` SDK |

---

## Reusable (extend, do not duplicate)

| Asset | Reuse pattern |
|-------|---------------|
| `AIOrchestrationService` | Inject profile/policy context before execution |
| `IAIModelAdapter` | Provider-independent; record model in audit |
| `AIAuditEntry` | Transition to `AIExecutionAudit`; keep compat one release |
| `AIFeaturePolicy` | Org-level kill switch alongside AG permissions |
| `AutonomyPolicyEngine` | Merge with `AIAutomationPolicy` resolver |
| `PromptTemplateRegistry` | Seed `AIPromptProfile` v1 from hardcoded templates |
| `AccessGovernanceService` | Seed `AI.*` permission codes; no new permission engine |
| `GovernanceService` approval chains | `AIApprovalPolicy` references chain ID |
| `CommunicationService` | `AIApprovalRequested` notifications |
| Gateway proxy pattern | MediatR + `ProxyToAIServiceAsync` for admin routes |
| Copilot personas | Bridge via `CopilotPersona` → `AIAssistantProfile` ID map |

---

## Needs enhancement

| Area | Current | Phase 9 target |
|------|---------|----------------|
| **Audit** | 11 fields, no user/model/tokens | Rich `AIExecutionAudit` + trace ID |
| **Permissions** | JWT scope + config flags | Access Governance `AI.*` codes |
| **Prompts** | Hardcoded C# | DB `AIPromptProfile` + publish/rollback |
| **Approval** | Boolean `HumanApprovalRequired` | Configurable `AIApprovalPolicy` + Governance chains |
| **Conversation** | Ephemeral request fields | `AIConversationContext` + retention |
| **API envelope** | Raw objects from `AiController` | `ApiResponse<T>` for identity admin APIs |
| **Copilot approval** | Hardcoded roles in `ApprovalPolicyService` | `Copilot.Proposal.Approve` via evaluator |
| **Analytics** | In-memory `MetricsTracker` | `AIUsageStatistics` + `/ai/analytics` |

---

## Missing

| Capability | Phase 9 deliverable |
|------------|---------------------|
| `AIAssistantProfile` | Governed AI identities with scope |
| `AIIdentity` | Platform ID, lifecycle, governance owner |
| `AIAutomationPolicy` | Manual/auto/approval modes |
| `AIProfilePermissionGrant` | Profile action allowlist → AG codes |
| `AIPromptProfile` + versions | Layered prompts with rollback |
| `AIExecutionAudit` (rich) | Full traceability |
| `AIConversationContext` | Persistent history |
| `AIApprovalPolicy` | Manager/committee/executive types |
| Admin APIs | `/ai/profiles`, `/policies`, `/prompts`, `/executions`, `/analytics` |
| Gateway admin proxy | Mirror Phase 8 pattern |
| Angular `ai-identity` feature | Admin UI at `/app/ai-identity` |
| Outbox domain events | `AIProfileCreated`, `PromptPublished`, etc. |

---

## Technical debt

| Item | Risk | Phase 9 action |
|------|------|----------------|
| Dual AI tracks (Orchestrator vs Copilot) | Inconsistent governance | Bridge personas to profiles |
| No user id in audit | Cannot attribute requests | Add `RequestedByUserId` |
| `AiScope` not on Gateway | Weaker than orchestrator | Add `RequirePermission` on Gateway |
| Secrets in appsettings | Security | Document; rotate in ops |
| Service Bus disabled | Event autonomy dormant | Outbox events for identity lifecycle |
| Sprint 12D persona prompts | Only generic ExplainCompliance | Seed profile-specific prompts |

---

## Definition of Done

- [ ] `AIAssistantProfile` + identity aggregates deployed (M9 migration)
- [ ] AI permissions seeded in Access Governance
- [ ] Execution pipeline enforces profile + policy + audit
- [ ] Prompt DB with publish/rollback APIs
- [ ] Gateway + Angular admin UI
- [ ] Copilot bridged to profile IDs
- [ ] Unit tests + architecture docs

---

## Boundary rules

- **AIOrchestratorService** owns AI profiles, policies, prompts, execution audit.
- **AccessGovernanceService** owns permission evaluation — AI does not create a parallel security model.
- **GovernanceService** owns approval chain **state**; AI references chains for approval requirements.
- **Compliance Copilot** remains in ComplianceService — bridged, not migrated.
- **Gateway** proxies only; no AI entity DbContext dependencies.
