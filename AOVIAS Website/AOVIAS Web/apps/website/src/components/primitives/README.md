# AOVIAS UI Primitives

Reusable foundation components for the public website. Import from `@/components/primitives`.

## Groups

| Group | Components |
| --- | --- |
| Layout | `Container`, `Section`, `Grid`, `Stack`, `Divider`, `Spacer` |
| Typography | `Heading`, `Text`, `Paragraph`, `Caption`, `Label`, `Link`, `Code`, `Quote` |
| Controls | `Button`, `IconButton`, `Badge`, `Input`, `Textarea`, `Select`, `Checkbox`, `Radio`, `Switch` |
| Display | `Card` (+ slots), `Surface`, `IconWrapper`, `AvatarPlaceholder`, `Separator` |
| Feedback | `Spinner`, `Skeleton`, `EmptyState`, `Alert` |

## Principles

- Visual values come from design tokens (`@aovias/design-system`) via semantic Tailwind utilities.
- Server Components by default; only interactive controls mark `'use client'`.
- No page-specific or marketing-section components live here.
- Living showcase: `/dev/design-system`

## Documentation

See `docs/design-system/` for API summary, variant matrix, and accessibility notes.
