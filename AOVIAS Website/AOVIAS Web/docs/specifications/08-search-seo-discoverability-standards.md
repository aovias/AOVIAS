---
title: "AOVIAS Search, SEO & Discoverability Standards"
source: appflowy
workspace: AOVIAS Website
space: General
view_id: "9df334fc-8066-4643-a423-8ff229d3d076"
exported_at: "2026-07-19"
---

# AOVIAS Search, SEO & Discoverability Standards
### Purpose

This document defines the mandatory standards for search engine optimization, discoverability, structured metadata, and search experience across the AOVIAS Public Website.

The objective is to ensure that every public page is:

- Easily discoverable

- Technically optimized

- Semantically structured

- AI-search friendly

- Enterprise-ready

- Future-proof

Search optimization is considered part of the product architecture rather than a post-development activity.

---

## 1. Core Principle

Every public page shall answer three questions:

1. What is this page about?

2. Why is it important?

3. How should search engines and AI systems understand it?

If the answer is unclear, the page is incomplete.

---

## 2. Metadata Standards

Every public page shall include:

- Unique <title>

- Unique meta description

- Canonical URL

- Open Graph metadata

- Twitter/X Card metadata

- Theme color

- Favicon metadata

- Robots directives

Metadata shall be generated centrally and consistently.

---

## 3. Page Titles

Titles shall:

- Be unique.

- Describe page intent.

- Include the AOVIAS brand where appropriate.

- Avoid keyword stuffing.

Examples:

Healthcare Compliance Platform | AOVIAS Command

Enterprise Software Platform | AOVIAS

About AOVIAS

Engineering at AOVIAS

---

## 4. Meta Descriptions

Every page shall contain:

- A unique description.

- 140–160 characters where practical.

- Natural language.

- Clear value.

- No clickbait.

Descriptions should accurately summarize page content.

---

## 5. Canonical URLs

Every public page shall declare a canonical URL.

Requirements:

- One canonical per page.

- HTTPS only.

- No duplicate canonical targets.

- No query parameters in canonical URLs unless intentionally canonicalized.

---

## 6. Open Graph

Every page shall define:

- og:title

- og:description

- og:image

- og:url

- og:type

- og:site_name

Social previews should accurately represent the page.

---

## 7. Twitter/X Cards

Every page shall support:

- Summary Large Image Card

- Title

- Description

- Preview Image

Metadata should mirror Open Graph where appropriate.

---

## 8. Structured Data (JSON-LD)

Structured data shall be implemented wherever applicable.

Supported schemas include:

- Organization

- WebSite

- WebPage

- BreadcrumbList

- Product

- SoftwareApplication

- FAQPage

- Article

- BlogPosting

- JobPosting

- Person

- ContactPage

- CollectionPage

- SearchAction

Schema should reflect the page's true purpose.

---

## 9. Breadcrumbs

Pages beyond the first navigation level shall provide:

- Visual breadcrumbs

- Breadcrumb structured data

Breadcrumbs improve both usability and discoverability.

---

## 10. XML Sitemap

The sitemap shall:

- Be generated automatically.

- Include all indexable pages.

- Exclude non-public routes.

- Update automatically.

- Support future multilingual expansion.

---

## 11. Robots

Provide:

- robots.txt

- robots meta tags

- Crawl directives

- Sitemap location

Sensitive or duplicate pages should be excluded intentionally.

---

## 12. URL Standards

URLs shall be:

- Human-readable

- Stable

- Lowercase

- Hyphen-separated

- Meaningful

Example:

/products/command

/products/studio

/company/our-story

/resources/case-studies

Avoid IDs or implementation-specific URLs in public routes.

---

## 13. Heading Hierarchy

Every page shall contain:

- One H1

- Logical H2 hierarchy

- Proper H3 usage

- No skipped heading levels

Headings should reflect document structure, not visual styling.

---

## 14. Internal Linking

Pages shall link naturally to related content.

Examples:

- Product → Pricing

- Product → Documentation

- Blog → Related Products

- Careers → Culture

- Company → Leadership

Internal linking should support both navigation and search understanding.

---

## 15. Image Optimization

Every image shall include:

- Descriptive filename

- Meaningful alt text

- Appropriate dimensions

- Modern formats (WebP/AVIF where suitable)

- Responsive sizing

Decorative images should use empty alt attributes.

---

## 16. Performance & SEO

Search performance depends on technical performance.

Targets:

- Core Web Vitals within Google's recommended thresholds

- Optimized font loading

- Optimized image loading

- Minimal layout shifts

- Efficient JavaScript delivery

Performance regressions are SEO regressions.

---

## 17. AI Search Optimization

Content should be authored for machine understanding as well as human readers.

Requirements:

- Clear information hierarchy

- Descriptive section headings

- Well-structured content

- Semantic HTML

- Explicit relationships between concepts

- Consistent terminology

The site should be easy for AI systems to summarize accurately.

---

## 18. Content Standards

Content shall:

- Be original.

- Be factual.

- Avoid keyword stuffing.

- Use natural language.

- Reflect AOVIAS' institutional tone.

Trustworthy writing is preferred over aggressive optimization.

---

## 19. Internationalization Readiness

Prepare for future multilingual support.

Requirements:

- hreflang-ready architecture

- Locale-aware metadata

- Localized structured data

- Locale-specific sitemaps (future)

The initial implementation may be English-only, but it should not prevent future expansion.

---

## 20. Analytics Readiness

Every page should expose consistent metadata for analytics.

Track events such as:

- Page views

- CTA interactions

- Demo requests

- Contact submissions

- Pricing engagement

- Resource downloads

Tracking should be privacy-conscious and aligned with applicable regulations.

---

## 21. Technical Validation

Every page shall pass:

- No missing titles

- No duplicate titles

- No missing descriptions

- Valid canonical URL

- Valid structured data

- Valid Open Graph metadata

- Valid robots directives

- Valid sitemap inclusion

- No broken internal links

Automated validation should be part of CI.

---

## 22. SEO Acceptance Criteria

A page is considered search-ready only when:

- Metadata is complete.

- Structured data is valid.

- Internal links are present.

- Images are optimized.

- Performance targets are met.

- Sitemap includes the page.

- Robots configuration is correct.

- Accessibility requirements are satisfied.

---

## 23. Governance

Search configuration shall be centralized.

Recommended structure:

src/
├── config/
│   ├── seo.ts
│   ├── metadata.ts
│   ├── schema.ts
│   ├── sitemap.ts
│   └── robots.ts
│
├── lib/
│   └── seo/
│       ├── metadata-builder.ts
│       ├── schema-builder.ts
│       ├── breadcrumbs.ts
│       └── canonical.ts

No page should manually duplicate SEO logic.

---

## 24. Future Search Features

The architecture should accommodate:

- Site-wide search

- AI-powered semantic search

- Documentation search

- Product search

- Changelog search

- Knowledge base search

Search should become a platform capability rather than an isolated feature.

---

## 25. Final Principle

> Every page should be understandable by a human in seconds, by a search engine in milliseconds, and by an AI system with complete contextual clarity.

The AOVIAS website should not chase search rankings through tricks or volume. It should earn discoverability by publishing well-structured, authoritative, and technically excellent content that accurately reflects the company's mission and expertise.

---

### One recommendation I'd add

Given AOVIAS' long-term ambitions, I'd create a separate Content Taxonomy & Knowledge Graph Specification (Document 09). It would define relationships between concepts such as:

- Company → Products

- Products → Industries

- Industries → Solutions

- Technologies → Capabilities

- Resources → Products

- People → Articles

- Case Studies → Industries

That taxonomy can drive navigation, internal linking, structured data, related content, breadcrumbs, and future AI-assisted search, giving the website a coherent information model rather than just a collection of pages.
