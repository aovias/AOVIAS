# Marketing Components API

Import: `import { Hero, FeatureCard, … } from '@/components/marketing'`

Showcase: `/internal/design-system`

All content is **props-only**. Sample strings in the showcase are for QA.

| Component | Key props | Variants |
| --- | --- | --- |
| `Hero` | `title`, `eyebrow?`, `description?`, `primaryCta?`, `secondaryCta?`, `illustration?`, `backgroundClassName?`, `align?`, `reveal?` | align: start \| center |
| `SectionHeader` | `heading`, `eyebrow?`, `description?`, `align?`, `width?`, `headingLevel?` | align, width: narrow \| default \| wide |
| `CtaSection` | `title`, `description?`, `primaryCta?`, `secondaryCta?`, `background?`, `reveal?` | background: muted \| subtle \| inverse \| none |
| `FeatureGrid` | `children`, `cols?`, `stagger?` | cols: 2 \| 3 \| 4 |
| `FeatureCard` | `title`, `description`, `icon?`, `href?`, `linkLabel?` | hover when linked |
| `StatisticCard` | `value`, `label`, `description?`, `trend?`, `trendLabel?` | trend: up \| down \| neutral |
| `PrincipleCard` | `principle`, `description`, `icon?` | — |
| `ValueCard` | `heading`, `description`, `icon?` | — |
| `Timeline` | `events[]`, `orientation?` | vertical \| horizontal |
| `QuoteBlock` | `quote`, `author`, `position?`, `organization?`, `avatar?`, `avatarInitials?` | — |
| `LogoCloud` | `items[]`, `size?`, `label?` | size: sm \| md \| lg |

## Motion

- `MarketingReveal` → registry `sectionReveal`
- Linked `FeatureCard` → `MotionHover` / `cardHover`
- `FeatureGrid` stagger → `staggerContainer` / `staggerItem`

## Accessibility

- Semantic headings, lists (`Timeline`), figures (`QuoteBlock`)
- CTAs are links with button styles (`MarketingCtaLink`)
- Logo items expose accessible names
- Focus rings from primitives
