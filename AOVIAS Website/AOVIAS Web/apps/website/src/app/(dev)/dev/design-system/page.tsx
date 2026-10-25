import type { Metadata } from 'next';

import { Container, Heading, Paragraph, Section, Stack } from '@/components/primitives';

import { ShowcaseSections } from './showcase-sections';

export const metadata: Metadata = {
  title: 'Design System Showcase',
  robots: { index: false, follow: false },
};

/**
 * Living component reference for development and QA (Storybook-equivalent).
 * Route: /dev/design-system
 */
export default function DesignSystemShowcasePage() {
  return (
    <Container width="wide">
      <Section spacing="lg">
        <Stack gap={3}>
          <Heading level={1} size="h1">
            Primitive component showcase
          </Heading>
          <Paragraph tone="muted">
            Primitives (Sprint 1.5) and Motion Foundation (Sprint 1.6) — variants, states, and
            the motion registry. Toggle theme and OS reduced-motion to verify tokens. Not for
            production indexing.
          </Paragraph>
        </Stack>
      </Section>
      <ShowcaseSections />
      <Section spacing="lg">
        <Paragraph size="small" tone="muted">
          Documentation: <code>docs/design-system/</code> · Reports:{' '}
          <code>docs/development/sprint-1.5-completion-report.md</code>,{' '}
          <code>docs/development/sprint-1.6-completion-report.md</code>
        </Paragraph>
      </Section>
    </Container>
  );
}
