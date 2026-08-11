# OE Execution Full-Screen Wizard — UX & Structure Specification

**Product:** PRAMAN Journey — OE compliance execution  
**Entry:** **Start / Execute Journey** (Journey Context Panel, execution screen)  
**Rule:** All visual styling uses **`DESIGN_RULES.md`** and **`src/styles/tokens/*`** — no ad-hoc literals in production UI.

---

## 1. Experience principle

This is a **guided execution workspace**, not a long form. The user should feel: *“I am being guided step-by-step to complete compliance.”* Actions and **next-step guidance** take priority over dense data dumps.

---

## 2. Modal shell

| Aspect | Behavior |
|--------|----------|
| Size | **Full viewport** width and height (true overlay workspace). |
| Backdrop | App content **dimmed** using overlay token (`--overlay-backdrop` / semantic equivalent). |
| Layer | Above shell; use documented z-index (`--z-modal` family). |
| Close | **X** (and optional **Save / Exit**). **Unsaved** or **in-progress** state → **confirmation** before dismiss. |
| Resume | **Persist** current step (and minimal wizard state) so the user can return later; scope by user + OE (see research: session vs API). |

---

## 3. Four-region layout

1. **Header bar (fixed top)**  
2. **Wizard stepper (spine)** — below header or integrated into header band  
3. **Main content** — scrollable; content swaps per step  
4. **Right context panel** — persistent; **always visible on desktop**; collapses on smaller breakpoints  

---

## 4. Region A — Header bar (fixed)

| Zone | Content |
|------|---------|
| **Left** | **Breadcrumb:** Framework → Chapter → Standard → OE (truncation rules: ellipsis on small widths). |
| **Center / left-secondary** | **OE code** (strong) + **title** (prominent, wraps within max lines). |
| **Right** | **Status badge** (Compliant / Partial / Non-compliant per existing execution semantics); **Close**; optional **Save / Exit**. |

**Interaction:** Header stays visible while main content scrolls.

---

## 5. Region B — Wizard stepper (five steps)

**Steps (order):**  
1. **Understand** → 2. **Execute** → 3. **Evaluate** → 4. **Improve** → 5. **Complete**

| Behavior | Rule |
|----------|------|
| Current step | Visually **highlighted** (primary accent per tokens). |
| Completed | **Checkmark** (✔) or equivalent completed state. |
| Locked | **Future** steps not yet unlocked — **disabled** or non-interactive styling. |
| Click | **Only** if step is **unlocked** (per locking rules below). |
| Responsive | **Desktop:** horizontal stepper. **Tablet:** visible; may compress labels. **Mobile:** **horizontal scroll** or **compact tabs**; step names shorten or icon-only. |

---

## 6. Region C — Main content (dynamic)

**Global:** Spacious vertical rhythm; **single primary column**; **scrollable** (`praman-scrollbar` utilities); **no** cluttered grids of unrelated metrics.

### Step 1 — Understand

**Goal:** User understands the requirement.

- OE **title** + **description** (readable typography scale from tokens).  
- **“What is required”** — bullet list of expectations.  
- **Tags:** Department, Role, Frequency (chip pattern per `DESIGN_RULES`).  
- **Supporting:** Sample documents (preview / download); **reference links** (external icon + link style).  
- **Smart assist (optional):** AI summary block; suggested actions (secondary).  
- **Primary CTA:** **Proceed to Execute**.

---

### Step 2 — Execute

**Goal:** User performs required actions.

- **Task management:** List of tasks linked to OE; each row: title, status (To Do / In Progress / Done), assignee, due date, priority.  
- **Actions:** Add task, assign, change status, comments.  
- **Evidence (light):** Quick upload; **indicator** when evidence missing.  
- **Primary CTA:** **Proceed to Evaluate** — **enabled only** when **minimum execution conditions** met (define in implementation: e.g. required tasks done + evidence threshold).

---

### Step 3 — Evaluate

**Goal:** Assess compliance.

- **Score:** **1–5** visual scale (aligned with NABH / existing score semantics).  
- **Status:** Compliant / Partially compliant / Non-compliant.  
- **Justification:** **Required** for Partial / Non.  
- **Remarks:** Optional.  
- **Warnings:** Missing evidence, incomplete tasks (non-blocking display with clear remediation links).  
- **Primary CTA:** **Submit Evaluation**.

---

### Step 4 — Improve

**Goal:** Address gaps.

- **Shown when:** Non-compliant or open findings (see locking rules).  
- **Findings:** List with **severity**.  
- **Actions:** Create action plan, link tasks, update evidence.  
- **Tracking:** Open / In Progress / Resolved.  
- **Primary CTA:** **Mark Ready for Completion** — **only** when **no open** critical issues (policy-defined).

---

### Step 5 — Complete

**Goal:** Finalize.

- **Summary checklist:** Tasks completed, evidence uploaded, score submitted, findings resolved (each ✔ or warning).  
- **Final compliance status** + **completion timestamp**.  
- **Primary CTA:** **Mark as Complete**.

---

## 7. Region D — Right context panel (persistent)

**Always visible on desktop** beside main content.

| Block | Purpose |
|-------|---------|
| OE summary | Code + short title |
| Progress | Step progress (e.g. “Step 3 of 5”) + determinate indicator |
| Current step | Label |
| Status badge | Same family as header |

**Most important — Next action:** Short, imperative copy (e.g. “2 tasks pending”, “Upload evidence”, “Submit evaluation”). Updates each step.

**Quick actions:** Upload evidence, Add task, View timeline (secondary actions; icon + label per enterprise patterns).

**Tablet:** Panel becomes **slide-over** (toggle or auto-collapse).  
**Mobile:** **Bottom sheet** or **collapsible** section — **above** or **below** main CTA per usability; do not hide “Next action” without a discoverable control.

---

## 8. Smart behavior

### Step locking

| Transition | Rule |
|------------|------|
| Execute | After **Understand** acknowledged / completed. |
| Evaluate | After **minimum execution** conditions satisfied. |
| Improve | **Only** when evaluation indicates **gaps** / non-compliance / open findings (product rule). |
| Complete | After **Improve** satisfied (or skipped if not applicable) and **all** completion prerequisites met. |

### Dynamic guidance

- **Always** surface **next action** in context panel.  
- **Inline** highlights for missing items (e.g. empty evidence, pending tasks).

### State persistence

- Persist **step index + dirty form flags + IDs** needed to resume; **clear** on successful completion or explicit abandon.

---

## 9. Responsiveness summary

| Viewport | Layout |
|----------|--------|
| Desktop | Main **center** + **right** context panel (two-column body). |
| Tablet | Context **slide-over**; stepper remains visible. |
| Mobile | **Stacked**; stepper **horizontal scroll** or compact; context **bottom sheet** or **accordion**. |

---

## 10. Theming & micro-interactions

- **Light / dark:** Full parity via `data-theme` and semantic tokens.  
- **Motion:** Step change uses **subtle** transition (`prefers-reduced-motion` respected).  
- **Feedback:** Success / warning via existing **snackbar** or inline status patterns (tokens only).

---

## 11. Accessibility

- **Keyboard:** Navigate steps (where unlocked), focus order logical, **Escape** closes with confirm if dirty.  
- **Focus:** Visible focus rings (`--shadow-focus-ring-primary` / documented patterns).  
- **ARIA:** Wizard region; **current step**; **progressbar** or **aria-current** on active step; **live region** optional for step changes.  
- **Screen readers:** Step titles and headings structured (h1–h3 hierarchy).

---

## 12. Design principles (guardrails)

**Do:** Clean hierarchy, one primary CTA per step, clear **next action**, token-only styling.  
**Do not:** overload screens, break linear guidance with unrelated navigation, **hardcode** colors or spacing in components.

---

## Stakeholder brief checklist (Plan 01 validation)

| Requirement | § Reference |
|-------------|-------------|
| Full-viewport overlay, dimmed background | §2 |
| Close + unsaved confirmation; resume persistence | §2 |
| Four regions + five named steps | §3–7 |
| Entry from Start/Execute Journey (Context Panel) | §1 |
| Next action + right context panel | §7 |
| Tablet slide-over / mobile bottom sheet or stack | §7, §9 |
| Step locking (Evaluate after execution; Improve conditional; Complete gating) | §8 |
| Light/dark tokens only | §10 |
| Keyboard, focus, ARIA | §11 |
