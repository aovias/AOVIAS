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

import { ResourcesShowcase } from './resources-showcase';

export const metadata: Metadata = {
  title: 'Resource Components — Design System',
  robots: { index: false, follow: false },
};

/**
 * Internal gallery for knowledge / resource presentation components.
 * Route: /internal/design-system/resources
 */
export default function ResourcesDesignSystemPage() {
  return (
    <Container width="wide">
      <Section spacing="lg">
        <Stack gap={3}>
          <Text as="p" size="small" tone="muted">
            <Link
              href="/internal/design-system/pricing"
              className="text-primary underline-offset-4 hover:underline"
            >
              ← Pricing components
            </Link>
          </Text>
          <Heading level={1} size="h1">
            Resource components
          </Heading>
          <Paragraph tone="muted">
            Sprint 2.6 — content-agnostic knowledge presentation via the Content
            Domain Model. Sample props are for QA only. Toggle theme in the
            header.
          </Paragraph>
        </Stack>
      </Section>
      <ResourcesShowcase />
    </Container>
  );
}
