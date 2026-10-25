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

import { FormsShowcase } from './forms-showcase';

export const metadata: Metadata = {
  title: 'Form Engine — Design System',
  robots: { index: false, follow: false },
};

/**
 * Internal gallery for enterprise Form Engine.
 * Route: /internal/design-system/forms
 */
export default function FormsDesignSystemPage() {
  return (
    <Container width="wide">
      <Section spacing="lg">
        <Stack gap={3}>
          <Text as="p" size="small" tone="muted">
            <Link
              href="/internal/design-system/trust"
              className="text-primary underline-offset-4 hover:underline"
            >
              ← Trust components
            </Link>
          </Text>
          <Heading level={1} size="h1">
            Form Engine
          </Heading>
          <Paragraph tone="muted">
            Sprint 2.4 — configuration-driven enterprise forms with shared
            validation, fields, and success/error states. No backend integration
            in this sprint. Toggle theme in the header.
          </Paragraph>
        </Stack>
      </Section>
      <FormsShowcase />
    </Container>
  );
}
