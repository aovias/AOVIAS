# Sprint 3.2 Completion Report — Company

**Date:** 2026-07-19  
**Location:** `V:\DevelopmentRepo\AOVIAS Website\AOVIAS Web`  
**Status:** Complete (Company page only; Sprint 3.3 not started)

---

## 1. Company Page Architecture Overview

```text
(company)/layout.tsx          → MarketingLayout → AppShell (SiteHeader + SiteFooter)
(company)/company/page.tsx    → metadata + JSON-LD + CompanyPage
features/company/company-page.tsx → module orchestration only
features/company/{module}/
  content.ts                  → module copy + meta
  section.tsx                 → composition from shared libraries
features/company/seo.ts       → page SEO payload
```

- **Layout:** Marketing shell only — no Company-specific chrome or infrastructure
- **Composition:** Phase 1–2 libraries exclusively
- **Content model:** Feature modules own copy, metadata, and section composition
- **Route:** Production `/company` (layout preview retained at `/company/preview`)

---

## 2. Section Hierarchy

| # | Module | Heading |
| --- | --- | --- |
| 1 | About | h1 — An engineering-led institution |
| 2 | Story | h2 — Why AOVIAS exists |
| 3 | Mission | h2 — Strengthen institutions… (+ outcome h3s via ValueCard) |
| 4 | Vision | h2 — long-term future (+ pillar h3s) |
| 5 | Philosophy | h2 — Principles that guide our work (+ principle h3s) |
| 6 | Constitution | h2 — Values that shape how we build (+ principle h3s) |
| 7 | Leadership | h2 — Leadership as stewardship |
| 8 | Culture | h2 — An engineering culture of ownership (+ feature h3s) |
| 9 | Footer | SiteFooter via AppShell |

---

## 3. Component Reuse Report

| Module | Components |
| --- | --- |
| About | `Hero`, `Text` |
| Story | `SectionHeader`, `MarketingReveal`, `Paragraph`, `Container`, `Stack` |
| Mission | `SectionHeader`, `MarketingReveal`, `Paragraph`, `FeatureGrid`, `ValueCard` |
| Vision | `SectionHeader`, `MarketingReveal`, `Paragraph`, `FeatureGrid`, `PrincipleCard` |
| Philosophy | `SectionHeader`, `FeatureGrid`, `PrincipleCard` |
| Constitution | `SectionHeader`, `FeatureGrid`, `PrincipleCard`, `TrustBadge` |
| Leadership | `SectionHeader`, `MarketingReveal`, `AuthorBlock`, `Paragraph`, `Text` |
| Culture | `SectionHeader`, `FeatureGrid`, `FeatureCard` |
| Chrome | `SiteHeader`, `SiteFooter` (MarketingLayout) |
| SEO | `createPageMetadata`, `JsonLdScript`, `buildPageSchemas` |

No new UI primitives were introduced for page assembly.

---

## 4. SEO Summary

| Item | Implementation |
| --- | --- |
| Metadata | `createPageMetadata({ path: '/company', type: 'marketing' })` |
| Description / keywords | `features/company/seo.ts` |
| Canonical / OG / Twitter | Via SEO builder defaults |
| JSON-LD | WebPage + Organization + BreadcrumbList |
| Heading hierarchy | Single h1 in About Hero; section h2s thereafter |
| Sitemap | `/company` registered (priority 0.9, monthly) |
| Nav | Company / About → `/company` |

---

## 5. Accessibility Summary

- Semantic `<section>` regions with `aria-label`s
- Skip link from root layout; keyboard-focusable CTAs and navigation
- Author profiles via `AuthorBlock` with labeled aside / avatar
- Decorative / placeholder illustration uses `role="img"` + label
- Motion Foundation respects reduced-motion preferences
- Lighthouse Accessibility on `/company`: **100**

---

## 6. Performance Report

- Server Components for route + all company modules
- Motion via MarketingReveal / FeatureGrid stagger only
- Minimal client JS (navigation chrome / theme / motion)
- Placeholder media (no heavy hero assets)
- Lighthouse Performance on `/company`: **100** (desktop preset)

---

## 7. Lighthouse Results

Desktop LHCI (`lighthouserc.cjs`), production server on port 3002:

| URL | Performance | Accessibility | Best Practices | SEO |
| --- | --- | --- | --- | --- |
| `/company` | **100** | **100** | **100** | **100** |
| `/` | 100 | 100 | 100 | 100 |
| `/products/preview` | ≥95 gate | 100 | 96 (warn)* | 100 |
| `/legal/preview` | ≥95 gate | 100 | 96 (warn)* | 100 |

\*Preview routes warn on best-practices (unrelated footer/nav links to unbuilt pages). Company hard gates passed.

`/company` added to `lighthouserc.cjs` collect URLs for ongoing CI.

---

## 8. Validation Results

| Check | Result |
| --- | --- |
| About AOVIAS | Pass |
| Our Story | Pass |
| Mission | Pass |
| Vision | Pass |
| Philosophy | Pass |
| Constitution Summary | Pass |
| Leadership (scalable slots) | Pass |
| Culture | Pass |
| Reusable components only | Pass |
| Responsive (token/layout system) | Pass |
| Accessibility | Pass (LH 100) |
| SEO | Pass (LH 100) |
| Theme / tokens | Pass |
| TypeScript | Pass |
| ESLint | Pass |
| Production build | Pass |
| Lighthouse `/company` ≥95 Perf + 100 A11y/SEO/BP | Pass |

---

## 9. Recommendations before production launch

1. Replace About hero illustration placeholder with a commissioned institutional visual via Media Slot / `OptimizedImage`
2. Publish real Founder / Executive / Advisory profiles into `leadership/content.ts` (AuthorBlock already wired)
3. Point interim homepage CTAs from `/company` to Contact / Demo Form Engine routes when those pages ship
4. Editorial review of mission / vision / constitution summary with brand and legal owners
5. Add real OG image at `seoConfig.defaultOgImage.path`
6. Re-run mobile Lighthouse before launch (CI uses desktop preset)
7. Consider in-page anchors in the Company mega-menu (Mission, Leadership, Culture) once content is stable

Do **not** begin Sprint 3.3 automatically.
