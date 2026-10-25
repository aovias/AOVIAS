# Phase V7 — Editorial Layout System Report

## Pattern

Magazine layout via `EditorialMagazineGrid`:

- Lead featured card (wide column)
- Two supporting cards
- Remaining items in a tertiary row

Avoids monotonous equal-weight 3-column grids on knowledge hubs.

## Components

| Component | Role |
|-----------|------|
| `KnowledgeEditorialTemplate` | Section hubs |
| `EditorialMagazineGrid` | Varied editorial rhythm |
| `ArticleCard` | Cover + meta + featured state |
| `PlatformSectionBand` | Section chrome |
| `TechnologyStoryFlow` | Learning path visualization |

## Reading pages

Article/insight/library details retain long-form layout with:

- Reading progress
- Sticky TOC
- Related knowledge
- Principle callout block for editorial emphasis
