# Phase V7 — Search & Discovery Readiness Report

## Current (Release 1)

- Static catalog search (`searchKnowledge`)
- Filters: query, kind, category, pagination
- Global enterprise search overlay links to `/resources/search?q=`

## Ready for Release 2 without architectural rewrite

| Capability | Status |
|------------|--------|
| Full-text search | Adapter-ready (`MockKnowledgeClient` / SDK) |
| Filters | Live |
| Categories | Live |
| Tags | On ContentItem model |
| Related articles | Live (`getRelatedKnowledge`) |
| AI-assisted discovery | Not implemented — catalog + search API surface preserved |

## Guidance

Do not invent a parallel search stack. Extend `features/knowledge/domain/search.ts` and the adapter boundary.
