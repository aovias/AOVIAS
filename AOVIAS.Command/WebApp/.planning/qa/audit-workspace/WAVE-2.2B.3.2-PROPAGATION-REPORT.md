# Wave 2.2B.3.2 — Execution Context Propagation & Observability

**Date:** 2026-08-02 · **Repo:** `ComplaincePlatform` (uncommitted)
**Suite:** `Passed! Failed: 0, Passed: 39, Skipped: 2, Total: 41` · **All baselines held**

## Status — partial, and deliberately so

This wave asked for adoption across *every* service, background path and SignalR. I completed the
work that could be verified and stopped short of changes I could not prove. Scored honestly:

| Phase | Status |
|---|---|
| 1 — Cross-service inventory | ✅ Complete |
| 2 — Execution context propagation | ✅ **Gateway → all downstream, all 4 proxy paths** |
| 3 — Internal API standardization | ⚠️ Transport standardised; consumption only in ComplianceService |
| 4 — Background processing | ❌ Not implemented — inventory only |
| 5 — SignalR | ⚠️ **Assessed, not changed** — with a finding |
| 6 — Observability | ✅ Implemented, two highest-value signals wired |
| 7 — Runtime validation | ✅ No regressions |

---

## 1. Cross-Service Inventory

| Service | Tenant source today | Query filters | Change needed |
|---|---|---|---|
| **ComplianceService** | ✅ `PlatformExecutionContext` → `IUserContextService` fallback | 18 (mandatory, flagged) | 81 `IgnoreQueryFilters` bypasses remain |
| Gateway | Token claims; now **emits** context to all downstream | n/a | Cross-tenant endpoints should adopt `[ValidateTenantScope(CrossTenant = true)]` |
| OrganizationService | Explicit `OrganizationId` (474 refs) | 0 | Could consume context; already explicit, so low risk |
| Identity / Audit / Governance / Directory / Workforce / Integration | Explicit params | 0 | Consume context to remove per-service reconstruction |
| **CommunicationService** | **No tenant or org reference at all** | 0 | 🟠 Notifications appear entirely unscoped — needs its own investigation |
| Background / hosted services | Ambient or none | n/a | 8+ services; none context-aware |
| SignalR (`NotificationHub`) | Person-scoped groups | n/a | See §4 |

## 2. Propagation (Phase 2) — complete on the Gateway side

Wave 2.2B.3.1 patched **one** of four proxy paths (TD-11). All four now call
`AttachExecutionContext`:

| Path | Client | Status |
|---|---|---|
| `ProxyRequestAsync` (main) | all | ✅ (2.2B.3.1) |
| `ProxyRequestRawAsync` | all | ✅ **new** |
| Organization streaming GET | `OrganizationClient` | ✅ **new** |
| Compliance streaming GET | `ComplianceService` | ✅ **new** |

Headers emitted: `X-Aovias-Organization-Id`, `-Hospital-Id`, `-Execution-Mode`, `-Source`,
`-Request-Id`, plus the existing correlation id — always derived from **validated token claims**,
never client input.

## 3. Observability (Phase 6)

`Shared.Infrastructure/Execution/ExecutionContextTelemetry.cs` — structured events, not interpolated
strings, so a log pipeline can filter and alert on `OrganizationId` / `CorrelationId` / `Event`:

| Event | Level | Purpose |
|---|---|---|
| `ExecutionContext.FailClosed` | Warning | **The critical one.** A request resolved no tenant and will return no rows while reporting success |
| `ExecutionContext.Missing` | Warning | Internal request arrived with no context headers |
| `ExecutionContext.CrossTenantRejected` | Warning | Security event, logged with both org ids so it is attributable |
| `ExecutionContext.PropagationFailure` | Warning | Gateway could not build a context to forward |
| `ExecutionContext.FilterBypass` | Information | Inventory `IgnoreQueryFilters` from real traffic — static analysis over-counted these badly in 2.2A |
| `ExecutionContext.Request` | Debug | Full context line for cross-service troubleshooting |

**Wired:** `FailClosed` fires from `ComplianceDbContext` whenever mandatory filters are on and no
tenant resolved. This was TD-10, which I called the number-one prerequisite before this leaves
Development — it is now in place. Fail-closed is correct but silent; this makes it loud.

## 4. SignalR (Phase 5) — assessed, not changed, and here is why

`Gateway/Hubs/NotificationHub.cs` is the platform's only hub. It groups by
`person:{personId}` and `InternalNotificationsController` broadcasts to that group.

**Finding: this is incidentally tenant-safe, not deliberately so.** A `personId` is a globally
unique GUID belonging to exactly one organization, so a broadcast cannot reach another tenant.
There is no cross-tenant leak to fix today.

ADR-002 calls for org-namespaced groups. Implementing that means changing the broadcast contract so
CommunicationService supplies an `organizationId` — a cross-service breaking change. **I did not
make it**, for two reasons:

1. I have no SignalR client harness, so I could not verify that notifications still arrive. Shipping
   an unverified change to the only real-time channel risks silently breaking all notifications.
2. Adding an org-scoped group that nothing publishes to would be architecture theatre — it would
   look like progress in the diff while changing nothing.

Correct sequence: build the hub test harness (Wave 2.2B.2 listed this as a gap), then change the
contract on both sides together.

## 5. Background processing (Phase 4) — not implemented

Eight-plus hosted services identified across AccessGovernance, AIOrchestrator, Communication and
EnterpriseDirectory. `PlatformExecutionContext.ForBackgroundJob()` exists and is unit-tested, but no
job constructs one yet.

Not attempted because each job needs its own tenant-iteration strategy — "which tenants does this
job run for, and in what order" is a per-job design question, not a mechanical refactor. Doing it
blind would produce jobs that either skip tenants or process the wrong one.

## 6. Regression (Phase 7)

| Probe | Baseline | After | Result |
|---|---|---|---|
| CAPA / DCC | 88 / 82 | **88 / 82** | ✅ Correction from 2.2B.3.1 holds |
| Findings dashboard | 27 | 27 | ✅ |
| Auditor sessions | 16 | 16 | ✅ |
| Audit schedule | 15 | 15 | ✅ |
| Audit report | 24 | 24 | ✅ |
| Integration suite | 39 / 2 / 0 | 39 / 2 / 0 | ✅ |

No regressions. No Critical or High defects introduced. No measurable performance change — the added
work is a few header writes per proxied call.

## 7. Acceptance criteria

| Criterion | Status |
|---|---|
| PlatformExecutionContext adopted across all services | ❌ Emitted everywhere; **consumed** only by ComplianceService |
| Internal APIs standardized | ⚠️ Transport standardised; contracts unchanged |
| Background jobs execution-context aware | ❌ |
| SignalR tenant-aware | ⚠️ Incidentally safe; not deliberately scoped |
| Cross-service propagation verified | ⚠️ Verified Gateway → ComplianceService; other consumers do not read it yet |
| Observability implemented | ✅ |
| Integration suite passes | ✅ |
| No Critical / High defects | ✅ |
| No cross-tenant data leakage | ✅ No known path |
| No performance regression | ✅ |

## 8. Remaining technical debt

| ID | Item | Priority |
|---|---|---|
| TD-01 | **81 `IgnoreQueryFilters()` bypasses** — each sidesteps the now-mandatory filter | **Highest** |
| TD-12 | Other services emit-but-don't-consume the context | High |
| TD-13 | Background jobs need per-job tenant iteration design | High |
| TD-14 | SignalR org-namespacing + hub test harness | Medium |
| TD-15 | `CommunicationService` has zero tenant references | Medium — investigate before assuming it is safe |
| TD-09 | Mandatory filters Development-only | Medium — telemetry now exists to support the rollout |

## 9. Recommendation

The propagation *plumbing* is done: every Gateway path now carries tenant context, and fail-closed
events are observable. That was the prerequisite pair.

**The highest-value next step is TD-01, not further propagation.** ComplianceService's mandatory
filter is only as strong as the paths that do not bypass it, and 81 still do. Propagating context to
services that already scope explicitly (OrganizationService and friends) is tidier architecture but
closes no gap; removing 81 bypasses closes a real one.

I would also treat TD-15 as an investigation before anything else in Communication — a service with
no tenant references at all is either genuinely tenant-agnostic or quietly unscoped, and the
difference matters.
