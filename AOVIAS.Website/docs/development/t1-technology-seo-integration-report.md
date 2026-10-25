# T1 — SEO Integration Report

**Date:** 2026-07-20

---

## Metadata

- Landing: `createPageMetadata` + `technologyLandingSeo`
- Topics: `generateMetadata` + `technologyTopicSeo(slug)`
- OG image: `/og/default.png` (no invented Technology OG asset in T1)

## Structured data

`buildPageSchemas` with Organization + WebPage + BreadcrumbList on landing and topic pages.

## Sitemap

All T1 Technology paths registered in [`seoConfig.sitemap.routes`](../../apps/website/src/config/seo.ts):

`/technology` + architecture, platform, ai, security, privacy, integrations, apis, developer, roadmap  

Verified live sitemap includes Technology URLs (10 `/technology` matches).

## Internal linking

Landing cards, footer Technology column, mega menu, and placeholder “related topics” all derive from the registry.
