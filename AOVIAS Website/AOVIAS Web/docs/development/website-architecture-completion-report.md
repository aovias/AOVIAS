# Website Architecture Completion Report

**Product:** AOVIAS Public Website  
**Codebase:** `V:\DevelopmentRepo\AOVIAS Website\AOVIAS Web` (`@aovias/website`)  
**Date:** 2026-07-20  
**Scope:** Engineering handoff from Phase 1–3 (foundation → components → pages) into Phase 4 (launch preparation)  
**Status:** Page implementation complete; production launch not started  

---

## Executive summary

AOVIAS ships a production-shaped Next.js 15 App Router website with specialized layouts, a tokenized design system, reusable component libraries, SEO/performance foundations, and a full Phase 3 public surface (marketing, products, industries, knowledge, careers, contact, pricing, identity, checkout, trust & governance).

**Health verdict:** Architecture is **launch-ready for frontend structure**, with **backend/integration and content/legal certification remaining as Phase 4 blockers**. Desktop Lighthouse baselines on audited public routes are consistently strong (typically **100 / 100 / 100 / 100** for Performance / Accessibility / Best Practices / SEO). Form-heavy conversion routes carry higher JS weight and should be re-validated on mobile before go-live.

---

## 1. Page inventory

### 1.1 Totals

| Category | Count |
| --- | ---: |
| App Router `page.tsx` files | **51** |
| Generated production routes (build) | **~81** (includes SSG slug expansions) |
| Public indexable sitemap entries | **26** |
| Layout shells / previews (non-product) | **~7** |
| Design-system / internal (`/dev`, `/internal`) | **9** |

### 1.2 Layout map

| Layout | App Router group(s) | Surfaces |
| --- | --- | --- |
| `MarketingLayout` | `(marketing)`, `(company)`, `(solutions)` | Home, company, careers, contact, industries |
| `ProductLayout` | `(products)` | Products, pricing, checkout |
| `KnowledgeLayout` | `(knowledge)` | Knowledge hubs, blog, insights, library, categories, search |
| `DocumentationLayout` | `(resources)` | Docs hub + docs detail |
| `LegalLayout` | `(legal)` | Trust & Governance Center |
| `AuthenticationLayout` | `(auth)` | Sign-in / sign-up / recovery / verify |
| `ErrorLayout` | `not-found` | Shared 404 |
| `DevLayout` (inline) | `(dev)` | Design-system showcases |

### 1.3 Public marketing & company

| Route | Feature | Render | Notes |
| --- | --- | --- | --- |
| `/` | Home (`HomeView` + content) | Static | Primary brand surface |
| `/company` | `features/company` | Static | |
| `/company/careers` | `features/careers` | Static | Config-driven roles |
| `/company/contact` | `features/contact` | Static | Form Engine |
| `/company/preview` | Layout QA | Static | Robots preview disallow |

### 1.4 Products, pricing, checkout

| Route | Feature | Render | Notes |
| --- | --- | --- | --- |
| `/products` | `features/products` | Static | |
| `/products/[slug]` | `features/products` | SSG | command, studio, sentinel, nexus, insight, flow |
| `/products/preview` | Layout QA | Static | |
| `/pricing` | `features/pricing` | Static | |
| `/pricing/preview` | Redirect | Redirect → `/pricing` | |
| `/checkout` | `features/checkout` | Dynamic | `noIndex` |
| `/checkout/billing` | `features/checkout` | Dynamic | `noIndex` |
| `/checkout/confirm` | `features/checkout` | Dynamic | `noIndex` |
| `/checkout/success` | `features/checkout` | Dynamic | `noIndex` |

### 1.5 Industries / solutions

| Route | Feature | Render | Notes |
| --- | --- | --- | --- |
| `/solutions` | `features/industries` | Static | |
| `/solutions/[slug]` | `features/industries` | SSG | Available + planned industry templates |

### 1.6 Knowledge platform

| Route | Feature | Layout | Render |
| --- | --- | --- | --- |
| `/resources` | knowledge | Knowledge | Static |
| `/resources/blog` (+ `[slug]`) | knowledge | Knowledge | Static / SSG |
| `/resources/insights` (+ `[slug]`) | knowledge | Knowledge | Static / SSG |
| `/resources/library` (+ `[slug]`) | knowledge | Knowledge | Static / SSG |
| `/resources/categories` (+ `[slug]`) | knowledge | Knowledge | Static / SSG |
| `/resources/search` | knowledge | Knowledge | Static shell + client search |
| `/resources/docs` (+ `[slug]`) | knowledge | Documentation | Static / SSG |
| `/resources/preview` | Layout QA | Documentation | Static |

### 1.7 Trust & governance

| Route | Feature | Render | Notes |
| --- | --- | --- | --- |
| `/legal` | `features/legal` | Static | Hub |
| `/legal/privacy` | legal | Static | Versioned document framework |
| `/legal/terms` | legal | Static | |
| `/legal/cookies` | legal | Static | CMP UI out of scope |
| `/legal/accessibility` | legal | Static | |
| `/legal/security` | legal | Static | High-level only |
| `/legal/preview` | Layout QA | Static | |

### 1.8 Identity

| Route | Feature | Render | Notes |
| --- | --- | --- | --- |
| `/auth/sign-in` | identity | Dynamic | `noIndex` |
| `/auth/sign-up` | identity | Static | `noIndex` |
| `/auth/forgot-password` | identity | Static | `noIndex` |
| `/auth/reset-password` | identity | Dynamic | `noIndex` |
| `/auth/verify-email` | identity | Dynamic | `noIndex` |
| `/auth/preview` | Redirect | Redirect → sign-in | |

### 1.9 Internal / design system

| Route pattern | Purpose |
| --- | --- |
| `/dev/design-system` | Motion / foundation showcase |
| `/internal/design-system/*` | Component library showcases (product, trust, forms, pricing, resources, system-states, …) |

---

## 2. Component reuse analysis

### 2.1 Shared libraries

| Library | Approx. exported UI | Role |
| --- | ---: | --- |
| Primitives | ~38 | Tokens-backed building blocks |
| Layout | ~17 | Page chrome helpers (+ primitive re-exports) |
| Marketing | 13 (+ home atmosphere) | Heroes, grids, CTAs, reveals |
| Navigation | 10 | Header, footer, mega-menu |
| Product | 10 | Product landing / detail blocks |
| Pricing | 8 | Commerce presentation |
| Resources | 8 | Knowledge UI |
| Trust | 9 | Credibility / security / FAQ |
| System states | ~17 | Empty/error/loading/skeletons |
| Forms | 12 | Form Engine bindings |
| SEO | 1 | `JsonLdScript` |
| Media | 1 | `OptimizedImage` |

### 2.2 Reuse estimates (composition sampling)

Methodology: for representative feature pages, classify meaningful UI as **shared** (`@/components/*`) vs **feature-local** shells. Wrappers without independent design language are ignored.

| Surface class | Estimated shared reuse |
| --- | ---: |
| Homepage | ~95% |
| Company / industries / product detail | ~90–95% |
| Knowledge / legal | ~90% |
| Careers / contact / pricing | ~85–90% |
| Identity | ~80–90% |
| Checkout | ~70–80% |
| **Portfolio-wide (features)** | **~85–90%** |

### 2.3 Page-specific additions (intentional)

| Local construct | Location | Rationale |
| --- | --- | --- |
| `HomeHeroAtmosphere` | marketing (home) | Full-bleed atmosphere; home-only |
| `RoleCard` | careers | Domain list item |
| `CheckoutSteps` / `OrderSummary` | checkout | Flow chrome |
| `AuthPageShell` | identity | Auth column composition |
| Product browse section | products overview | Directory UX (naming overlaps library `ProductNavigation`) |
| Legal document page composer | legal | Shared template over registry content |

**Finding:** No parallel design system emerged inside features. Local UI is domain orchestration, not new primitives.

---

## 3. Design system coverage

| Concern | Status | Evidence |
| --- | --- | --- |
| Design tokens | **Pass** | Features use semantic Tailwind tokens (`bg-background`, `text-muted-foreground`, `border-border`, etc.). Hardcoded hex limited to `app/icon.tsx`. |
| Layout system | **Pass** | All public surfaces use specialized `@/layouts/*` via App Router groups. |
| Primitives | **Pass** | Typography, controls, cards, stacks consumed through shared barrels. |
| Motion foundation | **Pass** | `MarketingReveal` / `MotionReveal` / stagger via Hero, FeatureGrid, CtaSection, ResourceGrid, system states; reduced-motion respected in foundation. |
| Theme system | **Pass** | `next-themes` + design-system CSS themes (light/dark). |
| Form system | **Pass** | Contact, identity, checkout, partner/demo schemas via Form Engine. |
| New UI primitives in Phase 3 | **None required beyond home atmosphere** | Atmosphere is composition, not a new primitive API. |

---

## 4. SEO coverage

### 4.1 Foundation

- Centralized `createPageMetadata` (title, description, path, type, keywords, robots, OG, Twitter)
- Site-wide default schemas in root layout
- Per-page `JsonLdScript` + `buildPageSchemas` (breadcrumbs / WebPage) on public marketing surfaces
- Specialized schemas: products, industries, careers job postings, knowledge articles
- Canonical URLs derived from configured site origin + path
- `robots.txt` disallows `/dev/`, `/api/`, `/auth/`, `/checkout/`, and `/*/preview`

### 4.2 Indexability matrix

| Class | Metadata | JSON-LD | Sitemap | Verdict |
| --- | --- | --- | --- | --- |
| Home, company, careers, contact | Yes | Yes | Yes | Complete |
| Products hub + command/studio | Yes | Yes | Yes | Complete |
| Remaining product slugs | Yes | Yes | **Missing** | Gap |
| Solutions hub + 4 available industries | Yes | Yes | Yes | Complete |
| Planned industry slugs | Yes | Yes | **Missing** | Gap |
| Knowledge hubs | Yes | Yes | Yes | Complete |
| Knowledge / docs / category detail URLs | Yes | Yes | **Missing** | Gap |
| Legal hub + 5 documents | Yes | Yes | Yes | Complete |
| Auth / checkout | `noIndex` | No | No | Correct |
| Previews / internal | Disallow / noIndex | No | No | Correct |

### 4.3 Sitemap debt

`createSitemapRoute` exists but is **not yet wired** to auto-collect SSG catalogs. Approximately **~25 live public detail URLs** are absent from `seoConfig.sitemap.routes` (knowledge articles, docs, categories, additional products/industries).

**Priority Phase 4 action:** generate sitemap entries from product/industry/knowledge registries.

---

## 5. Accessibility audit

### 5.1 Engineering controls

- Semantic landmarks via AppShell / MainContent / Skip link
- Form Engine labels, errors, live regions, focusable controls
- Heading hierarchy enforced in marketing/legal/knowledge templates
- Keyboard mega-menu and utility controls
- WCAG 2.2 AA target documented in sprint validation gates
- Accessibility Statement published at `/legal/accessibility`

### 5.2 Lighthouse Accessibility (desktop, sprint evidence)

Audited public surfaces from Phase 3 consistently scored **Accessibility = 100**, including homepage, company, products, industries, knowledge, careers, contact, pricing, checkout (after CLS fix), identity, and legal documents.

### 5.3 Residual a11y / UX notes (not LH failures)

| Item | Severity | Notes |
| --- | --- | --- |
| Knowledge media placeholders | Low | Dashed/placeholder imagery until brand assets land |
| Contact map placeholder | Low | Explicit forthcoming map |
| Dual `EmptyState` (primitives vs system-states) | Low | Prefer system-states; deprecate primitive |
| Mobile keyboard QA on checkout/auth | Medium | Desktop LH strong; mobile pass still recommended |
| Counsel-facing legal copy | Process | Informational framework — not certified legal text |

### 5.4 Keyboard navigation

Primary chrome (header, mega-menu, footer), forms, TOC links, and CTAs are keyboard-operable by construction. Recommend a formal Phase 4 checklist pass on: mega-menu, search dialog, checkout multi-step, auth password fields, legal TOC.

---

## 6. Performance summary

### 6.1 Targets (`src/config/performance.ts`)

| Metric | Target |
| --- | --- |
| Lighthouse Performance | ≥ 95 |
| Accessibility / Best Practices / SEO | 100 |
| LCP | ≤ 2500 ms |
| FCP | ≤ 1800 ms |
| CLS | ≤ 0.1 |
| INP | ≤ 200 ms |
| TTFB | ≤ 800 ms |
| Initial JS (gzip budget) | 150 KB |
| Route JS / CSS / fonts / third-party | See performance config |

### 6.2 Lighthouse evidence (desktop preset, Phase 3 reports)

Representative scores (Performance / Accessibility / Best Practices / SEO):

| Route class | Typical score |
| --- | --- |
| `/`, `/company`, `/products`, `/solutions/*` | **100 / 100 / 100 / 100** |
| Knowledge hubs | **100 / 100 / 100 / 100** |
| Careers, contact, pricing | **100 / 100 / 100 / 100** |
| Auth | Perf **100**; SEO intentionally lower (`noIndex`) |
| Checkout | Perf **100** (confirm CLS fixed); SEO N/A/`noIndex` |
| Legal documents | **100 / 100 / 100 / 100** |

Config `lighthouseConfig.formFactor` is **mobile** for CI; many sprint audits used **desktop** presets. Phase 4 should run the configured mobile LHCI suite end-to-end.

### 6.3 Bundle sizes (Next First Load JS, approximate)

| Surface | First Load JS |
| --- | ---: |
| Shared baseline | ~102 kB |
| Careers | ~159 kB |
| Legal / company-like marketing | ~171 kB |
| Products / solutions | ~174–175 kB |
| Pricing | ~177 kB |
| Knowledge | ~184 kB |
| Contact (forms) | ~202 kB |
| Auth (forms) | ~207 kB |
| Checkout (forms + pricing) | ~222 kB |

### 6.4 Budget compliance

| Budget | Assessment |
| --- | --- |
| Lighthouse Perf ≥ 95 (desktop audited routes) | **Met** |
| CLS ≤ 0.1 (post checkout-confirm fix) | **Met** on audited routes |
| Initial JS 150 KB gzip | **At risk / exceeded** on Form Engine routes when using Next First Load JS as proxy — treat as Phase 4 optimization focus |
| Third-party scripts | **Met** (near-zero on marketing) |
| `output: 'standalone'` | Configured; local `next start` warns — production should use standalone server entry |

---

## 7. Architecture health

### 7.1 Strengths

- Clear layering: **config → features → components → layouts → app routes**
- Thin App Router pages; features own composition + SEO content
- Provider-agnostic Identity / Checkout service contracts (mock clients ready to swap)
- Versioned legal document framework
- Strong lint/typecheck/build discipline across sprints

### 7.2 Duplicate / overlap findings

| Finding | Recommendation |
| --- | --- |
| Dual `EmptyState` (primitives vs system-states) | Deprecate primitive; standardize on system-states |
| `ProductNavigation` vs feature `ProductNavigationSection` | Rename feature section to avoid collision |
| Marketing Timeline vs TrustTimeline | Keep — different domain models |
| Showcase-only trust/marketing blocks unused on public pages | Wire in Phase 4 content or document as optional |

### 7.3 Unused / showcase-only components (examples)

`ScreenshotGallery`, `NewsletterForm`, `MaintenanceScreen`, `ComingSoon`, skeleton suite, `CertificationsBlock`, `TrustTimeline`, `Testimonial` (public pages currently prefer SecuritySection / SuccessMetrics / TrustBadge).

### 7.4 Technical debt

| Debt | Impact |
| --- | --- |
| Identity / checkout mocks | Blocks real auth & payments |
| Sitemap incomplete for SSG catalogs | SEO discovery gap |
| Cookie CMP not implemented | Policy page only |
| Placeholder media across knowledge/products | Visual polish |
| Legal content informational (not counsel-certified) | Compliance process |
| SessionStorage billing draft in checkout | Replace with server draft cart |
| Standalone output warning on `next start` | Align deploy path with `standalone` |
| Lighthouse still audits `/products/preview` & `/legal/preview` | Replace with public URLs in CI list |

### 7.5 Scalability observations

- Registries (products, industries, knowledge, pricing, legal) scale by data, not new page templates
- Form Engine centralizes conversion surfaces
- Design-system package (`@aovias/design-system`) isolates tokens/themes from app code
- Next concern for scale: CMS/editorial pipeline and real API adapters, not UI fragmentation

---

## 8. Production readiness checklist

### Must resolve before launch (blockers)

- [ ] Replace Identity mock client with production Identity Platform adapters
- [ ] Replace Checkout mock client; integrate payment session adapter (PSP-agnostic UI already prepared)
- [ ] Counsel review / approval of Privacy, Terms, Cookies, Accessibility, Security content
- [ ] Cookie consent management (CMP) if required by jurisdiction
- [ ] Complete sitemap generation for all public SSG detail URLs
- [ ] Production environment config (site URL, secrets, HTTPS, standalone deploy path)
- [ ] Mobile Lighthouse / real-device pass on home, contact, pricing, auth, checkout
- [ ] Remove or lock down `/dev` and `/internal` in production edge config (robots already disallow `/dev/`)

### Should resolve before launch (high priority)

- [ ] Replace knowledge/product placeholder media with brand assets
- [ ] Wire analytics consistent with Privacy / Cookie policies
- [ ] Error monitoring (e.g. Sentry) + uptime checks
- [ ] Formal keyboard + screen-reader QA script
- [ ] Resolve dual EmptyState; clean showcase-only orphans policy
- [ ] Update LHCI URL list (drop previews; add contact/pricing/auth smoke)
- [ ] JS budget plan for Form Engine routes (code-splitting / lazy islands)

### Nice to have

- [ ] Legal document version history UI
- [ ] Newsletter surface (schema exists)
- [ ] Certifications / timeline trust blocks when claims are approved
- [ ] Interactive contact map

---

## 9. Recommendations for Phase 4

### 9.1 Content refinement

1. Replace placeholder imagery and map stubs  
2. Finalize product/industry copy with product marketing owners  
3. Counsel-certified legal documents + change-control process  
4. Expand knowledge catalog only through the existing registry (avoid one-off pages)

### 9.2 QA

1. Cross-browser matrix (Chromium, Safari, Firefox)  
2. Responsive pass: mobile / tablet / laptop / desktop on conversion paths  
3. Accessibility manual audit against WCAG 2.2 AA checklist  
4. Form abuse / rate-limit readiness once backends exist  
5. Checkout happy path + enterprise-sales branch regression

### 9.3 Deployment

1. Deploy via Next **standalone** output (match `next.config`)  
2. Pin local/dev default port **3000** (already set in `package.json` scripts)  
3. Edge headers from `cachePolicies`  
4. Preview environments with `noIndex`  
5. Sitemap/robots verification in production only

### 9.4 Analytics & monitoring

1. Privacy-aligned analytics (consent-gated where required)  
2. Core Web Vitals RUM  
3. Error tracking + form submission failure alerts  
4. Synthetic checks on `/`, `/pricing`, `/company/contact`, `/auth/sign-in`

### 9.5 Launch activities

1. Soft launch checklist (DNS, TLS, redirects, 404, OG previews)  
2. Hotfix plan for legal/identity/commerce  
3. Content freeze window + rollback plan  
4. Post-launch LHCI + budget gate in CI  

---

## 10. Phase completion context

| Phase | Scope | Status |
| --- | --- | --- |
| Phase 1 | Foundations (theme, layouts, nav, primitives, motion, SEO, performance) | Complete (Sprints 1.1–1.8) |
| Phase 2 | Component libraries (marketing, product, trust, forms, pricing, resources, system-states) | Complete (Sprints 2.1–2.7) |
| Phase 3 | Website pages (home → trust & governance) | Complete (Sprints 3.1–3.12) |
| Phase 4 | Launch preparation (integrations, content certification, QA, deploy, observability) | **Not started** |

Supporting sprint reports live under `docs/development/sprint-*-completion-report.md`.

---

## 11. Handoff statement

This report is the formal engineering handoff from **page implementation** to **launch preparation**.

The website’s **architecture, design system adherence, accessibility baseline, and public page inventory** are in strong condition. Remaining work is primarily **integration, certified content, SEO completeness for detail URLs, mobile performance confirmation, and production operations** — not a rebuild of the frontend architecture.

**Do not treat this document as legal, security, or compliance certification.**  
**Do not begin Phase 4 execution automatically from this report alone — schedule Phase 4 explicitly.**
