# ADR-014 — Framework Presentation vs Journey Identity

**Status:** Accepted (documents existing, intentional behaviour)
**Date:** 2026-08-07
**Supersedes:** nothing. **Amends:** `AOVIAS.Infrastructure/Aovias.ReleaseValidation/TESTID-CONTRACT.md` §2 (documentation only)
**Raised by:** RC3 Foundation F1.2A.1 identifier audit

---

## Context

AOVIAS Command presents **Compliance Frameworks** to hospital operators — NABH, JCI — because that
is the vocabulary of accreditation. Internally, a hospital does not hold a framework; it holds a
**ComplianceJourney**, which is that hospital's enrolment in a framework. The journey carries the
progress, the obligations, the evidence and the audit history.

The Context Selection dialog therefore shows a framework and selects a journey. This is correct and
deliberate. It has never been written down outside implementation comments, and every consumer that
did not read those comments has eventually written a defect.

## Problem

Two identifiers describe what an operator experiences as one choice:

- `JourneyId` — the hospital's enrolment. What the selector actually selects.
- `FrameworkId` — the accreditation standard. What audit, schedule and compliance APIs filter by.

They are not interchangeable, they are both GUIDs, and until recently one was stored in a field named
after the other. The result is a class of defect that fails **silently**: a query filtered by a
journey id where a framework id was expected matches nothing, returns an empty set, and reports
success.

That failure mode — wrong but not loud — is the reason this ADR exists.

---

## Phase 1 — Identifier lifecycle, with references

```
Hospital
  │   OrganizationHospital { id, name, isOnboarded, journey[] }
  │   organization.service.ts
  ▼
ComplianceJourney  (the hospital's enrolment in a framework)
  │   hospital.journey[] = { journeyId, frameworkId, ... }
  │   organization.service.ts:143
  ▼
ComplianceFramework  (the standard itself: name, code)
  │   reached only through the journey — never selected directly
  ▼
Framework displayed to the operator
  │   FrameworkSelectorItem { id, name, code }
  │   hospital-selector-dialog.component.ts:27-31
  │   "id is the journey id — a hospital enrols in a framework via a journey,
  │    and that is what the dialog selects. name / code describe the framework behind it."
  ▼
Journey selected
  │   selectedJourneyId() ; HospitalFrameworkSelection.journeyId
  │   hospital-selector-dialog.component.ts:34-42
  ▼
Framework derived  ← the single mapping point
  │   OrganizationService.getFrameworkIdForJourney(hospitalId, journeyId)
  │   organization.service.ts:137-144
  │   returns hospital.journey.find(j => j.journeyId === journeyId)?.frameworkId ?? null
  │   Consumers: user-profile-menu.component.ts:309 ; sidebar.component.ts:1693
  ▼
Context persisted  ← BOTH ids are stored, separately
      AppContextService
        SELECTED_JOURNEY_KEY   = 'selected_framework_journey_id'   (app-context.service.ts:29)
        SELECTED_FRAMEWORK_KEY = 'selected_framework_id'           (app-context.service.ts:31)
```

**The critical property:** `getFrameworkIdForJourney` returns `null` when the journey is unknown or
the payload predates `frameworkId`. Its documented contract is that callers then **omit the framework
filter rather than substituting a journey id** (organization.service.ts:134-136). That instruction
exists because substitution is exactly what produced the silent-empty-result defects.

---

## Phase 2 — Historical evidence

Every item below is drawn from RC2/RC3 reports or from source comments written during those fixes.

| # | Where discovered | What happened | How the ambiguity contributed |
| --- | --- | --- | --- |
| 1 | Angular context selector *(pre-RC3, recorded in source)* | A field named `frameworkId` carried a journey id | Source comment, hospital-selector-dialog.component.ts:38-41: *"this field was previously named `frameworkId` while carrying a journey id, which is what caused every framework-filtered audit query to silently match nothing."* **The clearest statement of the failure mode in the codebase.** |
| 2 | `ComplianceService` — `ResolveFrameworkOrJourneyIdAsync(hospitalId, frameworkId)` | A server-side resolver accepts *either* id under a parameter named `frameworkId` | The ambiguity is institutionalised in a function name. Used by `EvidenceLibraryService.GetUploadContextAsync` and `EvidenceReviewService.GetReviewQueueAsync`. |
| 3 | RC2S1-M4.1 — evidence consistency | Deliberately declined to add a framework predicate to `GetEvidenceStatsAsync` | Recorded reason: *"`ResolveFrameworkOrJourneyIdAsync` shows framework/journey ids are used interchangeably in places — adding a framework predicate blind could zero a currently-correct count."* **Ambiguity blocked a correct fix.** |
| 4 | RC2S1-M6 — audit reporting | `AuditReportService:47` filters `where f.ComplianceJourneyId.HasValue`; the surface is called "Audit Report" and the operator concept is framework | Findings without a journey were invisible to the report while visible in the workbench (28 vs 24). Journey — not framework — turned out to be the reportability key. |
| 5 | RC2S1-M6.1 — traceability fix | `ComplianceJourneyId` was never assigned at finding creation | The value was absent from the request DTO entirely; no caller could supply it. Diagnosis required distinguishing which of the two ids the report actually needed. |
| 6 | RC2S1-M6.1.1 — historical backfill | Rule A resolves journey via `OE → Standard → Chapter.FrameworkId → tenant journey for that framework` | The backfill had to traverse **framework** to reach **journey**, making the two-step relationship explicit in a migration. |
| 7 | RC3 F1.2A — TESTID implementation | `data-framework-id` was added carrying `framework.id`, which is a journey id | The automation contract inherited the ambiguity within hours of being written. Caught by F1.2A.1 audit before any operator consumed it. |
| 8 | `AppContextService` storage keys | `SELECTED_JOURNEY_KEY = 'selected_framework_journey_id'` | The persisted key name is itself a hybrid — evidence the ambiguity reached storage. |

**Pattern:** in six of eight cases the symptom was an **empty or under-counted result**, not an error.
This ambiguity does not throw. It under-reports.

---

## Phase 3 — Decision

### Decision

**The UI presents Frameworks. The domain identifies Journeys. Both are correct, and the boundary
between them is explicit and single.**

1. A hospital's enrolment is a **ComplianceJourney**. It is the selectable, persistable unit of
   compliance context.
2. A **ComplianceFramework** is reference data — the standard. It is what an operator recognises and
   what accreditation-scoped APIs filter by.
3. Surfaces may present framework vocabulary while carrying journey identity, **provided the
   identifier is named for what it carries.**
4. **`OrganizationService.getFrameworkIdForJourney()` is the single client-side mapping point.**
   No other client code may derive one id from the other.
5. When the mapping returns `null`, callers **omit the framework filter**. Substituting a journey id
   for a framework id is prohibited.

### Consequences

**Positive**

- Operators keep accreditation vocabulary; the domain keeps a correct model.
- One mapping point means one place to audit and one place to fix.
- The failure mode is now named, so reviewers can look for it.

**Negative**

- Every consumer must know which id it holds. The type system does not help — both are `string`.
- Two ids persist in context storage, and one key name (`selected_framework_journey_id`) still
  conflates them.
- Server-side `ResolveFrameworkOrJourneyIdAsync` remains ambiguous by design; this ADR documents it
  rather than resolving it (see RC4).

### Alternatives considered

| Alternative | Rejected because |
| --- | --- |
| **Rename the UI to speak of Journeys** | Operators do not enrol in "journeys"; they pursue NABH accreditation. Correct domain vocabulary, wrong product vocabulary. |
| **Make the selector emit a framework id** | Loses the journey, which is the entity carrying progress, obligations and history. The framework alone cannot identify a hospital's enrolment. |
| **Branded/nominal types (`JourneyId`, `FrameworkId`)** | Genuinely the right long-term fix, and it makes the compiler enforce this ADR. Rejected **for now** only because it is a cross-cutting type change during an engineering freeze. **Recommended for RC4.** |
| **One id, resolved server-side always** | Removes the client mapping point but pushes the ambiguity into the API surface, where `ResolveFrameworkOrJourneyIdAsync` shows it already causes trouble. |

### Deferred work (RC4)

Branded types; renaming `ResolveFrameworkOrJourneyIdAsync` and splitting its two behaviours; renaming
the `selected_framework_journey_id` storage key. See Phase 6.

### Automation implications

- The Context dialog exposes **journey identity only**. There is **no framework id in the DOM.**
- Automation needing a framework id must resolve it through the application, never by reading an
  attribute.
- Companion attributes must be named for the entity whose id they carry — see Phase 5.

### API guidance

- Parameters named `frameworkId` must accept **only** framework ids. Where a resolver tolerates
  either, say so in its name and XML docs (`ResolveFrameworkOrJourneyIdAsync` at least does).
- New endpoints: take `journeyId` when scoping to a hospital's enrolment; take `frameworkId` when
  scoping to a standard. Do not accept "either".
- Never default a missing framework id to a journey id.

### UI guidance

- Display framework `name` and `code`. Never display either GUID.
- Any control that selects "a framework" is selecting a journey; name its model field `journeyId`.

### Naming guidance

| Situation | Name |
| --- | --- |
| Value is a journey id | `journeyId` — even where the surface says "framework" |
| Value is a framework id | `frameworkId` |
| Storage keys | Name for the value stored, not the feature |
| DOM companion attributes | `data-journey-id` / `data-framework-id`, matching the actual value |
| Functions crossing the boundary | Name both sides: `getFrameworkIdForJourney` |

---

## Phase 4 — Canonical vocabulary

| Term | Meaning | Identity | Displayed to user? | Persisted? |
| --- | --- | --- | --- | --- |
| **Framework** | An accreditation standard (NABH, JCI). Reference data, shared across hospitals. | `frameworkId` | **Yes** — name and code | Yes — `selected_framework_id` |
| **Journey** | One hospital's enrolment in one framework. Carries progress, obligations, evidence, audit history. | `journeyId` | **No** — never named to the operator | Yes — `selected_framework_journey_id` |
| **FrameworkId** | GUID of the standard. Used by audit, schedule and compliance filters. | — | No | Yes |
| **JourneyId** | GUID of the enrolment. What the Context dialog selects and emits. | — | No | Yes |
| **Program** | `ComplianceProgramId` on `AuditSession`; a programme of audits. **Distinct from both.** Observed code format `CP########`. | `complianceProgramId` | Indirectly | Via the session |
| **Audit Context** | The open audit session within a hospital context. Session-scoped. | `sessionId` — `aovias.audit.workspace.context.v1` | Indirectly | sessionStorage |
| **Hospital Context** | The hospital + framework + journey triple an operator is working in. | `hospitalId` + `frameworkId` + `journeyId` | As hospital and framework names | Yes |

**Open item, not asserted:** the dialog's `FrameworkSelectorItem.code` is documented as the
framework's code, but the observed runtime value (`CP00000001`) matches the Programme code format. It
is recorded here as **unverified** and must be traced before automation relies on it.

---

## Phase 5 — Contract alignment (documentation changes only)

Recommended amendments to `TESTID-CONTRACT.md`. **No implementation in this ADR.**

1. **Add a naming rule to §2:**
   > Companion attributes must be named for the domain entity whose identifier they carry, not for
   > the concept the surface presents. Where UI terminology diverges from the domain, the attribute
   > follows the **domain**. See ADR-014.

2. **Correct the §2 example table:** `data-framework-id` → **`data-journey-id`** on
   `context-framework-option`.

3. **Add an explicit absence note:**
   > The Context dialog exposes no framework id. Automation requiring one resolves it through
   > `OrganizationService.getFrameworkIdForJourney()`.

4. **Flag `data-framework-code` as unverified** pending the Phase 4 open item.

---

## Phase 6 — RC4 migration considerations

Affected areas only. **No effort estimate**, per the brief.

| Layer | Affected |
| --- | --- |
| **Client types** | `FrameworkSelectorItem.id`, `HospitalFrameworkSelection.journeyId`, `OrganizationHospital.journey[]` |
| **Storage keys** | `selected_framework_journey_id` → journey-named key. **Breaking:** existing sessions lose context on upgrade unless a read-both/write-new shim ships first |
| **Client services** | `AppContextService`, `OrganizationService`, `ComplianceContextService` |
| **Server** | `ResolveFrameworkOrJourneyIdAsync` — rename and split into two intention-revealing methods; callers in `EvidenceLibraryService`, `EvidenceReviewService` |
| **APIs / DTOs** | Any parameter named `frameworkId` that tolerates a journey id. **Breaking** for external consumers; requires versioning |
| **Reports** | `AuditReportService` journey filter — semantics correct, naming should follow |
| **Automation** | `data-journey-id`; ContextOperator; TESTID contract |
| **Persistence** | None expected — `Finding.ComplianceJourneyId` is already correctly named |

### Compatibility risks

1. **Storage key rename logs every active session out of context** unless a compatibility shim reads
   the old key for one release.
2. **API parameter renames are breaking** for anything integrating with the Gateway.
3. **Branded types will surface latent misuse as compile errors** — that is the point, but the
   volume is unknown until attempted, so it must be a dedicated pass rather than a side effect.
4. **Any change to `ResolveFrameworkOrJourneyIdAsync` risks the evidence and review-queue paths**,
   which RC2 showed had never fully executed until M5.A.

---

## Exit criteria

| Criterion | Status |
| --- | --- |
| Every identifier has one meaning | **Met** — Phase 4 |
| Every ambiguity documented | **Met** — Phase 2, eight instances |
| Every recommendation evidence-backed | **Met** — all cite source or an RC2/RC3 report; one item explicitly marked unverified |
| Automation has canonical guidance | **Met** — Phase 3 and Phase 5 |
| RC4 has a migration reference | **Met** — Phase 6 |

---

**One sentence for the next engineer:** *the dialog says framework, means journey, and
`getFrameworkIdForJourney()` is the only bridge between them — everything else in this document
follows from that.*
