# Phase 46 — Research: Shell Left Sidebar as Operational Command Center

## Research question

What must be true for the **shell left navigation** to function as an **operational command center** (guidance, state, actions, context) while remaining **enterprise-scalable** (50+ destinations), **non-destructive** (existing modules preserved), and **aligned** with PRAMAN’s existing shell patterns (hospital/framework context, responsive drawer, theme tokens)?

## Findings

### 1. Current foundation (do not throw away)

- The shell already separates **identity** (header) from **wayfinding** (sidebar) and uses a **context block** for hospital + framework with a **switcher dialog**.
- **Responsive** behavior exists: overlay drawer on small viewports, inline sidebar on larger; **theme** tokens drive surfaces and text.
- **Journey** is already a primary destination in the mental model of recent phases; the sidebar should **amplify** that with hierarchy, not duplicate journey-specific panels from Phase 41–42.

### 2. Transformation axis: static list → operational surface

| Dimension | From | Toward |
|-----------|------|--------|
| Purpose | Navigate | Navigate + **orient** + **act** |
| Context | Hospital + framework only | + **operational mode** (assessment / audit phases) + **progress signal** |
| Work visibility | Mostly implicit | **Action Center** (findings, risks, alerts) with **badges** |
| Density | Single expansion model | **Expanded** vs **collapsed rail** with tooltips |
| Scale | Few top-level groups | **Named sections** + **config-driven** growth for many modules |

### 3. Information architecture principles

- **Stable section order** (top → bottom): Brand / **Context** / Primary work / **Action & risk** / Audit lifecycle / Knowledge / Admin.
- **One primary “home” for execution**: **Journey** under Compliance Execution, visually **primary** (weight, placement, optional default route behavior — product decision in execution).
- **Action Center** is a **cross-cutting** group: it aggregates “things needing attention” and must not duplicate deep feature UIs—only **entry points** and **counts/severity**.
- **Audit Management** items reflect **lifecycle**; **disabled** states communicate “not available in current mode” better than hiding (unless role forbids entirely).

### 4. Context panel: minimum viable “command” story

- **Organization / facility** and **active framework** remain mandatory anchors.
- **Mode** (e.g. Self Assessment, Internal Audit, Final Audit) must be **visible at a glance** and **changeable** via the same family of flows as framework switch (single switcher vs separate control — plan phase decides; consistency matters).
- **Compliance progress** is optional in v1 but **reserved space** (progress ring, bar, or compact percentage) avoids rework when data exists.

### 5. Badges and real-time expectations

- Badges are **signals**, not dashboards: **neutral** (count), **warning**, **critical** — tied to **polling**, **WebSocket**, or **lazy refresh on expand** to avoid hammering APIs (execution detail).
- Placement: **end-aligned** on row or **superscript** on icon in collapsed mode; **consistent** across groups.

### 6. Collapsed mode

- **Icons-only** rail with **accessible names** (`aria-label` / tooltip) is mandatory.
- **Expand** via **pin** or **hover flyout** is a product choice; **preserve width state** in session or profile storage (implementation phase).
- **Smooth transition** (width + opacity) supports perceived quality; respect **prefers-reduced-motion**.

### 7. Quick actions

- **Small fixed strip** (top or bottom of scroll region) with **3–5** high-value actions (e.g. add task, upload evidence, log finding).
- Must **not** compete with primary nav; **icon-forward**, **keyboard reachable**, **duplicate** longer paths elsewhere.

### 8. Context-aware behavior (phased)

- **Phase A (UX spec)**: define **rules** (when to emphasize Journey, when to show Audit items disabled, when Action Center is empty).
- **Phase B (execution)**: bind to **role**, **framework**, **mode**, and **route** via a single **navigation model** (config + runtime predicates — not in this research file).

### 9. Accessibility

- **Focus order** matches visual order; **drawer** traps focus when open on mobile; **Escape** closes; **visible focus** on all items.
- Collapsed mode: **tooltips** do not replace **accessible name** on interactive elements.

### 10. Risks

| Risk | Mitigation |
|------|------------|
| Navigation config becomes unmaintainable | Single **registry** with section + sort order + feature flags |
| Badge latency / wrong counts | Conservative defaults; skeleton or omit until loaded |
| Cognitive overload | Strict **section caps** and progressive disclosure (submenus) |
| Conflict with header notifications | **Different scopes**: header = alerts feed; sidebar = **work queues** linked to destinations |

## RESEARCH COMPLETE

This research informs `46-01-PLAN.md` through `46-03-PLAN.md`. No implementation code is specified here.
