# 01 — AOVIAS Intelligence Layer (AIL)

**Phase:** 5 — Intelligence Layer  
**Sprint:** 1  
**Date:** 2026-07-26

## Purpose

AIL is the central intelligence engine of AOVIAS Command. Modules consume AIL through Gateway `/api/v1/ai/*` — they never embed LLM logic.

## Architecture

| Concern | Owner |
|---------|-------|
| Orchestration, RAG, prompts, ACE, governance | `AIOrchestratorService` |
| Structured hospital facts | Compliance `AIContextBundle` + enterprise graph |
| Framework/KB text | KnowledgeBase `/knowledge/search` + `/ai/ask` |
| Chat providers | Sarvam / Azure OpenAI via `ProviderRoutingModelAdapter` |
| Notifications for ACE | CommunicationService (Sprint 5 bridge) |

## Contracts

- `GroundedAnswer` — answer, citations, reasoning, confidence, modelVersion, traceId
- `AIResponse.Grounded` — projection populated on every read/assist turn
- `AISourceReference` — extended with `DocumentId`, `Version`, `Excerpt`

## Safety defaults

- AI is advisory; regulatory writes require human approval
- `EnableAIProactive` / autonomous workflows remain off unless acceptance gates pass
- ACE (Sprint 5) emits insights and proposals only

## ACE (strategic)

Autonomous Compliance Engine continuously monitors compliance posture and surfaces prioritized recommendations. It does **not** silently change journey/CAPA/evidence state.
