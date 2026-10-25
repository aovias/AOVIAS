# AI Assistant Retrieval Architecture Notes

Documenting the PRAMAN-aligned confirmation that the new NABH-aware retrieval work honors architecture boundaries, governance expectations, and user experience obligations before implementation.

## Architecture boundaries & reusable pattern alignment

- **Domain separation** is preserved: the KnowledgeBase service continues to supply content (AI domain) while the ComplianceService provides tenancy-scoped context; the AI Orchestrator sits in the AI domain and composes both sources via `IRetrievalService`. No new direct database calls are introduced, and the existing `KnowledgeBaseClient`/`ComplianceContextClient` abstractions remain untouched.
- **Reusable patterns** are reused: `CompliancePromptBuilder` still composes the prompt, the existing repositories (`IKnowledgeRepository`, `INodeRepository`) power the new search path, and the retrieval policy enforces the same ranking/token budget logic. Frontend responsibilities remain in `AiAssistantApiService`/AI components and continue hitting the canonical `ai/read/query` endpoint with framework/hospital identifiers, so no duplicate UI logic was added.

## Security, RBAC, and tenant isolation impact

- The canonical `frameworkId` and `hospitalId`, still required by `AiAssistantApiService` and validated by the `AiController` guard (`AiScope` policy), remain the only tenancy inputs. All new queries reuse the tenant-aware EF filters (`node.FrameworkId`, seeded NABH framework lookup), so cross-tenant leakage is prevented. Logging, token usage, and RBAC remain as before.

## Performance and scalability impact

- The new search path limits results to the top 10 active knowledge items, ordered by version, and adds case-insensitive filtering via indexed columns (code/title/explanation/HowToComply). This keeps token budgets manageable, reuses existing EF queries and indexes on `KnowledgeItems`/`ComplianceNodes`, and respects the retrieval policy’s context token cap. No new background jobs or caches were introduced.

## Responsive/enterprise UX impact

- The sidebar AI Copilot and other AI surface areas continue to render the orchestrator’s `complianceHint`, confidence, and source metadata without layout changes. Because the backend now surfaces NABH-focused knowledge even when a node context is absent, the UI will show richer, healthcare-aware replies for queries like “NABH” while preserving the polished fallback experience for lower-confidence queries that still respect enterprise spacing, access controls, and accessibility semantics.

## Required documentation updates

- This note plus the consolidated `ai-assistant-d0-verification-checklist` ensures the verification steps explicitly cover the new search behavior (see section 3.3). Any retrieval- or UX-related follow-up will add cross-domain notes and D0 verification novelties to the existing checklist so architecture, security, performance, and UX trade-offs remain traceable.
