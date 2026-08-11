# PRA-40 — Dashboard UI Verification

Build: TypeScript clean (`npx tsc --noEmit` → exit 0)

## Components delivered

| Component | File | Notes |
|---|---|---|
| `DashboardComponent` | `pages/dashboard.component.ts` | OnPush, loading skeleton, error/retry, snapshot-pending banner, alert banner, context-reactive refetch, 5-min auto-refresh |
| `ExecutiveSummaryComponent` | `components/executive-summary.component.ts` | 4 KPI cards: Readiness, Tasks, Overdue, Expiries |
| `GaugeChartComponent` | `components/gauge-chart.component.ts` | SVG semicircular gauge, gradient by threshold |
| `ScoreTrendChartComponent` | `components/score-trend-chart.component.ts` | SVG line chart, avg + per-framework lines, legend |
| `RiskDistributionComponent` | `components/risk-distribution.component.ts` | SVG pie chart for task priority |
| `HighRiskChaptersComponent` | `components/high-risk-chapters.component.ts` | Framework table with score bars, risk badges, task cols |
| `ChapterBreakdownComponent` | `components/chapter-breakdown.component.ts` | Vertical bar chart, color by threshold |
| `TaskTypeBreakdownComponent` | `components/task-type-breakdown.component.ts` | Type breakdown with mini progress bars |
| `HospitalOverviewComponent` | `components/hospital-overview.component.ts` | Hospital cards with journey progress rows |
| `ComplianceHeatmapComponent` | `components/compliance-heatmap.component.ts` | Framework readiness grid, 5 color bands, range summary |
| `MilestonesComponent` | `components/milestones.component.ts` | Upcoming milestones sorted by due date, urgency badges |
| `ActivityStreamComponent` | `components/activity-stream.component.ts` | Cross-framework activity feed, newest-first, type icons |

## Architecture compliance (PRAMAN Constitution)

- [x] All components standalone + OnPush (§4.1)
- [x] No API calls from templates — all via `DashboardService` → `ProgramOverviewSnapshotService` (§4.2)
- [x] All styles use CSS custom property tokens — no raw hex/rgba in feature CSS (§4.2)
- [x] `DashboardService` delegates to core shared service (reuse §1.3)
- [x] Responsive breakpoints for desktop (≥1024), tablet (768), mobile (640) (§4.3)
- [x] RBAC unchanged — reads org-scoped snapshot via existing auth interceptors (§5.1)
- [x] `dashboard/routes.ts` populated with lazy-loaded `DashboardComponent`

## Human QA checklist (pending)

- [ ] Light/dark theme parity across all 12 components
- [ ] Desktop (≥1280px): heatmap 4-col, cards 4-col
- [ ] Tablet (768–1024px): analysis grid 1-col, heatmap 3-col
- [ ] Mobile (<640px): all grids 1-col, heatmap 2-col
- [ ] Auto-refresh fires silently at 5-min mark (no spinner)
- [ ] Snapshot-pending banner appears when `isSnapshotAvailable=false`
- [ ] Alert banner appears and shows count for unacknowledged alerts
- [ ] Error state shows and Retry button reloads successfully
- [ ] Hospital/framework selector context switch triggers re-fetch
- [ ] Gateway E2E: `/dashboard/program-overview-snapshot` response renders all panels
