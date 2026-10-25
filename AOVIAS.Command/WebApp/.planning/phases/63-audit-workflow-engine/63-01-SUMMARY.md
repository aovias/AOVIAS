# Phase 63 — Stage 5C.1 — Configurable Audit Lifecycle Engine

**Status:** Implementation complete. Build green, 219/220 unit tests pass (the one failure is
pre-existing and unrelated).
**human_needed:** rendered-UI signoff for the seven lifecycle transitions (§5 explains why I could
not reach them), and a decision on the permission-model note in §6.
**Repositories touched:** `WebApp`, plus one **data-only** SQL seed script in
`ComplaincePlatform/OrganizationService`. No backend code, no schema changes, no migrations.

---

## 1. What this wave did

The audit lifecycle is now a declarative definition the Configuration Engine can serve. Stage
ordering, completion rules, outstanding work, entry prerequisites, recommendations and surface
permissions all moved out of code and into data.

- **9 files** held workflow logic; **1 engine** now answers workflow questions.
- **4 `switch` statements** over stage ids deleted.
- **3 duplicate permission checks** in shell components deleted.
- **6 duplicated route-guard declarations** collapsed into the surface definitions they guard.
- **6 stages · 9 surfaces · 33 rules** expressed as conditions.
- **33 tests** added; the two existing workflow specs pass **unchanged**, which is the primary
  evidence that behaviour did not move.

---

## 2. The condition language

A rule has to be data before it can be configuration, and data needs a grammar. Rules are now trees
of three combinators (`all` / `any` / `not`) over five tests applied to named facts:

| Test | Means | Replaces |
| --- | --- | --- |
| `true` / `false` | strict boolean identity | `x === true` / `x === false` |
| `known` | reported at all | `known(x)` |
| `positive` | reported and greater than zero | `has(x)` |
| `zero` | reported and not greater than zero | `known(x) && !has(x)` |

Deliberately *not* a general expression language: no arithmetic, no comparisons, no user code. The
ceiling is the point — a payload arriving from a tenant can only ask questions about facts the
workspace already reports.

The subtle part is `false` versus `not{is: true}`. Every entry prerequisite tested `=== false`, so a
page that had not loaded a fact stayed silent rather than announcing a blockage it could not know
about. Collapsing those two into one test would have been a behaviour change disguised as a
simplification, and the spec would not have caught it — nothing asserts the absent case. It has a
test now.

---

## 3. The engine

`AuditWorkflowEngine` is the only thing that makes a workflow decision:

| Question | Method | Previously answered by |
| --- | --- | --- |
| Which stage am I in? | `stageForTab` | a module function called from a state service |
| What comes next / before? | `nextStage` / `previousStage` | the same |
| How far along positionally? | `progressTo` | `buildStageProgress` |
| Is this stage done, what remains, why is it blocked? | `evaluate` | four `switch` statements |
| Which surfaces may this user see? | `surfacesFor` | three shell components, independently |
| Which roles must a guard enforce? | `rolesFor` | six route-config declarations |

**All or nothing, per part.** A configured lifecycle is taken whole. Stages reference each other by
order and their rules reference each other's facts, so three tenant stages overlaid on six platform
ones is not a lifecycle — it is a corruption of one. A set that fails to parse is rejected in favour
of the seed, so a bad edit degrades to platform behaviour rather than to a broken workspace. Two
tests cover that: a stage with no completion rule, and a stage carrying a malformed condition.

---

## 4. Consumers migrated

One at a time, typechecked between each.

| Consumer | What it stopped deciding |
| --- | --- |
| `audit-lifecycle-state.model` | four `switch` statements — now evaluates a definition |
| `audit-workflow.model` | now a positional *view* over the seed, not the lifecycle itself |
| `audit-workspace-state.service` | calls the engine instead of module functions |
| `audit-workspace-participation.service` | asks the engine to evaluate instead of calling a free function |
| `audit-overview-shell` | surface list + role check → `surfacesFor('overview', roles)` |
| `audit-execution-shell` | same, for execution |
| `audit-reports-shell` | same, for reports |
| `routes.ts` | six `roleGuard(AUDITOR_ROLE_IDS)` → `auditSurfaceGuard(tab, surface)` |
| `audit-workflow-status-bar` | untouched — already read from the services above |

---

## 5. Verification

| Check | Result |
| --- | --- |
| `audit-lifecycle-state.model.spec` (20 tests, **unchanged**) | passes — the completion, outstanding, prerequisite and recommendation rules behave identically |
| `audit-workflow.model.spec` (10 tests, **unchanged**) | passes — ordering, chaining, tab mapping and positional progress unchanged |
| `AuditWorkflowEngine` spec (21 new) | passes |
| `workflow-condition.model` spec (12 new) | passes |
| Round-trip through configuration (2 new) | passes |
| `tsc --noEmit` | clean, after every consumer migration |
| `ng build` (AOT, production) | succeeds; warnings unchanged from baseline, all in untouched files |
| `ng test` | 219 passed, 1 pre-existing failure |
| `eslint` | 7 errors, all pre-existing `no-output-native`; none introduced |
| Dev server boot | clean; no console errors, no server errors |

**The round-trip test is the important one.** The platform defaults ship as configuration rows,
which means every organization evaluates its lifecycle through the *configured* path rather than the
in-code seed. That is only safe if serialising the seed out and reading it back reproduces it
exactly — so the test asserts that property directly, comparing full lifecycle state across a
14-case fact matrix, plus surfaces and role restrictions across three tabs. Same serialiser the SQL
script uses, so the two cannot drift.

**What I could not verify.** The Audit routes sit behind the auth guard, there is no backend running
locally, and I will not enter credentials. The dev server boots cleanly, but that proves less here
than it might: the login page does not load the audit module, so boot cleanliness does not exercise
the engine. The AOT build proves the workspace compiles; the unit tests carry the behavioural proof.

**Still needed:** a signed-off pass over the seven transitions with a real session —
planning → execution → evidence → findings → CAPA → verification → closure → reports — confirming
the status bar, recommended action, blocked-stage tooltips and sub-tab visibility per role.

---

## 6. Two things needing your decision

**1. Route guards are now tenant-configurable.** Collapsing the duplicate permission declaration
means the route guard reads `requiresAnyRole` from the lifecycle definition at activation time. A
tenant editing its lifecycle can therefore widen what the client offers. I judged this acceptable —
the server is the authority on what data anyone may read or write, and these guards stop a user
reaching a screen that would be empty for them, which is a usability job. But it is a real change in
where that decision can be made, and you should agree with it rather than inherit it. The
alternative is keeping the guards on a static role set and accepting that the declaration exists
twice.

**2. Stage ids are no longer a closed union.** `AuditWorkflowStageId` was a six-member union; it is
now `string`, for the same reason `AuditTypeCode` became one last wave — a stage a tenant adds is
valid at runtime and would not typecheck against a closed set. The cost is that `stage: 'findigns'`
in a page no longer fails to compile. The engine resolves an unknown id to `null` and the surfaces
render nothing, so a typo is a missing status bar rather than a crash, and there is a test for that.
If you would rather keep compile-time checking on the six platform stages, the alternative is a
union that configured stages cannot extend.

---

## 7. Scope note — Verification and Closure

The brief lists **Verification** and **Closure** among the stages to migrate. The lifecycle has six
stages and neither is one of them; they exist as rules *inside* CAPA and Reports (`verifiedCapaCount`
drives a CAPA recommendation, `sessionClosed` gates Reports).

I did not invent them. Adding a stage is now a configuration change — which is this wave's success
metric — but doing it inside a wave whose first engineering rule is "no business behaviour changes"
would have contradicted the brief. If you want them as first-class stages, that is a short,
well-defined follow-up: two rows in `AuditWorkflowStage` with their conditions, no Angular change.

Similarly, there are no client-side transitions with side effects to migrate. The client never
performs a lifecycle transition — status changes go to the API and the server decides. What the
client owns is *derivation*, and that is what moved.

---

## 8. Platform seed data

`ComplaincePlatform/OrganizationService/Infrastructure/Persistence/Scripts/2026-08-03_SeedAuditWorkflowLifecycle.sql`

Data only, generated from the definition using the round-trip-tested serialiser. Idempotent.
6 stages + 9 surfaces.

Applying it is **optional** for correctness — without it every organization runs on the in-code seed
and behaves identically. Applying it is what makes the lifecycle *editable*, and switches evaluation
onto the configured path that the round-trip test covers.

---

## 9. The pre-existing test failure

`src/app/app.spec.ts > should render title` expects an `<h1>` containing "Hello, praman-web". The
untouched Angular CLI scaffold test; `app.ts` has no `<h1>` and was already modified in the working
tree before this work began. Unrelated, and left alone.
