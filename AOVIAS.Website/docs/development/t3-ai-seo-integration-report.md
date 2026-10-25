# T3 — AI SEO Integration Report

**Date:** 2026-07-20

---

## Metadata

Each AI page uses `createPageMetadata` via `aiPagesSeo` (title, description, path, keywords, OG image).

## Structured data

`buildPageSchemas` with Organization, WebPage, and BreadcrumbList on every AI route.

## Sitemap

Registered in `seoConfig.sitemap.routes` and `technologyNestedLivePaths`:

- `/technology/ai`  
- `/technology/ai/principles`  
- `/technology/ai/capabilities`  
- `/technology/ai/human-in-the-loop`  
- `/technology/ai/responsible`  
- `/technology/ai/roadmap`  

## Target themes

Enterprise AI · Responsible AI · Human-in-the-loop · AI governance · AI-assisted operations

## Internal linking

Hub explore cards, section CTAs to Platform / Principles / HITL / Responsible / Roadmap, Technology mega menu (Intelligence column, registry-driven), footer Technology column includes AI at AOVIAS.

## Breadcrumbs

Path-aware labels in `breadcrumbs.ts` (AI hub = “AI at AOVIAS”; nested AI roadmap ≠ Technology Roadmap).
