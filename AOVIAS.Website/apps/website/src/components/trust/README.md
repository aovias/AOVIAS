# Trust Components

Reusable credibility, security, and institutional-trust building blocks.

Import: `import { TrustBadge, Faq, … } from '@/components/trust'`

## Trust Data Model

Components consume structured models from `models.ts`:

- `TrustSignal`, `SecurityControl`, `ComplianceFramework`
- `Certification`, `CustomerReference`, `TrustMetric`
- `TestimonialReference`, `FaqItem`, `TrustTimelineEvent`

Update structured data — not UI — when attestations or references change.

Showcase: `/internal/design-system/trust`

See `docs/design-system/trust-components.md`.
