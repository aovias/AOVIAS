# ADR-001: AOVIAS Command Dashboard — Read Model & Progressive Composition Architecture

| Field | Value |
|-------|-------|
| **Status** | Proposed |
| **Date** | 2026-07-09 |
| **Type** | Architecture Design (no implementation) |
| **Deciders** | Platform Architecture / Product Engineering |
| **Scope** | `/app/dashboard` (Home), `/app/control-center`, shell-adjacent dashboard consumers |

---

## 1. Problem Statement

AOVIAS Command’s post-login Home Dashboard (`/app/dashboard`) is blocked by a single aggregated API — `GET …/program-overview-snapshot` — with **~25 seconds** observed cold latency, while most other platform APIs complete in **under 100 ms**. Authentication, Gateway routing, and supporting services are healthy.

The current model couples **first paint**, **operational widgets**, **analytics**, and **live score computation** into one synchronous request/response cycle. This is misaligned with:

- Enterprise UX expectations (shell + KPIs in &lt;1 s)
- Platform growth (multi-hospital, multi-framework, AI modules)
- Existing platform patterns (Outbox, domain events, `ComplianceSnapshotProjector`, separate Command Center)

The question is not only “how to make one endpoint faster,” but **whether the Dashboard should remain a monolithic snapshot** for the next 5–10 years.

---

## 2. Constraints

| Constraint | Source |
|------------|--------|
| Compliance lifecycle owned by `ComplianceJourney` + state machine only | Architecture constitution |
| Cross-service messaging via Outbox; no direct RabbitMQ from domain services | Architecture rules |
| Gateway as BFF via HTTP + `IProxyService` | Platform pattern |
| Scores must remain canonical via `IComplianceScoreService` / aggregation | ComplianceService |
| Multi-tenant scope: organization → hospital → framework/journey | UI context model |
| Two dashboard surfaces today: Home Dashboard + Command Center | Product reality |
| No NgRx; Angular signals + root stores | Frontend stack |

---

## 3. Dashboard Purpose (Phase 1)

### 3.1 Intended role

The **Home Dashboard** (`/app/dashboard`) should primarily function as:

| Role | Weight |
|------|--------|
| **Compliance KPI landing** | Primary |
| **Executive orientation** | Primary |
| **Operational signal board** (what needs attention) | Secondary |
| **Navigation hub** to Journey, Tasks, Command Center | Secondary |

It should **not** fully subsume:

| Surface | Home | Dedicated route |
|---------|------|-----------------|
| Deep operational workspace | Partial | `/app/journey`, `/app/tasks` |
| AI decision center | Sidebar only | `/app/control-center`, Copilot |
| Full analytics | Summary only | `/app/analytics`, `/app/reports` |
| Audit workspace | Alerts only | `/app/audit` |

The **Compliance Command Center** (`/app/control-center`) is the **operational + AI command surface** — real-time ops, blocking tasks, audit readiness, intelligence panel.

**Long-term vision:** Home = *“Am I on track?”* in &lt;1 s. Command Center = *“What do I do now?”* with richer aggregation and AI.

### 3.2 Personas — first 3 seconds expectation

| Persona | First seconds need | Home widgets |
|---------|-------------------|--------------|
| **Administrator** | Org/hospital context valid; onboarding status | Header, hospital overview, framework count |
| **Compliance Officer** | Readiness %, overdue tasks, active frameworks | KPI strip, gauges, alert banner |
| **Department Head** | Department readiness | Heatmap (secondary paint) |
| **Executive** | Score, trend direction, expiries | KPI strip, gauges, milestones |
| **Auditor** | Critical alerts, audit-adjacent signals | Alerts; deep audit on CC/audit routes |

---

## 4. Widget Domain Decomposition (Phase 2)

### 4.1 Home Dashboard (`/app/dashboard`)

| Widget | Category | Data source | Owner | Refresh | Payload | First paint | Lazy? |
|--------|----------|-------------|-------|---------|---------|-------------|-------|
| Header / subtitle | Immediate ops | Snapshot summary | ComplianceService | On context change | Tiny | **Yes** | No |
| Executive KPI strip | Compliance KPIs | `summary`, `tasksSummary` | ComplianceService | 5 min / event | Small | **Yes** | No |
| Alert banner | Notifications | `alerts` | ComplianceService | Event-driven | Small | **Yes** | No |
| Gauge charts | Executive metrics | `overallScore`, completion | ComplianceService | Score events | Tiny | **Yes** | No |
| Score trend chart | Analytics & trends | `scoreTrends` | ComplianceService | Daily/hourly | Medium | No | **Yes** |
| Framework overview table | Compliance KPIs | `frameworks`, tasks by FW | ComplianceService | Event | Medium | No | **Yes** |
| Chapter breakdown chart | Analytics | `frameworks` | ComplianceService | Event | Small | No | **Yes** |
| Risk / task type charts | Operational | `tasksSummary` | ComplianceService | Task events | Small | No | **Yes** |
| Hospital overview | Executive | `hospitals` | ComplianceService + Org | Event | Medium | No | **Yes** |
| Milestones | Actionable | `milestones` | ComplianceService | Daily | Small | No | **Yes** |
| Activity stream | Notifications | `frameworks.recentActivity` | ComplianceService | Event | Medium | No | **Yes** |
| Department heatmap | Operational | `/dashboard/hospital` | ComplianceService | Task/score events | Medium | No | **Yes (idle)** |
| Sidebar compliance ring | Compliance KPIs | Snapshot summary | Shared store | Same as summary | Tiny | Shell | Background |
| Sidebar AI cards | AI insights | Snapshot summary | Shared store | Same | Tiny | Shell | Background |

### 4.2 Command Center (`/app/control-center`)

| Widget | Category | Endpoint | First paint |
|--------|----------|----------|-------------|
| AI Intelligence panel | AI insights | `/control-center/compliance/overview` (section) | After KPIs |
| Executive KPI bar | Executive metrics | overview.kpis | **Yes** |
| Risk command center | Operational | high-risk + dept risk | No |
| Action center | Actionable work | action-center | No |
| Compliance analytics | Analytics | trend, chapter, evidence | No |
| Audit readiness | Compliance KPIs | audit-readiness | No |
| Activity / alerts | Notifications | activity-stream, alerts | No |
| AI Copilot | AI decision | AIOrchestrator (on demand) | On interaction |

### 4.3 Conclusion on monolithic snapshot

A **single full snapshot endpoint is not appropriate** as the long-term primary model. It should evolve to:

1. **Summary read model** (critical path, &lt;100 ms)
2. **Section endpoints or SSE** for secondary widgets
3. **Precomputed projections** backing summary and trends

---

## 5. Composition Strategy Evaluation (Phase 3)

| Criterion | A Monolithic | B Composed | C BFF Composer | D CQRS Read Model | E Hybrid |
|-----------|-------------|------------|----------------|-------------------|----------|
| UX (first paint) | Poor | Good | Good | Excellent | **Excellent** |
| Scalability | Poor | Medium | Good | **Excellent** | **Excellent** |
| Maintainability | Simple initially | Medium | Medium | Good | **Good** |
| Complexity | Low | Medium | High | Medium | **Medium** |
| Freshness | Live (slow) | Per-widget | Per-widget | Event + TTL | **Tiered** |
| Failure isolation | None | Good | Good | Good | **Excellent** |
| Cacheability | One key | Many keys | Many keys | **Natural** | **Natural** |
| AOVIAS fit | **Current** | Partial | Gateway-ready | **Constitution-aligned** | **Recommended** |

### Recommendation: **Option E — Hybrid**

```
┌─────────────────────────────────────────────────────────────┐
│  Angular Portal (progressive shell + widget stores)         │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────┐
│  Gateway BFF                                                  │
│  GET /dashboard/summary          ← read model, <100ms         │
│  GET /dashboard/sections/{id}    ← optional parallel widgets  │
│  GET /control-center/...         ← operational aggregate      │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────┐
│  ComplianceService                                          │
│  DashboardReadModel (CQRS projection)                        │
│  ComplianceSnapshotProjector (existing)                     │
│  Widget query handlers (tasks, trends, activity)            │
└─────────────────────────────────────────────────────────────┘
```

**Rationale:**

- Platform **already has** event-driven snapshot projection (`ComplianceScoreUpdatedDomainEvent` → `ComplianceSnapshotProjector`).
- Gateway **already acts as BFF**; extend with summary + section contracts, not a new microservice initially.
- Home Dashboard needs **fast read**; Command Center needs **richer live ops** — different freshness tiers.
- Avoids big-bang rewrite: migrate monolithic snapshot → summary + sections incrementally.

---

## 6. Data Freshness & Computation (Phase 4)

| Data element | Strategy | Staleness OK | Invalidation |
|--------------|----------|--------------|--------------|
| Overall / journey scores | **Event-driven projection** → read table | 1–5 min | `ComplianceScoreUpdatedDomainEvent` |
| KPI counts (tasks open/overdue) | Incremental counter or periodic rollup | 30 s – 2 min | Task status events |
| Score trends | **Persisted daily snapshots** | 1 hour | Nightly job + score events |
| Department heatmap | On-demand or cached rollup | 2–5 min | Task/score events |
| Alerts / findings | Event projection | 30 s – 1 min | Finding/deficiency events |
| Activity stream | Append-only event log | 1–2 min | Activity events |
| Milestones / expiries | Journey projection | 5–15 min | Lifecycle events |
| AI insights | **On demand** | Session | User prompt |
| Org/hospital metadata | Cache | Session | Org admin changes |

**Rule:** Anything on the **critical path for first paint** must not require live `AggregationService.ComputeFrameworkAsync` per request.

---

## 7. Performance Budget & SLA (Phase 5)

Validated/refined targets for **Home Dashboard** (p95, warm cache, production LAN):

| Operation | Target | Notes |
|-----------|--------|-------|
| Login → Dashboard **shell** (title, nav, skeleton) | **&lt; 500 ms** | No snapshot required |
| Critical KPIs visible | **&lt; 1 s** | Summary read model only |
| Operational widgets (tasks, alerts) | **&lt; 1.5 s** | Parallel section fetches |
| Charts & trends | **&lt; 3 s** | Lazy / deferred |
| AI insights (sidebar / CC) | **&lt; 5 s** | Never block Home shell |
| Dashboard **revisit** (same context) | **&lt; 200 ms** | Client + server cache hit |
| Hospital/framework **context switch** | **&lt; 2 s** | New scope summary |

**SLA measurement:** Gateway `DashboardRequestTiming`, frontend `__AOVIAS_PERF_REPORT__`, RUM Web Vitals (LCP on KPI strip).

---

## 8. Rendering & Loading Strategy (Phase 6)

### Recommended sequence

```
T+0ms     Shell + header + empty KPI skeletons (from route activation)
T+0ms     Parallel: GET /dashboard/summary (critical)
T+100ms   Render KPI strip + gauges + alerts from summary
T+150ms   Parallel (non-blocking): trends, frameworks table, activity
T+200ms   Idle: department heatmap
T+300ms+  Charts animate in as sections arrive
Background  Sidebar AI, notification badge, 5-min refresh
```

### Failure isolation

| Failure | Behavior |
|---------|----------|
| Summary unavailable | Shell + error on KPI zone only; nav works |
| Trends section fails | KPIs visible; chart shows retry |
| Heatmap fails | Dashboard usable; heatmap placeholder |
| Full outage | Actionable error + link to Journey/Tasks |

---

## 9. Caching Architecture (Phase 7)

| Layer | Scope | TTL | Invalidation | Owner |
|-------|-------|-----|--------------|-------|
| Browser | Static assets | Long | Deploy | CDN |
| Angular signal store | tenant+hospital+framework | 2–5 min | Context change | WebApp |
| HTTP client | GET summary/sections | `Cache-Control` + ETag | Server invalidation | WebApp + Gateway |
| Gateway BFF | Proxy responses | 30 s – 2 min | Pass-through headers | Gateway |
| ComplianceService memory | Summary DTO | 1–2 min | Event bus hook | ComplianceService |
| Distributed cache (future) | Summary per scope | 2–5 min | Redis pub/sub on events | ComplianceService |
| Read model (SQL) | Projections | Until event | Domain events | ComplianceService |

**Context key:** `{tenantId}:{hospitalId}:{frameworkId}` — all layers must use the same composite key.

---

## 10. Event-Driven Feasibility (Phase 8)

**Existing foundation:**

- `ComplianceScoreUpdatedDomainEvent` → `ComplianceSnapshotProjector`
- Outbox + `OutboxDomainEventDispatcher`
- Lifecycle events (submitted, audit scheduled, renewed, etc.)

**Incremental projection candidates:**

| Event | Dashboard impact |
|-------|------------------|
| OE scored / assessment completed | Journey score, readiness % |
| Task status changed | Overdue counts, heatmap |
| Evidence uploaded | Evidence coverage (CC) |
| Deficiency identified/closed | Alerts, action center |
| Journey lifecycle transition | Milestones, status counts |
| Framework activated | Framework list, filters |

**Recommendation:** Expand projector pipeline to maintain **`DashboardSummaryProjection`** (narrow table/document) incrementally; reserve full recomputation for nightly reconciliation only.

---

## 11. Scalability (Phase 9)

| Scenario | Risk with current monolith | Mitigation in Option E |
|----------|---------------------------|------------------------|
| Hundreds of hospitals | O(n) journeys × aggregation | Org-level rollups; hospital-scoped summary |
| Thousands of concurrent logins | Thundering herd on cold snapshot | Warm cache + precompute after login |
| Multiple frameworks | Multiplies scoring work | Per-framework projection rows |
| AI insights growth | Blocks if coupled | Always async / separate service |
| Multi-region | Stale reads | Read replicas + event replication to projections |

---

## 12. Decision

**Adopt Option E — Hybrid CQRS read model with progressive BFF composition.**

### Target state (3 horizons)

| Horizon | Architecture |
|---------|--------------|
| **H1 (0–6 mo)** | Split snapshot → `summary` + `sections`; progressive UI; strengthen caches |
| **H2 (6–18 mo)** | Event-maintained summary projection; deprecate live aggregation on read path |
| **H3 (18+ mo)** | Optional dedicated Dashboard Read Service; multi-product widgets via Gateway registry |

### Trade-offs accepted

- More API contracts to version vs one fat DTO
- Eventual consistency (seconds) for some widgets vs always-live
- Projection maintenance cost vs request-time compute

### Risks

| Risk | Mitigation |
|------|------------|
| Projection drift | Nightly reconciliation job |
| Dual dashboard surfaces (Home vs CC) | Shared summary read model; CC adds ops layers |
| Over-engineering early | H1 is split API + UI only; CQRS deepens in H2 |

### Migration strategy (implementation — future sprints)

1. Define `DashboardSummaryDto` (subset of current snapshot)
2. Gateway route `GET /dashboard/summary` backed by read model when available, fallback to trimmed compute
3. Frontend: shell-first render; migrate widgets to section loaders
4. Move heatmap data into summary or dedicated section
5. Align Command Center granular endpoints with same read models
6. Deprecate full `program-overview-snapshot` for Home first paint

---

## 13. Success Criteria

After implementation (future), the team can affirm:

- Home shell + KPIs meet **&lt;1 s** p95
- Cold path no longer runs per-journey live aggregation on user request
- Revisit meets **&lt;200 ms** p95 with cache
- Widget failures do not blank the dashboard
- Architecture supports AI, multi-hospital, and new AOVIAS products without new monolithic endpoints

---

## References

- [`docs/performance/loading-opportunity-assessment.md`](../performance/loading-opportunity-assessment.md)
- [`docs/performance/dashboard-baseline-report.md`](../performance/dashboard-baseline-report.md)
- `ComplianceSnapshotProjector`, `DashboardSnapshotService`, `ControlCenterOverviewDto`
- WebApp: `dashboard.component.ts`, `control-center.facade.ts`, `program-overview-snapshot.store.ts`
