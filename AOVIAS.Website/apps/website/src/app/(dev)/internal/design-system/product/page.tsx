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

import { ProductShowcase } from './product-showcase';

export const metadata: Metadata = {
  title: 'Product Components — Design System',
  robots: { index: false, follow: false },
};

/**
 * Internal gallery for product presentation components.
 * Route: /internal/design-system/product
 */
export default function ProductDesignSystemPage() {
  return (
    <Container width="wide">
      <Section spacing="lg">
        <Stack gap={3}>
          <Text as="p" size="small" tone="muted">
            <Link
              href="/internal/design-system"
              className="text-primary underline-offset-4 hover:underline"
            >
              ← Marketing components
            </Link>
          </Text>
          <Heading level={1} size="h1">
            Product components
          </Heading>
          <Paragraph tone="muted">
            Sprint 2.2 — data-driven product presentation with Media Slot architecture.
            Sample props are for QA only. Toggle theme in the header.
          </Paragraph>
        </Stack>
      </Section>
      <ProductShowcase />
    </Container>
  );
}
