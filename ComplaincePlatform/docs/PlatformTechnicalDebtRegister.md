# Platform Technical Debt Register

**PRAMAN · Sprint 13F**  
**Status:** Catalog only — no automatic deletions

---

## High

| ID | Category | Area | Description | Recommendation |
|----|----------|------|-------------|----------------|
| TD-H01 | Performance | Copilot Analytics | `GetPersonaMetricsAsync` loads persona events per persona (N+1 pattern) | Batch SQL aggregation by persona + event type |
| TD-H02 | Data | Activity Events | Client-side events (`DraftCreated`, `ProposalApproved`) not consistently recorded server-side | Wire recording in existing controllers without changing engines |
| TD-H03 | Performance | CopilotActivityService (13E) | `GetMetricsAsync` capped at 500 in-memory events | Superseded by 13F SQL aggregations; deprecate or align |
| TD-H04 | Architecture | Analytics | Two analytics surfaces: Compliance `AnalyticsHub` + Copilot analytics | Document boundaries; optional unified nav in future sprint |

## Medium

| ID | Category | Area | Description | Recommendation |
|----|----------|------|-------------|----------------|
| TD-M01 | UX | Copilot Workspace | `usageMetrics` from 13E loaded but superseded by 13F governance dashboard | Consolidate signals in store |
| TD-M02 | Data | Effectiveness Snapshots | Daily capture on trends API call only | Scheduled job for nightly snapshot |
| TD-M03 | Performance | Recommendation Refresh | Full rule-engine refresh per org on demand | Incremental refresh or persona-scoped refresh (already partial) |
| TD-M04 | UX | Sidebar AI | Generic welcome chips not persona-aware | Already partially addressed in 13E conversation injection |
| TD-M05 | Analytics | Readiness KPI | Readiness appears in Analytics Hub, Compliance Maturity, Copilot landing, Dashboard | Single SSOT display with deep-links (duplicate KPI presentation) |

## Low

| ID | Category | Area | Description | Recommendation |
|----|----------|------|-------------|----------------|
| TD-L01 | UX | Persona Header | Emoji icons instead of Material icon set | Align with design system |
| TD-L02 | Docs | Sprint Reports | Multiple sprint reports without index | Add PRAMAN-Copilot-Maturity-Index.md |
| TD-L03 | Security | Activity Events | ActorId stored as string from Guid | Consistent actor model across audit tables |
| TD-L04 | Copilot | Impact Analysis | ROI uses proposal execution notes only | Correlate with analytics readiness deltas in 14+ |

---

## Copilot-Specific Notes

- **Do not modify** Recommendation Engine, Draft Engine, Approval Framework, or Persona Router to fix debt items — extend observability and recording at API boundaries.
- All 13F metrics derive from `CopilotRecommendations`, `AiDrafts`, `CopilotActionProposals`, `CopilotActivityEvents`, `CopilotProposalAuditEvents`, and `CopilotEffectivenessSnapshots`.
