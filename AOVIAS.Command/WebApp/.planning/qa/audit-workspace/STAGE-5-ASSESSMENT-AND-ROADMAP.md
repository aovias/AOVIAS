# Stage 5 — Assessment, Configuration Engine Foundation & Roadmap

**Date:** 2026-08-02 · **Repos:** `WebApp`, `ComplaincePlatform` (uncommitted)
**Suite:** `Passed! 43 / Skipped 2 / Failed 0` · Typecheck clean · No regressions

## Scope reality check — read this first

Stage 5 as briefed is:

| Stage | Scope |
|---|---|
| 5A | 12 administration domains, **21 pages currently marked coming-soon** |
| 5B | 14 configuration domains, most needing schema + API + UI |
| 5C | Rewire 9 audit surfaces to consume that configuration |

That is comparable in size to the entire Platform Integrity Program — which took fifteen waves. It
is not deliverable in one session, and claiming otherwise would produce thirty half-built pages
instead of something that works.

**What I did instead:** built the foundation 5B and 5C both depend on, proved it end to end on a
real vertical, and produced a sequenced roadmap. Everything below is working and verified; nothing
is scaffolding.

---

## 1. Current state of the Administration Workspace (5A)

| Measure | Value |
|---|---|
| Tabs | 12 (Stage 5A structure complete from Phase 61) |
| Navigation items | **81** |
| Marked `comingSoon` | **21** |
| Built and routed | **60** |

Gaps by domain:

| Tab | Coming-soon items |
|---|---|
| Organization | Facilities & Locations, Financial Year, Business Calendar, Localization |
| Programs | Program Schedule, Coverage & Progress |
| Templates, Risk, Workflow | Majority of items |
| AI, Security, System, Teams, Notifications, Approvals | Largely built (9–11 items each) |

**The workspace shell, tab strip, context drawer and routing are done.** What remains is page-level
implementation, and it is unevenly distributed — Organization/Programs/Templates/Risk/Workflow hold
almost all of the gap.

## 2. Configuration Engine — delivered

The premise of 5B ("metadata-driven, no hardcoded business rules") and 5C ("consume Administration
configuration") needs a seam between the two. That seam did not exist. It does now.

### The problem it solves

The same nine audit types were declared independently in **seven files**:

```
audit-planning.component.ts                 9 references (two hardcoded <option> lists)
auditor-workspace.model.ts                  6 (union type + label map)
auditor-sessions-page.component.ts          5
audit-management-api.service.ts             3
findings-workspace.model.ts                 3
audit-prep-template.service.ts              1
findings-create-panel.component.ts          1
```

Adding a type meant finding all seven. Missing one produced a blank cell or a filter that silently
matched nothing — **which is exactly what DEF-004 and DEF-013 were** in the original QA sprint.

### What was built

| File | Role |
|---|---|
| `core/configuration/audit-type.catalog.ts` | `AuditTypeDescriptor` — the shape Administration will configure. Attributes describe **behaviour** (`selectableWhenScheduling`, `surveillance`, `external`) so consumers branch on capability, not identity |
| `core/configuration/platform-configuration.service.ts` | Resolves configuration for the current organization. Signal-backed, with `allAuditTypes`, `schedulableAuditTypes`, `surveillanceAuditTypes`, and a casing-insensitive `auditTypeLabel()` |

Two design decisions worth stating:

**The default catalog is a *seed*, not a fallback branch.** There is no `if (configured) … else …`
in any consumer, so the configured and unconfigured paths cannot drift. When the Administration API
lands it calls `applyAuditTypes()` and nothing downstream changes.

**`selectableWhenScheduling` makes an omission explicit.** The old Planning dropdown offered six of
nine types; the other three (Consultant Review, NABH Pre/Final) are recorded by the system but never
hand-scheduled. The list expressed that by simply leaving them out, with no way to tell a deliberate
exclusion from an oversight. It is now an attribute with a reason attached.

### Proven on a real vertical

`audit-planning.component.ts` — both hardcoded `<option>` lists replaced by
`schedulableAuditTypes()`.

**Runtime verification** — the rendered dropdown, from configuration:

```
InternalAudit=Internal, DepartmentAudit=Department, FollowUpAudit=Follow-up,
MockAudit=Mock NABH, FocusedAudit=Focused, Surveillance=Surveillance
```

Identical options, identical order, identical labels. **Behaviour unchanged; the source is now
singular.** Adding an audit type is a configuration change, not a seven-file edit.

## 3. Validation

| Check | Result |
|---|---|
| Angular typecheck | ✅ Clean |
| Planning dropdown | ✅ Configuration-driven, 6 options unchanged |
| Integration suite | ✅ 43 passed / 2 skipped / 0 failed |
| CAPA dashboard | ✅ 88 total, openCapas 1 |
| Platform Integrity guardrails | ✅ Still green — no bypass regressions |

No Critical or High regressions.

## 4. Roadmap

### Stage 5B-1 — Configuration API (the gating dependency)

Everything else waits on this. A generic metadata store **already exists** and is the natural
home — `OrganizationLookupCategories` / `OrganizationLookupValues` in the Organization database,
currently holding `DepartmentType`, `MembershipType`, `UnitType`.

| | |
|---|---|
| Work | Expose read/write over lookup categories; add `AuditType`, `Severity`, `RiskLevel`, `NumberingSequence` categories |
| Why this store | Configuration becomes **data**, not schema — new domains need no migration, which is what "metadata-driven" has to mean to be real |
| Effort | Medium · **Risk** Low — additive |
| Unblocks | Every 5B domain and all of 5C |

### Stage 5B-2 — Configuration domains, in dependency order

| Order | Domain | Rationale |
|---|---|---|
| 1 | **Audit Types** | Descriptor and consumer already built — first to go live |
| 2 | **Severity / Risk Matrix** | Findings and CAPA both branch on severity today |
| 3 | **Numbering Sequences** | `PlatformSequences` exists; NEW-07 showed it is load-bearing and under-modelled |
| 4 | Financial Year + Business Calendar | Reports already need a programme year (DEF-014) |
| 5 | Workflow Rules, Approval Chains, Escalation | Largest; depends on 1–4 |
| 6 | Templates, Feature Flags, Digital Signatures | Independent, can parallelise |

### Stage 5C — Consumption, per surface

Each surface moves from constants to configuration once its domain exists. Ordered by how much
hardcoding each currently carries:

1. Planning ✅ **done** (audit types)
2. Execution / Sessions — audit type + status labels
3. Findings — severity, source, classification
4. CAPA — severity, SLA thresholds
5. Reports — period definitions (currently the programme-year default from DEF-014)
6. Executive Intelligence — KPI thresholds

## 5. What is NOT done

Stated plainly against the exit criteria:

| Criterion | Status |
|---|---|
| Administration Workspace complete | ❌ 21 pages coming-soon |
| Enterprise configuration implemented | ⚠️ **Engine + one domain**; 13 domains remain |
| Audit Workspace fully configuration-driven | ⚠️ Planning only; 8 surfaces remain |
| No hardcoded business rules remain | ❌ Audit types consolidated; severity, status, SLA, workflow remain |
| Runtime validation passes | ✅ |
| No Critical/High regressions | ✅ |
| Stage 5 formally completed | ❌ |

## 6. Recommendation

**Build the Configuration API next (5B-1).** It is the single dependency gating all fourteen
configuration domains and all nine consumption surfaces, and the storage layer already exists. Every
domain built before it will need reworking afterwards.

The pattern is now proven end to end on audit types: descriptor → service → consumer, with the
default as a seed rather than a branch. Each subsequent domain is the same shape, which makes them
parallelisable once the API is in place.

### Carried from Platform Integrity (unchanged, non-blocking)

TD-13 background job contexts · TD-19 24 unprobed Gateway endpoints · TD-14 SignalR namespacing ·
TD-15 CommunicationService verification · TD-04 88 filtered duplicate rows · TD-01 66 governed
bypasses.

### One housekeeping note

**Both repositories still have uncommitted work** — fifteen integrity waves plus this stage. That is
a large amount of unversioned change, and it should be committed before Stage 5 proceeds further.
