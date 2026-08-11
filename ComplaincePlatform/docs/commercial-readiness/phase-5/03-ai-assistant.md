# 03 — AI Assistant

**Phase:** 5.2  
**Sprint:** 2  
**Date:** 2026-07-26

## Purpose

Governed sidebar / control-center assistant that answers with grounded citations, mode-aware confidence, and durable (PII-trimmed) conversation memory.

## Backend

| Item | Implementation |
|------|----------------|
| Citation confidence | `AIResponseValidator` lowers confidence + `CITATION_GAP` note for `OrganizationSpecific` / `Hybrid` without grounded citations; SmallTalk untouched |
| Sanitizer note | `ResponseSanitizer.MissingCitationReasoningNote` |
| Prompt unify | `AIOrchestrationService` prefers `IAIPromptProfileService.ResolveMergedPromptAsync` when `AiProfileId` set; else `PromptTemplateRegistry` |
| Memory | `IConversationMemoryService` loads/saves last N messages per user+hospital; fills `ConversationSummary` when empty; email/phone regex trim |
| Read tools | `list_overdue_capas`, `list_oe_evidence_gaps`, `get_dept_readiness` via `IComplianceClient` → Compliance readonly-tools |

## Frontend

- `AiAssistantApiService.knowledgeSearch()` + `grounded` / citation DTO fields
- Sidebar shows **Confidence**, **References / Citations**, and Knowledge Source chips from hybrid search

## APIs

- Existing: `POST /api/v1/ai/query` (Gateway) → orchestrator read/query
- Knowledge chips: `POST /api/v1/ai/knowledge/search`

## Safety

- Advisory only; no lifecycle mutation from chat
- Org/hybrid answers without citations are marked lower confidence
