# ADR-005: Deterministic Copilot Recommendation Engine

**Status:** Accepted  
**Date:** 2026-06-24 (Sprint 13B)  
**Index:** [24-Decision-Log.md](../24-Decision-Log.md)

## Context

Healthcare compliance recommendations must be explainable to auditors and quality managers. Free-form LLM outputs are difficult to audit and may hallucinate priorities.

## Decision

Implement Copilot recommendations as a **deterministic rule engine** (`RecommendationEngine` + `IRecommendationRule` implementations) reading operational SSOT without writing during generation.

## Alternatives Considered

| Alternative | Rejected because |
|-------------|------------------|
| LLM-only recommendations | Poor auditability, variable outputs |
| Hybrid with LLM as primary | Regulatory trust concerns |

## Consequences

- **Positive:** Repeatable results, `RationaleJson` for evidence trail, testable rules.
- **Negative:** New recommendation types require new rules (engineering effort).
- **Future:** LLM may enrich descriptions while rules remain SSOT.

## References

- `ComplianceService/Application/Copilot/Services/RecommendationEngine.cs`
- [15-AI-Copilot-Architecture.md](../15-AI-Copilot-Architecture.md)
