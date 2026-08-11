## AI Assistant Knowledge Retrieval Updates

### Architecture boundaries & reusable alignment
- The retrieval pipeline stays within the `AI` domain and reuses the shared `KnowledgeBaseClient` contract rather than introducing a new service or endpoint, preserving domain boundaries between AI, Compliance, and Knowledge Base services.
- The new `SearchAsync` contract ensures the AI orchestrator can tap existing knowledge search logic without duplicating business logic or bypassing the compliance context orchestration.
- This follows the PRAMAN preference for reuse and contract-first cross-domain access: the knowledge search happens through the well-defined knowledge base HTTP client, while AI enrichment aggregates results through existing DTOs and policy knobs.

### Security, RBAC & tenant isolation impact
- Search queries still require a canonical framework identifier; requests that lack it bail out so we do not leak cross-tenant or unauthorized knowledge.
- Searches run within the authenticated `KnowledgeBaseServiceClient` boundary, leveraging the same RBAC/tenant filters that already protect `AskAsync`.
- No new endpoints or privileges are introduced, so the RBAC surface remains unchanged and audit logging remains localized to the knowledge client layer.

### Performance & scalability impact
- Results are capped by `KnowledgeSearchResultLimit` and later trimmed by `AIRetrievalPolicy.MaxKBNodes`, keeping token usage bounded for large responses.
- `SearchAsync` reuses the existing HTTP client and serialization pipeline, so we only add a lightweight read path without additional infrastructure.
- The policy-driven budget enforcement already throttles context tokens per request, and this change merely adds higher-quality content within that budget.

### Responsive & enterprise UX impact
- Knowledge highlights populate `context.Facts["knowledgeHighlights"]` before the LLM call, so the UI can display familiar healthcare concepts (e.g., NABH) even when no tasks or findings exist, improving perceived responsiveness.
- Yesterday's empty `Facts`/`Sources` warnings are replaced with grounded references, which reduces fallback responses and maintains enterprise-grade trust.

### Documentation updates
- This document now records the knowledge retrieval enhancement and serves as the reference for the new `SearchAsync` contract.
