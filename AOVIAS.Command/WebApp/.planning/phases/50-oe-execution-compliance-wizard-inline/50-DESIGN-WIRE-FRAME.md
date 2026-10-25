# OE Execution — Compliance Wizard (Phase 50) — Wireframe & Layout Contract

**Scope:** UI/UX only inside **`app-oe-execution-wizard`**, hosted by **`ExecutionScreenComponent`**.  
**Width:** Match **Journey Execution** content area — use same **page padding** (`--space-*` / section gaps) and **max readable width** as `.execution-screen__workspace` inner columns (do not use full viewport `inset: 0` sheet).

---

## 1. Information architecture

| # | Step | Primary blocks | Existing data (no new API fields) |
|---|------|----------------|-----------------------------------|
| 1 | **Understanding** | Explanation; Why It Matters | OE description / intro copy from VM or mock mapper (`splitParagraphs` today) |
| 2 | **Compliance Guidance** | How To Comply | Current “Execute” step prose / requirement bullets |
| 3 | **Evidence** | Evidence required list; drag-drop upload | `OeEvidenceItem` list + shared file-upload UI; store upload handlers |
| 4 | **Audit Checklist** | Checkbox list | Map from current Evaluate/Improve checklist UI or scored toggles already in wizard |
| 5 | **Review & Submit** | Summary; Submit CTA | Aggregates above + `Complete` acknowledgment pattern |

### Code label mapping (Phase 44 → Phase 50)

| Index | Old step label | New step label | Notes |
|------|----------------|----------------|-------|
| 0 | Understand | Understanding | Same intent; copy blocks retitled |
| 1 | Execute | Compliance Guidance | Tasks + “how to comply” |
| 2 | Evaluate | Evidence | Upload / readiness (evaluation moved to step 3) |
| 3 | Improve | _(merged)_ | Folded into Audit Checklist when non-compliant |
| 3 | _(n/a)_ | Audit Checklist | Score, tone, justification + improvement checklist |
| 4 | Complete | Review & Submit | Summary + mark complete |

**Rename in code:** `OE_WIZARD_STEP_LABELS` → new five labels; `evidenceAck` added to `OeWizardPersistedState` (local wizard gate).

---

## 2. Desktop wireframe (≥64rem)

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│ [←]  Journey Execution                                    [Framework context]      │
╞══════════════════════════════════════════════════════════════════════════════════╡
│ WIZARD PANEL (constrained width = execution workspace inner width)               │
│ ┌──────────────────────────────────────────────────────────────────────────────┐ │
│ │ [← icon]  Framework  ›  Chapter  ›  OE          [OE CODE]  [● Status pill]  │ │
│ │ OE Title (2-line max)                              Progress ████░░  62%       │ │
│ │ Sub: Estimated time · 8 min remaining · Auto-saved 2m ago                     │ │
│ ├──────────────────────────────────────────────────────────────────────────────┤ │
│ │  (1) Understanding — (2) Guidance — (3) Evidence — (4) Checklist — (5) Review│ │
│ │  [====active====]                                                            │ │
│ ├──────────────────────────────────────────────────────────────────────────────┤ │
│ │ ┌─ Step card ─────────────────────────────────────────┐ ┌─ Optional ─────┐ │ │
│ │ │ § Understanding                                        │ │ Tips / policy │ │ │
│ │ │ [icon] Explanation                                     │ │ (reuse context│ │ │
│ │ │ …                                                      │ │  panel copy)  │ │ │
│ │ │ [icon] Why it matters                                  │ │               │ │ │
│ │ │ …                                                      │ └───────────────┘ │ │
│ │ └────────────────────────────────────────────────────────┘                   │ │
│ ├──────────────────────────────────────────────────────────────────────────────┤ │
│ │ [Save draft]                    [Previous]              [Continue →]         │ │
│ └──────────────────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────────────────┘
```

- **Breadcrumb:** Include **Framework** as first segment (stakeholder: Framework > Chapter > OE). Extend `wizardHierarchySegments()` / VM to pass framework label when available from `ComplianceContextService` or snapshot.
- **Status:** Not Started / In Progress / Completed — map from existing `journeyStatus` + OE status VM (tone classes already: `oe-wizard__badge--*`).

---

## 3. Tablet (48–64rem)

- Horizontal stepper may **truncate labels** to short titles or show step numbers + tooltip.
- Optional right rail becomes **collapsible** (same as current “Guidance” toggle).

---

## 4. Mobile (<48rem)

```
┌─────────────────────────┐
│ [←]  OE Code   [Status] │
│ Title                   │
│ Progress 62%            │
├─────────────────────────┤
│ ┃ 1 Understanding  ✓    │
│ ┃ 2 Guidance       …    │
│ ┃ 3 Evidence            │
│ ┃ 4 Checklist           │
│ ┃ 5 Review              │
├─────────────────────────┤
│ [Step card content]     │
│                         │
├─────────────────────────┤
│ [ Save draft  (full) ]  │
│ [   Previous (full)  ]  │
│ [   Next (primary)   ]  │
└─────────────────────────┘
```

- **Vertical stepper:** `flex-direction: column`; active step **left border** `--color-primary-*`.
- **Touch:** min 44px hit targets per `DESIGN_RULES.md`.

---

## 5. Focus mode (decision for implementer)

**Recommended:** When `oeWizardOpen()`, **hide** the three-column workspace (`execution-screen__workspace`) and **show only** header + wizard host so width naturally matches execution route without competing columns. Alternative: overlay with **dimmed** background — heavier engineering; prefer hide for “same width” clarity.

---

## 6. Visual tokens (reference only — use project token names)

- Cards: `--radius-card`, `--border-width-hairline`, `--shadow-xs` or `--shadow-sm`
- Info / success / warning / danger: map to `--color-semantic-*` or documented journey tones
- Spacing: `--section-gap-*`, `--space-*`

---

## 7. Interactions

| Action | Behavior |
|--------|----------|
| Next | Validates **current step** inline; scroll to first error |
| Previous | No validation rollback |
| Save draft | Sets dirty + persists sessionStorage (existing pattern) |
| Auto-save hint | Optional static string + last saved timestamp if store exposes timer |
| Resume Journey | Same as today — opens wizard at `oeWizardStepIndex` from persistence |

---

## Document status

Aligned with stakeholder brief (April 2026) and Phase 50 roadmap entry.
