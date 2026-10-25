# Resource Components API

Import UI: `import { ArticleCard, ResourceGrid, … } from '@/components/resources'`  
Import models: `import type { ContentItem, … } from '@/components/content'`

Showcase: `/internal/design-system/resources`

All content is **props-only** via the Content Domain Model. No editorial copy is hardcoded in components.

## Content Domain Model

`apps/website/src/components/content/models.ts`

```text
ContentItem
├── Article / Insight
├── DocumentationPage
├── CaseStudy
├── Whitepaper
├── ReleaseNote
├── AcademyLesson
└── Resource (alias)
```

Also: `ContentAuthor`, `ContentCategory`, `ContentTag`, `ContentImage`, `SearchResult`, `TocItem`.

Helpers in `@/components/resources`: `sortContentItems`, `filterContentByKind`, `formatPublishDate`, `formatReadingTime`, `splitHighlightedText`.

## Component API

| Component | Key props | Notes |
| --- | --- | --- |
| `ArticleCard` | `item`, `featured?`, `reveal?` | Any `ContentItem` kind |
| `AuthorBlock` | `author`, `variant?` | compact \| detailed |
| `CategoryBadge` | `category`, `active?`, `onClick?` | Filter-ready chips |
| `ResourceGrid` | `items[]`, `cols?` | Parent filters/sorts |
| `SearchResults` | `results[]`, `query?`, `totalCount?` | Highlights + empty state |
| `Pagination` | `currentPage`, `totalPages`, `getPageHref?` / `onPageChange?` | Mobile page status |
| `ReadingProgress` | `targetSelector?` | Sticky progressbar |
| `TableOfContents` | `items[]`, `activeId?`, `sticky?` | Nested + IntersectionObserver |

### Usage

```tsx
import {
  ResourceGrid,
  Pagination,
  sortContentItems,
  type ContentItem,
} from '@/components/resources';

const pageItems = sortContentItems(items, 'publishedAt', 'desc').slice(0, 9);

<>
  <ResourceGrid items={pageItems} cols={3} />
  <Pagination
    currentPage={1}
    totalPages={4}
    getPageHref={(page) => `/resources?page=${page}`}
  />
</>
```

## Motion

- Cards / grid / search: optional `MarketingReveal` → `sectionReveal`
- TOC active state: CSS class transitions (no inline Framer variants)
- Reading progress: width update (accessibility progressbar)

## Accessibility

- Cards: linked titles, `<time dateTime>`, list semantics in grids
- Search: `role="status"` counts; `<mark>` for highlights
- Pagination: `nav` + `aria-current="page"`; keyboard via links/buttons
- Reading progress: `role="progressbar"` + valuetext
- TOC: `nav`, `aria-current="location"` on active item
- Author: labelled `aside`

## Responsive

- Grid: 1 → 2 → 3/4 columns via primitive `Grid`
- Pagination: page numbers from `sm`; mobile shows “Page X of Y”
- TOC: sticky from `lg`
- Cards: stacked media + metadata

## Theme

- Semantic tokens only; light/dark via Theme System
- Highlight marks use `bg-primary/15`
