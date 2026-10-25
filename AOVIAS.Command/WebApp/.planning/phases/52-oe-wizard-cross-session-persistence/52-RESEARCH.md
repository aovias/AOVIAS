# Phase 52 — Research: OE Execution Wizard Cross-Session Persistence

**Date:** 2026-04-13  
**Question:** What must be stored (session + database) so a user can resume the OE execution wizard on another computer?

---

## 1. Current client persistence (sessionStorage)

**Key:** `journey.wizard.${userId}.${journeyId}.${objectiveElementId}`  
(`JourneyWorkspaceStore.wizardStorageKey()`)

**Serialized type:** `OeWizardPersistedState` (`oe-execution-wizard.model.ts`)

| Field | Purpose |
|-------|---------|
| `stepIndex` | Current step (0–4, maps to `OE_WIZARD_STEP_LABELS`) |
| `understandAck` | Step 0 — Understanding acknowledged |
| `executeReady` | Step 1 — Compliance Guidance ready |
| `evidenceAck` | Step 2 — Evidence step gate |
| `evalSubmitted` | Step 3 — Audit checklist / evaluation submitted |
| `evalScore` | 1–5 score |
| `evalTone` | `compliant` \| `partial` \| `non` |
| `evalJustification` | Optional text |
| `needsImprove` | Improvement path flag |
| `improveReady` | Improvement readiness |
| `completeAck` | Step 4 — Review & Submit acknowledgment |

**Not persisted today (gaps for “full resume”):**

1. **`auditChecklistChecked: boolean[]`** — Lives only in `OeExecutionWizardComponent` (`auditChecklistChecked` signal). Reset when KB `auditChecklist` text changes (content key). **Must** be serialized if the user completed partial checklist lines.
2. **Optional: `auditChecklistContentKey` or hash** — If KB text changes server-side, client can detect mismatch and reset or merge (product decision).
3. **`oeWizardOpen`** — Ephemeral UI; do **not** persist to DB (avoid “stuck open” on another device). Session may optionally remember for same-browser UX only.
4. **`contextPanelOpen`** — Pure UI; do not sync.
5. **Knowledge base payload** (`knowledgeBaseNodeDetails`, loading/error) — **Not** wizard state; refetch by node id after load. Same for journey context panel VM (evidence counts come from OE/journey APIs).

---

## 2. Identity and scoping (server)

Minimum composite key for a **draft** row or document:

- **User** (subject from JWT — must match row owner).
- **Hospital / tenant** (if multi-tenant; align with existing Journey APIs).
- **`journeyId`**
- **`objectiveElementId`** (OE id in journey domain; align with Phase 49 DTOs).

Optional: **`knowledgeBaseNodeId`** for audit/debug — wizard progress is still **OE-scoped** as primary.

---

## 3. Recommended payload shape (API + DB JSON column)

Extend `OeWizardPersistedState` into a **versioned** envelope:

```ts
interface OeWizardDraftPayloadV1 {
  version: 1;
  stepIndex: number;
  understandAck: boolean;
  executeReady: boolean;
  evidenceAck: boolean;
  evalSubmitted: boolean;
  evalScore: number;
  evalTone: 'compliant' | 'partial' | 'non';
  evalJustification?: string;
  needsImprove: boolean;
  improveReady: boolean;
  completeAck: boolean;
  /** Parallel to component `auditChecklistChecked`; omit or empty if no checklist lines */
  auditChecklistChecked?: boolean[];
}
```

**Concurrency:** Store **`updatedAt`** + integer **`revision`** (or ETag) on server; client sends **`If-Match`** / `revision` on PATCH to avoid blind overwrites.

---

## 4. Load / merge order (web portal)

1. **Authenticate** → resolve `userId`, hospital, `journeyId`, `selectedOeId`.
2. **GET wizard draft** (if endpoint exists) → apply to store + extend session snapshot.
3. **Else** → **`restoreWizardFromStorage()`** from `sessionStorage` (same key as today).
4. **Merge rule (if both exist):** Prefer **newer** by `updatedAt` vs session `savedAt` (add optional `savedAt` in session JSON). If equal, prefer **server** for cross-device truth.
5. **KB load** → `getNodeDetails` / knowledge APIs as today; then **reconcile** `auditChecklistChecked` length with current line split (truncate/pad false if line count changed).

---

## 5. Backend dependency

Phase 49 roadmap notes **wizard progress API not implemented** on Gateway. Phase 52 **requires** product/backend agreement on one of:

- **`PATCH /journeys/{journeyId}/objective-elements/{oeId}/wizard-draft`** (or nested under existing OE resource), **or**
- **Dedicated progress** sub-resource with **JSON body** + **revision**.

Until the API ships, implement **client-only** improvements: full `OeWizardPersistedState` + checklist array in session; **feature-flag** server sync.

---

## 6. Security

- Enforce **same user + same tenant + same journey** on every read/write.
- Do not put secrets in persisted JSON.
- Session key already includes `userId` — keep that to avoid cross-user leakage on shared machines (still clear on logout if applicable).

---

## RESEARCH COMPLETE

Findings are sufficient to plan: extend model + store + component; add optional HTTP service + merge; coordinate Gateway contract.
