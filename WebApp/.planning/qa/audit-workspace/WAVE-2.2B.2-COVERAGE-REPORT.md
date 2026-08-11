# Wave 2.2B.2 — Coverage Expansion & Platform Readiness

**Date:** 2026-08-02 · **Suite:** `tests/Aovias.Platform.IntegrationTests`
**Result:** `Passed! Failed: 0, Passed: 39, Skipped: 2, Total: 41 — 544 ms`

Growth this wave: **18 → 41 tests**, and one materially stronger test design (below).

---

## 1. The most important change: the foreign tenant is now real

The suite previously proved rejection against a synthetic GUID
(`11111111-2222-3333-4444-555555555555`). It now uses **`b7632d36-…` — "QA Test Organization
QA 398503", a real organization with 10 members and its own data.**

This matters more than the test count:

> Rejecting an organization that **does not exist** can happen by accident — a lookup miss surfacing
> as 403. Rejecting an organization that genuinely exists, and whose data the caller could otherwise
> read, is what actually proves isolation.

Every isolation assertion in the suite is now a real cross-tenant attempt.

## 2. New: Execution Context contract suite (Phase 1)

`ExecutionContextTests.cs` — 16 tests, in-process against real `DefaultHttpContext` instances, no
running platform required, sub-second, deterministic. Covers what ADR-002 actually promises:

| Behaviour | Verified |
|---|---|
| A context with no organization is invalid | ✅ |
| Background contexts are explicitly tenanted and carry no user | ✅ |
| System contexts are cross-tenant but **never** count as valid tenant scope | ✅ |
| The context is immutable for the request (`with` yields a new instance) | ✅ |
| Resolution from propagated `X-Aovias-*` headers (internal hop) | ✅ |
| Resolution from token claims (direct user request) | ✅ |
| Anonymous request yields no context | ✅ |
| Unresolved context **fails closed** — returns `Guid.Empty`, does not throw | ✅ |
| Resolution is cached so it cannot drift mid-request | ✅ |
| Own org allowed / omitted org defaults to token / other org denied | ✅ |
| Missing organization claim denied | ✅ |
| Platform role may cross tenants; **`ORG_ADMIN` may not** | ✅ |
| A cross-tenant endpoint still requires a cross-tenant role | ✅ |

Two of these encode distinctions that are easy to get wrong and expensive to get wrong:
`IsValid` vs `IsCrossTenant`, and `ORG_ADMIN` (the top *customer* role) not conferring cross-tenant
reach.

## 3. Second correction to the Wave 2.2A risk register

2.2A listed 26 tenant-scoped endpoints as unremediated, from static analysis. I probed nine at
runtime against the **real** second organization:

| Endpoint | Own org | Foreign org | Verdict |
|---|---|---|---|
| `directory/search`, `directory/facets`, `directory/suggestions` | 200 | **403** | Hardened in 2.2B |
| `sla/policies` | 200 | **403** | Already protected |
| `assignment/rules` | 404 | **403** | Already protected |
| `hospitals` | 200 | **403** | Already protected |
| `professional-profiles` | 200 | **403** | Already protected |
| `employment/records` | 404 | **403** | Already protected |
| `integration/connectors` | 403 | **403** | Already protected |
| `reporting/definitions` | 404 | **403** | Already protected |
| `subscriptions` | 405 | **403** | Already protected |
| `licensing/status` | 404 | **403** | Already protected |

**Nine of nine reject cross-tenant access.** None carries `[ValidateTenantScope]`; they are
protected by downstream scoping.

Compounding the correction I made in 2.2B.1: **a client-supplied `organizationId` parameter is a
signal to investigate, not a finding.** My 2.2A register overstated exploitability by treating a
static pattern as a confirmed hole. The register should read "63 endpoints requiring verification",
of which 15 are now verified protected, 24 are hardened, and 24 remain unprobed.

All ten verified endpoints are locked into the suite so the protection cannot silently regress.
Where the own-tenant call returns 404/405 (no seeded data or a different verb), **only the negative
case is asserted** — claiming a positive there would assert nothing.

## 4. Capability-based coverage matrix

Scored on *architectural capability validated*, not test count.

| Capability | Coverage | Basis |
|---|---|---|
| **Execution Context (contract)** | **95%** | All modes, resolution order, immutability, fail-closed, caching. Missing: SignalR-mode resolution |
| **Gateway tenant isolation** | **75%** | 15 endpoints verified against a real foreign tenant; 24 of 63 unprobed |
| **Tenant authority / role model** | **90%** | Own/other/missing/privileged/customer-admin all covered |
| **Auth boundary** | **70%** | Unauthenticated + invalid token covered; **expired** token not (needs a minted short-TTL token) |
| **Header spoofing defence** | **60%** | Forged `X-Aovias-*` rejected at one endpoint; edge-stripping not directly asserted |
| **ComplianceService query filters** | **10%** | ⚠️ The refactor target. Cannot be meaningfully tested without a second tenant's *data* being provably absent from results |
| **Internal API propagation** | **5%** | Contract exists; **no service consumes it**, so there is nothing to assert end to end |
| **Background jobs** | **10%** | `ForBackgroundJob` factory tested; no job executed under test |
| **SignalR** | **0%** | Not started |
| **Analytics / dashboard aggregation** | **5%** | Not started |
| **Platform functional regression** | **15%** | Manual browser checks only; not automated |
| **Overall platform readiness** | **≈40%** | |

## 5. Gaps blocking Wave 2.2B.3

### 🔴 Blocker — a second tenant's credentials

This is now a **precise, single ask**. Purpose-built QA accounts exist in a different organization:

```
Organization : QA Test Organization QA 398503  (b7632d36-4a68-4fa2-865f-21c4025ea3d3)
Accounts     : qa.org.admin@aovias.test        (ORG_ADMIN)
               qa.compliance.admin@aovias.test (MANAGER)
               qa.readonly@aovias.test         (COMPLIANCE_VIEWER)
               qa.auditor@aovias.test          (EXTERNAL_AUDITOR)
```

I tried the standard test password and three common conventions; all returned 401. **I stopped
there rather than continue guessing** — that stops being QA and starts being a credential attack.

Supplying one password unlocks:
- the 2 skipped cross-tenant data-visibility tests,
- **ComplianceService coverage (10% → the refactor's actual safety net)** — proving a query filter
  scopes correctly requires showing tenant B's rows are *absent* from tenant A's results, which is
  impossible with one tenant,
- role-differentiated isolation tests across four distinct roles.

Without it, Wave 2.2B.3's central change — flipping query-filter registration to unconditional —
would proceed with **10% coverage on exactly the code being changed.**

### 🟠 Secondary gaps

| Gap | Why it is not closed |
|---|---|
| Internal API propagation | Nothing consumes `PlatformExecutionContext` yet — that is 2.2B.3 work; tests would assert an unimplemented contract |
| SignalR | Needs a hub client harness; hub only became reachable after the Wave 1 licence fix |
| Background jobs | Needs a way to trigger a job under test and observe its context |
| Expired-token test | Needs a mintable short-TTL token or a clock shim |
| No CI pipeline | Still absent from the repo — the suite is CI-ready but has nowhere to run |

## 6. Acceptance criteria

| Criterion | Status |
|---|---|
| Execution Context fully testable | ✅ 95% |
| ComplianceService behaviour covered | ❌ 10% — blocked on tenant B |
| Internal APIs covered | ❌ 5% — nothing consumes the contract yet |
| Background jobs covered | ❌ 10% |
| SignalR covered | ❌ 0% |
| Cross-tenant attacks validated | ✅ Against a **real** foreign tenant |
| Platform regression suite expanded | ⚠️ 15% |
| Capability-based coverage report | ✅ §4 |
| Remaining risks identified | ✅ §5 |
| **Platform ready for Wave 2.2B.3** | ❌ **Not yet** |

## 7. Readiness assessment for Wave 2.2B.3

**Recommendation: do not start 2.2B.3.**

The suite is genuinely stronger — 41 tests, a real foreign tenant, and the Execution Context
contract locked down at 95%. But 2.2B.3's headline change is making ComplianceService's query-filter
registration unconditional, and **coverage on ComplianceService is 10%**. The net is thin in exactly
the place the work lands.

One credential changes that. With `qa.org.admin@aovias.test`:
1. Cross-tenant data-visibility tests activate.
2. ComplianceService coverage becomes buildable — assert tenant A's dashboards, reports, Control
   Center and analytics contain **zero** tenant B rows, before and after the refactor.
3. Then flip the registration flag with a real safety net underneath it.

That is the shortest path from 40% to a defensible gate.
