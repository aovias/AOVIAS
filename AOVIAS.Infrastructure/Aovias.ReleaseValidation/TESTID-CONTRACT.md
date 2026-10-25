# TESTID Contract

**Status:** normative. No `data-testid` may be added to AOVIAS Command without conforming to this document.
**Version:** 2.0 — finalized against ADR-014
**Owner:** Release Validation (RC3 Workstream 1)
**Applies to:** `WebApp/src/**/*.html` and inline Angular templates.
**Domain authority:** [ADR-014 — Framework Presentation vs Journey Identity](../docs/architecture/ADR-014-Framework-Presentation-vs-Journey-Identity.md)

> **This document is the operational specification for automation developers.** ADR-014 explains
> *why* the domain model is what it is; this contract states *what you must do*. An automation
> developer should never need to read source comments, and should need ADR-014 only when asking why.

---

## 1. Why this contract exists

A `data-testid` is not a testing convenience. It is a **published interface between the product and
the release gate**, with the same stability obligations as a public API.

Two RC2/RC3 failures shaped it:

- A false defect report — *"login submit is broken"* — that was stale click coordinates against a
  re-rendered form. The product was fine; the automation addressed the UI by position.
- `data-framework-id` was added to the context dialog carrying a **journey** id, inheriting a
  five-instance domain ambiguity into the automation layer within hours of the contract's first
  draft. Caught by audit before any operator consumed it.

The first produced §3 (Prohibited). The second produced §2 (The Four Rules).

Consequences, both binding:

- **Renaming or removing a testid is a breaking change.** It breaks the release gate.
- **A testid is not free.** Each is a commitment to keep that control identifiable across redesigns.

---

## 2. The Four Rules

These are the load-bearing rules. Everything else in this document elaborates them.

### 2.1 Domain Truth Rule

> **Automation attributes must describe the actual domain identity they expose, never the
> presentation concept.**

Where UI terminology diverges from the domain, the attribute follows the **domain**.

The Context dialog presents a *Framework* and selects a *Journey* (ADR-014, Phase 1). The attribute
is therefore `data-journey-id`, not `data-framework-id` — regardless of the column heading above it.

### 2.2 Companion Attribute Rule

> **Business identity lives in companion attributes. Never encode identity inside the testid.**

```html
<!-- correct -->
<button data-testid="context-framework-option" [attr.data-journey-id]="framework.id">

<!-- prohibited -->
<button data-testid="context-framework-NABH">
<button data-testid="context-framework-option-2">
```

Identity in a testid makes automation tenant-dependent. It breaks the moment a scenario provisions
its own tenant — which every scenario will, once the Test Data Manager lands.

### 2.3 Presentation Rule

> **Testids describe structure. Companion attributes describe identity. Visible text is never a
> stable selector.**

| Concern | Mechanism |
| --- | --- |
| "Where is the confirm button?" | `data-testid="context-confirm"` |
| "Which journey is this row?" | `data-journey-id="..."` |
| "Is this row selected?" | `data-selected="true"` |
| "Does the report say X?" | visible text — **content assertions only** |

Text may be asserted. Text may not be *located by* for product structure.

### 2.4 Resolution Rule

> **If a required identity is not available from the DOM, automation must resolve it through the
> product's supported APIs or services — never infer it, and never substitute another identifier.**

The Context dialog exposes **no framework id**. Automation needing one resolves it via
`OrganizationService.getFrameworkIdForJourney(hospitalId, journeyId)`.

When that returns `null`, automation **omits the framework filter**. Substituting a journey id for a
framework id is prohibited — it is the exact defect that caused *"every framework-filtered audit
query to silently match nothing"* (ADR-014, Phase 2, item 1).

---

## 3. Naming convention

```
data-testid="<surface>-<control>[-<qualifier>]"
```

All lowercase, hyphen-separated. **Business language only.**

| Part | Meaning | Examples |
| --- | --- | --- |
| `surface` | the business surface, not the component or route | `login`, `context`, `finding`, `capa`, `evidence`, `audit`, `report` |
| `control` | what the operator does or reads, in domain terms | `email`, `submit`, `severity`, `promote`, `objective-element` |
| `qualifier` | only when one surface has repeated controls | `row`, `confirm`, `cancel`, `empty` |

### 3.1 Implemented — Authentication

| Testid | Control | Companion attributes |
| --- | --- | --- |
| `login-email` | Email input | — |
| `login-password` | Password input | — |
| `login-submit` | Sign-in submit | — |

### 3.2 Implemented — Context Selection

`hospital-selector-dialog.component.ts`

| Testid | Control | Companion attributes |
| --- | --- | --- |
| `context-dialog` | Dialog root — discovery and readiness | — |
| `context-close` | Dismiss (X) | — |
| `context-hospital-list` | Hospital container | — |
| `context-hospital-option` | One selectable hospital | `data-hospital-id`, `data-selected`, `data-onboarded` |
| `context-framework-list` | Framework container | — |
| `context-framework-option` | One selectable framework | ⚠ `data-framework-id` *(pending correction)*, `data-framework-code` *(unverified)*, `data-selected` |
| `context-framework-empty` | Hospital has no frameworks | — |
| `context-cancel` | Abandon selection | — |
| `context-confirm` | Commit selection | — |

### 3.3 Pending corrections — implementation must follow the contract

| Item | Required change | Authority |
| --- | --- | --- |
| `data-framework-id` on `context-framework-option` | **Rename to `data-journey-id`.** It carries `FrameworkSelectorItem.id`, which is a journey id. | Rule 2.1; ADR-014 Phase 1 |
| `data-framework-code` | **Verify before use.** Documented as the framework's code; observed runtime value `CP00000001` matches the Programme code format. Do not rely on it until traced. | ADR-014 Phase 4, open item |

**The contract governs the implementation, not the reverse.** These are recorded divergences to be
closed by the next implementation session, not accepted state.

### 3.4 States that do not exist

The context dialog has **no loading, error, or hospital-empty state**. Testids for them are
prohibited until the states exist, and creating them is product behaviour — out of scope under the
RC3 engineering freeze.

Automation must not assert on states the product does not render.

---

## 4. Prohibited

| Prohibited | Why |
| --- | --- |
| CSS class or structural selectors in tests | Classes are styling; they change without notice |
| XPath | Encodes document structure, the least stable thing about a UI |
| Positional selectors (`nth`, coordinates) | Caused RC2's false defect report |
| Text-content selectors for **product structure** | Breaks under i18n and copy edits. Permitted for **content assertions** only |
| Technical names — `btn-primary`, `mat-input-3` | Names the implementation, not the business act |
| Component or route names — `login-component-submit` | Couples the gate to Angular's file layout |
| IDs reused as testids | `id` carries accessibility and form semantics |
| Identity inside the testid — `finding-FND-2026-00004` | Rule 2.2 |
| Attributes named for presentation — `data-framework-id` holding a journey id | Rule 2.1 |

---

## 5. Consuming Domain Identity

**Framework presentation ≠ Framework identity. Automation consumes Journey identity.**

### 5.1 Hospital selection

Identity is directly addressable.

```
locate   [data-testid="context-hospital-option"]
filter   [data-onboarded="true"]          ← non-onboarded hospitals are disabled
select   click
verify   [data-selected="true"]
```

Never select by hospital name — names are tenant data.

### 5.2 Journey selection (presented as "Framework")

```
locate   [data-testid="context-framework-option"]
read     data-journey-id                   ← THIS IS A JOURNEY ID
select   click
verify   [data-selected="true"]
confirm  [data-testid="context-confirm"]
```

The column is headed *Framework* and the rows show framework names. **The identity is the journey.**

### 5.3 Framework derivation

There is no framework id in the DOM. Per Rule 2.4:

```
frameworkId = OrganizationService.getFrameworkIdForJourney(hospitalId, journeyId)
```

- Returns `null` when the journey is unknown, or when the Gateway payload predates `frameworkId`.
- On `null`, **omit the framework filter.** Do not substitute the journey id.
- This is the **single** mapping point. Automation must not derive one id from the other by any other
  route.

### 5.4 Context persistence

Both ids persist, under separate keys:

| Value | Storage | Key |
| --- | --- | --- |
| Hospital | localStorage | `hospital_id` |
| Framework | sessionStorage | `selected_framework_id` |
| Journey | sessionStorage | `selected_framework_journey_id` ⚠ hybrid name — holds a **journey** id |
| Audit session | sessionStorage | `aovias.audit.workspace.context.v1` → `.sessionId` |

Automation asserting "context is selected" must check **hospital and framework**. Asserting journey
selection means reading `selected_framework_journey_id` — despite its name.

**Session-scoped storage is intentional.** A new browser tab legitimately starts without context;
that is expected behaviour, not a defect (ADR-014, Phase 4).

---

## 6. Ownership, lifecycle, review

| Stage | Rule |
| --- | --- |
| **Added** | By the engineer changing the component, in the same commit. Never retrofitted by automation alone. |
| **Owned** | By the product team owning the surface. Release Validation owns the *contract*. |
| **Changed** | Renaming requires updating every consumer in `Aovias.ReleaseValidation` in the same PR. |
| **Removed** | Only when the control is removed. |
| **Reviewed** | Any PR touching a `data-testid` requires a Release Validation reviewer. |

### Review rules

A PR is rejected if it:

1. Adds a testid not conforming to §3.
2. Uses any prohibited form from §4.
3. **Names a companion attribute for the presentation concept rather than the domain entity** (Rule 2.1).
4. Encodes identity inside a testid (Rule 2.2).
5. Renames or removes a testid without updating consumers.
6. Adds a selector to a test that is not a testid, except content assertions.
7. Adds a testid with no consuming scenario — speculative testids are debt.
8. Adds a testid for a state the product does not render (§3.4).

---

## 7. Versioning

Testids are **unversioned by design**; the name is the contract. Where a surface is genuinely
replaced rather than changed, the new surface takes a new `surface` prefix and both coexist until the
old scenarios retire.

```
finding-promote        → existing promotion control
finding-v2-promote     → only if a genuinely separate promotion surface ships alongside
```

Suffixing versions onto unchanged controls is prohibited.

---

## 8. Product-side rules

- Testids are **not conditional**. A control that renders carries its testid in every state,
  including disabled and loading — automation must be able to assert *present and disabled*.
- Testids survive `@if` / `@for`. Repeated elements share one testid and differentiate by companion
  attribute (Rule 2.2).
- **Retain testids in all builds.** If the gate runs against a build without them, the gate is not
  certifying what ships.

---

## 9. Cross-reference matrix

The navigation map for contributors.

| Document / Component | Governs | Answers |
| --- | --- | --- |
| **ADR-014** | Domain semantics | *What is a Journey vs a Framework, and why?* |
| **TESTID-CONTRACT** (this) | Automation surface | *How do I address and identify a control?* |
| **RuntimeInvariantEngine** | Runtime correctness | *Did the platform stay healthy?* |
| **ContextOperator** | Interaction | *How is context actually selected?* |
| **FoundationScenario** | Validation | *Does the automation platform itself work?* |
| `ARCHITECTURE.md` | Platform design | *How do the layers fit together?* |

Precedence: **ADR-014 → TESTID-CONTRACT → implementation.** Where they disagree, the higher document
wins and the lower is corrected.

---

## 10. Readiness assessment

### Is the automation contract now semantically complete?

**Yes**, with two recorded divergences that implementation must close (§3.3).

Every identifier the Context surface exposes is defined, traced to a domain entity, and named by rule
(§3.2, §5). The one identity *not* obtainable from the DOM — the framework id — has an explicit
resolution path (Rule 2.4, §5.3). Absent states are declared rather than left ambiguous (§3.4).

### Can ContextOperator now be implemented without architectural uncertainty?

**Yes.**

The three questions that blocked it are answered:

1. *What does the dialog select?* A journey, presented as a framework — ADR-014 Phase 1, §5.2.
2. *How do I identify a row without tenant text?* `data-journey-id` / `data-hospital-id` — §5.1–5.2.
3. *How do I get a framework id?* `getFrameworkIdForJourney()`; on `null`, omit the filter — §5.3.

**One implementation prerequisite:** rename `data-framework-id` → `data-journey-id` (§3.3) *before*
the operator consumes it. Building against the wrong name would encode the ambiguity ADR-014 exists
to remove. This is a one-attribute change, permitted under the freeze as automation metadata.

### Is any additional documentation required before implementation?

**No.** Two items are tracked but neither blocks:

- `data-framework-code` verification (§3.3) — the operator does not need it.
- The route-dependence of the dialog (it appears when a route requires framework scope, not
  immediately after login) is an **operator design concern**, not a contract concern. It belongs in
  the ContextOperator's own documentation.

---

**One sentence for the automation developer:** *the dialog says Framework, selects a Journey, exposes
`data-journey-id`, and the framework id comes from `getFrameworkIdForJourney()` — never from the DOM,
never by substitution.*
