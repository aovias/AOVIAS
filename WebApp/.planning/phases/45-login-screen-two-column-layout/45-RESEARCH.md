# Phase 45 — Research: Login Two-Column Layout

## RESEARCH COMPLETE

### Current implementation (inventory)

| File | Role |
|------|------|
| `src/app/login/login.component.html` | Root `min-h-screen flex`; **lg** `brand-panel` left `w-1/2` (hidden below lg); right column `w-full lg:w-1/2` with `login-content-wrapper login-card` containing **all** interactive blocks in one vertical stack |
| `src/app/login/login.component.ts` | Reactive `loginForm`, `onSubmit`, SSO stubs, `biometricLogin`, password visibility; **no change** for layout-only phase |
| `src/app/login/login.component.css` | `.brand-panel`, `.login-card`, `.ai-badge`, `.biometric-section`, etc. |

### Order of controls in template (current single column)

1. `#region` AI Security Assistant (`.ai-badge`) — **to move to right column**
2. Title “Welcome Back” + subtitle
3. `<form>`: email, password, remember/forgot, Sign In
4. Divider “Or continue with” + Microsoft / Google (`grid-cols-2`)
5. Signup link “Create Hospital Account”
6. Biometric “Quick Secure Login” — **to move to right column**
7. Footer: “Secure access for authorized…” + “Need help… Contact Support”

### Gap vs target

- **Brand panel** is currently `hidden lg:flex` half-width; **not** in the stakeholder spec for this phase (focus is **2:1 form vs supporting**). The plan should either (a) **repurpose** the existing right-side gradient area as the **secondary 1/3 column** and move AI + biometric there, while the **primary 2/3** holds the form stack, or (b) **replace** the outer structure with a single row: `2fr 1fr` without duplicating brand. **Decision left to PLAN** — must preserve **no removed controls** (brand marketing content still exists if kept; if merged into one row, brand panel may need to be **below** or **full-width** strip—see plan).

### Recommended layout approach (minimal DOM churn)

- **Outer:** `display: flex` or CSS grid **full viewport**; two direct children: `login__primary` (~66.67%), `login__secondary` (~33.33%).
- **Primary:** `login-card` (or equivalent) containing **only** order 2–6 from the list above (welcome through signup link), plus **footer meta** (items 7–8) **after** signup link, **left-aligned**, consistent width.
- **Secondary:** `.ai-badge` block first, `.biometric-section` second; `align-items: center` or `flex-start` with vertical gap; same `max-width` behavior as reference.
- **No new** form controls; **no** wrapper divs around individual inputs beyond one panel per column.

### Responsive

- **Desktop (≥1025px):** `grid-template-columns: 2fr 1fr` or `flex: 2` / `flex: 1` with `min-width: 0`.
- **Tablet:** same ratio with `minmax(0, 1fr)`; optional `gap` reduction.
- **Mobile:** `flex-direction: column` — primary block first, secondary second; full-width `sso-button` row remains `grid-cols-2` or stacks if needed.

### Risk

- **Tailwind + component CSS:** template uses Tailwind utility classes; layout refactor may prefer **semantic BEM classes** in `.login.component.css` for the two columns to avoid conflicting `lg:w-1/2` with `2:1`. Plan should call out **class consolidation** on the outer shell only.

### References

- Stakeholder: reference screenshot (2:1, AI + biometric right).
- `Phase 17` login API integration (behavior frozen).
