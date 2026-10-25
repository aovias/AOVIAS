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

import { SystemStatesShowcase } from './system-states-showcase';

export const metadata: Metadata = {
  title: 'System States — Design System',
  robots: { index: false, follow: false },
};

/**
 * Internal gallery for system state presentation components.
 * Route: /internal/design-system/system-states
 */
export default function SystemStatesDesignSystemPage() {
  return (
    <Container width="wide">
      <Section spacing="lg">
        <Stack gap={3}>
          <Text as="p" size="small" tone="muted">
            <Link
              href="/internal/design-system/resources"
              className="text-primary underline-offset-4 hover:underline"
            >
              ← Resource components
            </Link>
          </Text>
          <Heading level={1} size="h1">
            System states
          </Heading>
          <Paragraph tone="muted">
            Sprint 2.7 — reusable empty, error, loading, skeleton, maintenance,
            and recovery states. Copy is props-only. Toggle theme in the header.
          </Paragraph>
        </Stack>
      </Section>
      <SystemStatesShowcase />
    </Container>
  );
}
