# Phase V11 — Final Approval Checklist

## Must be true for unequivocal Release 1 approval

- [x] Public pages follow AVLS
- [x] Visual language consistent across Products / Technology / Knowledge / Company / Commerce
- [x] EBAS + illustration registries used correctly (direct BrandShell residual documented)
- [x] Desktop experience polished
- [x] Mobile experience polished (hero illustration deferred `<md` by design)
- [x] Light and Dark equally refined
- [x] Accessibility meets WCAG 2.2 AA intent (LH A = 100)
- [ ] **Lighthouse mobile Performance ≥95** ← **OPEN (91–92)**
- [x] Lighthouse Accessibility / Best Practices / SEO = 100 (auth SEO exempt)
- [x] Cohesive, trustworthy, unmistakably AOVIAS
- [x] Unified enterprise narrative
- [x] No critical/major visual or usability defects remaining
- [ ] No major performance lab defects remaining ← **OPEN**

## Validation commands

```bash
npm run lint --workspace=@aovias/website
npm run typecheck --workspace=@aovias/website
npm run build --workspace=@aovias/website
# production on :3000
npm run lighthouse:r1.4 --workspace=@aovias/website
```

## Sign-off

| Role | Status |
| --- | --- |
| Design / Brand | Approve |
| Accessibility | Approve |
| Engineering | Approve with perf follow-up |
| Performance | **Hold** |
| Executive Release Gate | **Hold** |

**Final box:** Release 1 public launch certification — **☐ Approved · ☑ Held**
