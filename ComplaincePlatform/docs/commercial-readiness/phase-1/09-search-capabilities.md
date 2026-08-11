# Phase 1 — Search Capabilities

**Sprint:** 5  
**Status:** Delivered

## Facade

| Layer | Path | Notes |
|-------|------|-------|
| Gateway | `GET /api/v1/compliance/search?q=&hospitalId=&status=&owner=&department=` | Unified hit list |
| ComplianceService | `GET /api/v1/graph/search?q=` | Primary engine (enterprise graph) |
| Optional | `GET /api/v1/evidence/list?search=` | Best-effort enrichment; failures ignored |

### Unified DTO

```json
{
  "query": "fire",
  "totalResults": 3,
  "source": "graph+evidence",
  "results": [
    {
      "type": "document",
      "id": "...",
      "title": "...",
      "status": "...",
      "hospitalId": "...",
      "department": null,
      "owner": null,
      "href": "/app/..."
    }
  ]
}
```

## Design choice

Graph search already covers multi-entity discovery well. The Gateway controller is a **thin facade** that:

1. Proxies graph search via `IProxyService`
2. Flattens grouped graph results into a commercial search hit shape
3. Optionally merges a short evidence-library page when cheap
4. Applies `status` / `owner` / `department` filters on the flattened list when those fields are present

Direct graph proxy remains available at `GET /api/v1/graph/search` for the Compliance Graph UI.

## Exit criteria

Clients can call one Gateway search endpoint and receive a typed hit list without coupling to graph group shapes.
