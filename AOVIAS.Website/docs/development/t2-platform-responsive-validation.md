# T2 — Platform Responsive Validation

**Date:** 2026-07-20

---

## Approach

Reuse validated marketing layout primitives (`Hero`, `Container`, `FeatureGrid`) and responsive `BrandShell` SVGs. Flagship story stacks as a single column.

## Breakpoints

Covered by shared site chrome (320–ultrawide). Platform pages introduce no fixed-width overflow sources; diagrams use `viewBox` + `size-full`.

## Build verification

All five Platform routes statically generated; smoke HTTP **200** on production server.
