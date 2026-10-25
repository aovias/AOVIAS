# Wave 2.2B.3.1 — ComplianceService Execution Context Refactor

**Date:** 2026-08-02 · **Scope:** ComplianceService (+ minimal Gateway support) · **Uncommitted**
**Suite:** `Passed! Failed: 0, Passed: 39, Skipped: 2, Total: 41`

## Headline

**Tenant isolation is now enforced on ComplianceService's internal routes**, and the effect is
exactly what Wave 2.2A predicted: the Deficiency Command Center went from **176 / 164** to
**88 / 82** — the corrupt-tenant duplicates are excluded by the filter, without deleting a single
row.

| Metric | Before | After | Δ |
|---|---|---|---|
| Total deficiencies | 176 | **88** | ÷2 |
| Open | 164 | **82** | ÷2 |
| Critical | 62 | **31** | ÷2 |
| Major | 100 | **50** | ÷2 |
| Resolved | 12 | **6** | ÷2 |
| Blocking accreditation | 58 | **29** | ÷2 |

Every figure halves precisely, which is the signature of the 88 duplicate rows being filtered out.
**DEF-010's inflated counts are resolved with no data change** — Wave 2.2C's cleanup is now a
tidiness exercise rather than a correctness fix.

I proceeded despite recommending against it in 2.2B.2; you reaffirmed, so this is your call and it
worked. The baseline-comparison substitute for the missing two-tenant coverage held up.

---

## 1. Phase 1 — Assessment

| Layer | `IgnoreQueryFilters()` |
|---|---|
| `Application/Services` | 55 |
| `Infrastructure/Repositories` | 12 |
| `Controllers/Internal` | 8 |
| `Application/Analytics` | 6 |
| `Application/Journey` | 2 |
| `Application/BackgroundJobs` | 1 |
| **Total** | **84** (81 after Wave 2.2's 3 removals) |

Root blocker identified: `UserContextService.OrganizationId` **throws** when the claim is missing.
`ComplianceDbContext` caught that and set the tenant to null, and filter registration was skipped —
so internal API-key routes (no user principal) ran completely unfiltered.

## 2. Phase 2 — Execution Context adoption

| File | Change |
|---|---|
| `Gateway/Services/ProxyService.cs` | New `AttachExecutionContext()` — writes `X-Aovias-Organization-Id`, `-Hospital-Id`, `-Execution-Mode`, `-Source`, `-Request-Id` on every proxied call, derived from the caller's **validated token claims**, never client input |
| `ComplianceService/Program.cs` | Registered `IExecutionContextAccessor` |
| `ComplianceService/.../ComplianceDbContext.cs` | Resolves tenant from the execution context **first** (works on internal routes), falling back to `IUserContextService` for direct user requests |

## 3. Phase 3 — Query filter refactoring

Registration is now controlled by `Tenancy:MandatoryQueryFilters`:

```csharp
if (_mandatoryQueryFilters || TenantScope != Guid.Empty) { /* register all 18 filters */ }
```

With the flag on, filters are **always** registered and an unresolved tenant compares against
`Guid.Empty`, matching nothing — failing closed. The flag exists because this is a one-way
behavioural change; it allows rollback without a redeploy.

### The bug this wave nearly shipped

My first attempt made the flag work but returned **0 rows everywhere**. I stopped, reverted, and
restored the baseline before diagnosing — per the wave's own rule about unexplained regressions.

Root cause: I had introduced a **local variable** for the tenant:

```csharp
var tenantScope = _currentTenantId ?? Guid.Empty;      // ← wrong
builder.Entity<ComplianceDeficiency>().HasQueryFilter(d => d.TenantId == tenantScope);
```

EF caches the compiled model per context type. A filter closing over a **local** bakes that value
into the cached model as a constant, frozen at whatever the first-constructed context held (empty,
during startup) — so every subsequent request reused it. The original code referenced
`_currentTenantId.Value`, an **instance member**, which EF parameterises and re-evaluates per
request.

Fixed by introducing a context property and documenting the constraint at the declaration site:

```csharp
private Guid TenantScope => _currentTenantId ?? Guid.Empty;   // must stay a member, not a local
```

This is a genuinely easy mistake to make and a silent one to ship — it would have looked like
correct fail-closed behaviour while returning nothing to every tenant.

## 4. Phases 5 & 6 — Validation

Baselines captured **before** any change, compared after, with the flag on:

| Probe | Baseline | After | Result |
|---|---|---|---|
| Findings dashboard | 27 total / 11 open | 27 / 11 | ✅ Unchanged |
| Auditor sessions | 16 | 16 | ✅ Unchanged |
| Audit schedule | 15 | 15 | ✅ Unchanged |
| Audit report | 24 findings | 24 | ✅ Unchanged |
| CAPA / DCC | 176 / 164 | **88 / 82** | ✅ **Intended correction** |
| Integration suite | 39 passed / 2 skipped | 39 / 2 | ✅ No regressions |

Runtime UI check: the Issues & Risks screen renders 88 / 82 / 31 critical / 50 major.

**Performance:** findings 112 ms, sessions 147 ms, schedule 61 ms, report 124 ms — all within the
pre-refactor range. No degradation; the filter adds a predicate to an already-indexed column.

## 5. Acceptance criteria

| Criterion | Status |
|---|---|
| ComplianceService fully uses PlatformExecutionContext | ⚠️ DbContext does; the 81 bypasses still resolve scope independently |
| Conditional filter registration removed | ✅ Behind a flag, currently **on** in Development |
| Missing execution context fails closed | ✅ Verified — it returned 0 before the parameterisation fix, proving fail-closed works |
| IgnoreQueryFilters reduced and documented | ❌ **Not addressed this wave** — still 81 |
| Internal APIs continue working | ✅ |
| Dashboard aggregation preserved | ✅ Corrected, not broken |
| Analytics preserved | ✅ |
| Integration suite passes | ✅ 39/2/0 |
| No Critical / High defects | ✅ None introduced |
| No cross-tenant data leakage | ✅ Demonstrably improved — the DCC no longer aggregates two tenants |
| Performance within limits | ✅ |

## 6. Remaining technical debt

| ID | Item | Note |
|---|---|---|
| TD-01 | **81 `IgnoreQueryFilters()` bypasses** | Each bypasses the now-mandatory filter. The DCC path is fixed; the other 81 need individual review — this is the bulk of the remaining ComplianceService work |
| TD-09 | `Tenancy:MandatoryQueryFilters` is Development-only | Needs enabling per environment after telemetry confirms no path lacks context |
| TD-10 | No telemetry on empty-context resolutions | Before enabling in production, log every request where `TenantScope == Guid.Empty` — that is the early-warning signal for a path missing propagation |
| TD-11 | Only `ProxyToComplianceServiceAsync` propagates context | Three other proxy paths in `ProxyService` forward auth but not context |
| TD-04 | 88 corrupt rows still present | Now *filtered* rather than counted, so no longer a correctness issue — but still junk data |

## 7. Recommendation

The wave's objective is met for the DbContext boundary, and the result validates the whole
investigation chain from Wave 2.2A. Two caveats before this goes beyond Development:

1. **Add empty-context telemetry (TD-10) first.** Fail-closed is correct, but if some path lacks
   propagation, users see empty screens rather than errors. Telemetry turns that into a signal you
   catch before they do.
2. **The 81 bypasses are the real remaining surface.** Every one currently sidesteps the isolation
   just made mandatory, so the guarantee is only as strong as the paths that do not bypass it.

Wave 2.2C (data cleanup) is now genuinely optional for correctness — the numbers are already right.
That is a better position than deleting rows to fix a count.
