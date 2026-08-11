# ADR-015 — Workspace Activation Lifecycle

**Status:** Accepted, with one unresolved question (§7)
**Date:** 2026-08-08
**Depends on:** [ADR-014 — Framework Presentation vs Journey Identity](./ADR-014-Framework-Presentation-vs-Journey-Identity.md)
**Governs:** Release Validation, WorkspaceActivationOperator, Foundation Scenario, RC4 context redesign
**Evidence base:** F1.3A Trigger Inventory, F1.4A Context Ownership Matrix. Every claim traces to one of these.

---

## Context

RC3 automation assumed that a logged-in application was a usable application. It is not.

F1.3 observed a steady state after login in which `hospital_id` was populated while
`selected_framework_id` and `selected_framework_journey_id` were both null, with no dialog raised and
no guard blocking navigation. F1.3A proved the dialog has **exactly two openers, both user-initiated**
(`SidebarComponent.onChangeHospital()` at `sidebar.component.ts:1664`, and the user profile menu at
`user-profile-menu.component.ts:287`) — no guard, resolver, interceptor, bootstrap path or effect
opens it.

F1.4A then proved why: `hospital_id` is **not a selection**. It is written by `SessionService` at
login from the JWT `hospital_id` claim (`session.service.ts:40`). Authentication produces a hospital;
it does not produce a workspace.

**The architectural gap: authentication and workspace activation were treated as one step. They are
two, and only the first happens automatically.**

## Decision

**Authentication, Identity Context, Workspace Activation and Operational Readiness are four distinct
lifecycle stages with distinct owners.** No business workflow may execute before Operational
Readiness, and reaching it requires a deliberate activation act that the product does not perform on
its own.

---

## 1. Lifecycle

```
   Unauthenticated
        │  AuthService.login()
        ▼
   Authentication Complete
        │  SessionService writes tokens
        ▼
   Identity Context Established          ← automatic, from the JWT
        │
        ▼
   Workspace Activation Required         ← THE GAP: nothing exits this state on its own
        │  operator invokes an affordance (F1.3A)
        ▼
   Workspace Activated
        │  AppContextService derives framework from journey
        ▼
   Operational Ready
```

| State | Owner | Entry condition | Exit condition | Persisted | Invariants | Failure modes |
| --- | --- | --- | --- | --- | --- | --- |
| **Unauthenticated** | `AuthService` | initial, or after logout | valid credentials accepted | none | no tokens present | invalid credentials; identity service unavailable |
| **Authentication Complete** | `AuthService` | login returns tokens | `SessionService` persists identity | access + refresh tokens | both tokens present (RV Authentication Contract) | refresh token absent → session cannot be renewed (DEF-RC2S1-001) |
| **Identity Context Established** | `SessionService` | tokens persisted | — *automatic and immediate* | `hospital_id` (localStorage, `session.service.ts:40`) | `hospital_id` equals the JWT claim | JWT lacks `hospital_id` → `normalizeGuid` yields null |
| **Workspace Activation Required** | **none — see §7** | Identity established, journey absent | operator activates a workspace | identity only | **none enforced** | **terminal without operator action** (F1.3A) |
| **Workspace Activated** | `AppContextService` | journey selected and persisted | framework derivation attempted | `selected_hospital_id`, `selected_framework_journey_id` (sessionStorage) | journey belongs to the selected hospital | activation cancelled → state unchanged |
| **Operational Ready** | `AppContextService` | framework derived and persisted | logout, or tab close | + `selected_framework_id` | §3 invariant holds | derivation returns null → framework removed (`app-context.service.ts:124`) |

**The critical property: `Workspace Activation Required` has no owner and no automatic exit.** It is
reachable in one step from login and is not treated as invalid by any component.

---

## 2. Context taxonomy

### 2.1 Identity Context

Identity-derived. Never chosen.

| Property | Value |
| --- | --- |
| Members | `hospital_id` |
| Owner | `SessionService` |
| Authority | **The JWT.** The server is the source of truth |
| Writer | `SessionService` at login (`:40`); `auth.interceptor.ts:68` on token refresh |
| Reader | `SessionService.getHospitalId()` (`:79`) |
| Persistence | **localStorage** — survives reload, tab close, new tab |
| Lifecycle | Created at authentication, refreshed with the token, cleared at logout |

**Identity Context must never be written by a selection.** It describes who the operator is, not what
they are working on.

### 2.2 Workspace Context

Operator-selected. The result of activation.

| Property | Value |
| --- | --- |
| Members | `selected_hospital_id`, `selected_framework_journey_id`, `selected_framework_id` |
| Owner | `AppContextService` |
| Authority | The operator's activation choice |
| Writers | `AppContextService` (`:97`, `:109`, `:122`) — **and `AuthService:95`, which is a boundary violation, §7** |
| Readers | `AppContextService.getHospitalId()` (`:141`), `.getJourneyId()` (`:145`) |
| Persistence | **sessionStorage** — per-tab, cleared on tab close |
| Activation requirement | A journey must be selected. Hospital alone is insufficient |

**Per-tab persistence is intentional.** A new tab legitimately starts unactivated; that is expected
behaviour, not a defect (TESTID-CONTRACT v2 §5.4).

### 2.3 Derived Context

Never selected directly. Computed from Workspace Context.

| Value | Derived from | Via | Authority |
| --- | --- | --- | --- |
| `selected_framework_id` | `selected_framework_journey_id` | `OrganizationService.getFrameworkIdForJourney(hospitalId, journeyId)` | **ADR-014** |

Rules, inherited from ADR-014 and confirmed by the writer's own behaviour:

1. Framework is **never** operator-selected. The dialog presents a framework and selects a journey.
2. When derivation returns `null`, the framework key is **removed** (`app-context.service.ts:124`),
   never populated with the journey id. Substitution is the defect ADR-014 exists to prevent.
3. Journey and framework are removed together when the hospital changes (`:115–116`).

---

## 3. Operational Readiness Contract

**The application is operational when, and only when:**

```
Identity Context established        hospital_id present
              +
Workspace journey selected          selected_framework_journey_id present
              +
Framework resolved                  selected_framework_id present
```

**No business workflow may execute before this holds.** RC2 demonstrated the cost of violating it:
findings created outside a scoped workspace carried no journey binding and were invisible to every
audit report (DEF-M6-001).

### Legal intermediate states

| State | Classification | Rationale |
| --- | --- | --- |
| Identity established; journey missing; framework missing | **Valid, terminal, recoverable** | Reachable in one step from login. **Terminal** because no component exits it automatically (F1.3A). **Recoverable** only by operator action. Not an error state — no component treats it as one |
| Journey present; framework null | **Valid, transient or terminal** | Transient during derivation; terminal when the journey resolves no framework. `AppContextService:124` removes the framework rather than substituting. Consumers omit the framework filter (ADR-014) |
| Framework present; journey null | **Impossible** | `:115–116` remove both together; no writer produces this |
| Hospital changed; journey retained | **Impossible** | `:115` clears the journey on hospital change |

**The first row is the architectural finding of this ADR:** a legal, terminal, unenforced state in
which the application appears usable and is not scoped.

---

## 4. Canonical Ownership Matrix

Replaces all previous ownership notes.

| Context | Key | Owner | Authority | Writer | Reader | Lifetime | Derived | Required for Operational |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Identity | `hospital_id` | `SessionService` | JWT claim | `SessionService:40`; `auth.interceptor:68` | `SessionService:79` | localStorage | No | **Yes** |
| Workspace | `selected_hospital_id` | `AppContextService` | Operator choice | `AppContextService:97`; ⚠ `AuthService:95` | `AppContextService:141` | sessionStorage | No | No — §7 |
| Workspace | `selected_framework_journey_id` | `AppContextService` | Operator choice | `AppContextService:109` | `AppContextService:145` | sessionStorage | No | **Yes** |
| Derived | `selected_framework_id` | `AppContextService` | ADR-014 derivation | `AppContextService:122` | `AppContextService` | sessionStorage | **Yes** | **Yes** |

---

## 5. Workspace Activation Contract

Responsibilities of the future operator, in business terms. No implementation, no UI, no tooling.

### `discoverRequirement()`

- **Precondition:** Identity Context established.
- **Postcondition:** returns which lifecycle state the workspace occupies and what activation, if
  any, is outstanding. Makes no change.
- **Diagnostics:** names the state, the values observed, and the missing member.
- **Failure contract:** never throws for an unactivated workspace — that is a legitimate state to
  report, not an error.

### `activateWorkspace()`

- **Precondition:** Identity Context established; an activation affordance is reachable.
- **Postcondition:** Operational Readiness (§3) holds, or a deterministic diagnostic explains which
  member could not be established.
- **Behaviour:** invokes the same affordance available to a real operator. **Must not assume an
  activation surface is already presented** — F1.3A proved none is raised automatically.
- **Idempotent:** returns immediately when already operational.
- **Failure contract, each deterministic and distinct:**
  | Condition | Diagnostic |
  | --- | --- |
  | No activation affordance reachable | names the state and that activation is operator-initiated |
  | No hospital available | reports the available set and their eligibility |
  | No journey available for the hospital | reports that the hospital has no enrolment |
  | Framework derivation returns null | reports journey activated, framework unresolved — **not an error** (§2.3 rule 2) |
  | Activation abandoned | reports the workspace unchanged |

### `verifyOperationalReadiness()`

- **Precondition:** none.
- **Postcondition:** true only when §3 holds in full.
- **Must assert** that the resolved framework id **differs** from the journey id — a framework equal
  to its journey means derivation regressed to substitution (ADR-014 Phase 2 item 1).

---

## 6. Architectural validation

| Document | Consistent? | Notes |
| --- | --- | --- |
| **ADR-014** | **Yes** | Framework-derived-from-journey is adopted here unchanged. §2.3 restates it; §5 enforces it |
| **TESTID-CONTRACT v2** | **Yes** | §5.4 storage semantics and the session-scoped expectation are consistent. Contract §2.4 Resolution Rule is this ADR's §2.3 |
| **Context Ownership Matrix (F1.4A)** | **Yes**, with the §7 exception carried forward, not resolved |

No contradiction was found between ADR-014, TESTID-CONTRACT v2 and this ADR.

---

## 7. Unresolved question — carried, not resolved

**`AuthService:95` writes `selected_hospital_id` from the JWT claim at login.**

```
auth.service.ts:95   sessionStorage.setItem('selected_hospital_id', jwtHospital);
```

This crosses the boundary this ADR draws: an **Identity** value is written into **Workspace**
Context by a component that owns neither.

Two readings, and the evidence does not distinguish them:

1. **Deliberate default** — seeding the workspace hospital from identity so a single-hospital
   operator need not choose. Sound, and would make the boundary a convenience rather than a
   violation.
2. **Accidental duplication** — predating the identity/workspace split, leaving two keys with two
   meanings and no marker of which produced the current value.

**Consequence either way:** after login, `selected_hospital_id` holds an identity-derived value
indistinguishable from a deliberate choice. **No subsystem can determine whether a workspace has been
activated or merely inherited**, which is precisely why `Workspace Activation Required` is
unenforced.

**This ADR does not resolve it.** Resolution requires an owner's intent, not inference. It is
recorded as the primary RC4 context-redesign input.

**It does not block activation automation:** §3 defines Operational Readiness on journey and
framework, neither of which `AuthService` writes. An operator can implement §5 against this ADR
today.

---

## 8. Acceptance against the brief

| Criterion | Status |
| --- | --- |
| Every lifecycle state evidence-backed | **Met** — each cites F1.3A, F1.4A or a source line |
| Identity and Workspace Context explicitly separated | **Met** — §2.1, §2.2, with the §7 exception named |
| One authoritative Operational Readiness definition | **Met** — §3 |
| Workspace Activation responsibilities fully specified | **Met** — §5 |
| ADR-014 and TESTID-CONTRACT v2 remain consistent | **Met** — §6 |
| No implementation assumptions introduced | **Met** — §5 names no tool or control |
| No product behaviour modified | **Met** — documentation only |

---

**One sentence for the next engineer:** *authentication gives you a hospital, not a workspace —
nothing activates the workspace for you, and no business operation is safe until a journey is
selected and a framework derived from it.*
