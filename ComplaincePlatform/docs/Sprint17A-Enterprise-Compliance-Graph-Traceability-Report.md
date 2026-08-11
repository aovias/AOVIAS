# Sprint 17A — Enterprise Compliance Graph & Traceability Report

## Executive Summary

Sprint 17A delivers the **Enterprise Compliance Graph** — a platform read-model projection that connects operational, governance, quality, safety, and intelligence objects across PRAMAN. This is not a new business module; it is connective tissue that answers: *What am I related to? What depends on me? What changed because of me? What will be impacted if I change?*

The graph consumes existing SSOTs (EF aggregates, FKs, cross-references, audit trails) without duplicating business logic, workflows, or ownership.

---

## Architecture

### Read-Model Projection Flow

```
Bounded Context SSOTs (EF entities, FKs, audit trails)
    ↓
EnterpriseComplianceGraphService (projection)
    ↓
Graph APIs (/graph/*)
    ↓
Frontend Compliance Graph Workspace
    ↓
Traceability · Timeline · Impact · Dependencies · Navigation
```

**No graph database.** Relationships are projected at query time from existing data, extending the Sprint 15C Documentation Intelligence pattern org-wide.

### Backend Structure

| Layer | Location |
|-------|----------|
| DTOs | `Application/DTOs/EnterpriseComplianceGraphDtos.cs` |
| Contract | `Application/Contracts/IEnterpriseComplianceGraphService.cs` |
| Service | `Application/Services/EnterpriseComplianceGraphService.cs` |
| API | `Controllers/V1/ComplianceGraphController.cs` |
| Gateway | `Gateway/Controllers/ComplianceGraphV1ProxyController.cs` |
| Copilot rule | `Application/Copilot/Rules/Graph/TraceabilityGapRule.cs` |

### Frontend Structure

| Layer | Location |
|-------|----------|
| Feature module | `WebApp/src/app/features/compliance-graph/` |
| Route | `/app/compliance-graph` |
| Shell nav | Graph Viewer, Enterprise Search, Traceability, Executive Impact, Analytics |

---

## Graph Projection

### Supported Node Types (Phase 1)

Organization-scoped resolution for: ControlledDocument, Finding, Deficiency, CAPA, Incident, ClinicalIndicator, ObjectiveElement, Evidence — extensible via `ResolveEntityAsync` probing.

Stable node IDs: `{EntityType}:{Guid}` (e.g. `Finding:abc-123`).

### Relationship Types (Phase 2)

| Type | Example |
|------|---------|
| CreatedFrom | Incident → Finding |
| GeneratedBy | Finding → CAPA |
| MappedTo | Document → Objective Element |
| MeasuredBy | Clinical Indicator → OE |
| DependsOn | Deficiency → Objective Element |
| References | Document cross-reference |
| LinkedTo | Incident → CAPA |
| RelatedTo | Incident ↔ Clinical Indicator (department) |
| AssignedTo | Deficiency → Task |

All relationships are directional with incoming/outgoing views.

---

## Business Rules

1. **Graph is read-only** — projection only; bounded contexts remain data owners
2. **Relationships are projections** — derived from FKs, promotions, cross-refs
3. **No duplicated business logic** — trend/KPI/readiness delegated to Analytics Hub SSOT
4. **Threshold breaches remain advisory** — graph recommends, never auto-remediates
5. **Timeline aggregates existing audit trails** — no duplicate history tables
6. **Benchmark/threshold history immutable** — graph reads, never writes
7. **Navigation is bidirectional** — forward and backward paths exposed
8. **Archive/supersede/close warnings** — dependency explorer shows soft blockers

---

## Timeline Model (Phase 4)

Universal timeline aggregates existing audit sources per entity type:

| Entity | Sources |
|--------|---------|
| Incident | `IncidentTimelineEntry`, `IncidentAuditTrail` |
| Finding | `FindingAuditTrail` |
| Deficiency | `DeficiencyAuditTrail` |
| Clinical Indicator | `IndicatorAuditTrail` |
| Controlled Document | `DocumentLifecycleAuditEntry` |
| All | Platform `AuditLog` by EntityId |

Entries sorted chronologically (max 50), read-only.

---

## API Endpoints

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/graph/entity/{id}` | Entity summary card |
| GET | `/graph/relationships/{id}` | Incoming/outgoing + visual graph |
| GET | `/graph/timeline/{id}` | Universal timeline |
| GET | `/graph/impact/{id}` | Impact explorer |
| GET | `/graph/dependencies/{id}` | Dependency explorer |
| GET | `/graph/search?q=` | Enterprise search |
| GET | `/graph/navigation/{id}` | Cross-module navigation paths |
| GET | `/graph/traceability/{id}` | Full traceability chain |
| GET | `/graph/executive` | Executive impact view |
| GET | `/graph/analytics` | Graph analytics metrics |
| GET | `/graph/visual?centerId=` | Relationship graph viewer data |

All proxied via Gateway `ComplianceGraphV1ProxyController`.

---

## Integrations

### Documentation (Phase 11)
- Reuses `DocumentCrossReference` and OE mapping from QDMS
- Document revision impact traverses linked OEs, findings, CAPAs

### Patient Safety (Phase 12)
- Incident promotion FKs: FindingId, DeficiencyId, CapaId
- Department-correlated clinical indicators

### Compliance Maturity (Phase 13)
- OE-centric traceability delegates to `AuditorTraceabilityService`
- Readiness score in traceability DTO

### Copilot (Phase 14)
- `TraceabilityGapRule` — advisory when documents/findings lack OE linkage
- Persona routing: `ComplianceGraph` → AccreditationCoach
- Deep link: `/app/compliance-graph/traceability`

### Analytics Hub (Phase 17)
- Graph analytics exposes relationship density, traceability completeness, domain coverage
- No duplicate KPI formulas

### External Auditor (Phase 14)
- Read-only graph APIs under existing `ComplianceScope` policy
- Auditor assessment OE traceability remains at `/auditor/traceability/{oeId}`

---

## Impact & Dependency Explorers

**Impact Explorer** groups affected assets by category from relationship traversal.

**Dependency Explorer** returns:
- `CanArchive`, `CanSupersede`, `CanClose` flags
- Blocking, downstream, upstream dependency lists
- Soft warnings — never deletes referenced assets

---

## Enterprise Search (Phase 7)

Federated search across: Documents, Findings, Deficiencies, CAPA, Incidents, Clinical Indicators, Objective Elements.

Results grouped by entity type with deep links and traceability shortcuts.

---

## Validation

- Backend builds with `EnterpriseComplianceGraphService` registered in DI
- Gateway proxy routes all graph endpoints
- Angular build succeeds
- Compliance Graph workspace at `/app/compliance-graph`
- Entity Explorer loads traceability, timeline, relationships, impact, dependencies in parallel
- Graph viewer reuses Sprint 15C canvas patterns
- Copilot `TraceabilityGapRule` registered
- No new graph DB, workflow engine, or duplicate analytics

---

## Future Extensibility

- Pluggable `IGraphEdgeProvider` per bounded context
- Materialized projection via outbox (if query performance requires)
- Additional node types: AuditSession, LessonLearned, CopilotRecommendation, Proposal
- Hospital-to-hospital graph comparison
- Graph-aware Copilot NL queries via AIOrchestrator
- Embed traceability tab in existing detail panels (Findings, Incidents, Documents)

---

## Success Criteria

✓ Enterprise Compliance Graph implemented  
✓ Universal relationships  
✓ Universal traceability  
✓ Universal timeline  
✓ Enterprise search  
✓ Impact explorer  
✓ Dependency explorer  
✓ Relationship graph viewer  
✓ Cross-module navigation  
✓ Executive impact view  
✓ Copilot graph awareness  
✓ External Auditor read-only access pattern  
✓ Analytics integration  
✓ No duplicated business logic  
