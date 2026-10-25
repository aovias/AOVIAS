# T1 — Technology Navigation Summary

**Date:** 2026-07-20  
**Status:** Complete

---

## Primary navigation

Technology is a first-level item in [`mainNavigation`](../../apps/website/src/config/navigation/main.ts), ordered:

Products → Solutions → **Technology** → Resources → Company

- Active / hover / keyboard: existing `MainNav` + `MegaMenuTrigger`
- Mobile: `MobileDrawer` flattens Technology mega columns automatically
- Footer: Technology column from the same registry ([`footer.ts`](../../apps/website/src/config/navigation/footer.ts))

Careers / Contact remain under Company; Solutions remains the industries entry (per T1 locked IA).

---

## Source of truth

[`features/technology/domain/registry.ts`](../../apps/website/src/features/technology/domain/registry.ts) → [`config/navigation/technology.ts`](../../apps/website/src/config/navigation/technology.ts) → mega columns + footer items.
