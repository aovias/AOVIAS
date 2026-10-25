import type { Metadata } from 'next';
import Link from 'next/link';

import {
  Container,
  Heading,
  Paragraph,
  Section,
  Stack,
  Text,
} from '@/components/primitives';

import { PricingShowcase } from './pricing-showcase';

export const metadata: Metadata = {
  title: 'Pricing Components — Design System',
  robots: { index: false, follow: false },
};

/**
 * Internal gallery for pricing & commerce presentation components.
 * Route: /internal/design-system/pricing
 */
export default function PricingDesignSystemPage() {
  return (
    <Container width="wide">
      <Section spacing="lg">
        <Stack gap={3}>
          <Text as="p" size="small" tone="muted">
            <Link
              href="/internal/design-system/forms"
              className="text-primary underline-offset-4 hover:underline"
            >
              ← Form Engine
            </Link>
          </Text>
          <Heading level={1} size="h1">
            Pricing & commerce
          </Heading>
          <Paragraph tone="muted">
            Sprint 2.5 — product-agnostic pricing presentation via the Pricing
            Domain Layer. Sample figures are for QA only and are not commercial
            offers. Toggle theme in the header.
          </Paragraph>
        </Stack>
      </Section>
      <PricingShowcase />
    </Container>
  );
}
