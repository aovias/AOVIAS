# Phase V7 — Content Structure Report

## Catalog

Single `knowledgeCatalog` (`ContentItem[]`) remains source of truth for:

- Blog articles
- Insights
- Documentation
- Resources / release notes
- Search
- Category pages
- Related / adjacent navigation

## Editorial content

Section narratives live in `features/knowledge/editorial/content/sections.tsx` — separate from article bodies so hubs can evolve without rewriting every slug.

## Detail bodies

`features/knowledge/detail/bodies.ts` — problem → principle → practice → close. Unchanged structurally; reading template presentation improved.
