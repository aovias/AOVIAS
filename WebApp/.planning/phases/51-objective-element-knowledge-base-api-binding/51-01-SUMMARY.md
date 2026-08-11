---
plan: 51-01-PLAN.md
wave: 1
status: complete
---

# Phase 51 Wave 1 — Summary

## Delivered

- `src/app/features/journey/models/knowledge-base-api.model.ts` — DTOs for Knowledge Base API (envelope sketch, `NodeDetailsDto`, `KnowledgeItemDto`, `ComplianceNodeDto`, `RequirementDetailDto`, `RequirementScoreDto`, AI ask types).
- `src/environments/environment*.ts` — `knowledgeBasePath: 'knowledge-base'` (suffix after `apiBaseUrl`).
- `src/app/features/journey/services/knowledge-base-data.service.ts` — `getNodeDetails`, `getKnowledgeByNodeId`, `getKnowledgeByNodeCode` (encoded path + optional `frameworkId`), `postAiAsk`, `resolveNodeDetailsForObjectiveElement` (404 + empty-knowledge → code fallback via mapper helpers).
- `src/app/features/journey/services/knowledge-base-data.service.spec.ts` — HttpTestingController: URL shape, 404 fallback, non-404 error propagation.

## Verification

- `ng build` — pass.
- `ng test --include=**/knowledge-base-data.service.spec.ts` — pass.

## Commits

_(Orchestrator: record per GSD commit rules if bundling separately.)_
