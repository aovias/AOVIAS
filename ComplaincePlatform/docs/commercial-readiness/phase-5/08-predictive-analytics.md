# 08 — Predictive Analytics

**Phase:** 5.7  
**Sprint:** 4  
**Date:** 2026-07-26

## Delivered

| Item | Implementation |
|------|----------------|
| Service | `Application/AI/Predictive/PredictiveAnalyticsService.cs` |
| Model | Deterministic-first from context facts + horizon factors |
| Forecasts | Readiness %, CAPA delay probability, doc expiry risk, training expiry, dept performance |
| Horizons | `weekly` / `monthly` / `quarterly` |
| Narrative | Template narrative (LLM optional / deferred) |
| APIs | `GET /api/v1/ai/predict/readiness`, `/delays`, `/overview` |

## Notes

- Confidence is reduced when fallback context is used.
- Department scores prefer `deptScore:{name}` facts; otherwise sample departments are projected.

## Exit criteria

Overview returns readiness + delay risks + department outlook for the requested horizon.
