# Product Components API

Import: `import { ProductHero, MediaSlotView, … } from '@/components/product'`

Showcase: `/internal/design-system/product`

All content is **props-only**. No product names are hardcoded in components.

## Media Slot Architecture

```ts
type MediaSlot =
  | { kind: 'image' | 'screenshot' | 'diagram'; src; alt; … }
  | { kind: 'video'; src; alt; poster?; … }
  | { kind: 'code'; code; language?; … }
  | { kind: 'placeholder'; label; aspect?; … }
  | { kind: 'custom' | 'demo' | 'animation'; node; alt?; … }
```

Use `MediaSlotView` inside heroes, features, galleries, and architecture blocks.

| Component | Key props | Variants |
| --- | --- | --- |
| `ProductHero` | `name`, `badge?`, `description?`, CTAs, `media?`, `highlights?` | dual media optional |
| `ProductOverview` | `heading`, `description`, `media?`, `metrics?`, `mediaPosition?` | start \| end media |
| `FeatureShowcase` | `title`, `description`, `icon?`, `media?`, `align?` | start \| end |
| `CapabilityGrid` | `items[]`, `cols?`, `stagger?`, `showCategories?` | cols 2–4 |
| `ScreenshotGallery` | `items[]`, `mode?`, `preview?` | grid \| carousel + modal |
| `ProductNavigation` | `items[]` (`href` = `#section-id`) | sticky + active section |
| `ArchitectureDiagramBlock` | `title`, `diagram` (MediaSlot), `caption?`, `download?` | — |
| `TechnicalSpecificationBlock` | `categories[]`, `layout?` | auto \| table \| cards |
| `IntegrationGrid` | `items[]`, `cols?` | linked / static cards |

## Motion

- Section bands: `MarketingReveal` → `sectionReveal`
- Capability stagger: Motion Registry stagger variants
- Gallery modal: focus trap + Esc (interaction, not decorative motion)

## Accessibility

- Single `h1` in ProductHero; section headings below
- Gallery dialog: `aria-modal`, focus trap, arrow keys
- Spec tables: captions + `scope` headers
- Product nav: `aria-current` on active link
