## RESEARCH COMPLETE

### Primary source

- **Gateway Knowledge Base API:** `ComplaincePlatform/docs/backend/gateway-knowledge-base-api-specification.md` (authoritative for this phase).
- **Related (already bound):** Journey module REST — `49-journey-module-api-binding` / `JourneyDataService` — loads `ObjectiveElement` for execution workspace.

### API surface (summary)

| Item | Value |
|------|--------|
| Base path | `/api/v{version}/knowledge-base` (doc uses `v1.0`; align with deployed Gateway + `environment.apiBaseUrl`) |
| Auth | JWT + `ComplianceScope` / `compliance.api` |
| Envelope | `ApiResponse<T>` — `success`, `code`, `message`, `data`, `errors`, `meta` (Angular: typically unwrapped by `api-response.interceptor` after success) |

**Endpoints most relevant to OE execution wizard content**

| Method | Path | Use for wizard |
|--------|------|----------------|
| GET | `/nodes/{id}/details` | Aggregated **`NodeDetailsDto`**: node + requirement + knowledge[] + requirementScore |
| GET | `/knowledge/node/{nodeId}` | Knowledge items only (alternative / refresh) |
| GET | `/knowledge/node/code/{nodeCode}` | Resolve by OE **code** (e.g. `PRE.1`) + optional `frameworkId` |
| GET | `/knowledge/search` | Free-text search; optional `frameworkId` |
| POST | `/ai/ask` | Guided “compliance assistant” questions (`AiAskRequestDto` / `AiAskResponseDto`) |
| POST | `/decisions` | Log structured decision (`CreateDecisionRequestDto` → `DecisionLogDto`) |
| POST | `/assessments` + PUT `.../items/{nodeId}` + GET `.../summary` | If product ties wizard completion to formal assessment (optional wave) |

**Less critical for first binding:** `GET /frameworks`, `GET /nodes` tree (unless building a KB-first navigator).

### Domain DTOs (binding targets)

- **`ComplianceNodeDto`:** `id`, `frameworkId`, `parentId`, `level`, `code`, `title`, `description`, `nodeType` (e.g. `ObjectiveElement`), `sequence`.
- **`KnowledgeItemDto`:** `explanation`, `whyItMatters`, `howToComply`, `evidenceRequired`, `auditChecklist`, risk/impact, tags, `version`.
- **`NodeDetailsDto`:** bundles node + requirement + knowledge array + score — **preferred single call** for wizard steps **Understanding** / **Compliance Guidance** / **Evidence** / **Audit** if templates map 1:1.

### Critical open question: ID correlation

- **Route param** today: `Progress/:oeGuid` — same UUID as **`ObjectiveElement.id`** from Journey APIs (Phase 49).
- Knowledge Base uses **`ComplianceNodeDto.id`** as **node** identity. **Product/backend must confirm:** `objectiveElements[].id` **equals** `nodes[].id` for the same logical OE, **or** expose a field on Journey OE (e.g. `knowledgeNodeId`) / a small Gateway join. **Do not assume** without contract — implement a **resolver strategy** (see plans): try `GET /nodes/{oeGuid}/details` first; fallback `GET /knowledge/node/code/{encodeURIComponent(oe.code)}?frameworkId=…` when 404.

### Current frontend state

- **`ObjectiveElementExecutionScreenComponent`:** loads workspace via `JourneyWorkspaceStore`, applies `oeGuid` from route, opens wizard.
- **`OeExecutionWizardComponent`:** step copy largely **static / mock-derived** from store VM — no Knowledge Base fetch yet.

### Non-goals (this phase)

- Replacing Journey session/dashboard/chapters/OE detail calls — remain on **`JourneyDataService`**.
- Implementing backend features not in the spec (e.g. new aggregate endpoints).

### Risks

- **Version string:** `v1` vs `v1.0` in URL — match Gateway deployment (same lesson as Phase 49).
- **Error codes:** `RESOURCE_NOT_FOUND`, `RESOURCE_CONFLICT` (ambiguous node code) — map to user-visible empty/error states in wizard.
- **Rate limits / latency:** AI ask and search — debounce, loading skeletons, cancel in-flight on route change.

---

## RESEARCH COMPLETE
