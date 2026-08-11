# PRAMAN Themed Scrollbar System — Design Specification

**Audience:** Design system, UX, and implementation.  
**Rule:** Every visual value must resolve through **`src/styles/tokens/*`** (including new scrollbar semantic aliases that reference existing `--color-*`, `--radius-*`, `--space-*`, motion, and opacity patterns). **No hardcoded literals** in feature styles.

---

## 1. Principles

| Principle | Definition |
|-----------|--------------|
| Minimal | Thin track and thumb; scrollbar is never the dominant visual element. |
| Context-aware | Same design language; **visibility weight** changes by context variant (primary surface vs panel vs dense list). |
| Consistent | One system app-wide; no one-off scrollbar styles per module. |
| Discoverable | Idle state is subtle; hover, scroll, drag, and focus make affordances clear. |
| Enterprise | Dark-first harmony; light theme parity using the same token families. |

---

## 2. Semantic tokens (names to define in theme/foundation)

Implementations **must** bind these names to existing palette and motion tokens (light/dark blocks). Values are **not** listed here — they live only in token files.

### Structure

| Token role | Purpose |
|------------|---------|
| `--scrollbar-width` | Default (desktop) scrollbar thickness. |
| `--scrollbar-width-touch` | Tablet / touch-friendly step (slightly wider). |
| `--scrollbar-radius` | Thumb capsule radius; maps to `--radius-sm` or `--radius-md` (rounded, no sharp edges). |
| `--scrollbar-track` | Track / gutter fill (low contrast vs surface). |
| `--scrollbar-thumb` | Idle thumb fill. |
| `--scrollbar-thumb-hover` | Hover thumb fill (higher contrast). |
| `--scrollbar-thumb-active` | Drag/active thumb (brand-aligned accent via `--color-brand-primary` / legacy `--primary-color` and `color-mix`). |
| `--scrollbar-thumb-focus` | Optional thumb emphasis when scroll region has **focus-visible** (may match active or a dedicated focus step). |
| `--scrollbar-opacity-idle` | Optional idle opacity for thumb (token, not literal in components). |
| `--scrollbar-opacity-hover` | Stronger apparent opacity or contrast on hover. |
| `--scrollbar-motion-duration` | Transition duration for state changes. |
| `--scrollbar-motion-easing` | Easing curve for state changes. |

### Context variants (same tokens, different bound values or data-attribute scope)

| Variant | Use | Intent |
|---------|-----|--------|
| **default / primary** | Main content, large scroll areas | Slightly more visible thumb at idle. |
| **panel** | Side panels (Journey context, filters) | More subtle thumb; lower idle weight. |
| **dense** | Tables, OE lists, task lists, evidence lists | Balance with row density; avoid covering row actions — pair with padding/gutter rules. |
| **timeline** | Horizontal + vertical scroll regions | Horizontal scroll may use the same thumb tokens; document if a dedicated horizontal width alias is needed. |

---

## 3. States

1. **Default (idle)** — Low contrast thumb on track; optional reduced opacity via token.
2. **Hover** — Pointer over scroll area or thumb: thumb uses `--scrollbar-thumb-hover`; brief tokenized fade-in.
3. **Active (dragging)** — `--scrollbar-thumb-active`; brand-aligned accent; clear feedback.
4. **Focus (keyboard)** — Scroll container (or policy-defined focus target) shows **focus-visible** outline per app focus tokens; thumb may use `--scrollbar-thumb-focus` for extra visibility. Must not rely on color alone where focus ring already applies.

---

## 4. Behavioral expectations

| Behavior | Requirement |
|----------|----------------|
| Auto visibility | Subtle when idle; more apparent on hover, scroll interaction, or keyboard focus into scrollable region. |
| Smooth interaction | State transitions use `--scrollbar-motion-duration` / `--scrollbar-motion-easing`; native scroll physics unchanged. |
| Non-blocking | Do not obscure primary row actions or critical chrome; use stable gutter / padding / layout rules as documented with dense variant. |
| Performance | CSS-only enhancement of native scrolling; no default JS scrollbar engines. |

---

## 5. Responsiveness

| Mode | Behavior |
|------|----------|
| Desktop (`--bp-desktop-min` and up) | `--scrollbar-width`; thin, elegant. |
| Tablet (between tablet min and mobile max) | `--scrollbar-width-touch`; slightly wider for touch targets. |
| Mobile (at or below `--bp-mobile-max`) | **Native scrolling only** — do not force custom scrollbar chrome that harms touch UX. |
| `pointer: coarse` (optional guard) | Align with mobile policy: prefer native behavior. |

---

## 6. Accessibility

- Keyboard scrolling must work for scrollable regions that take focus (or roving focus policy as per app).
- **Focus-visible** must remain visible; scrollbar thumb contrast increases must not replace focus ring policy.
- Active/drag/focus states must use sufficient contrast **via token ladder**, not one-off hex in features.
- **`prefers-reduced-motion`:** reduce or eliminate thumb/track transitions (instant or near-zero duration via tokens).

---

## 7. Cross-browser (design intent)

- **Firefox:** Standard `scrollbar-width` + `scrollbar-color` fed by the same semantic tokens.
- **Chrome / Edge / Safari:** WebKit scrollbar pseudo-elements fed by the **same** semantic tokens.
- Visual parity is **approximate** where engines differ; token source stays single.

---

## 8. Forbidden

- Thick scrollbars as default.
- Bright, decorative, non-semantic colors.
- Sharp-edged thumbs.
- Per-module scrollbar styling outside context variants.
- Heavy JS scrollbar libraries for default application behavior.

---

## 9. Consistency with PRAMAN surfaces

Scrollbars must read as part of **cards, panels, tables, modals** — track and thumb colors derive from the same neutral and brand tokens as borders, surfaces, and primary actions.

---

## Implementation reference (Phase 43 execute)

- **Tokens:** `src/styles/tokens/foundation.tokens.css` (`--scrollbar-width*`, `--scrollbar-radius`, motion/opacity) and `src/styles/tokens/theme.tokens.css` (`--scrollbar-track`, `--scrollbar-thumb*`, panel/dense/timeline variants) for `:root[data-theme='light']` and `:root[data-theme='dark']`.
- **Rules:** `src/styles/scrollbar.css` (imported from `src/styles.css`).
- **Utilities:** `praman-scrollbar`, `praman-scrollbar--panel`, `praman-scrollbar--dense`, `praman-scrollbar--timeline` — see `DESIGN_RULES.md` (Scrollbar system).
