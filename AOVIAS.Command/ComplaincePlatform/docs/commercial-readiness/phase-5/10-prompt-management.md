# 10 — Prompt Management

**Phase:** 5.9  
**Sprint:** 2  
**Date:** 2026-07-26

## Model

| Layer | Store | Notes |
|-------|-------|-------|
| Seed / default templates | `PromptTemplateRegistry` | In-code ExplainCompliance, SuggestMapping, RiskAnalysis, TaskRecommendation |
| Published identity prompts | `AIPromptProfile` + `AIPromptVersion` | Seeded by `AIIdentityBootstrapHostedService` |
| Merge | `IAIPromptProfileService.ResolveMergedPromptAsync` | System + Role (by profile) + Template layers |

## Resolution order (orchestration)

1. Resolve registry template for `PromptTemplateId` / version (backward compatible default).
2. If `AIRequest.AiProfileId` is set, merge AI Identity prompt layers and use as `SystemPrompt`.
3. On identity resolve failure, fall back to registry `SystemPrompt`.

## Governance

- Publish / rollback via AI Identity prompt APIs
- Execution audit records prompt version when identity services are used
- Templates remain the seed when DB profiles are empty

## Exit criteria

Requests with `AiProfileId` use published identity prompts; requests without continue to work on registry defaults.
