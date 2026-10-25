# 04 — AI Evidence Reviewer

**Phase:** 5.3  
**Sprint:** 3  
**Date:** 2026-07-26

## Purpose

Advisory scoring of evidence completeness, freshness, duplicate risk, version consistency, and OE coverage. **Never auto-approves.**

## Service

`EvidenceReviewerService` (`Application/AI/Evidence/`)

- Pulls compliance context bundle + optional hybrid knowledge citations
- Returns `EvidenceReviewResult` with scorecard, recommendation, reasoning, citations
- `HumanApprovalRequired` always `true`
- Audited via `IAIAuditService` (`evidence.review`)

## API

- Orchestrator: `POST /api/v1.0/ai/evidence/review`
- Gateway: `POST /api/v1/ai/evidence/review`

## Binder AI mapping

`EvidenceBinderQueryService.GetAiMappingAsync` calls AIOrchestrator `assist/suggest` when available; otherwise returns non-empty heuristic suggestions with `HumanApprovalRequired = true`.

## Exit criteria

Review endpoint returns scores + recommendation; approval remains a human action in Compliance workflows.
