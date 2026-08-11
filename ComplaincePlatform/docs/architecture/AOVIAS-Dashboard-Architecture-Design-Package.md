# AOVIAS Command Dashboard — Architecture & Performance Design Package

**Sprint:** Architecture & Performance Design  
**Date:** 2026-07-09  
**Status:** Design complete — no implementation  
**Related ADR:** [ADR-001-dashboard-read-model-architecture.md](./ADR-001-dashboard-read-model-architecture.md)

---

## Executive Summary

The AOVIAS Command Dashboard should evolve from a **monolithic snapshot blocking first paint** to a **Hybrid (Option E) architecture**: a lightweight **CQRS read-model summary** for critical KPIs, **progressive widget loading** for analytics and operational sections, and **event-driven projections** aligned with existing platform patterns (`ComplianceSnapshotProjector`, Outbox).

**Recommended target:** Home Dashboard answers *“Am I on track?”* in under 1 second; Command Center answers *“What do I do now?”* with richer, independently loaded operational and AI surfaces.

---

## Phase 1 — Dashboard Purpose & Personas

### 1.1 Role definition

| Surface | Route | Primary role |
|---------|-------|--------------|
| **Home Dashboard** | `/app/dashboard` | Compliance KPI landing + executive orientation + navigation hub |
| **Command Center** | `/app/control-center` | Operational workspace + AI decision support |
| **Shell / Sidebar** | Global | Context, compliance ring, deferred AI cards |

The platform should treat these as **complementary**, not competing monoliths. Convergence happens at the **read-model layer**, not by merging UI routes prematurely.

### 1.2 Persona matrix (first 3 seconds)

| Persona | Mental model | Must see immediately | Can wait |
|---------|--------------|---------------------|----------|
| Administrator | “Is my org configured?” | Hospital count, context, framework activation | Heatmap, trends |
| Compliance Officer | “Are we compliant?” | Readiness %, overdue tasks, alerts | Activity stream |
| Department Head | “How is my dept?” | Overall score, dept signal | Heatmap detail |
| Executive | “Trend up or down?” | Score, completion, expiries | Chapter charts |
| Auditor | “What’s blocking audit?” | Critical alerts | Full audit workspace (other route) |

---

## Phase 2 — Widget Inventory & Domain Decomposition

### 2.1 Home Dashboard widgets

See ADR Section 4 for full table. Summary classification:

| Category | Widgets | First paint |
|----------|---------|-------------|
| **Immediate operational** | Header, alert banner | Yes |
| **Compliance KPIs** | KPI strip, gauges, framework table | KPIs yes; table deferred |
| **Executive metrics** | Hospital overview, milestones | Deferred |
| **Analytics & trends** | Score trend, chapter breakdown, risk charts | Lazy |
| **Notifications & activity** | Activity stream | Lazy |
| **Operational (deferred)** | Department heatmap | Idle callback |

### 2.2 Command Center widgets

| Category | Widgets |
|----------|---------|
| **AI insights** | Intelligence panel, Copilot |
| **Executive metrics** | KPI bar |
| **Operational** | Risk command, action center |
| **Compliance KPIs** | Audit readiness |
| **Analytics** | Trend, chapter, evidence charts |
| **Notifications** | Activity stream, smart alerts |

### 2.3 Shell consumers (cross-cutting)

| Consumer | Data | Criticality |
|----------|------|-------------|
| Sidebar compliance ring | Summary scores | Medium — defer after shell |
| Sidebar AI assistant cards | Summary counts | Low — background |
| Notification badge | Separate API | Low — deferred |

### 2.4 Endpoint model conclusion

| Current | Target |
|---------|--------|
| `GET …/program-overview-snapshot` (full) | **Deprecated for first paint** |
| — | `GET …/dashboard/summary` (read model) |
| — | `GET …/dashboard/sections/{trends\|frameworks\|activity\|heatmap}` |
| Control Center overview | Split to reuse summary + section endpoints |

---

## Phase 3 — Composition Strategy Comparison

### Option A — Monolithic Snapshot API

- **Pros:** Single contract, simple frontend
- **Cons:** 25s cold path, no failure isolation, poor cache granularity
- **Verdict:** **Retain only as transitional fallback**

### Option B — Composed Dashboard

- **Pros:** Parallel fetches, widget isolation
- **Cons:** Still computes on demand per widget unless backed by projections
- **Verdict:** **Good UI pattern; insufficient alone**

### Option C — Dashboard BFF / Composition Service

- **Pros:** Central orchestration, Gateway already fits
- **Cons:** New service overhead if separate from Gateway
- **Verdict:** **Implement in Gateway first**; extract only if multi-product widgets explode

### Option D — CQRS Read Model

- **Pros:** Sub-100ms reads, event alignment, scales with hospitals
- **Cons:** Projection complexity, eventual consistency
- **Verdict:** **Core of long-term architecture**

### Option E — Hybrid (Recommended)

Lightweight summary projection + independent widget endpoints + progressive UI + Gateway BFF.

```
Login → Shell (500ms)
     → Summary API (1s) → KPIs + gauges + alerts
     → Parallel sections (1.5–3s) → charts, tables, activity
     → Idle heatmap + background AI
```

---

## Phase 4 — Data Freshness & Computation Strategy

| Category | Compute model | Staleness | Invalidation |
|----------|---------------|-----------|--------------|
| Journey/framework scores | Event projection | 1–5 min | Score events |
| Task counts | Incremental / rollup | 30s–2 min | Task events |
| Trends | Daily snapshot + append | 1 hour | Score events + nightly |
| Heatmap | Cached rollup | 2–5 min | Task/score events |
| Alerts | Event projection | 30s–1 min | Finding events |
| Activity | Append-only log | 1–2 min | Domain activity |
| AI | On-demand inference | Session | N/A |
| Org metadata | Session cache | Until logout | Admin change |

**Principle:** First-paint data never triggers `AggregationService.ComputeFrameworkAsync` across all journeys.

---

## Phase 5 — Performance Budget & SLA

| Milestone | Target (p95) | Measurement |
|-----------|--------------|-------------|
| Login → shell | &lt; 500 ms | `PerformanceTimelineService` |
| Critical KPIs | &lt; 1 s | LCP on KPI strip |
| Operational widgets | &lt; 1.5 s | Section timing |
| Charts & trends | &lt; 3 s | Deferred marks |
| AI insights | &lt; 5 s | Never blocks shell |
| Revisit (same context) | &lt; 200 ms | Store + server cache hit |
| Context switch | &lt; 2 s | New scope summary |

These targets are **validated** against enterprise SaaS expectations and current baseline (~25s blocking snapshot).

---

## Phase 6 — Rendering & Loading Strategy

### 6.1 Immediate (T+0)

- App shell, sidebar, route title
- KPI skeleton placeholders (not full-page blocking skeleton)

### 6.2 Critical path (T+0–1s)

- `GET /dashboard/summary` only
- Render: KPI strip, gauges, alert banner

### 6.3 Progressive (T+1–3s)

- Parallel: trends, framework table, task charts, activity
- Each widget: own skeleton → data → error boundary

### 6.4 Background / idle

- Department heatmap (`requestIdleCallback`)
- Sidebar AI cards, notification badge
- Stale-while-revalidate refresh (5 min)

### 6.5 Failure isolation

Widget-level errors never replace entire dashboard with loading or error state.

---

## Phase 7 — Multi-Layer Caching Architecture

| Layer | Key | TTL | Invalidation |
|-------|-----|-----|--------------|
| Browser | Static assets | Long | Deploy hash |
| Angular store | `{org}:{hospital}:{framework}` | 2–5 min | Context change |
| HTTP | ETag / Cache-Control | Per endpoint | Server |
| Gateway | Response cache | 30s–2 min | Pass-through |
| ComplianceService IMemoryCache | Summary DTO | 1–2 min | Event hook |
| Redis (future) | Summary + sections | 2–5 min | Pub/sub |
| SQL read model | Projections | Until event | Outbox consumer |

**Context consistency:** All layers must invalidate on hospital/framework switch and tenant boundary change.

---

## Phase 8 — Event-Driven Projection Feasibility

### 8.1 Existing infrastructure

- `ComplianceScoreUpdatedDomainEvent`
- `ComplianceSnapshotProjector`
- Outbox pattern
- Lifecycle domain events

### 8.2 Proposed `DashboardSummaryProjection` fields

Maintained incrementally:

- `overallScore`, `completionRate`
- `activeFrameworkCount`, `journeyStatusCounts`
- `openTasks`, `overdueTasks`, `criticalFindings`
- `lastScoreChangeAt`, `projectionVersion`

### 8.3 Events → projection updates

| Event | Updates |
|-------|---------|
| Evidence uploaded | Evidence coverage (CC) |
| Assessment / OE scored | Journey score, readiness |
| Task status changed | Task counts, heatmap |
| Deficiency opened/closed | Alerts, risk counts |
| Journey transition | Milestones, status |
| Framework activated | Framework list |

### 8.4 Reconciliation

Nightly job compares projection vs live aggregation sample; alerts on drift &gt; threshold.

---

## Phase 9 — Scalability Assessment

| Growth vector | Current risk | Mitigation |
|---------------|-------------|------------|
| 100+ hospitals per org | O(n) journey scoring | Org rollup tables; hospital-scoped reads |
| 1000+ concurrent users | Cold snapshot stampede | Pre-warm on auth; CDN + edge cache for static |
| Multiple frameworks | Multiplied aggregation | Per-framework projection rows |
| AI module growth | Latency coupling | Always async; separate AIOrchestrator SLA |
| Multi-region | Read lag | Read replicas; projection per region |
| Future AOVIAS products | Widget explosion | Gateway widget registry + plugin sections |

---

## Phase 10 — ADR & Implementation Roadmap

### 10.1 Formal ADR

See **[ADR-001-dashboard-read-model-architecture.md](./ADR-001-dashboard-read-model-architecture.md)**.

### 10.2 Recommended roadmap

| Horizon | Deliverables | Value |
|---------|--------------|-------|
| **H1 (0–6 mo)** | Summary endpoint; progressive UI; route reuse; section APIs | 80% UX win |
| **H2 (6–18 mo)** | Event-maintained summary; deprecate live aggregation on read | Scales with growth |
| **H3 (18+ mo)** | Optional read service; multi-product widget registry | Platform extensibility |

### 10.3 Success criteria (post-implementation)

1. **What should architecture look like in 5–10 years?** Hybrid CQRS + Gateway BFF + progressive portal.
2. **What data is computed vs projected vs lazy?** See Phase 4 table.
3. **Performance targets?** See Phase 5.
4. **Greatest long-term value path?** H1 split + H2 projections — not premature microservice extraction.

---

## Appendix — Current vs Target Architecture

### Current (problematic)

```
DashboardComponent
  └─ await program-overview-snapshot (25s)
       └─ DashboardSnapshotService (live aggregation per journey)
            └─ AggregationService.ComputeFrameworkAsync × N
```

### Target

```
DashboardComponent
  ├─ shell immediate
  ├─ summary (read model, <100ms)
  ├─ parallel sections
  └─ idle heatmap

ComplianceService
  ├─ DashboardSummaryProjection (event-maintained)
  ├─ Section query handlers
  └─ ComplianceSnapshotProjector (extended)
```

---

*This document is analysis-only. No application code was modified during this sprint.*
