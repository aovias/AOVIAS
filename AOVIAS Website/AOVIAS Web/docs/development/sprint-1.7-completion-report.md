# Sprint 1.7 Completion Report — Search & SEO Foundation

**Date:** 2026-07-19  
**Location:** `V:\DevelopmentRepo\AOVIAS Website\AOVIAS Web`  
**Status:** Complete (reusable SEO infrastructure only; Sprint 1.8 not started)

---

## 1. SEO Architecture Overview

SEO is **centralized and metadata-driven**. Future pages call a typed builder instead of hand-rolling Next.js `Metadata` objects.

```text
config/seo.ts + config/metadata.ts + config/site.ts
        │
        ▼
lib/seo/*  (canonical, OG, Twitter, schema, builder, robots, sitemap)
        │
        ├── createRootMetadata() → app/layout.tsx
        ├── createPageMetadata() → future page exports
        ├── buildDefaultSiteSchemas() → JsonLdScript (Organization + WebSite)
        ├── app/robots.ts
        └── app/sitemap.ts
```

**Out of scope (intentionally):** homepage/product/blog page-specific SEO copy, on-site search engine.

---

## 2. Folder Structure

```text
apps/website/src/
├── config/
│   ├── site.ts
│   ├── seo.ts
│   └── metadata.ts
├── lib/seo/
│   ├── env.ts
│   ├── canonical.ts
│   ├── opengraph.ts
│   ├── twitter.ts
│   ├── schema.ts
│   ├── metadata-builder.ts   # createPageMetadata / createRootMetadata
│   ├── robots.ts
│   ├── sitemap.ts
│   └── index.ts
├── components/seo/
│   ├── json-ld.tsx
│   └── index.ts
└── app/
    ├── layout.tsx            # createRootMetadata + site JSON-LD
    ├── robots.ts
    └── sitemap.ts

docs/design-system/seo-builder.md
```

---

## 3. Metadata Framework Summary

| Capability | Implementation |
| --- | --- |
| Typed builder | `createPageMetadata({ title, description, path, type })` |
| Root defaults | `createRootMetadata()` — title template, OG, Twitter, robots, icons |
| Canonical | `buildCanonicalUrl` / `getSiteUrl` (env-aware via `NEXT_PUBLIC_SITE_URL`) |
| Open Graph | `buildOpenGraph` — title, description, url, image, siteName, locale, type |
| Twitter/X | `buildTwitterCard` — summary / summary_large_image |
| Theme color / icons | from `seoConfig` |
| Robots metadata | production index; non-production noindex |
| Page types | website, article, product, profile, documentation, legal, marketing, company |

---

## 4. Structured Data Summary

| Schema | Builder | Emitted |
| --- | --- | --- |
| Organization | `buildOrganizationSchema` | Root via `JsonLdScript` |
| WebSite (+ SearchAction stub) | `buildWebsiteSchema` | Root via `JsonLdScript` |
| WebPage | `buildWebPageSchema` | Ready for future pages |
| BreadcrumbList | `buildBreadcrumbListSchema` | Used by `breadcrumbsToJsonLd` |

Composable helper: `buildPageSchemas({ title, description, path, breadcrumbs })`.

---

## 5. Sitemap Summary

- Generator: `app/sitemap.ts` → `buildSitemapEntries()`
- Registry: `seoConfig.sitemap.routes` (currently `/` only)
- Production-only URLs; empty sitemap when indexing is disabled
- Preview and `/dev` routes are not registered
- Future pages: append to the registry (or pass extras into `buildSitemapEntries`)

---

## 6. Robots Summary

- Generator: `app/robots.ts` → `buildRobotsConfig()`
- **Non-production:** `Disallow: /`
- **Production:** allow `/`, disallow `/dev/`, `/api/`, `/auth/`
- Sitemap URL + host derived from `getSiteUrl()`

---

## 7. Validation Results

| Check | Result |
| --- | --- |
| Metadata framework + builder | Pass |
| Canonical / OG / Twitter | Pass |
| JSON-LD Organization + WebSite | Pass |
| Sitemap route (`/sitemap.xml`) | Pass (build) |
| Robots route (`/robots.txt`) | Pass (build) |
| TypeScript | Pass |
| ESLint | Pass |
| Production build | Pass (12 static routes) |
| No page-specific SEO content | Pass |

---

## 8. Recommended improvements before Sprint 1.8

1. Add real OG/favicon assets under `public/` (`/og/default.png`, logo, apple icon)
2. When public pages ship, register them in `seoConfig.sitemap.routes` and call `createPageMetadata`
3. Emit per-page `WebPage` + `BreadcrumbList` JSON-LD from layouts that already show breadcrumbs
4. Optional CI check: validate JSON-LD / metadata snapshots
5. Expand robots disallow patterns if staging hosts share the production domain

Do **not** begin Sprint 1.8 automatically.
