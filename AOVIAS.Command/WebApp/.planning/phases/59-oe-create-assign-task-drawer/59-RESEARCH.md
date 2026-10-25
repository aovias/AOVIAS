# Phase 59 — Research: Create & Assign Task (right drawer)

## RESEARCH COMPLETE

### Product intent

Deliver a **modern enterprise SaaS** “Create & Assign Task” experience as a **right-side drawer**, opened from **`oe-wizard__quick-btn`** (“Add task”) on the OE execution wizard context rail. UX references: **Notion** (clean sections, inline validation), **Linear** (priority, compact metadata), **Azure Portal** (dense forms, sticky footers, section icons).

### Stack constraints (WebApp)

- **Angular 21** standalone components, **Signals** + **OnPush** where applicable.
- Styling: project **`DESIGN_RULES.md`** + **`src/styles/tokens/*`** are authoritative. **Tailwind CSS v4** is present (`@tailwindcss/postcss`); use it **only** if utilities map to tokens (extend theme) or for layout helpers that do not introduce one-off hex/radius values. Prefer **SCSS + CSS variables** alongside existing Journey patterns (`oe-wizard__*`, BEM) for consistency with Phase 50/58 work.
- Icons: **`@ng-icons/lucide`** (already used in wizard).
- Forms: **Reactive forms** + inline field errors (no `alert()`); optional **Angular Material** (`MatFormField`, `MatSelect`, `MatDatepicker`, `MatChips`/`MatAutocomplete` for assignees) — align with Material M3 overrides already in repo.

### Drawer implementation options

| Approach | Pros | Cons |
|----------|------|------|
| **`MatDialog`** with `panelClass`, `width`, `height: 100%`, `position` strategy for right edge | Focus trap, ESC, `role="dialog"`, `restoreFocus` | Needs custom panel styles to feel like drawer not centered modal |
| **CDK Overlay** + template | Full control | More a11y/scroll plumbing |
| **In-template fixed panel** in wizard (signal `drawerOpen`) | No portal; simple data flow from store | Must handle focus trap (use **`cdkTrapFocus`** like wizard root) |

**Recommendation:** **`MatDialog`** opening a **tall right-aligned panel** (or full-viewport on mobile) with `disableClose: false`, `autoFocus: 'first-tabbable'`, `panelClass: 'praman-task-drawer-dialog'`. Alternative acceptable: **inline slide-over** in `OeExecutionWizardComponent` with backdrop + `cdkTrapFocus` if team wants zero dialog service coupling.

### Responsive behavior

- **Desktop/tablet:** drawer width **~min(440px, 92vw)**; **left backdrop** dims shell; scroll **inside** drawer body.
- **Mobile (e.g. &lt; 640px):** **full viewport** width/height; header + sticky footer remain; sections stack with **16px** spacing system (use `--space-4` / token equivalent).

### Data binding sources (session / OE context)

- **`JourneyWorkspaceStore`**: `snapshot()?.journeyId`, `selectedOeId()`, `findOe(id)` → `ObjectiveElement` (`code`, `title`, `standardId`, `evidenceStatus`, department/assignee labels if present).
- **Wizard header VM**: `store.oeWizardHeaderVm()` — `oeCode`, `oeTitle`, journey status label.
- **Knowledge / compliance context** (optional Phase 51 fields): use for **default “evidence required”** or tags only if already on VM; do not invent new API fields.
- **Organization / members**: `OrganizationService` / `organization-detail` members list for **assignee pickers** (multi-select + avatars: initials from `firstName`/`lastName`, optional `profilePicUrl` per Phase 31 model).

### Auto-fill rules

When opened from wizard:

- **Linkage:** prefill **Standard** (from selected standard in store / OE’s `standardId` resolved to label via snapshot) and **Objective** (OE code + title).
- **Journey id** attached to payload as hidden context for future API (store only in form model until POST exists).

### Validation (inline)

- Required: **title** (min length 2–3), **due date** (if product mandates), at least one **assignee** OR allow empty with warning — **clarify in implementation** from product; plan defaults: title required, due date optional, assignee optional with chip hint.
- Show **`mat-error` / inline text** under fields; disable **Create Task** until form valid; **Save Draft** may allow partial (local state only).

### Attachments

- Reuse patterns from **OE evidence** Phase 54.2: hidden `input type="file"`, optional link to **`/app/evidence`**; if no task API, **UI-only** file list with clear “not persisted” or wire to future endpoint placeholder.

### Reusable subcomponents (suggested)

- `TaskDrawerSectionHeaderComponent` — icon + title + optional subtitle.
- `PriorityBadgeComponent` or map to existing badge tokens — color-coded **Low / Medium / High / Critical** per design tokens (semantic status colors).
- `AssigneeChipsComponent` — multi-select + avatar stack.

### Out of scope for Phase 59 (unless roadmap expands)

- **POST /tasks** real backend — use **mock submit** + snackbar success or `console.debug` behind flag, consistent with other journey stubs.

### References in repo

- `oe-execution-wizard.component.*` — quick actions, context panel.
- `journey-context-panel.component.*` — “Add Task” tile pattern.
- `DESIGN_RULES.md` — spacing, radii, shadows.

---

## RESEARCH COMPLETE
