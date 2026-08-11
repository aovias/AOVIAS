# Workflow Inventory — Audit Lifecycle

Every workflow rule found in the Audit workspace before this wave, where it lived, and what it
became. This is the Phase 1 deliverable and the checklist the migration was worked against.

---

## 1. Where workflow lived

| Location | What it held |
| --- | --- |
| `audit-workflow.model.ts` | Stage list, order, tab/route mapping, handoff contract, positional progress |
| `audit-lifecycle-state.model.ts` | Four `switch` statements: completion, outstanding, prerequisites, recommendations |
| `audit-workspace-state.service.ts` | Called the positional helpers directly |
| `audit-workflow-status-bar.component.ts` | Rendered both progress sources |
| `audit-overview-shell.component.ts` | Surface list + role check |
| `audit-execution-shell.component.ts` | Surface list + role check |
| `audit-reports-shell.component.ts` | Surface list + role check |
| `routes.ts` | Six `roleGuard(AUDITOR_ROLE_IDS)` declarations |
| `audit-workspace-roles.ts` | The auditor role set, in two forms |

Nine files. A stage's behaviour could not be read in one place.

---

## 2. Stage sequencing

Order, handoff and navigation were the array order of `AUDIT_WORKFLOW_STAGES`:

`planning → execution → evidence → findings → capa → reports`

Each stage declared `consumes` / `produces`, and a spec asserted the chain is continuous. Next,
previous, index and positional progress were all derived from array position.

**Now:** `WorkflowDefinition.stages`, ordered by `order`. The handoff contract and its assertion are
unchanged.

---

## 3. Completion rules (exit conditions)

| Stage | Rule as written | As a condition |
| --- | --- | --- |
| planning | `hasScheduledAudit === true` | `{fact: hasScheduledAudit, is: true}` |
| execution | `hasSession === true && has(observationCount)` | `all[hasSession true, observationCount positive]` |
| evidence | `has(evidenceCount)` | `{fact: evidenceCount, is: positive}` |
| findings | `has(findingCount) && known(openFindingCount) && !has(openFindingCount)` | `all[findingCount positive, openFindingCount zero]` |
| capa | `has(capaCount) && known(openCapaCount) && !has(openCapaCount)` | `all[capaCount positive, openCapaCount zero]` |
| reports | `reportAvailable === true` | `{fact: reportAvailable, is: true}` |

`has(x)` was "reported and > 0"; `known(x) && !has(x)` was "reported and not > 0". These became the
`positive` and `zero` tests. The distinction is load-bearing: an unreported count must not settle or
unsettle a stage.

---

## 4. Outstanding work (mandatory actions)

| Stage | Items and their guards |
| --- | --- |
| planning | "Schedule an audit" when not scheduled |
| execution | "Start the audit session" when no session; **else** "Record observations" when session exists and count is zero |
| evidence | "Collect and verify evidence" when no evidence |
| findings | "Raise findings from observations" when none; **else** "{n} finding(s) still open" |
| capa | "Promote findings to CAPA" when none; **else** "{n} CAPA(s) awaiting closure" |
| reports | "Close the audit" when not closed **and** "Generate the audit report" when unavailable |

Three stages used `if / else if`, so the second item silently depended on the first being false.
Those became explicitly mutually exclusive conditions — same output, dependency now stated.

Reports is the exception: its two items are independent and both can appear. Preserved.

The counted messages used template literals, which is the one part of a rule that cannot move into
data as-is. They became `{openFindingCount}` placeholders, substituted at render.

---

## 5. Entry conditions (prerequisites)

| Stage | Blocked when | Reason shown |
| --- | --- | --- |
| execution | `hasScheduledAudit === false` | Schedule an audit before executing it |
| evidence | `hasSession === false` | Start the audit before collecting evidence |
| findings | `hasSession === false` | Findings are raised during an audit session |
| capa | `known(findingCount) && !has(findingCount)` | A CAPA is raised from a finding |
| reports | `sessionClosed === false` | Close the audit to finalise its report |

Every one tested `=== false`, **not** `!x`. A page that never loaded the fact reported no blockage
rather than asserting one it could not know about. This is why the condition language has both
`false` and `not{is: true}` and why they are not interchangeable.

**These are guidance, never enforcement.** The status bar dims the stage and explains; navigation is
not prevented. Unchanged.

---

## 6. Recommendations

Eight rules across six stages, each a conjunction over counts — for example CAPA suggests chasing
owners while any remain open, then switches to scheduling effectiveness verification once none do.
All transcribed as conditions; the "returns nothing rather than filler" property is preserved by
rules simply not matching.

---

## 7. Role restrictions

One role set, `AUDITOR_ROLE_IDS`, applied to six surfaces — declared **twice** for each: once as a
route guard and once inside the shell that decides which sub-tabs to advertise.

| Tab | Surface | Restricted |
| --- | --- | --- |
| overview | programme | no |
| overview | auditor-home | yes |
| execution | sessions | yes |
| execution | observations | yes |
| execution | surveillance | no |
| execution | simulation | no |
| reports | audit | no |
| reports | sessions | yes |
| reports | history | yes |

**Now:** `requiresAnyRole` on the surface definition, read by both the shell and the route guard.

---

## 8. Action availability

Toolbar actions are contributed by the hosting page with `disabled` / `disabledReason`, computed
from loaded records — `d.isPromoted`, `selected.ownerAcceptedAt`, `this.running()`.

**Deliberately not migrated.** These are properties of a loaded record, not lifecycle rules, and the
registry is already a single mechanism. `AuditRecordWorkflowState` already carries optional
`allowedActions` / `blockedActions` for a page that wants to report what the lifecycle permits;
nothing populates it yet. Noted rather than changed.

---

## 9. Rules the brief names that do not exist here

The brief lists **Verification** and **Closure** as lifecycle stages. The lifecycle has six stages
and neither is among them. They exist as *rules inside* other stages — `verifiedCapaCount` drives a
CAPA recommendation, `sessionClosed` gates the Reports stage.

They were not invented. Adding them is now a configuration change, which is the wave's success
metric; doing it as part of a wave whose first rule is "no business behaviour changes" would have
contradicted the brief.

Likewise there are no transitions with side effects to migrate: the client never performs a
lifecycle transition. Status changes go to the API (`patchStatus`, promotion, session close) and the
server decides. What the client owns is *derivation* — where the work sits and what remains — and
that is what this wave moved.
