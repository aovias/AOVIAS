# Knowledge Base — Objective Element wizard (read-only binding)

This document is for **frontend binding** of Knowledge Base content into the **Objective Element execution wizard** (**read path only**). It complements the full HTTP reference in the Compliance Platform repo: `ComplaincePlatform/docs/backend/gateway-knowledge-base-api-specification.md`.

**Scope:** Populate wizard copy from **`nodeCode`** (same string family as `ObjectiveElement.code`, e.g. `AAC.1.a`, `PRE.1`). **User submissions** (evaluations, evidence, decisions, assessments, AI) use **other APIs** — not covered here.

**Principle:** Requests use **`nodeCode` in the URL path** (encoded). You do **not** need to send **node GUIDs** for these reads. GUIDs in responses may be ignored for display-only binding.

---

## Minimum endpoints (Gateway)

Base: `/api/v1.0/knowledge-base` (see main spec for auth and `ApiResponse<T>`).

| Purpose | Method | Relative path |
|--------|--------|-----------------|
| Knowledge rows by code | GET | `knowledge/node/code/{nodeCode}` |
| Full node aggregate (node + requirement + knowledge + score) | GET | `nodes/code/{nodeCode}/details` |

**Build URLs:** `encodeURIComponent(nodeCode)` for the path segment (required if the code contains `/`, spaces, or other reserved characters).

**Current backend behavior (ambiguity):** Lookup is by **`Code`** string only. If **more than one** `ComplianceNode` shares the same `Code`, the API returns **`RESOURCE_CONFLICT`** (HTTP **409**). There is **no** `frameworkId` query on these routes today. If product later needs disambiguation without GUID paths, optional `?frameworkId=` may be added in a small API follow-up.

---

## When to call which endpoint

1. **`GET .../knowledge/node/code/{nodeCode}`** — Use when you only need **`KnowledgeItemDto[]`** for wizard strings (explanation, why it matters, etc.).
2. **`GET .../nodes/code/{nodeCode}/details`** — Use when you also need **`NodeDetailsDto`**: **`requirement`**, **`requirementScore`**, and **`knowledge`** in one aggregate, plus **`node.code` / `node.title`** for headers.

You do **not** need **`GET .../knowledge/node/{nodeId}`** or **`GET .../nodes/{id}/details`** for binding if the code-based routes return enough data for your screens.

---

## Field binding matrix

### `KnowledgeItemDto` (array from list endpoint, or `nodeDetails.knowledge`)

| Wizard / UI | JSON property |
|-------------|----------------|
| Understanding / explanation | `explanation` |
| Why it matters | `whyItMatters` |
| How to comply | `howToComply` |
| Evidence expectations | `evidenceRequired` |
| Audit checklist | `auditChecklist` |
| Optional badges | `riskLevel`, `impactLevel`, `tags` |
| Pick “current” row | `isActive`, `version` |

**Multiple rows:** The API may return more than one item. Apply a **client-side product rule**, e.g. prefer **`isActive === true`**, then highest **`version`**, else first element — implement in a small Angular selector, not assumed by the backend.

### `NodeDetailsDto` (details endpoint `data`)

| Section | Use for |
|---------|---------|
| `node` | Display: `code`, `title` (strings). `id` optional to ignore. |
| `knowledge` | Same mapping as `KnowledgeItemDto` above. |
| `requirement` | Map only fields PM needs: e.g. `intent`, `measurableElements`, `classification`, `isMandatory`, … |
| `requirementScore` | Map e.g. `maxScore`, `weight`. `classificationId` is a GUID — omit in UI unless product requires it. |

---

## Error codes (typical)

| `code` | HTTP | When |
|--------|------|------|
| `VALIDATION_FAILED` | 400 | Empty or invalid `nodeCode` |
| `RESOURCE_NOT_FOUND` | 404 | No node matches the code |
| `RESOURCE_CONFLICT` | 409 | More than one node has the same `Code` (ambiguous lookup) |

---

## Implementation

Angular: `KnowledgeBaseDataService.resolveNodeDetailsForObjectiveElement`, `knowledge-base-oe-wizard.mapper.ts`, `JourneyWorkspaceStore` / `OeExecutionWizardComponent`.

---

## Document history

| Date | Change |
|------|--------|
| 2026-04-13 | Mirrored from Compliance Platform wizard binding guide |
