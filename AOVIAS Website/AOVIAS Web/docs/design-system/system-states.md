# System States API

Import: `import { EmptyState, ErrorState, LoadingState, … } from '@/components/system-states'`

Showcase: `/internal/design-system/system-states`

All messaging is **props-only**. No page copy, retry logic, or status monitoring lives in these components.

Composes primitives (`Spinner`, `Skeleton`) and marketing CTA links.

> Note: A lower-level `EmptyState` also exists in `@/components/primitives`. Prefer `@/components/system-states` for page-level empty/error/recovery UX.

## Component API

| Component | Key props | Variants |
| --- | --- | --- |
| `EmptyState` | `title`, `description?`, `illustration?`, actions | default \| subtle \| plain |
| `ErrorState` | `title`, `description?`, `retryAction?`, `supportAction?` | generic \| not-found \| unavailable \| permission |
| `LoadingState` | `message?`, `size?`, `progress?` | sm \| md \| lg · spinner or progress |
| `MaintenanceScreen` | `title`, `statusLabel?`, `estimatedAvailability?`, contact | — |
| `ComingSoon` | `title`, CTAs, `countdownPlaceholder?`, `newsletterPlaceholder?` | — |
| `SearchEmptyState` | `query?`, `suggestions?`, clear/browse actions | — |
| `NoResults` | `title`, `variant?`, `contextLabel?`, `suggestions?` | generic \| filtered \| category |

### Skeleton System

| Skeleton | Purpose |
| --- | --- |
| `TextSkeleton` | Multi-line text |
| `CardSkeleton` | Card + optional media |
| `ImageSkeleton` | Aspect-ratio image |
| `GridSkeleton` | Card grid |
| `ListSkeleton` | Avatar + text rows |
| `HeroSkeleton` | Hero band |
| `TableSkeleton` | Header + rows |

### Usage

```tsx
import { EmptyState, ErrorState, GridSkeleton } from '@/components/system-states';

{isLoading ? <GridSkeleton count={6} cols={3} /> : null}

<EmptyState
  title="No documents yet"
  description="Published documents will appear here."
  primaryAction={{ label: 'Browse catalog', href: '/resources' }}
/>

<ErrorState
  title="We could not load this page"
  description="Please try again. If the issue continues, contact support."
  retryAction={{ label: 'Try again', onClick: () => refetch() }}
  supportAction={{ label: 'Contact support', href: '/contact' }}
/>
```

## Motion

- Empty / error / maintenance / coming soon / search empty / no results: optional `MarketingReveal` → `sectionReveal`
- Skeletons: primitive `animate-pulse`
- Spinner: primitive `animate-spin`
- No inline Framer variant objects

## Accessibility

- Empty / search / no results / loading: `role="status"` (+ `aria-busy` where loading)
- Error: `role="alert"`
- Progress: `role="progressbar"` with valuemin/max/now
- Skeletons: `aria-busy` / `aria-label` via Skeleton primitive
- Actions: focusable links/buttons
- Never rely on color alone for meaning

## Responsive

- Centered panels with `max-w-lg` / narrow containers
- Maintenance / Coming Soon use layout Container
- Skeleton grids follow primitive Grid breakpoints

## Theme

- Semantic tokens only (`bg-muted`, `border-error/30`, `bg-error-subtle`, primary progress)
- Light/dark via Theme System
