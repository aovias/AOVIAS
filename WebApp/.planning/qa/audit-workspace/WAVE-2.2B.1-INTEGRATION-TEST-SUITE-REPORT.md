# Wave 2.2B.1 — Multi-Tenant Integration Test Suite

**Date:** 2026-08-02 · **Project:** `tests/Aovias.Platform.IntegrationTests` (uncommitted)
**Result:** `Passed! Failed: 0, Passed: 16, Skipped: 2, Total: 18 — 483 ms`

## What was built

A real integration suite that runs against the **live platform over HTTP**. Nothing about tenant
isolation is mocked, because isolation is an emergent property of the Gateway, the token, the query
filters and the database acting together — a mocked harness would assert only that the mock behaves,
which is exactly the class of false assurance that let query filters ship silently disabling
themselves.

| File | Role |
|---|---|
| `Aovias.Platform.IntegrationTests.csproj` | xUnit 2.9.3 + `Xunit.SkippableFact`, `testsettings.json` copied to output |
| `testsettings.json` | Gateway URL, tenant A credentials, optional tenant B, foreign org id |
| `PlatformFixture.cs` | Collection fixture: authenticates, reads real claims from the JWT, exposes per-tenant `HttpClient`s |
| `JwtReader.cs` | Reads token claims (deliberately does not validate signatures — the platform is what is under test) |
| `TenantIsolationTests.cs` | The isolation battery |

### Design decisions worth noting

**Skips are reported, never silently passed.** xUnit 2.x has no dynamic skip, so
`Xunit.SkippableFact` was added to central package management. A guard that returned early would
report green when a precondition is missing — the precise failure mode this suite exists to prevent.
The two two-tenant tests currently report **Skipped**, not Passed.

**Every isolation test holds caller, endpoint and permission constant and varies only the
organization.** That is what makes a `403` attributable to the tenant boundary. It matters: the first
verification attempt in Wave 2.2B was inconclusive because a `RequirePermission` denial and a tenant
denial both surface as `403`, and the account lacked the permission.

**A control case guards against over-blocking.** `Own_organization_is_reachable` fails if isolation
starts rejecting legitimate traffic — without it, a blanket-deny bug would make every isolation test
pass.

**The fixture refuses a false positive.** If `tenantB` resolves to the *same* organization as
`tenantA`, `InitializeAsync` throws rather than running cross-tenant tests that would pass without
proving anything.

## Coverage

| Test | Cases | Result |
|---|---|---|
| `Own_organization_is_reachable` | 6 endpoints | ✅ Passed |
| `Foreign_organization_is_rejected` | 6 endpoints | ✅ Passed |
| `Omitted_organization_falls_back_to_the_token_not_to_everything` | 1 | ✅ Passed |
| `Spoofed_context_headers_from_a_client_are_not_honoured` | 1 | ✅ Passed |
| `Unauthenticated_requests_are_rejected` | 1 | ✅ Passed |
| `An_invalid_token_is_rejected` | 1 | ✅ Passed |
| `Tenants_cannot_read_each_others_data` | 1 | ⏭️ Skipped — no tenant B |
| `The_two_configured_tenants_are_genuinely_different` | 1 | ⏭️ Skipped — no tenant B |

Endpoints under test: `directory/search`, `directory/facets`, `directory/suggestions`,
`sla/policies`, `assignment/rules`, `hospitals`.

### Does the suite have teeth?

Yes, and this is verifiable rather than asserted. In Wave 2.2B I measured
`GET /api/v1/directory/facets?organizationId={foreign}` returning **200** before
`[ValidateTenantScope]` was applied, and **403** after. `Foreign_organization_is_rejected` asserts
403 — so these three directory cases **would have failed against the pre-fix build.**

## New finding — the risk register was pessimistic

Wave 2.2A flagged 26 tenant-scoped endpoints as unremediated on the basis of static analysis (they
accept `organizationId` from the query string). Probing three of them at runtime:

| Endpoint | Own org | Foreign org |
|---|---|---|
| `sla/policies` | 200 | **403** |
| `assignment/rules` | 404 | **403** |
| `hospitals` | 200 | **403** |

**They already reject cross-tenant access** via downstream scoping, despite carrying no
`[ValidateTenantScope]`. The correction matters:

> A client-supplied `organizationId` parameter is **not**, by itself, proof of exploitability.
> Wave 2.2A's "26 remaining" was a list of *candidates requiring verification*, not 26 confirmed
> holes — and I should have framed it that way.

All three are now locked into the suite so that protection cannot silently regress. **The remaining
23 still need the same runtime probe**; static analysis alone cannot classify them.

## Not delivered

Stated plainly against the acceptance criteria:

| Criterion | Status |
|---|---|
| Two organizations supported | ⚠️ Framework supports it; **no tenant B credentials available**, so 2 tests skip |
| Two hospitals supported | ❌ Not exercised |
| Execution Context verified | ⚠️ Only the client-spoofing case; `PlatformExecutionContext` is not yet consumed by any service, so propagation cannot be tested end to end |
| Tenant isolation verified | ⚠️ 6 endpoints of ~63 |
| Cross-tenant spoofing rejected | ✅ Verified, including via forged `X-Aovias-*` headers |
| Gateway validated | ⚠️ Partial |
| Background jobs validated | ❌ Not started |
| SignalR validated | ❌ Not started |
| Regression suite passes | ⚠️ This suite passes; no broad functional regression added |
| **Test suite integrated into CI** | ❌ **No CI pipeline exists in the repo** — no `.github/workflows`, only docker-compose files. Cannot integrate into something absent. |

Phases 5 (ComplianceService pre/post-refactor tests) and 6 (background processing) were not built.
Phase 5 in particular needs the two-tenant fixture to be meaningful — asserting that a query filter
scopes correctly requires a second tenant's rows to be absent from the result.

## Two blockers for full coverage

1. **A second organization's credentials.** Five distinct `TenantId` values exist in
   `ComplianceJourneys`, so real multi-tenant data is present — but I have credentials for only one.
   With a tenant B user configured in `testsettings.json`, the two skipped tests activate and Phase 5
   becomes buildable. **This is the single highest-value input you can provide.**
2. **A CI pipeline.** The suite is designed for it — deterministic, read-only against tenant data,
   sub-second, and it fails loudly when the platform is unreachable. It needs a pipeline to live in.

## Recommendation

The suite is operational and already protects six endpoints, but it is **not yet the release gate
the wave intended** — it covers a fraction of the surface and the two most important tests are
skipped for want of a credential.

Before resuming Wave 2.2B implementation:
1. Supply tenant B credentials → unlocks cross-tenant data-visibility and Phase 5.
2. Run the runtime probe across the remaining 23 candidate endpoints and add each to
   `TenantScopedEndpoints` — that converts Wave 2.2A's static list into a verified one.
3. Only then flip query-filter registration to unconditional, with the suite as the safety net.

Step 3 remains the highest-risk change in the programme, and it should not be attempted until
steps 1 and 2 give it real cover.
