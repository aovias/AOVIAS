# 12 — Model Orchestration

**Phase:** 5.11  
**Sprint:** 6  
**Date:** 2026-07-26

## Routing

`ProviderRoutingModelAdapter` selects the active chat provider from:

- `AIProviderRouting:AIProvider`, or
- top-level `AIProvider` (PostConfigure override)

| Provider | Status | Adapter |
|----------|--------|---------|
| `AzureOpenAI` (default) | Production path | `AzureOpenAIModelAdapter` |
| `Sarvam` | Production D1 path + Azure fallback | `SarvamModelAdapter` |
| `Anthropic` | Stub (deterministic fallback) | `AnthropicModelAdapter` |
| `Ollama` | Stub (deterministic fallback / NotSupported) | `OllamaModelAdapter` |

Unknown providers log a warning and fall back to AzureOpenAI.

## Stub adapters

- Registered in DI via `AddLLMAdapter` so they are available without compile-time `#if`.
- **Not selected** unless config explicitly sets `AIProvider` / `AIProviderRouting:AIProvider`.
- Do not call external Anthropic/Ollama APIs in this phase.

## Options documentation

See XML remarks on `AIProviderRoutingOptions.AIProvider` in `Application/AI/Configuration/AIOptions.cs`.

## Governance metrics

`GET /api/v1/ai/governance/metrics` (AIOrchestrator + Gateway proxy) exposes the in-process `IMetricsTracker` snapshot (validator outcomes, timeouts, hallucination flags, accept/reject).

## Exit criteria

Stub providers are registered; only config selection activates them; metrics endpoint is reachable.
