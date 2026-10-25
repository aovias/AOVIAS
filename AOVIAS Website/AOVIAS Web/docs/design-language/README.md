# AOVIAS Visual Language Specification (AVLS) v1

**Release:** 1.1 — Enterprise Visual Language Integration  
**Phase:** V1 — AOVIAS Visual Language Foundation (AVLF)  
**Status:** Authoritative engineering documentation  
**Date:** 2026-07-21

---

## Purpose

AVLS converts the approved Creative Direction package into a permanent visual foundation for AOVIAS.

Future visual work must reference **this specification**, not interpret individual creative frames independently.

| Layer | Role |
|-------|------|
| Creative Direction frames | Inspiration & approved visual intent |
| `AOVIAS_Doctrine.md` | Governing visual doctrine (see note below) |
| **AVLS (this folder)** | Engineering specification |
| Design System tokens & components | Implementation substrate |
| Phase V2+ | Page application against AVLS registries |

---

## Doctrine status note

`aovias_creative_direction/AOVIAS_Doctrine.md` is present but **empty (0 bytes)** at the time of this sprint.

AVLS v1 therefore synthesizes doctrine from:

1. All approved frames in `aovias_creative_direction/` (phases 01–05)
2. `Guidelines/06-enterprise-design-language.md`
3. `Guidelines/07-motion-design-system.md`
4. Explicit style-guide frames `e1`–`e4` (surface, buttons, domain colors, hero/illustration rules)

When a written doctrine file is later populated, reconcile it into AVLS via a revision note — do not bypass AVLS.

---

## Document map

| # | Document | Scope |
|---|----------|-------|
| 01 | [Brand philosophy](./01-brand-philosophy.md) | Institutional stance |
| 02 | [Visual principles](./02-visual-principles.md) | Recurring rules |
| 03 | [Color language](./03-color-language.md) | Surfaces, signal, domains |
| 04 | [Background & atmosphere](./04-background-atmosphere-system.md) | EBAS |
| 05 | [Illustration language](./05-illustration-language.md) | Stroke, isometric, accent |
| 06 | [Hero composition](./06-hero-composition-system.md) | Hero types |
| 07 | [Product visualization](./07-product-visualization-system.md) | Command / Studio |
| 08 | [Diagram language](./08-diagram-language.md) | Diagram families |
| 09 | [Card language](./09-card-language.md) | Card personality |
| 10 | [Navigation language](./10-navigation-language.md) | Header, mega menu, search |
| 11 | [Footer language](./11-footer-language.md) | Closing chrome |
| 12 | [Motion language](./12-motion-language.md) | Signature moments |
| 13 | [Page composition](./13-page-composition-patterns.md) | Page rhythm |
| 14 | [Domain identity](./14-domain-identity-system.md) | Accents by domain |
| 15 | [Visual governance](./15-visual-governance.md) | Rules & review |

### Registries & planning

| Artifact | Path |
|----------|------|
| EBAS registry | [registries/ebas.md](./registries/ebas.md) |
| Illustration registry | [registries/illustrations.md](./registries/illustrations.md) |
| Hero registry | [registries/heroes.md](./registries/heroes.md) |
| Diagram registry | [registries/diagrams.md](./registries/diagrams.md) |
| Frame mapping matrix | [frame-mapping-matrix.md](./frame-mapping-matrix.md) |
| Visual component mapping | [visual-component-mapping.md](./visual-component-mapping.md) |
| Implementation priority | [implementation-priority-matrix.md](./implementation-priority-matrix.md) |
| Release 1.1 roadmap | [r1.1-visual-integration-roadmap.md](./r1.1-visual-integration-roadmap.md) |
| Sprint V1 completion | [v1-completion-report.md](./v1-completion-report.md) |

### Typed catalog stubs (data only — not wired)

`apps/website/src/registries/avlf/` — TypeScript catalogs mirroring the markdown registries. **No page imports in V1.**

---

## Creative Direction package map

```
aovias_creative_direction/
├── 01_round1_foundation/          # Core atmospheres, heroes, cards, footer, illustration
├── 02_phase1_color_and_refinement/# Surfaces, buttons, domains, hero rules, refined heroes
├── 03_phase2_products_and_diagrams/# Command/Studio heroes + diagram styles
├── 04_phase3_page_heroes/         # Page-family heroes
└── 05_phase4_navigation_and_interaction/ # Header, menus, motion signatures
```

---

## Phase V2 — Global Experience Integration (complete)

Shared shell / chrome / EBAS / search / illustration pipeline / motion — no homepage redesign.

| Report | File |
|--------|------|
| Completion | [v2-10-phase-v2-completion-report.md](./v2-10-phase-v2-completion-report.md) |
| GEI | [v2-01-global-experience-integration-report.md](./v2-01-global-experience-integration-report.md) |
| Header | [v2-02-enterprise-header-report.md](./v2-02-enterprise-header-report.md) |
| Footer | [v2-03-enterprise-footer-report.md](./v2-03-enterprise-footer-report.md) |
| Search | [v2-04-enterprise-search-report.md](./v2-04-enterprise-search-report.md) |
| EBAS | [v2-05-ebas-integration-report.md](./v2-05-ebas-integration-report.md) |
| Illustrations | [v2-06-illustration-pipeline-report.md](./v2-06-illustration-pipeline-report.md) |
| Motion | [v2-07-motion-integration-report.md](./v2-07-motion-integration-report.md) |
| Components | [v2-08-component-alignment-report.md](./v2-08-component-alignment-report.md) |
| Themes | [v2-09-theme-validation-report.md](./v2-09-theme-validation-report.md) |

## Phase V3 — Homepage Experience / North Star (complete)

Canonical AVLS page implementation. Inheritance standard for all later page experiences.

| Report | File |
|--------|------|
| Completion | [v3-10-phase-v3-completion-report.md](./v3-10-phase-v3-completion-report.md) |
| Homepage | [v3-01-homepage-experience-report.md](./v3-01-homepage-experience-report.md) |
| Hero | [v3-02-hero-implementation-report.md](./v3-02-hero-implementation-report.md) |
| Platform story | [v3-03-platform-storytelling-report.md](./v3-03-platform-storytelling-report.md) |
| Products | [v3-04-product-showcase-report.md](./v3-04-product-showcase-report.md) |
| Technology | [v3-05-technology-showcase-report.md](./v3-05-technology-showcase-report.md) |
| Industries | [v3-06-industry-experience-report.md](./v3-06-industry-experience-report.md) |
| Trust | [v3-07-trust-experience-report.md](./v3-07-trust-experience-report.md) |
| Motion | [v3-08-motion-report.md](./v3-08-motion-report.md) |
| Before / After | [v3-09-visual-before-vs-after-report.md](./v3-09-visual-before-vs-after-report.md) |

---

## Non-goals (Phase V1)

- No page redesign or layout changes  
- No routing changes  
- No component duplication  
- No design-token architecture rewrite  
- No Phase V2 page application  

---

## How to use AVLS in later phases

1. Identify the page family → Hero registry + EBAS assignment  
2. Select illustration / diagram from registries  
3. Apply domain identity (one accent per viewport)  
4. Map to existing components via [visual-component-mapping.md](./visual-component-mapping.md)  
5. Cite frame IDs from [frame-mapping-matrix.md](./frame-mapping-matrix.md) in PRs  
