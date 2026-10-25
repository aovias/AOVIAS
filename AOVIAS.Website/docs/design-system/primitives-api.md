# Primitive Component API Summary

Import: `import { Button, Card, … } from '@/components/primitives'`

Living showcase: `/dev/design-system`

---

## Layout

| Component | Key props | Notes |
| --- | --- | --- |
| `Container` | `width?: default \| narrow \| wide \| full`, `as?` | Responsive gutters; `PageContainer` maps legacy `content/hero` |
| `Section` | `spacing?: none \| sm \| md \| lg \| xl`, `id?` | Semantic `<section>` by default |
| `Grid` | `cols?`, `gap?`, `align?`, `justify?` | Responsive column collapse |
| `Stack` | `direction?`, `gap?`, `align?`, `justify?`, `wrap?` | Flex composition |
| `Divider` | `orientation?`, `decorative?` | Layout breaks |
| `Spacer` | `size?`, `axis?` | Token empty space |

## Typography

| Component | Key props |
| --- | --- |
| `Heading` | `level` 1–6, `size` (display/h*) |
| `Text` | `size`, `tone`, `weight`, `as` |
| `Paragraph` | `size`, `tone` |
| `Caption` | `as` |
| `Label` | `htmlFor`, `required` |
| `Link` | `href`, `external`, `tone`, `underline` |
| `Code` | `block` |
| `Quote` | `cite`, `attribution` |

## Controls

| Component | Key props |
| --- | --- |
| `Button` | `variant`, `size`, `loading`, native button attrs |
| `IconButton` | `aria-label` (required), `variant`, `size`, `loading` |
| `Badge` | `variant` |
| `Input` / `Textarea` / `Select` | native attrs + token styles |
| `Checkbox` / `Radio` | native attrs |
| `Switch` | `checked` / `defaultChecked`, `onCheckedChange` |

## Display

| Component | Key props / slots |
| --- | --- |
| `Card` | `variant`: default \| elevated \| flat \| interactive |
| `CardHeader` / `CardTitle` / `CardDescription` / `CardContent` / `CardFooter` | composition slots |
| `Surface` | `tone`, `padding` |
| `IconWrapper` | `size`, `tone`, `decorative`, `label` |
| `AvatarPlaceholder` | `initials`, `size`, `label` |
| `Separator` | `orientation`, `decorative` |

## Feedback

| Component | Key props |
| --- | --- |
| `Spinner` | `size`, `label` |
| `Skeleton` | `className`, `label` |
| `EmptyState` | `title`, `description?`, `icon?`, `action?` |
| `Alert` | `tone`, `title?`, `live` |

---

## Variant matrix (high level)

| Component | Variants |
| --- | --- |
| Button | primary, secondary, ghost, outline, destructive, link × sm/md/lg × loading/disabled |
| Badge | default, primary, secondary, outline, success, warning, error, info |
| Card | default, elevated, flat, interactive |
| Alert | info, success, warning, error |
| Container | default, narrow, wide, full |
| Surface | default, muted, subtle, inverse |

---

## Accessibility notes

- Focus rings use `ring` token (`focus-visible:ring-2 focus-visible:ring-ring`).
- `IconButton` requires `aria-label`.
- `Switch` exposes `role="switch"` + `aria-checked`.
- `Alert` uses `role="alert"`; errors may set `live="assertive"`.
- `EmptyState` / `Spinner` / `Skeleton` use `role="status"`.
- Headings preserve semantic levels independent of visual size.
- Form controls are native elements for keyboard + AT support (foundations).

## Theme behavior

All colors/shadows/radii/typography map to semantic CSS variables from `@aovias/design-system`. Light/dark via `data-theme` — no duplicated theme styles in components.

## Responsive behavior

- Container gutters: `px-4 sm:px-6 lg:px-8`
- Grid columns collapse at `sm` / `lg`
- Stack supports `wrap` for horizontal overflow on small viewports
- Typography sizes are token-driven (fluid scaling owned by tokens)
