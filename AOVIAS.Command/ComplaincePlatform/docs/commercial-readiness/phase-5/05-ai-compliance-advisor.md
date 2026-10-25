# 05 — AI Compliance Advisor (CAPA)

**Phase:** 5.4  
**Sprint:** 3  
**Date:** 2026-07-26

## Purpose

Propose corrective and preventive CAPA actions as **proposals only**. No CAPA creation or status mutation.

## Service

`ComplianceAdvisorService` (`Application/AI/Advisor/`)

- Retrieval-grounded heuristics for corrective / preventive suggestions
- Optional LLM assist path (`UseLlmAssist`) via `IAIOrchestrationService.RunAssistiveAsync`
- `HumanApprovalRequired` always `true`
- Audited via `IAIAuditService` (`advisor.capa-suggest`)

## API

- Orchestrator: `POST /api/v1.0/ai/advisor/capa-suggest`
- Gateway: `POST /api/v1/ai/advisor/capa-suggest`

## Frontend

- Deficiency Command Center detail panel: **Suggest CAPA actions** button (best-effort)
- Binder AI Mapping tab surfaces mapping suggestions with human-approval banner

## Exit criteria

Suggestions render as proposals; operators must save CAPA fields manually after review.
