# Enterprise Readiness Report — Stage 6

Updates the Stage 5C.2 readiness assessment with what this wave changed.

---

## 1. Recommendation

### Conditional Go — unchanged verdict, but the blocking condition is now cleared.

Stage 5C.2 gave a Conditional Go with four conditions, of which the first was the only one on the
critical path: *"Build the Configuration Center write path. Until an administrator can save an
override, none of this capability is reachable by its intended user."*

**That condition is met.** There is a write path, and there is a screen. The remaining conditions are
verification and cleanup, not capability.

The verdict stays Conditional for one reason that has not moved: **nothing has been observed
running.** No browser session, no live backend, no administrator saving a value. That was true last
wave and it is true now, and it is the difference between "the evidence is strong at the service
boundary" and "this works".

---

## 2. Condition status since 5C.2

| # | Condition | Status |
| --- | --- | --- |
| 1 | Build the Configuration Center write path | **Done.** `saveOverride` / `restoreDefault`, `ConfigurationAdminService`, one editor page for all 48 domains. |
| 2 | Browser walkthrough | **Not done.** Same blocker: auth wall, no backend. Now also covers the Configuration Center itself. |
| 3 | Apply the seed scripts | **Not done** — deployment step. A third script is added by this wave. |
| 4 | Close DEF-CFG-004 | **Not done.** Still open; unchanged rationale. |

---

## 3. Coverage

| Measure | 5C.2 | Now |
| --- | --- | --- |
| Categories registered | 28 | **48** |
| Platform-default values | 130 | **228** |
| Categories with a consumer | 28 | **37** |
| Categories administrable | 0 *(no editor)* | **48** |
| Workflow coverage | 100% | 100% |
| Automated checks | 343 | **400** |

The row that matters most is the fourth. Last wave every category was configuration-driven and none
was configuration-*administered*. That gap is closed.

The third row is the honest counterweight: 11 of the 20 new categories are declared ahead of the
features that will read them, so "48 administrable" is not "48 behavioural".

---

## 4. Production risks

| # | Risk | Likelihood | Impact | Status |
| --- | --- | --- | --- | --- |
| 1 | Capability unreachable — no write UI | — | — | **Resolved this wave** |
| 2 | Stale `AuditType` metadata empties the session-type picker | High if script skipped | High | Open — deployment step |
| 3 | **A valid-but-wrong edit breaks a screen** | **Medium, now reachable** | Medium | **Open, and newly live** |
| 4 | Sign-in request volume | Low | Low | Slightly worse: 1 + N, N now up to 48 |
| 5 | Tenant-configurable route guards | Low | Medium | Open — still awaiting your decision |
| 6 | Stage ids not compile-checked | Low | Low | Open, tested |
| 7 | Evidence status labels not configurable | Certain | Low–Medium | Open — `EvidenceType` now exists as its home |
| 8 | **No permission guard on the Configuration Center** | Medium | **High** | **New this wave** |

### Risk 3 is now live, and I flagged it last wave as the thing to watch

Stage 5C.2 said: *"Every safety property tested so far protects against malformed configuration.
Nothing protects against coherent but wrong configuration, and a write UI makes that reachable."*

The write UI now exists. Malformed input is rejected — JSON is validated before Save enables, and a
malformed workflow definition is rejected wholesale in favour of the seed. But an administrator can
still save something valid and wrong: set every finding status to `selectableWhenFiltering: false`
and the filter empties; set `minScore` on every risk band above the maximum score and nothing bands.

A "this will leave no selectable values" warning at save time would cover most of it. That is the
first thing I would build next.

### Risk 8 is new and I would not ship without it

The Configuration Center route inherits the Administration shell's guarding and adds none of its own.
Every other sensitive route in this codebase carries an explicit guard —
`permissionGuard('Compliance.Standard.View')` on access governance, `auditSurfaceGuard` on the
auditor surfaces. This page writes tenant-wide business rules and should carry the strictest of them.
It is a one-line change; I have not made it because choosing *which* permission is a decision about
your role model, not a technical one.

---

## 5. Technical debt register

| Id | Severity | Item | Change |
| --- | --- | --- | --- |
| DEF-CFG-004 | Medium | Form defaults bypass configuration | Unchanged, open |
| DEBT-CFG-005 | Low | `statusTone` substring matching | Unchanged |
| DEBT-CFG-006 | Low | `timelineItemClass` layer map | Unchanged |
| DEBT-CFG-007 | Medium | Evidence status labels hardcoded | Home now exists (`EvidenceType`) |
| DEBT-CFG-008 | Low | Dashboard KPI captions | Home now exists (`ExecutiveKpi`, `DashboardCard`) |
| DEBT-CFG-009 | Low | No configuration validation at write time | **Now reachable** — see Risk 3 |
| DEBT-CFG-010 | Low | Only one page reports `lifecycleFacts` | Unchanged |
| DEBT-CFG-011 | Medium | 11 categories with no consumer | **New** |
| DEBT-CFG-012 | Low | Attributes edited as JSON | **New**, deliberate |
| DEBT-CFG-013 | Low | No permission guard on Configuration Center | **New** — see Risk 8 |

---

## 6. Where the platform stands

Across four waves the platform moved from business rules spread across label maps, `<option>` blocks,
`if` ladders and `switch` statements, to 48 categories in a table, a lifecycle expressed as
conditions over facts, and — as of this wave — a screen an administrator can change any of it from.

The claim I can support: **an administrator can change the behaviour of the Audit module, the audit
lifecycle, and risk scoring without a developer or a release**, and the platform will isolate those
changes per tenant.

The claims I cannot support, unchanged in character from last wave:

- Eleven of the new domains are contracts waiting for consumers.
- The Journey, Tasks, Evidence and Reporting modules were never in scope.
- Nobody has watched any of it run.

Recommendation stands: **Conditional Go**, conditions 2–4 from 5C.2 plus the permission guard
(Risk 8) and the save-time validation warning (Risk 3). None is large; the permission guard is the
one I would treat as blocking.
