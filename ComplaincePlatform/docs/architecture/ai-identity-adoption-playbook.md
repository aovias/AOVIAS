# AI Identity Adoption Playbook

How modules integrate with governed AI identities without bypassing platform security.

## Principles

1. AI is a digital workforce member — not a super-user.
2. Reuse **Access Governance** permission codes — no parallel security model.
3. Every execution must produce an **AIExecutionAudit** row.
4. Copilot personas bridge to profiles — do not duplicate profile data in Compliance.

## For application developers

### Invoke AI with a profile

Pass `X-AI-Profile-Id` header (or `AiProfileId` on `AIRequest`) when calling Gateway `/ai/query` or `/ai/assist`.

Default profiles are seeded at bootstrap (`compliance-assistant`, `audit-assistant`, etc.).

### Required permissions

| Action | Permission code |
|--------|-----------------|
| Read/query | `AI.Query.Read` |
| Assist | `AI.Assist.Suggest` |
| Tool preview | `AI.Action.Preview` |
| Tool execute | `AI.Action.Execute` |
| Copilot approve | `Copilot.Proposal.Approve` |

Grant via Access Governance role assignments.

## For Compliance Copilot

1. Persona catalog enriches with `aiProfileId` from internal bridge API.
2. Proposal approve/execute endpoints require `Copilot.Proposal.Approve` / `Copilot.Proposal.Execute`.
3. Do not migrate Copilot bounded context in Phase 9.

## For administrators

1. Manage profiles at `/app/ai-identity/profiles`.
2. Configure automation policies (manual / approval / auto) per profile.
3. Publish and rollback prompts in Prompt Library.
4. Review execution history and approval queue.

## Checklist for new AI capability

- [ ] Define or select `AIAssistantProfile`
- [ ] Add `AIProfilePermissionGrant` for allowed action types
- [ ] Configure `AIAutomationPolicy` (confidence threshold, approval mode)
- [ ] Publish prompt layers (system → role → template)
- [ ] Seed Access Governance permission if new action tier
- [ ] Verify audit row on test execution

## Smoke verification

```bash
dotnet test AIOrchestratorService.Tests
dotnet ef database update --project AIOrchestratorService
# GET /api/v1/ai/profiles via Gateway with admin JWT
# GET /api/v1/copilot/personas — verify aiProfileId populated
```
