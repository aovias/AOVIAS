# 11 — AI Governance Harden

**Phase:** 5.10  
**Sprint:** 6  
**Date:** 2026-07-26

## Purpose

Harden AOVIAS AI turns against unsupported numeric/ID claims, redact PII before model calls, and persist audit lineage (model version + citations).

## Delivered

| Item | Implementation |
|------|----------------|
| Hallucination heuristic | `Application/AI/Governance/HallucinationGuard.cs` |
| Wiring | `AIResponseValidator` (citation-only) + `AIOrchestrationService` (facts when present) |
| PII redaction | `Application/AI/Governance/SensitiveDataRedactor.cs` (email/phone) used before `IAIModelAdapter.GenerateAsync` and in Azure logging |
| Audit lineage | `AIAuditEntry.ModelVersion` + `AIAuditService` serializes grounded citations |
| Metrics | `IMetricsTracker.TrackHallucination` / `GetSnapshot` → `GET /api/v1/ai/governance/metrics` |
| Migration | `20260726150000_Phase5_AIAuditModelVersion` |

## Behaviour

- Guard extracts multi-digit numbers, percentages, GUID-like tokens, and letter+digit IDs (e.g. `CAPA-999`).
- Claims must appear in citation `SourceId` / `DocumentId` / `Summary` / `Excerpt` or retrieval facts.
- Small prose integers `0–10` are ignored.
- When citations and facts are both empty, the guard skips (general-knowledge / empty context).
- Flagged answers reduce confidence and add reasoning code `HALLUCINATION_GUARD`.

## Exit criteria

Unsupported numeric/ID claims lower confidence; prompts are redacted; audit rows store model version and citation JSON; metrics endpoint returns counters.
