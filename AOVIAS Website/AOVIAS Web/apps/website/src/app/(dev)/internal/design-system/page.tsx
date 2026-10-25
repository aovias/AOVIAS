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

import { MarketingShowcase } from './marketing-showcase';

export const metadata: Metadata = {
  title: 'Marketing Components — Design System',
  robots: { index: false, follow: false },
};

/**
 * Internal design-system gallery for marketing building blocks.
 * Route: /internal/design-system
 */
export default function InternalDesignSystemPage() {
  return (
    <Container width="wide">
      <Section spacing="lg">
        <Stack gap={3}>
          <Text as="p" size="small" tone="muted">
            <Link
              href="/dev/design-system"
              className="text-primary underline-offset-4 hover:underline"
            >
              ← Primitives showcase
            </Link>
          </Text>
          <Heading level={1} size="h1">
            Marketing components
          </Heading>
          <Paragraph tone="muted">
            Sprint 2.1 — prop-driven enterprise blocks. Sample strings below are for QA
            only; production pages supply their own content. Toggle theme in the header.
          </Paragraph>
          <Text as="p" size="small">
            <Link
              href="/internal/design-system/product"
              className="text-primary underline-offset-4 hover:underline"
            >
              Product components gallery →
            </Link>
          </Text>
        </Stack>
      </Section>
      <MarketingShowcase />
    </Container>
  );
}
