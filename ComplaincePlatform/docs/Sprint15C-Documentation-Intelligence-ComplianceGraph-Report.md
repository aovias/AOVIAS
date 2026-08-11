# Sprint 15C — Documentation Intelligence & Compliance Graph Report

**Date:** June 25, 2026  
**Status:** Implemented  
**Builds on:** Sprint 15A QDMS + Sprint 15B Authoring Studio (read-model projection — no new document storage, no duplicate mappings)

---

## 1. Objective

Transform Documentation from a document authoring platform into an **intelligent compliance knowledge graph**. Every controlled document becomes a living compliance asset connected to the operational platform — framework mappings, evidence, findings, CAPAs, audits, and analytics.

**Product philosophy:** Hospitals should never ask *"Where is this SOP used?"* — PRAMAN answers automatically.

---

## 2. Architecture

### 2.1 Design Principles

| Principle | Implementation |
|-----------|----------------|
| No separate graph DB | `DocumentationIntelligenceService` projects nodes/edges from existing EF Core entities |
| No duplicate mappings | Reuses `ControlledDocument.MappedObjectiveElementId` + `DocumentCrossReference` |
| AI advisory only | `ReviewAiAsync` returns scores/suggestions — never auto-fixes or publishes |
| Read-model driven | All intelligence endpoints are query-only (no migration required) |
| Reuse bounded contexts | Joins Findings, Deficiencies, CAPAs, Tasks, AuditSessions, OE Evidence |

### 2.2 Layering

```
ComplianceService/Application/
  Contracts/IDocumentationIntelligenceService.cs
  DTOs/DocumentationIntelligenceDtos.cs
  Services/DocumentationIntelligenceService.cs   ← read-model projection

ComplianceService/Controllers/V1/DocumentationController.cs  (+ intelligence endpoints)
Gateway/Controllers/DocumentationV1ProxyController.cs

WebApp/src/app/features/documentation/
  models/intelligence.models.ts
  services/intelligence-api.service.ts
  pages/document-graph-page.component.ts
  pages/document-explorer-page.component.ts
  pages/authoring-studio-page.component.ts       (+ Intelligence tab, @ autocomplete)
  pages/documentation-dashboard-page.component.ts (+ health KPIs)
```

### 2.3 Knowledge Graph Model

```
ControlledDocument ──mapsTo──► ObjectiveElement
        │
        ├──references──► Evidence / Finding / CAPA / Policy / Audit (via DocumentCrossReference)
        └──influences──► CAPA cluster (derived from OE linkage counts)
```

Nodes carry `nodeType`, `label`, optional `route` for navigation. Edges carry `relationshipType` and `bidirectional` flag.

---

## 3. API Surface

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/documentation/graph` | Organization-wide compliance graph |
| GET | `/documentation/health-dashboard` | Documentation health KPIs |
| GET | `/documentation/intelligence/{id}` | Live document intelligence panel |
| GET | `/documentation/impact/{id}` | Compliance impact analysis |
| GET | `/documentation/dependencies/{id}` | Dependency report (archive/delete guard) |
| GET | `/documentation/relationships/{id}` | Per-document relationship graph |
| POST | `/documentation/review-ai` | AI compliance review (advisory) |
| GET | `/documentation/explorer` | Hierarchical document tree + recent/pinned |
| GET | `/documentation/search?q=` | Enterprise search across title/sections |
| GET | `/documentation/cross-reference-suggestions` | `@` autocomplete for smart cross-refs |

All routes proxied through `DocumentationV1ProxyController`.

---

## 4. Compliance Impact Analysis

`BuildImpactAsync` traverses:

- Direct OE mapping on `ControlledDocument`
- OE cross-references on `DocumentCrossReference`
- Downstream counts: Evidence, Findings, Deficiencies, active CAPAs, Tasks, Audit Sessions, acknowledgement departments

Returns `DocumentImpactSummaryDto` with publishing risk (`Low` / `Medium` / `High`) and human-readable summary:

> *"This revision impacts N Objective Element(s) and M active evidence mapping(s)."*

Displayed live in Authoring Studio **Intelligence** tab and refreshed after autosave.

---

## 5. Document Intelligence Panel

Right-side **Intelligence** tab in Authoring Studio shows:

- Document Health Score (from existing `DocumentHealthScoreDto`)
- Compliance Mapping Coverage %
- Acknowledgement %
- AI Confidence %
- Publishing risk banner + impact summary
- Strengths / Risks
- Linked assets list
- **Run AI Compliance Review** button
- Link to per-document relationship graph

---

## 6. AI Compliance Review

`ReviewAiAsync` analyzes authoring session health:

- Missing mandatory sections
- Weak compliance mapping
- Thin procedure sections
- Readability score
- Missing definitions (SOP schema)
- Review frequency (from `ControlledDocument.ReviewFrequencyMonths`)

Returns `OverallComplianceReviewScore`, strengths, risks, categorized suggestions with severity. **`IsAdvisoryOnly = true`** always.

---

## 7. Relationship Graph UI

**Routes:**

- `/app/documentation/graph` — organization graph
- `/app/documentation/graph/:id` — document-centric graph

Read-only SVG edge rendering with expandable nodes, search filter, and navigation links.

---

## 8. Document Explorer

**Route:** `/app/documentation/explorer`

- Tree: DocumentType → Department → Document
- Enterprise search (debounced, min 2 chars)
- Recent and Pinned/Effective lists

---

## 9. Smart Cross References

Typing `@` in block editor triggers `GET /documentation/cross-reference-suggestions`:

- Policies / documents
- Objective Elements
- Findings

Selection inserts `insertText` and persists bidirectional link via existing `POST /documentation/authoring/cross-references`.

---

## 10. Healthcare Block Library (Expanded)

Additional reusable blocks in Authoring Studio toolbar:

Clinical Procedure, Patient Safety, Risk Warning, NABH Requirement, JCI Requirement, WHO Guideline, Drug Safety, Equipment Maintenance, Fire Safety, Biomedical Waste, Consent, Committee Decision, Quality Indicator, Best Practice, Legal Requirement, Clinical Tip.

---

## 11. Documentation Health Dashboard

Dashboard (`/app/documentation/dashboard`) now includes compliance graph KPIs from `GET /documentation/health-dashboard`:

- Healthy Documents, Needs Review, Weak Mapping
- AI Review Pending, High Impact, Recently Modified
- Affecting Accreditation
- Executive insights (mapping coverage, review pipeline, most referenced)

Links to Explorer and Compliance Graph.

---

## 12. Dependency Analysis

`GetDependenciesAsync` returns:

- `CanArchive`, `CanSupersede`, `CanDelete` flags
- `BlockReason` when inbound cross-refs or active CAPAs exist
- `UsedBy` breakdown by category

**Business rule:** Destructive operations blocked when dependencies exist.

---

## 13. Business Rules

1. Documents remain immutable at version level (15A lifecycle unchanged)
2. No duplicate OE mappings — single SSOT on document + cross-ref table
3. AI reviews; humans approve and publish
4. Knowledge graph is read-only projection
5. Impact analysis updates on every intelligence query / post-save refresh
6. Cross-references remain bidirectional via `DocumentCrossReference`
7. Explorer reflects document type + department hierarchy
8. Dependencies prevent destructive archive/supersede when blockers present

---

## 14. Validation

| Check | Result |
|-------|--------|
| `dotnet build` ComplianceService | ✅ Pass |
| `dotnet build` Gateway | ✅ Pass |
| `ng build` WebApp | ✅ Pass |
| Database migration | Not required (read-model only) |

---

## 15. Future Roadmap

| Phase | Item |
|-------|------|
| Training linkage | Training module nodes in graph (placeholder node type ready) |
| Clinical Indicators | Indicator nodes tied to OE evidence |
| JCI Requirement blocks | Full JCI schema templates |
| Auditor Workspace | Embed relationship graph + AI review summary (read-only) |
| Analytics Hub SSOT | Deep integration with `AnalyticsAggregationService` for cross-workspace KPIs |
| Copilot Document Insights | Executive read-only insights endpoint (most risky/outdated/influential) |
| Full-text search | SQL Server full-text or Elasticsearch for large corpora |

---

## 16. Success Criteria

| Criterion | Status |
|-----------|--------|
| Compliance Knowledge Graph | ✅ |
| Compliance Impact Analysis | ✅ |
| AI Compliance Review | ✅ |
| Document Intelligence panel | ✅ |
| Relationship Graph | ✅ |
| Smart cross references | ✅ |
| Enterprise Document Explorer | ✅ |
| Healthcare Block Library | ✅ |
| Intelligent Search | ✅ |
| Dependency Analysis | ✅ |
| Documentation Health Dashboard | ✅ |
| Existing QDMS reused | ✅ |
| No duplicated business logic | ✅ |
| Gateway proxies | ✅ |

---

## 17. Product Vision

Documentation is no longer a collection of controlled files. It becomes PRAMAN's **Compliance Knowledge Graph** — an intelligent, interconnected network where every policy, SOP, manual, form, and guideline understands its operational impact across Journey, Evidence, Findings, Deficiencies, CAPA, Audits, Analytics, and Compliance Copilot.

This positions PRAMAN beyond traditional QDMS solutions by turning documentation into an active participant in maintaining hospitals in an **Always Audit Ready** state.
