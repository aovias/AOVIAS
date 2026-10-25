# Trust Components API

Import: `import { TrustBadge, Faq, … } from '@/components/trust'`

Showcase: `/internal/design-system/trust`

All content is **props-only** via the Trust Data Model. No certifications, customers, or claims are hardcoded in components.

## Trust Data Model

Defined in `apps/website/src/components/trust/models.ts`. Update structured data — not UI — when attestations or references change.

| Model | Used by |
| --- | --- |
| `TrustSignal` | `TrustBadge` |
| `SecurityControl` | `SecuritySection` |
| `ComplianceFramework` | `ComplianceGrid` |
| `Certification` (+ `CertificationStatus`) | `CertificationsBlock` |
| `CustomerReference` | `CustomerLogoGrid` |
| `TrustMetric` | `SuccessMetrics` |
| `TestimonialReference` | `Testimonial` |
| `FaqItem` | `Faq` |
| `TrustTimelineEvent` | `TrustTimeline` |

```ts
type TrustSignal = {
  id: string;
  label: string;
  description?: string;
  icon?: ReactNode;
  variant?: 'default' | 'security' | 'compliance' | 'quality';
};

type Certification = {
  id: string;
  title: string;
  description?: string;
  status?: 'active' | 'in-progress' | 'planned';
  logo?: ReactNode; // omit → placeholder
  href?: string;
};

// See models.ts for SecurityControl, ComplianceFramework, CustomerReference,
// TrustMetric, TestimonialReference, FaqItem, TrustTimelineEvent.
```

## Component API

| Component | Key props | Variants / notes |
| --- | --- | --- |
| `TrustBadge` | `signal`, `size?`, `icon?` | size sm \| md \| lg · signal.variant |
| `SecuritySection` | `heading`, `description?`, `highlights?`, `controls?`, `cta?` | optional `MarketingReveal` |
| `ComplianceGrid` | `items[]`, `cols?` | cols 2–4 · optional `href` |
| `CertificationsBlock` | `items[]`, `cols?` | status badge · logo placeholder |
| `CustomerLogoGrid` | `customers[]`, `size?`, `label?` | composes marketing `LogoCloud` |
| `SuccessMetrics` | `metrics[]`, `cols?` | composes `StatisticCard` · optional trend |
| `Testimonial` | `testimonial`, `variant?`, `showRating?` | default \| emphasized · composes `QuoteBlock` |
| `Faq` | `items[]`, `groupByCategory?`, `searchable?`, `allowMultiple?` | client accordion · keyword filter |
| `TrustTimeline` | `events[]`, `orientation?` | vertical \| horizontal · composes `Timeline` |

### Usage examples

```tsx
import { TrustBadge, CertificationsBlock, type Certification } from '@/components/trust';

const certs: Certification[] = [
  {
    id: 'iso-27001',
    title: 'ISO/IEC 27001',
    description: 'Information security management.',
    status: 'active',
  },
];

<TrustBadge
  signal={{ id: 'enc', label: 'Encryption in transit', variant: 'security' }}
  size="md"
/>

<CertificationsBlock items={certs} cols={3} />
```

```tsx
import { Faq, type FaqItem } from '@/components/trust';

const items: FaqItem[] = [
  {
    id: 'data',
    question: 'How is customer data handled?',
    answer: '…',
    category: 'Security',
    keywords: ['privacy', 'retention'],
  },
];

<Faq items={items} groupByCategory searchable />
```

## Motion

- `SecuritySection` uses `MarketingReveal` → Motion Registry `sectionReveal` when `reveal` is true
- Other trust components are static or interaction-only (FAQ expand/collapse)
- No inline Framer variants

## Accessibility

- Semantic sections/lists; headings via primitives
- FAQ: `button` triggers, `aria-expanded`, `aria-controls`, keyboard toggle
- Testimonial rating: text + `aria-label` (not color-only)
- Certifications status via Badge text, not color alone
- Logo grid inherits LogoCloud labeling
- Focus rings from primitives / Input

## Responsive behaviour

- Grids use primitive `Grid` breakpoints (2–4 cols)
- SecuritySection stacks narrative then control cards
- TrustTimeline: vertical default; horizontal scrolls on narrow viewports via marketing Timeline
- FAQ full-width; search field stacks naturally
- CustomerLogoGrid follows LogoCloud responsive density

## Theme

- Semantic tokens only (`bg-muted`, `border-border`, `bg-card`, `bg-subtle`, primary accents)
- Light and dark via Theme System — no hardcoded colors
- Showcase uses shared `ThemeToggle`
