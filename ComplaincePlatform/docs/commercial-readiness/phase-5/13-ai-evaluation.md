# 13 — AI Evaluation Harness

**Phase:** 5.12  
**Sprint:** 6  
**Date:** 2026-07-26

## Scope

Unit/evaluation tests in `AIOrchestratorService.Tests` (no live LLM calls).

## Golden / grounding coverage

| Suite | File | What it proves |
|-------|------|----------------|
| Embedding cosine | `KnowledgeGroundingEvaluationTests` | Relevant infection-control text ranks above unrelated parking text via `HashingEmbeddingService.CosineSimilarity` |
| Knowledge search | same | Semantic mode returns version-aware citations from an in-memory `IVectorIndex` |
| GroundedAnswer shape | same | Required fields: answer, citations (DocumentId/Version/Excerpt), reasoning, confidence, modelVersion, traceId |
| HallucinationGuard | `HallucinationGuardTests` | Flags unsupported numbers/IDs; accepts grounded facts; skips empty grounding; ignores small prose integers |
| PII redaction | `KnowledgeGroundingEvaluationTests` | Email/phone masked |

## Packages

Existing test project packages are sufficient (`xunit`, `Microsoft.NET.Test.Sdk`, `Microsoft.EntityFrameworkCore.InMemory`). No Moq dependency — fakes are hand-rolled.

## Out of scope

- Live Azure/Sarvam/Anthropic/Ollama integration tests
- Playwright UI E2E (see testing report)

## Exit criteria

`dotnet test AIOrchestratorService.Tests` includes the evaluation suites above with passing assertions.
