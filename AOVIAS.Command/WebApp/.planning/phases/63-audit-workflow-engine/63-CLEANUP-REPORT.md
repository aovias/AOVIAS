# Cleanup Report — Stage 5C.1

Nothing was deleted before its replacement was in place and the two existing workflow specs — the
regression harness — passed unchanged.

---

## Deleted: workflow decisions in code

### Four `switch` statements over stage ids

`audit-lifecycle-state.model.ts` held the whole lifecycle as control flow:

| Function | Arms | Became |
| --- | --- | --- |
| `evaluate` | 6 | `completeWhen` + `outstanding` per stage |
| `recommend` | 6 | `recommendations` per stage |
| `prerequisite` | 5 | `entryBlockedWhen` per stage |
| plus the `has` / `known` helpers | — | the `positive` / `zero` / `known` fact tests |

The file went from 296 lines of rules-as-code to 157 lines of evaluation. What remains is the
derivation — earliest-incomplete-is-current, recommended action, completed count — which is
lifecycle *mechanics*, not lifecycle *policy*.

### Three duplicate permission checks

`audit-overview-shell`, `audit-execution-shell` and `audit-reports-shell` each carried:

```ts
private readonly isAuditor = computed(() =>
  this.userContext.getUserRoles().some((role) => AUDITOR_WORKSPACE_ROLES.has(role)),
);
```

…followed by its own hardcoded surface list and an `if (!this.isAuditor()) return [...]` branch.
Three copies of one rule, each free to drift from the route guards enforcing it. All three are now a
single `surfacesFor(tab, roles)` call; the components dropped from 54, 51 and 48 lines to 32, 30 and
29.

### Six duplicate route-guard declarations

`roleGuard(AUDITOR_ROLE_IDS)` appeared six times in `routes.ts`, restating a restriction the shells
also declared. Replaced by `auditSurfaceGuard(tab, surfaceId)`, which reads `requiresAnyRole` from
the same surface definition the shell reads. The role-alias handling (`normalizeRoleId`) is
reproduced exactly, so consolidating the declaration does not narrow who gets in.

`roleGuard` itself is untouched — three other features still use it.

### The stage-order array

`AUDIT_WORKFLOW_STAGES` was the lifecycle. It is now derived from `SEED_AUDIT_WORKFLOW.stages` and
sorted by `order`, so ordering cannot disagree with the definition the rules hang off.

---

## Deleted: dead symbols

| Symbol | Why it went |
| --- | --- |
| `AUDITOR_WORKSPACE_ROLES` | the `Set` form existed only for the three component role checks |
| `lifecycleStageIndex` | a re-export of `stageIndex` with no callers |

---

## Deleted: a closed union that had become false

`AuditWorkflowStageId` was a six-member union. It claimed a closed set, and the claim stopped being
true when the lifecycle became configurable — a stage a tenant adds is valid at runtime and would
not typecheck against it. Now `string`, documented, with a test covering the unknown-id path.

---

## Not deleted, and why

**`copilotPageContextFor`** — a `switch` in `audit-context-panel.model.ts` mapping a context
*subject kind* to a backend Copilot persona key. Not a workflow rule: it keys off subject kind, not
stage, and the keys are a wire contract with the AI service. Same category as `toFindingWireEnum`
from the previous wave.

**Toolbar action `disabled` / `disabledReason`** — computed by each page from the record it has
loaded (`d.isPromoted`, `selected.ownerAcceptedAt`). These are properties of a record, not lifecycle
rules, and the action registry is already one mechanism. `AuditRecordWorkflowState` already carries
optional `allowedActions` / `blockedActions` for a page that wants to report what the lifecycle
permits; nothing populates it. Noted in the inventory rather than changed.

**`AUDIT_WORKSPACE_TABS` and `resolveAuditTabFromUrl`** — URL topology and legacy-alias redirects,
not workflow. The tab strip is navigation chrome; which *stages* map onto which tabs is in the
definition.

**The positional helpers in `audit-workflow.model.ts`** — `getStage`, `stageIndex`, `nextStage`,
`previousStage`, `stageForTab`, `buildStageProgress`. Kept as pure functions over the platform
default, because they are what the existing specs exercise and what makes the regression proof
meaningful. Anything that must honour a tenant's configured lifecycle goes through the engine
instead, and every runtime consumer now does. These remain for the seed view and its spec.

---

## Verification after cleanup

- `audit-lifecycle-state.model.spec` (20 tests) and `audit-workflow.model.spec` (10 tests) — both
  **unchanged**, both passing.
- `tsc --noEmit` — clean.
- `ng build` — succeeds; warnings unchanged from baseline.
- `ng test` — 219 passed, 1 pre-existing failure.
- `eslint` — 7 pre-existing errors, none introduced.

No workflow decision is made outside the engine. Verified by scan: no `switch` over stage ids, no
`isAuditor` checks, no stage-order arrays, and no `roleGuard` in the audit routes.
