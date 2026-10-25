# 5 — EBAS Integration Report

**Phase:** V2 GEI · Release 1.1  
**Status:** Complete  
**AVLS refs:** `04-background-atmosphere-system.md`, `registries/ebas.md`

## Infrastructure

| Piece | Role |
|-------|------|
| `resolveAvlfAtmosphere` | AVLS ID → live `EnterpriseAtmosphere` key |
| `layoutAtmosphereDefaults` | Family → AVLS ID map |
| `LayoutAtmosphereProvider` | Layout requests atmosphere for consumers |
| `AppShell.atmosphere` | Declares family atmosphere + `data-layout-atmosphere` |
| `injectAtmosphere` | Subtle layout-level wash for legal/docs |

## Layout assignments

| Layout / route family | Atmosphere |
|-----------------------|------------|
| Homepage / marketing | `enterprise` |
| Products | `workflow` |
| Technology | `blueprint` |
| Knowledge / docs | `knowledge` |
| Company / solutions | `corporate` |
| Legal | `minimal` |
| Auth | `security` (+ panel `trust-geometry`) |
| Error | `minimal` |

## Compatibility

Existing page heroes keep live keys (`workflow`, `blueprint-grid`, etc.).  
`BrandAtmosphere` / `Hero` / `ProductHero` / `SectionSurface` now also accept AVLS IDs.

## Non-goals (honored)

- No page-specific backgrounds invented
- No hero redesign / page atmosphere rewrites beyond layout wiring
