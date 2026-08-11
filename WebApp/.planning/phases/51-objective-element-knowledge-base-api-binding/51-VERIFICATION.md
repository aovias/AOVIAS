---
phase: 51-objective-element-knowledge-base-api-binding
verified: 2026-04-13T00:00:00Z
status: gaps_found
score: 0/6 must-haves verified
re_verification:
  previous_status: pending
  previous_score: 0/4
  gaps_closed: []
  gaps_remaining:
    - "No KnowledgeBaseDataService or node details load in src/"
    - "Wizard copy is not KB-backed; mapping table in plan omits howToComply and misaligns score scale"
  regressions: []
gaps:
  - truth: "OE wizard shows API-backed KnowledgeItem / requirement text where the UI promises explanations, guidance, evidence, and audit content"
    status: failed
    reason: "Phase 51 not implemented: no knowledge-base service, store signals, or template bindings in codebase."
    artifacts:
      - path: "src/app/features/journey/components/oe-execution-wizard/oe-execution-wizard.component.html"
        issue: "Step 0 Explanation uses splitParagraphs(h.oeTitle); steps 1–3 use static or operational-mode copy; no NodeDetailsDto/KnowledgeItemDto."
    missing:
      - "KnowledgeBaseDataService + load path per 51-01-PLAN"
      - "Store/facade + template bindings per 51-02-PLAN"
  - truth: "Plan-documented field mapping matches gateway DTOs and wizard sections without semantic errors"
    status: failed
    reason: "51-02-PLAN must_haves omit howToComply for step 1; conflate step 0–1 with only explanation+whyItMatters; silent on RequirementScoreDto.maxScore vs UI 1–5."
    artifacts:
      - path: ".planning/phases/51-objective-element-knowledge-base-api-binding/51-02-PLAN.md"
        issue: "Mapping bullet incomplete; no explicit non-binding note for RequirementDetailDto.classification vs wizard eval tone."
    missing:
      - "Corrected mapping table (see body) and explicit out-of-scope for Tasks / Smart assist"
human_verification:
  - test: "With real Gateway JWT and aligned OE↔node UUID, open OE wizard and confirm displayed strings match KB CMS content for the node."
    expected: "Explanation / why it matters / how to comply / evidence / audit checklist text match authoritative content."
    why_human: "Content parity and ID correlation require live API and product-confirmed mapping."
  - test: "Confirm evaluation score scale (1–5 vs 0–10) with auditors."
    expected: "UI scale and labels match regulatory intent and RequirementScoreDto.maxScore."
    why_human: "Product decision; not derivable from grep."
---

# Phase 51: Objective Element Execution — Knowledge Base API Binding

**Phase Goal (ROADMAP):** Bind OE execution screen / wizard to Gateway Knowledge Base HTTP API with typed DTOs, `GET` node details and knowledge by node, `ApiResponse<T>` + auth; complement Phase 49 journey load; resolve OE id ↔ KB node id with backend.

**Verified:** 2026-04-13  
**Status:** gaps_found  
**Re-verification:** Yes — prior checklist was “pending execution”; this pass adds DTO↔wizard mapping review.

## Goal Achievement (goal-backward)

Executing **only** the current `51-01` / `51-02` plans would **not** yet achieve the roadmap goal because **no implementation exists in `src/`** (no `knowledge-base-data.service.ts`, no store fields for `NodeDetailsDto`). Even after Wave 1+2 ship, **51-02’s stated mapping line is incomplete** relative to the gateway DTOs and the actual wizard template: it under-specifies **step 1 “How to comply”** (`howToComply`), does not resolve **score scale** (`RequirementScoreDto.maxScore` vs **1–5** range input), and risks **wrong binding** of `RequirementDetailDto.classification` to **evaluation tone** radios.

Do **not** trust SUMMARY files for this phase (none provided); evidence is from `oe-execution-wizard` templates and plans.

---

## Corrected mapping: wizard steps 0–4 ↔ gateway DTO fields

Authoritative DTO shapes per product spec cited in ROADMAP / user input:  
`NodeDetailsDto` aggregates `node` (`ComplianceNodeDto`), `requirement` (`RequirementDetailDto`), `knowledge` (`KnowledgeItemDto[]`), `requirementScore` (`RequirementScoreDto`).  
`GET .../knowledge/node/{nodeId}` returns `KnowledgeItemDto[]` only (narrower than node details).

| Step | Wizard section (UI) | Primary API source(s) | Notes |
| --- | --- | --- | --- |
| **0** | Card **Explanation** | `KnowledgeItemDto.explanation` (from chosen item in `knowledge[]`) | Replace current `splitParagraphs(h.oeTitle)`; optionally show `ComplianceNodeDto.code` / `title` as kicker, not as full body. **Multi-item `knowledge[]`:** define rule (first `isActive`, highest `version`, or concatenate) — spec does not define merge; document in mapper. |
| **0** | **Why it matters** | `KnowledgeItemDto.whyItMatters` | Replace static `<ul>` bullets; if API returns prose, split lines or render as formatted text — do not assume array. |
| **0** | **Metadata** chips | Mixed: existing `journeyContextPanelVm` + optionally `RequirementDetailDto.applicableDepartments`, `requiresDocumentation`, `ComplianceNodeDto` metadata | Not purely KB; keep journey/session fields unless product says otherwise. |
| **0** | **Smart assist** | Out of scope | `POST /ai/ask` — already excluded in 51-02; do not map to DTO fields. |
| **1** | **How to comply** | `KnowledgeItemDto.howToComply` | **Required** for KB-backed guidance; **not** covered by “explanation + whyItMatters only” in current 51-02 must_haves. |
| **1** | **Tasks** | *No field in provided spec* | Remains stub, journey task API, or explicit backlog — do not pretend KB supplies tasks. |
| **1** | **Evidence expectations** | Operational mode copy + optional teaser from `KnowledgeItemDto.evidenceRequired` | Hybrid UX; spec has no separate “expectations” string. |
| **2** | **Evidence required** (body) | `KnowledgeItemDto.evidenceRequired` | Align with step 1 teaser to avoid contradiction. |
| **2** | Dropzone | N/A (upload wiring separate) | KB describes what evidence is required, not file storage. |
| **3** | **Score** (`range` 1–5) | `RequirementScoreDto.maxScore` (**example 10** in spec) | **Mismatch:** UI is **1–5**; API suggests **0–10** (or other) ceiling. Needs **product rule**: scale transform, change control, or UI change — **do not silently bind**. |
| **3** | **Status** radios (Compliant / Partial / Non-compliant) | **Local wizard state** (current implementation) | **Do not** map to `RequirementDetailDto.classification` unless product confirms identical enum/semantics — classification may be NABH category, not audit tone. |
| **3** | **Justification** | Local state | No KB field in provided DTO list. |
| **3** | **Improvement checklist** (conditional) | *Not in spec* (static today) | If backend adds findings API later — separate phase. |
| **3** | **Audit checklist text** (if product wants KB prose here) | `KnowledgeItemDto.auditChecklist` | Current UI **does not** surface this string prominently; it shows score + tone. If `auditChecklist` is required content, add a subsection or replace static improvement lines — **plan should say where** (step 3 reading area vs step 4 summary). |
| **4** | **Review & Submit — Summary** | Echo local wizard state + optional KB titles (`ComplianceNodeDto.title`, first line of `explanation`) | `POST` decisions/assessments out of scope per 51-02. |

---

## Issues (severity)

| Severity | Issue |
| -------- | ----- |
| **Blocker** | No KB client or bindings in codebase — roadmap goal not started in `src/`. |
| **Blocker** | **Score scale:** `RequirementScoreDto.maxScore` (e.g. 10) vs wizard **1–5** — implementing “bind to API” without resolving this yields incorrect audit semantics. |
| **Major** | **51-02 plan mapping** omits **`howToComply`** for **step 1** while UI has a dedicated “How to comply” card. |
| **Major** | Risk of **mis-binding** `RequirementDetailDto.classification` to **eval tone** — different concepts unless explicitly aligned by backend. |
| **Major** | **`auditChecklist` placement:** plan says use in “later steps” but **step 3** UI is dominated by scoring, not checklist prose — needs UX decision. |
| **Minor** | **Tasks** list and **Smart assist** should be labeled **non-KB** in must_haves to avoid implementer confusion. |
| **Minor** | **`GET /knowledge/node/{nodeId}`** returns array only — **preferred aggregate** `GET .../nodes/{id}/details`** (51-01) should be the single load for rich mapping; document fallback when only array endpoint is available. |

---

## Suggested concrete edits to `51-02-PLAN.md`

**must_haves — replace the mapping bullet with:**

- [ ] **Field mapping (documented in `knowledge-base-oe-wizard.mapper.ts` or equivalent):** For a resolved `NodeDetailsDto` and a chosen `KnowledgeItemDto` (document selection rule for `knowledge[]`):
  - Step **0**: `explanation` → Explanation card; `whyItMatters` → Why it matters; do **not** use `oeTitle` as Explanation body when KB data exists.
  - Step **1**: `howToComply` → “How to comply”; `evidenceRequired` optional teaser for “Evidence expectations”.
  - Step **2**: `evidenceRequired` → “Evidence required” body.
  - Step **3**: If displaying KB audit prose, `auditChecklist` → dedicated checklist section; **score UI** must document transform or alignment with `RequirementScoreDto.maxScore` (do **not** assume 1–5 equals API max).
  - Step **4**: Summary echoes local evaluation + KB titles as needed.
- [ ] **Explicit non-goals:** Tasks list (no DTO), Smart assist (`POST /ai/ask`), improvement checklist static strings — remain stub or follow-up unless new API exists.
- [ ] **Classification:** Wizard eval tone (`compliant` / `partial` / `non`) remains **local state** unless product maps `RequirementDetailDto.classification` — document “do not bind” in mapper.

**Tasks — add:**

- `<task id="51-02-e" title="Score scale decision">` Document product/engineering decision for `RequirementScoreDto.maxScore` vs 1–5 UI (slider max, mapping function, or API change). `</task>`

---

## Observable truths (verification status)

| # | Truth | Status | Evidence |
| --- | --- | --- | --- |
| 1 | Wizard Explanation / Why it matters show KB `explanation` / `whyItMatters` when loaded | ✗ FAILED | Step 0 uses `h.oeTitle` and static bullets (`oe-execution-wizard.component.html`). |
| 2 | Compliance Guidance shows KB `howToComply` | ✗ FAILED | Step 1 static prose; no store KB VM. |
| 3 | Evidence step shows KB `evidenceRequired` | ✗ FAILED | Step 2 generic static text. |
| 4 | Audit step aligns with `auditChecklist` / `requirementScore` | ✗ FAILED | No KB fields; score range 1–5 with no `maxScore` link. |
| 5 | Loading/error/retry for KB | ✗ FAILED | No KB load path. |
| 6 | Single preferred `getNodeDetails` + code fallback | ✗ FAILED | Not implemented. |

**Score:** 0/6 truths verified.

---

## Anti-patterns / stubs (wizard)

- `quickAction` in `oe-execution-wizard.component.ts` is an empty stub (documented).
- Step 1 Tasks: hardcoded rows.
- Step 3 improvement checklist: static bullets.

---

_Verified: 2026-04-13_  
_Verifier: Claude (gsd-verifier) — mapping-focused addendum_
