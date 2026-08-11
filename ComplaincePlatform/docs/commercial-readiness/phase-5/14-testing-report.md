# 14 — Phase 5 Testing Report

**Date:** 2026-07-26  
**Honesty bar:** status reflects what was executed in-repo for Sprint 6 close.

## Automated tests

| Project | Focus | Notes |
|---------|-------|-------|
| `AIOrchestratorService.Tests` | Classifier, sanitizer, presentation, validator, AI identity, HallucinationGuard, knowledge grounding eval | Primary Phase 5 harness |
| Other service test projects | Unchanged by Sprint 6 | Not re-run as Phase 5 gate |

## Sprint 6 evaluation additions

- `HallucinationGuardTests.cs`
- `KnowledgeGroundingEvaluationTests.cs` (cosine + mocked vector index + GroundedAnswer shape + redaction)
- Validator tests updated for `IResponseSanitizer` + `IHallucinationGuard` constructors

## Playwright / E2E

**Deferred.** No Playwright infra / WebApp E2E project was present for AOVIAS Phase 5 UI flows in this close-out. Manual smoke against Gateway `/api/v1/ai/*` remains the pragmatic path until an E2E harness is added.

## Manual smoke checklist (recommended)

1. `POST /api/v1/ai/knowledge/search` hybrid mode returns citations with `DocumentId`/`Version`/`Excerpt`
2. `POST /api/v1/ai/read/query` populates `Grounded` on `AIResponse`
3. `GET /api/v1/ai/governance/metrics` returns counters
4. Confirm `AIProvider=Anthropic` or `Ollama` yields stub fallback (not Azure) when intentionally set

## Known test gaps

1. Full orchestration path with live providers not covered in CI  
2. ACE notification bridge not integration-tested against CommunicationService  
3. Playwright UI for advisor cards deferred  

## Latest run (Sprint 6 close)

- `AIOrchestratorService` build: **0 errors** (alternate output path used when default `bin` was locked by a running process)
- `AIOrchestratorService.Tests`: **51 passed, 0 failed**

## Exit criteria

`AIOrchestratorService` and `AIOrchestratorService.Tests` build; evaluation unit tests pass. Playwright deferred with explicit note.
