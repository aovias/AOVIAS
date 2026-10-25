# Sprint 3.7 Completion Report — Careers

**Date:** 2026-07-19  
**Location:** `V:\DevelopmentRepo\AOVIAS Website\AOVIAS Web`  
**Status:** Complete (Careers experience + config-driven open roles; Sprint 3.8 not started)

---

## 1. Careers Page Architecture Overview

```text
app/(company)/layout.tsx              → MarketingLayout → AppShell
app/(company)/company/careers/page.tsx → metadata + JSON-LD + <CareersPage />

features/careers/
  careers-page.tsx                    → section orchestration
  seo.ts                              → careersSeo + JobPosting builders
  domain/
    types.ts                          → OpenRole model
    roles.ts                          → openRolesCatalog registry
  hero/ | culture/ | why-aovias/
  benefits/ | hiring-process/
  open-roles/ | cta/                  → content.ts + section.tsx each
```

**URL:** `/company/careers` (matches existing Company nav)

**Section order:** Hero → Culture → Why AOVIAS → Benefits → Hiring Process → Open Roles → Primary CTA

---

## 2. Careers Framework Summary

| Concern | Approach |
| --- | --- |
| Layout | Marketing Layout only |
| Composition | Feature modules own content; thin App Router page |
| Open roles | Configuration registry (`openRolesCatalog`) — not hardcoded in JSX |
| Apply CTAs | `applyHref` per role (mailto today; ATS URL later) |
| Hiring stages | Content-driven `Timeline` events (expandable) |
| Benefits | Content registry of principles (expandable) |
| Company link | Culture section links to `/company#culture` |
| Future ATS | Swap/sync `openRolesCatalog` without UI changes |

No Careers-specific UI primitives or `components/careers/` library introduced.

---

## 3. Open Roles Data Model

`OpenRole` (`features/careers/domain/types.ts`):

| Field | Purpose |
| --- | --- |
| `id` / `slug` | Stable identifiers + fragment `#role-{slug}` |
| `title` | Role title |
| `department` | Department chip |
| `employmentType` | full-time · part-time · contract · internship |
| `location` | Human-readable location |
| `status` | open · upcoming · filled · closed |
| `summary` / `description` | Card copy + JobPosting body |
| `applyHref` | Apply destination |
| `publishedAt` | ISO date for schema |
| `remote` / `addressCountry` | JobPosting location hints |

**Seeded roles:** Platform Engineer (open), Product Engineer — Command (open), Responsible AI Engineer (upcoming).

Helpers: `getOpenRoles()`, `getActiveOpenRoles()`, `getRoleBySlug()`.

---

## 4. Component Reuse Report

| Area | Components |
| --- | --- |
| Marketing | Hero, SectionHeader, FeatureGrid, FeatureCard, ValueCard, Timeline, CtaSection, MarketingCtaLink |
| Primitives | Container, Stack, Text, Card, Badge |
| SEO | createPageMetadata, JsonLdScript, Organization + WebPage + Breadcrumbs + JobPosting |
| Chrome | MarketingLayout → AppShell |
| Motion | FeatureGrid stagger / Motion Foundation only |

No new UI primitives.

---

## 5. SEO Summary

| Item | Implementation |
| --- | --- |
| Metadata | `careersSeo` via `createPageMetadata` |
| Canonical / OG / Twitter | SEO builder defaults |
| JSON-LD | Organization + WebPage + Breadcrumbs |
| JobPosting | `buildJobPostingSchema` / `buildCareersJobPostingSchemas` for open + upcoming |
| Sitemap | `/company/careers` registered |
| Nav | Existing Company → Careers href |
| Heading hierarchy | Hero h1; section h2; cards/timeline h3 |

---

## 6. Accessibility Summary

- Semantic sections with `aria-label`
- Open roles as a list of cards with status/department/type chips
- Apply CTAs are real links (keyboard + screen reader)
- Timeline as ordered list of steps
- Lighthouse Accessibility: **100**

---

## 7. Performance Report

- Fully static Server Components (`○ /company/careers`)
- First Load JS: **~159 kB** (lighter than many Phase 3 pages)
- Placeholder illustration only (dashed surface)
- Minimal client JS (shared chrome / motion stagger)
- Lighthouse Performance: **100** (desktop)

---

## 8. Lighthouse Results

Desktop audit (production server `PORT=3008`):

| URL | Performance | Accessibility | Best Practices | SEO |
| --- | --- | --- | --- | --- |
| `/company/careers` | **100** | **100** | **100** | **100** |

---

## 9. Validation Results

| Check | Result |
| --- | --- |
| Culture section | Pass |
| Why AOVIAS section | Pass |
| Benefits section | Pass |
| Hiring Process | Pass |
| Open Roles (config-driven) | Pass |
| Primary CTA | Pass |
| Reusable components only | Pass |
| TypeScript | Pass |
| ESLint | Pass |
| Production build | Pass (64 routes) |
| Lighthouse Perf ≥95 + A11y/SEO/BP 100 | Pass |

---

## 10. Recommendations before Sprint 3.8

1. Add Career Application form on Form Engine when conversion sprint allows (keep mailto until then)  
2. Sync `openRolesCatalog` from ATS/HR API behind the same domain helpers  
3. Commission employer-brand photography/illustration for the hero  
4. Optional FAQ (Trust `Faq`) for relocation, remote norms, and interview logistics  
5. Re-run mobile Lighthouse before launch  
6. Consider dedicated role detail routes (`/company/careers/[slug]`) when descriptions grow  

Do **not** begin Sprint 3.8 automatically.
