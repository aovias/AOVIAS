# Aovias.ReleaseValidation

## Enterprise Regression & Certification Platform — Architecture

**Status:** design complete, implementation not started
**Workstream:** RC3 Workstream 1 (highest priority)
**Mandate:** become the authoritative release gate. No RC2 defect may escape again undetected.

---

## 0. Design premise

RC2 produced 23 defects. Every one was found by hand. This platform's design is derived directly
from **how** they were found, because that is the only evidence available about what catches defects
in this system:

| RC2 evidence | Design consequence |
| --- | --- |
| Runtime execution found what static review missed; two confident root causes were later overturned by observation | **No mocks anywhere.** Real services, real Gateway, real SQL Server, real browser. |
| Browser execution found what API tests missed (DEF-M3-001, DEF-M6-001) | **Browser-first.** The operator surface is the system under test. |
| Reporting defects appeared only after a *complete* lifecycle ran (DEF-M6-001, DEF-M6-003) | **Journeys, not pages.** A scenario is not valid until it reaches the report. |
| Four defects sat stacked in one never-executed path (M5→M5.A) | **Fail loudly and capture everything on first failure.** Re-running to diagnose is a design failure. |
| DEF-M5-007 failed silently behind an HTTP 200; only the server log showed it | **Server logs are a first-class assertion source**, not a debugging aid. |
| DEF-M4-002 was a false positive — three adjacent numbers were all correct | **Consistency checks must encode semantics**, not just compare numbers. |
| Counters disagreed across modules (M6: 28 vs 24) | **A dedicated Consistency Engine**, because humans do this arithmetic badly. |
| Long interactive scenarios exceeded a session five times | **Scenarios must be restartable and independently executable.** |

---

## 1. Architecture

### Stack decision

| Concern | Choice | Rationale |
| --- | --- | --- |
| Browser automation | **Playwright + TypeScript** | Strongest evidence model available (trace, HAR, video, console, network in one artifact). The product UI is Angular/TS; assertions stay in the language of the system under test. |
| Test runner | **Playwright Test** | Native parallelism, retries, fixtures, sharding, HTML reporter. No second runner to maintain. |
| Environment orchestration | **Node + child_process**, consuming `Aovias.DeploymentValidation` | That workspace already owns deployment/health rules. **Consume it; do not reimplement.** |
| Database readiness | `sqlcmd` **for readiness probes only** | Never for test data — see §3. |
| Reporting | Custom generator over Playwright's JSON reporter | Playwright's HTML report is for engineers; the Certification Report is for a release board. |

**Why not .NET:** the decisive artifacts (trace, HAR, video, console) and the fastest selector
feedback loop live in Playwright TS. The Environment Manager's need to shell out to `dotnet run` is
trivial from Node; the reverse — driving a browser well from .NET — is not.

### Layered view

```
┌──────────────────────────────────────────────────────────────────────┐
│  Certification Report Generator      GO / NO-GO, coverage, evidence  │
├──────────────────────────────────────────────────────────────────────┤
│  Scenario Engine        business journeys, composable, restartable   │
│     ├── Assertion Engine      business facts, not DOM facts          │
│     └── Consistency Engine    cross-surface agreement                │
├──────────────────────────────────────────────────────────────────────┤
│  Operator Facade        what a Quality Manager can do (not pages)    │
├──────────────────────────────────────────────────────────────────────┤
│  Test Data Manager      tenants, hospitals, admins, datasets         │
├──────────────────────────────────────────────────────────────────────┤
│  Environment Manager    services, health, DB, tenant, framework      │
├──────────────────────────────────────────────────────────────────────┤
│  Evidence Collector     trace, HAR, video, console, network, SERVER  │
└──────────────────────────────────────────────────────────────────────┘
```

**The Evidence Collector spans all layers deliberately.** DEF-M5-007 proved evidence must be
gathered where the failure happens, not where the assertion fails.

---

## 2. Folder structure

```
Aovias.ReleaseValidation/
├── ARCHITECTURE.md                     ← this document
├── package.json
├── playwright.config.ts                projects, sharding, reporters, retries
├── tsconfig.json
│
├── src/
│   ├── environment/
│   │   ├── environment-manager.ts      start/stop/health/readiness/cleanup
│   │   ├── service-registry.ts         the 6 services + ports + health paths
│   │   ├── health-probe.ts             HTTP + HTTPS probes, backoff
│   │   ├── database-probe.ts           SQL Server reachability, migration state
│   │   └── diagnostics.ts              captures env state on failure
│   │
│   ├── data/
│   │   ├── test-data-manager.ts        orchestrates provisioning
│   │   ├── tenant-provisioner.ts       signup wizard: Organization→Hospital→Admin
│   │   ├── credential-vault.ts         generated creds, per-worker, never committed
│   │   ├── framework-selector.ts       framework + journey selection
│   │   └── dataset-builder.ts          named, deterministic starting states
│   │
│   ├── operator/                       ← NOT page objects
│   │   ├── operator.ts                 the facade a scenario talks to
│   │   ├── auth.operator.ts            login, logout, session
│   │   ├── context.operator.ts         hospital / framework / journey selection
│   │   ├── audit.operator.ts           schedule, open session, close
│   │   ├── finding.operator.ts         raise, review, accept, map, promote
│   │   ├── capa.operator.ts            RCA, corrective, preventive, complete
│   │   ├── evidence.operator.ts        upload, review queue, approve/reject
│   │   ├── report.operator.ts          generate, read, export
│   │   └── config.operator.ts          Configuration Center
│   │
│   ├── assertions/
│   │   ├── business-assert.ts          the assertion vocabulary (§5)
│   │   ├── consistency-engine.ts       cross-surface agreement (§6)
│   │   ├── surfaces.ts                 named readers for each surface
│   │   └── invariants.ts               always-true rules, checked every scenario
│   │
│   ├── evidence/
│   │   ├── evidence-collector.ts       per-step and per-failure capture
│   │   ├── server-log-reader.ts        tails ComplianceService + Gateway logs
│   │   └── artifact-store.ts           run/scenario/step directory layout
│   │
│   ├── certification/
│   │   ├── report-generator.ts         GO / NO-GO document
│   │   ├── coverage-model.ts           capability → scenario mapping
│   │   └── defect-register.ts          failures → defect entries
│   │
│   └── scenarios/
│       ├── scenario.ts                 base type: steps, pre/post, restart
│       └── steps/                      reusable journey fragments
│
├── scenarios/                          ← executable specs
│   ├── s01-internal-audit.spec.ts      Start → Close → Report
│   ├── s02-finding-lifecycle.spec.ts
│   ├── s03-capa-lifecycle.spec.ts
│   ├── s04-evidence-lifecycle.spec.ts
│   ├── s05-configuration-center.spec.ts
│   ├── s06-authentication.spec.ts
│   ├── s07-authorization.spec.ts
│   └── s08-reporting-consistency.spec.ts
│
├── regression/                         ← RC2 defect library (§9)
│   ├── def-cfg-001.tenant-config-leak.spec.ts
│   ├── def-rc2w7-001.workflow-monotonicity.spec.ts
│   ├── def-rc2s1-001.auth-renewal.spec.ts
│   ├── def-m3-001.post-creation-mapping.spec.ts
│   ├── def-m5-001.evidence-close-deadlock.spec.ts
│   ├── def-m5-005.projection-identity.spec.ts
│   ├── def-m5-007.projection-di.spec.ts
│   ├── def-m6-001.finding-traceability.spec.ts
│   └── def-m6-003.report-strengths.spec.ts
│
└── artifacts/                          gitignored; run output
    └── <runId>/<scenario>/<step>/
```

---

## 3. Test Data Manager

**Rule: no SQL inside tests. Ever.** Provisioning goes through supported application paths only —
the same signup wizard a hospital uses.

```
TenantProvisioner.createTenant()
  └─ /signup
     ├─ Step 1  Organization  name, type, address
     ├─ Step 2  Hospital
     └─ Step 3  Admin         generated email + generated password
  → CredentialVault.store(workerId, creds)
```

**Two constraints that must be designed around, not discovered later:**

1. **Credentials are generated, never human.** Format `rv-<runId>-<worker>@aovias.test` with a
   generated password held only in the run's vault, never committed, never logged. This also removes
   the human-in-the-loop that blocked the RC2 Final Certification.
2. **Tenant-per-worker.** Each Playwright worker provisions its own tenant. This is what makes
   parallel execution safe *and* doubles as the multi-tenancy isolation test (§7).

**Cleanup:** the product exposes no tenant deletion. Until it does, cleanup is **environment reset**
(database restore from a baseline snapshot taken by the Environment Manager), not per-test deletion.
This is recorded as a known constraint, not designed around silently.

---

## 4. Scenario model

A scenario is a **business journey**, expressed as named steps with explicit state.

```ts
export const InternalAudit = scenario({
  id: 'S01',
  title: 'Complete Internal Audit — start to certified report',
  capabilities: ['audit-scheduling', 'session-lifecycle', 'finding-lifecycle',
                 'compliance-mapping', 'capa-lifecycle', 'evidence-lifecycle', 'reporting'],

  precondition: env => env.servicesHealthy() && env.tenantReady(),

  steps: [
    step('schedule-audit',      op => op.audit.schedule({ type: 'Internal', scope: 'Quality Governance' })),
    step('open-session',        op => op.audit.openSession()),
    step('raise-finding',       op => op.finding.raise({ severity: 'Major', clause: 'PSQ.1.b' })),
    step('review-accept',       op => op.finding.review().accept()),
    step('map-objective',       op => op.finding.mapObjectiveElement('PSQ.1.b')),
    step('promote',             op => op.finding.promote()),
    step('author-capa',         op => op.capa.authorCorrective().authorPreventive().complete()),
    step('upload-evidence',     op => op.evidence.upload(fixture('verification-report'))),
    step('approve-evidence',    op => op.evidence.approve()),
    step('close-deficiency',    op => op.capa.awaitClosure()),
    step('close-audit',         op => op.audit.close()),
    step('generate-report',     op => op.report.generate()),
  ],

  postcondition: [ consistency.fullChain(), invariants.all() ],
});
```

**Properties, each traceable to an RC2 lesson:**

- **Deterministic** — no reliance on seeded data; every object created by the scenario.
- **Restartable** — each step records its output; a run resumes at the last completed step. *(Five
  RC2 waves died mid-journey; this is the direct remedy.)*
- **Independently executable** — S02–S08 provision their own preconditions rather than depending on
  S01 having run.
- **Capability-tagged** — feeds the coverage model automatically (§8).

---

## 5. Assertion model

Assertions are stated in **business language** and read through the Operator facade. No test contains
a selector.

```ts
await expectBusiness(op)
  .finding('FND-*').toExist()
  .and.toHaveStatus('Accepted')
  .and.toBeMappedTo('PSQ.1.b')
  .and.toAppearInSession(sessionId)
  .and.toAppearInReport();

await expectBusiness(op)
  .workflow().toHaveAdvancedTo('CAPA')
  .and.toBeMonotonic();                    // DEF-RC2W7-001, permanently

await expectBusiness(op)
  .dashboard('findings').counter('PROMOTED').toHaveIncreasedBy(1)
  .and.allCountersBalanced();
```

### Invariants — checked after **every** scenario, not just relevant ones

| Invariant | Prevents recurrence of |
| --- | --- |
| No `[ERR]` in any service log during the run | **DEF-M5-007** (silent failure behind HTTP 200) |
| No unhandled console error in the browser | general |
| Workflow rail is monotonic | DEF-RC2W7-001 |
| Every persisted record has its tenant, journey and (where applicable) session id | **DEF-M6-001** |
| No duplicate records created by a single operator action | DEF-M4-002 class, partial-commit class |
| Every counter has a traceable source | M6 arithmetic gaps |

**The server-log invariant is the single most important assertion in the platform.** It is the one
that would have caught the M5 cascade on day one.

---

## 6. Consistency Engine

Compares the **same business fact** across every surface that reports it. This is the direct answer
to RC2's arithmetic defects — and to its one false positive.

```ts
consistency.fact('promoted-findings')
  .from.dashboard('findings', 'PROMOTED')
  .from.dashboard('capa', 'AUDIT-ORIGIN DEFICIENCIES')
  .from.report('rollup', 'TotalFindings')
  .from.session('IA-*', 'Findings')
  .mustAgree();
```

**Semantics are declared, not assumed.** M4.1 proved three adjacent numbers can all be correct while
appearing to disagree. Every fact declares its *measure kind*:

```ts
consistency.fact('evidence-on-oe')
  .measure('ledger')                        // all links, any status
  .from.deficiencyPanel('Evidence Count')
  .and.separately.fact('evidence-awaiting-review')
  .measure('work-queue')                    // pending only
  .from.reviewQueue('count')
  .mustNotBeComparedTo('evidence-on-oe');   // encodes the M4.1 finding permanently
```

A mismatch produces a **defect entry with both source values and the query path**, not a bare
assertion failure — because RC2 showed that the diagnosis, not the detection, is the expensive part.

---

## 7. Evidence model

Captured **always** for trace-level artifacts, **fully** on failure.

| Artifact | When | Source |
| --- | --- | --- |
| Playwright trace | always, retained on failure | Playwright |
| Screenshot | every step boundary + on failure | Playwright |
| Video | on failure | Playwright |
| HAR (network) | always | Playwright |
| Browser console | always | Playwright |
| **Server logs** | **always, per service, time-sliced to the step** | ComplianceService + Gateway stdout |
| Step timings | always | Scenario Engine |
| Environment diagnostics | on failure | Environment Manager |
| Database state summary | on failure only, read-only | Database probe |

```
artifacts/<runId>/<scenario>/<step>/
    screenshot.png  trace.zip  video.webm  network.har
    console.log  server-compliance.log  server-gateway.log  timings.json
```

**Time-slicing server logs to the step** is what turns "something failed" into "this failed here" —
the capability that took M5.A three restarts to achieve manually.

---

## 8. Certification model

The Certification Report is generated for a **release board**, not for engineers.

```
Run 2026-08-08T09:14Z · commit a1b2c3d · environment: local-clean

CERTIFICATION OUTCOME .................... NO GO
  Blocking defects ....................... 1
  Scenarios ......................... 6 / 8 passed
  RC2 regressions ................. 9 / 9 held
  Invariant violations ................... 1

COVERAGE
  Finding lifecycle ............ Fully validated   S01, S02, DEF-M3-001
  CAPA lifecycle ............... Fully validated   S01, S03
  Authorization ................ Not validated     (no passing scenario)

DEFECTS
  RV-001  Critical  Report lists weakness under Strengths
          Scenario S01 step generate-report
          Expected: observation [Suggestion] under Observations
          Observed: text under ## Strengths
          Evidence: artifacts/<run>/s01/generate-report/
```

**Coverage is derived from capability tags on passing scenarios only.** A scenario that fails
contributes zero coverage — this prevents the RC2 pattern of counting partially-observed capability
as validated.

---

## 9. RC2 Regression Library

Every **verified** RC2 defect becomes a permanent, named regression. A defect enters the library only
if it was reproduced and fixed with evidence — the withdrawn ones (DEF-M4-002, the login false
positive) are recorded as **anti-regressions**: tests asserting the *correct* behaviour that was once
mistaken for a defect.

| Regression | Asserts |
| --- | --- |
| DEF-CFG-001 | Configuration does not survive logout or hospital switch |
| DEF-RC2W7-001 | Workflow rail is monotonic; no stage complete before its predecessor |
| DEF-RC2S1-001 | Expired access token triggers renewal, not logout *(closes RC2's "not demonstrated")* |
| DEF-M3-001 | An accepted finding can be mapped after creation; locked once promoted |
| DEF-M5-001 | Evidence upload does not attempt closure of an unverified deficiency |
| DEF-M5-005 | Multiple projection updates in one unit of work do not collide |
| DEF-M5-007 | Dashboard projection completes; **no `[ERR]` behind a 200** |
| DEF-M6-001 | A finding raised in a session carries session **and** journey ids |
| DEF-M6-003 | No observation appears under a report section contradicting its classification |
| **Anti-regression M4.1** | Ledger count and work-queue count may legitimately differ |

---

## 10. CI/CD, parallelism, maintenance, Definition of Done

### CI/CD integration

```
stage: build          → dotnet build + ng build
stage: unit           → ng test (414 tests, existing)
stage: environment    → EnvironmentManager.up()  services + SQL + migrations + baseline snapshot
stage: regression     → regression/**  (fast, parallel, tenant-per-worker)   ← gate
stage: scenarios      → scenarios/**   (S01 serial-ish, S02-S08 parallel)    ← gate
stage: certify        → report-generator → GO / NO-GO artifact
stage: teardown       → snapshot restore + artifact upload
```

**Gate policy:** regression failure blocks merge; scenario failure blocks release. Both publish the
certification artifact regardless of outcome.

### Parallel execution

- **Tenant-per-worker** is the isolation primitive. Workers never share a tenant.
- S01 (full audit) runs in its own worker; S02–S08 shard across the rest.
- The regression library is fully parallel — each test provisions what it needs.
- **Parallel execution doubles as the multi-tenancy test:** N workers operating N tenants
  concurrently is precisely the adversarial cross-tenant condition RC2 never ran. Any leak surfaces
  as a cross-worker assertion failure.

### Maintenance strategy

| Concern | Policy |
| --- | --- |
| Selectors | `data-testid` only. Any test containing a CSS/text selector fails review. Adding testids to the product is **allowed under the freeze** (observability/test automation). |
| Ownership | Each scenario has a named owner; each regression is owned by whoever fixed the defect. |
| Flake policy | **Zero tolerance.** A test failing intermittently is quarantined within 24h and either fixed or deleted within a week. A quarantined test does not count toward coverage. |
| Retries | `retries: 0` locally, `retries: 1` in CI **for infrastructure faults only** — a business assertion that passes on retry is treated as a defect, not a flake. |
| Drift | Scenarios are reviewed whenever the journey they model changes; the capability tag is the link. |

### Definition of Done — Workstream 1

1. `EnvironmentManager.up()` brings the full stack to healthy from cold, and proves it.
2. `TestDataManager` provisions a clean tenant + admin + framework + journey **with no human step
   and no SQL**.
3. **Scenario 01 executes from browser launch to certification report, unattended, twice in a row,
   with identical outcome.**
4. All nine RC2 regressions implemented and passing.
5. The server-log invariant is active on every scenario.
6. Certification report generates GO / NO-GO with coverage and evidence links.
7. Runs green in CI on a clean machine.
8. Zero quarantined tests.

---

## Implementation sequence

| Order | Item | Why first |
| --- | --- | --- |
| 1 | Environment Manager + health probes | Everything depends on it; cheapest to prove |
| 2 | Evidence Collector incl. **server-log reader** | Must exist before the first failure, not after |
| 3 | Operator facade — auth + context | Every scenario's first two steps |
| 4 | Test Data Manager — tenant provisioning | Unblocks clean-tenant execution, RC2's largest gap |
| 5 | **Scenario 01**, step by step | Forces every layer into existence against a real journey |
| 6 | Assertion + Consistency engines | Grown from S01's real needs rather than speculated |
| 7 | Certification report generator | |
| 8 | RC2 regression library | Fast once the facade exists |
| 9 | S02–S08 | Reuse S01's steps |
| 10 | CI integration + parallelism | Last, once green locally |

---

## Status and honest scoping

**This document is the design. No code has been written.**

Items 1–5 above constitute a substantial build — the Environment Manager alone must start six
services, probe two protocols, verify migration state and snapshot a database. I am flagging this
rather than starting it thinly: the programme's recurring failure mode has been beginning long work
that could not be finished in one session, and this platform is the wrong place to repeat it.

**Recommended next session:** implement items 1–3 (Environment Manager, Evidence Collector, auth +
context operator) and prove them against the running stack. That is a coherent, demonstrable unit and
the foundation everything else sits on.

**One decision needed before implementation:** adding `data-testid` attributes to the Angular
product. It is permitted under the freeze as test automation and it is the difference between a
maintainable suite and a brittle one — but it touches product source, so I want it explicitly
sanctioned rather than assumed.
