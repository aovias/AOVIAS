# Phase 4 — Governance Search (Sprint 6)

**Sprint:** 6  
**Status:** Delivered (extends enterprise graph search)

## Facades

| Path | Behavior |
|------|----------|
| `GET /api/v1/graph/search?q=` | Documents, findings, CAPA, incidents, tasks, audits, **risks**, **committee/meeting** (via `GovernanceTraceLinks`) |
| `GET /api/v1/compliance/search?q=` | Gateway flat facade over graph search (+ optional evidence) |

## Entity types added

- `Risk` — title/description/category  
- `Committee` / `Meeting` — labels from governance trace links  

Document and incident groups were already present; Sprint 6 ensures they remain in unfiltered search for governance operators.
