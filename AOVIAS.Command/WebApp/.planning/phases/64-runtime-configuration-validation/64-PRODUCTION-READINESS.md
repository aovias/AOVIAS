# Production Readiness Report — Stage 5C.2

**Scope of this certification:** the Audit and Auditor modules of the AOVIAS Command web client.
Not the backend, not the other feature modules, not the rendered UI.

---

## 1. Recommendation

### Conditional Go — for the configuration platform. No-Go for "certified metadata-driven enterprise platform" as stated.

Those are two different claims and the evidence supports them differently.

**What I can certify.** The Audit module's business behaviour is configuration-driven. 28 categories,
6 lifecycle stages and 9 permission-bearing surfaces resolve entirely from metadata. 343 automated
checks exercise it through the production services. Nine realistic administration changes — rename,
disable, reorder, add a stage, restrict a permission, hide a status, change labels, modify a
transition rule, restore defaults — were each demonstrated with configuration only and no code
change. The success criterion "no code modifications required for supported business changes" holds
for every scenario the brief lists.

**What I cannot certify, and why the stronger claim fails.** Three things, each concrete:

1. **No administrator can actually make these changes.** The Angular client has no write path.
   `ConfigurationApiService` exposes reads only; there is no Configuration Center UI. Every scenario
   in this wave was driven by injecting the JSON the API *would* return. The platform is
   configuration-*driven*; it is not yet configuration-*administered*. The locked-state list marks
   "Configuration Center" and "Write API" complete — on the backend they are; in this client they do
   not exist.
2. **Nothing has been observed running.** No browser session, no live backend, no rendered screen.
   The evidence is strong at the service boundary and silent above it.
3. **The claim says "platform"; the work covered two modules.** Journey, Tasks, Evidence and
   Reporting were never in scope. DEBT-CFG-007 is where that boundary becomes user-visible.

A Conditional Go is not a hedge. The conditions in §4 are specific, ordered, and none is large.

---

## 2. Coverage

| Measure | Figure | Basis |
| --- | --- | --- |
| Configuration coverage | **100%** (28/28) | Every category in the Wave 1 inventory plus the two workflow categories |
| Workflow coverage | **100%** (6/6 stages, 9/9 surfaces) | No workflow decision made outside the engine, verified by scan |
| Metadata coverage | **64%** (27/42 files) | Denominator includes files with no business-definition surface; see caveat |
| Runtime validation | **115 cases**, all passing | Executed against production services |
| Tenant validation | **6 properties**, all passing | Label, workflow, version, cache, default-window, permission isolation |

**Caveat on 64%.** The 15 files not reading configuration are shells, export utilities, API services
and panels that render only record data — they have no business definition to own. The meaningful
statement is that **no component owns a business rule**, which the Phase 6 scan confirms. I am
reporting the raw ratio rather than a flattering one because a denominator I chose is not evidence.

---

## 3. Defects

| Id | Severity | Status | Summary |
| --- | --- | --- | --- |
| DEF-CFG-001 | **Critical** | **Fixed** | Configuration leaked across logout and hospital switch; incoming tenant never fetched its own |
| DEF-CFG-002 | Medium | **Fixed** | Owner acceptance gated on the literal `'Assigned'` |
| DEF-CFG-003 | Medium | **Fixed** | Risk matrix band thresholds hardcoded (20/12/6) |
| DEF-CFG-004 | Medium | **Open** | Create-form defaults bypass configuration |

**No Critical or High defects remain open.**

DEF-CFG-001 deserves emphasis: it was a cross-tenant confidentiality failure in a multi-tenant
compliance product, it was permanent for the session rather than transient, and no existing test
could have caught it because the mechanism was correct and simply unreferenced. It was found by
asking "is this wired?" rather than "does this work?" — worth repeating for every other
clear-on-logout collaborator.

---

## 4. Conditions for unconditional Go

Ordered by what blocks what.

1. **Build the Configuration Center write path.** Until an administrator can save an override, none
   of this capability is reachable by its intended user. Needs `POST`/`DELETE` on
   `ConfigurationApiService` against the endpoints that already exist, plus a UI. *This is the
   condition that matters; the rest are verification.*
2. **Run the browser walkthrough** in §6 of the Runtime Validation Report — rendered propagation, the
   status bar under real facts, guards with real role claims, and the hospital-switch reload against
   a live Gateway.
3. **Apply the two seed scripts** (`2026-08-03_SeedAuditConfigurationCatalogs.sql`,
   `2026-08-03_SeedAuditWorkflowLifecycle.sql`). The first carries a required repair: existing
   `AuditType` rows predate `selectableWhenStartingSession`, and without it the Auditor
   "start a session" picker renders empty.
4. **Close DEF-CFG-004**, so that disabling a value actually stops it being submitted.

---

## 5. Production risks

| # | Risk | Likelihood | Impact | Mitigation |
| --- | --- | --- | --- | --- |
| 1 | Capability unreachable — no write UI | Certain | High | Condition 1 |
| 2 | Stale `AuditType` metadata empties the session-type picker | High if script skipped | High | Condition 3; the script repairs in place |
| 3 | A bad configuration edit breaks a screen | Medium once writes exist | Medium | Malformed workflow is rejected wholesale in favour of the seed; malformed value metadata degrades to "attribute not set". Both tested. **But**: a *valid* edit that is wrong — every status `selectableWhenFiltering: false` — is accepted and empties a filter. No validation UI exists. |
| 4 | Sign-in request volume | Low | Low | 1 + N requests, N = configured categories. 29 at full configuration, in parallel, cached for the session. Not measured. |
| 5 | Tenant-configurable route guards | Low | Medium | Wave 2 moved guards onto the configured surface definition, so a tenant can widen what the client offers. Server remains the authority. Flagged for your explicit agreement in the Wave 2 summary; still awaiting it. |
| 6 | Stage ids are no longer compile-checked | Low | Low | `AuditWorkflowStageId` is `string`, so `stage: 'findigns'` compiles. Unknown ids resolve to null and render nothing; tested. |
| 7 | Evidence status labels not configurable | Certain | Low–Medium | DEBT-CFG-007. The one user-visible place the module boundary shows. |

Risk 3 is the one I would watch after Condition 1 lands. Every safety property tested so far protects
against *malformed* configuration. Nothing protects against *coherent but wrong* configuration, and a
write UI makes that reachable. A "no selectable values" warning at save time would cover most of it.

---

## 6. Technical debt register

| Id | Severity | Item | Effort | Note |
| --- | --- | --- | --- | --- |
| DEF-CFG-004 | Medium | Form defaults bypass configuration | S | Open defect, fix specified |
| DEBT-CFG-005 | Low | `statusTone` substring matching | S | Needs status strings normalised first |
| DEBT-CFG-006 | Low | `timelineItemClass` layer→class map | S | Compliance-graph domain, outside this programme |
| DEBT-CFG-007 | Medium | Evidence status labels hardcoded | M | Spans four modules; carried from Wave 1 |
| DEBT-CFG-008 | Low | Dashboard KPI captions | M | Needs a metrics catalog; different problem |
| DEBT-CFG-009 | Low | No configuration validation at write time | M | Only becomes reachable once Condition 1 lands |
| DEBT-CFG-010 | Low | Only one page reports `lifecycleFacts` | S | Status bar falls back to positional progress everywhere else |

Nothing here is load-bearing for the Conditional Go.

---

## 7. Where the platform actually stands

Across three waves the Audit module moved from business rules spread over label maps, `<option>`
blocks, `if` ladders and four `switch` statements, to a state where a status label, a severity band,
a lifecycle stage, a completion rule and a permission are all rows in a table.

That transformation is real and it is tested. The honest qualification is that it stops at two
module boundaries and at the absence of a write UI — and that everything demonstrated here was
demonstrated at the service layer, not observed on a screen.

The final objective asks me to certify a "true metadata-driven enterprise platform". I can certify
the Audit module is metadata-driven, that its behaviour changes through configuration alone, and that
tenant isolation holds. I cannot certify the platform, because two-thirds of it was never in scope
and because no administrator can yet change a single value through the product itself. Those are
resolvable, and the first one is the only one that is on the critical path.
