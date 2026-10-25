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

import { TrustShowcase } from './trust-showcase';

export const metadata: Metadata = {
  title: 'Trust Components — Design System',
  robots: { index: false, follow: false },
};

/**
 * Internal gallery for trust presentation components.
 * Route: /internal/design-system/trust
 */
export default function TrustDesignSystemPage() {
  return (
    <Container width="wide">
      <Section spacing="lg">
        <Stack gap={3}>
          <Text as="p" size="small" tone="muted">
            <Link
              href="/internal/design-system/product"
              className="text-primary underline-offset-4 hover:underline"
            >
              ← Product components
            </Link>
          </Text>
          <Heading level={1} size="h1">
            Trust components
          </Heading>
          <Paragraph tone="muted">
            Sprint 2.3 — data-driven trust signals via the Trust Data Model. Sample props are
            for QA only; do not treat them as product claims. Toggle theme in the header.
          </Paragraph>
        </Stack>
      </Section>
      <TrustShowcase />
    </Container>
  );
}
