# Sarvam D1 Implementation Notes

## 1) Sarvam Capability Baseline
- API endpoint used: `POST /v1/chat/completions`.
- Request contract: chat-style `messages` payload with model and decoding params (`temperature`, `top_p`, `max_tokens`, `stream`).
- Supported auth modes implemented:
  - `Authorization: Bearer <apiKey>`
  - `api-subscription-key: <apiKey>`
- Model selection is configurable (`Sarvam:Model`) with D1 default `sarvam-30b`.
- Reliability constraints enforced in code:
  - strict timeout (`Sarvam:TimeoutSeconds`)
  - single retry for format correction (`Sarvam:RetryCount`)
  - invalid JSON detection and provider fallback.

## 2) PRAMAN Compatibility Map
- Existing seam preserved: `IAIModelAdapter`.
- Existing safety chain preserved:
  - retrieval: `RetrievalService`
  - mediator: `AIMediator`
  - validator: `AIResponseValidator`
  - confidence calibration: `ConfidenceCalibrationService`.
- Existing contracts preserved:
  - request: `AIRequest`
  - output: `AIResponse`
  - provider-neutral model contract: `AIModelRequest` and `AIModelResponse`.
- D1 guardrail compatibility:
  - provider scope restricted to assistive use-cases by routing policy.
  - no autonomous-execution bypass introduced.

## 3) Adapter and Configuration Design
- Added `SarvamModelAdapter` for request/response mapping.
- Added `ProviderRoutingModelAdapter` as runtime selector (`Sarvam` primary, `AzureOpenAI` fallback).
- Added failure taxonomy via `ProviderFailureException` and `ProviderFailureReason`.
- Added config classes and appsettings bindings:
  - `SarvamOptions`
  - `AIProviderRoutingOptions`
  - `AIUsagePolicyOptions`
  - extended `AIRetrievalPolicyOptions`.

## 4) D1 Prompt + Retrieval Strategy
- Prompt templates hardened for strict JSON output and grounding-only behavior.
- D1 use-cases covered:
  - compliance explanation
  - evidence-to-OE mapping
  - missing-task recommendation.
- Prompt versioning + deterministic bucket selection added in `PromptTemplateRegistry` to support controlled A/B behavior.
- Retrieval hard limits implemented:
  - objective elements: `MaxObjectiveElements`
  - evidence items: `MaxEvidenceItems`
  - findings: `MaxFindingsItems`
  - context budget: `MaxContextTokens`.

## 5) Safety and Reliability Layers
- `JSONRepairLayer` added before response parsing/validation path.
- `FallbackStrategy` added for Sarvam->Azure fallback on:
  - invalid JSON after repair/retry
  - timeout
  - 429 / 5xx / transport failures.
- `MetricsTracker` added for:
  - JSON validity
  - validator outcomes
  - fallback reasons
  - timeout and latency buckets
  - user approval/rejection hooks.
- Usage controls added in orchestration:
  - max requests per session
  - per-tenant rate limit per minute.

## 6) Risk and Test Blueprint
- Unit-level checks to execute:
  - JSON repair success/failure paths
  - fallback trigger matrix (`InvalidJson`, `Timeout`, `429`, `5xx`)
  - retrieval clipping and token-budget trimming
  - D1 use-case routing restrictions.
- Integration checks to execute:
  - `/ai/read/explain` and `/ai/assist/suggest` end-to-end through mediator+validator
  - Sarvam failure simulation and Azure fallback continuity
  - usage guardrail responses under request bursts.
- Shadow validation guidance:
  - run Sarvam and Azure with identical context bundles
  - compare JSON validity, validator rejection rates, and fallback frequency before wider rollout.
