# T1 — Technology Layout Overview

**Date:** 2026-07-20

---

## Composition

```text
(technology)/layout.tsx
  → TechnologyLayout
    → AppShell variant="product" contentWidth="full" showBreadcrumb
```

File: [`layouts/technology/technology-layout.tsx`](../../apps/website/src/layouts/technology/technology-layout.tsx)

| Concern | T1 behavior |
| --- | --- |
| Hero | Page-owned (`Hero` on landing) |
| Breadcrumbs | AppShell UI + JSON-LD on pages |
| Side navigation | Not wired (AppShell `sidebar` reserved for T2+) |
| Related topics | Placeholder pages render related cards from registry |
| CTA | Landing closing band; placeholders link Contact / Overview |

No duplicated chrome beyond the thin layout wrapper used by Products / Knowledge.
