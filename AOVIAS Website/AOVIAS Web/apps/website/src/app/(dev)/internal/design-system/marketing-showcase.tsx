'use client';

import {
  Box,
  Layers,
  Scale,
  Shield,
  Sparkles,
  Target,
} from 'lucide-react';
import type { ReactNode } from 'react';

import {
  CtaSection,
  FeatureCard,
  FeatureGrid,
  Hero,
  LogoCloud,
  PrincipleCard,
  QuoteBlock,
  SectionHeader,
  StatisticCard,
  Timeline,
  ValueCard,
} from '@/components/marketing';
import {
  Caption,
  Divider,
  Heading,
  Paragraph,
  Section,
  Stack,
  Surface,
  Text,
} from '@/components/primitives';

function ShowcaseBlock({
  id,
  title,
  notes,
  children,
}: {
  id: string;
  title: string;
  notes: string;
  children: ReactNode;
}) {
  return (
    <Section id={id} spacing="lg" className="border-border border-b">
      <Stack gap={8}>
        <Stack gap={2}>
          <Heading level={2} size="h3">
            {title}
          </Heading>
          <Paragraph size="small" tone="muted">
            {notes}
          </Paragraph>
        </Stack>
        {children}
      </Stack>
    </Section>
  );
}

export function MarketingShowcase() {
  return (
    <>
      <nav aria-label="Marketing components" className="border-border mb-8 border-b pb-6">
        <Stack direction="horizontal" gap={3} wrap>
          {[
            'hero',
            'section-header',
            'cta',
            'feature-grid',
            'cards',
            'timeline',
            'quote',
            'logo-cloud',
          ].map((id) => (
            <a
              key={id}
              href={`#${id}`}
              className="text-muted-foreground hover:text-foreground text-small focus-visible:ring-ring rounded-sm underline-offset-4 hover:underline focus-visible:ring-2 focus-visible:outline-none"
            >
              {id}
            </a>
          ))}
        </Stack>
      </nav>

      <ShowcaseBlock
        id="hero"
        title="Hero"
        notes="Align start/center · optional illustration · CTAs · token background. A11y: h1, labeled links."
      >
        <Stack gap={10}>
          <Hero
            eyebrow="Platform"
            title="Enterprise software with calm precision"
            description="Sample hero copy for layout QA — replace via props on real pages."
            primaryCta={{ label: 'Primary action', href: '/products/preview' }}
            secondaryCta={{ label: 'Secondary', href: '/company/preview' }}
            align="start"
            backgroundClassName="bg-muted/30"
            reveal={false}
          />
          <Hero
            eyebrow="Centered"
            title="Centered alignment variant"
            description="Use when illustration is absent and the band should read symmetrically."
            primaryCta={{ label: 'Continue', href: '/resources/preview' }}
            align="center"
            reveal={false}
          />
          <Hero
            title="Split layout with illustration slot"
            description="Illustration is an arbitrary React node — image, diagram, or placeholder."
            primaryCta={{ label: 'Explore', href: '/products/preview' }}
            illustration={
              <Surface
                tone="muted"
                padding="lg"
                className="flex min-h-48 items-center justify-center rounded-lg border border-border"
              >
                <Text tone="muted">Illustration slot</Text>
              </Surface>
            }
            reveal={false}
          />
        </Stack>
      </ShowcaseBlock>

      <ShowcaseBlock
        id="section-header"
        title="Section Header"
        notes="Eyebrow · heading · description · align/width variants."
      >
        <Stack gap={8}>
          <SectionHeader
            eyebrow="Capabilities"
            heading="Section heading start-aligned"
            description="Supporting description constrained to the default width."
          />
          <SectionHeader
            eyebrow="Centered"
            heading="Section heading centered"
            description="Useful above grids and logo clouds."
            align="center"
            width="narrow"
          />
        </Stack>
      </ShowcaseBlock>

      <ShowcaseBlock
        id="cta"
        title="CTA Section"
        notes="Background: muted | subtle | inverse | none."
      >
        <Stack gap={6}>
          <CtaSection
            title="Ready to continue?"
            description="Sample CTA band — wire real destinations in page composition."
            primaryCta={{ label: 'Primary', href: '/auth/preview' }}
            secondaryCta={{ label: 'Secondary', href: '/legal/preview' }}
            background="muted"
            reveal={false}
          />
          <CtaSection
            title="Inverse surface"
            description="High-contrast band for closing sections."
            primaryCta={{ label: 'Get started', href: '/auth/preview' }}
            background="inverse"
            reveal={false}
          />
        </Stack>
      </ShowcaseBlock>

      <ShowcaseBlock
        id="feature-grid"
        title="Feature Grid + Feature Card"
        notes="Responsive columns · stagger via Motion Registry · card hover when linked."
      >
        <FeatureGrid cols={3} stagger={false}>
          <FeatureCard
            icon={<Layers />}
            title="Composable"
            description="Assembled from primitives — no duplicated typography or buttons."
            href="/dev/design-system"
            linkLabel="View primitives"
          />
          <FeatureCard
            icon={<Shield />}
            title="Accessible"
            description="Semantic structure and focus-visible rings from the design system."
          />
          <FeatureCard
            icon={<Sparkles />}
            title="Calm motion"
            description="Hover uses registry cardHover — no inline animation objects."
            href="/internal/design-system"
          />
        </FeatureGrid>
      </ShowcaseBlock>

      <ShowcaseBlock
        id="cards"
        title="Statistic · Principle · Value cards"
        notes="Metric, principle, and value compositions for corporate/product bands."
      >
        <Stack gap={8}>
          <FeatureGrid cols={3} stagger={false}>
            <StatisticCard
              value="99.9%"
              label="Sample uptime metric"
              description="Placeholder statistic for layout QA."
              trend="up"
              trendLabel="QoQ"
            />
            <StatisticCard
              value="12"
              label="Regions"
              description="Neutral trend example."
              trend="neutral"
              trendLabel="Steady"
            />
            <StatisticCard
              value="48h"
              label="Response target"
              trend="down"
              trendLabel="Improved"
            />
          </FeatureGrid>
          <Divider />
          <FeatureGrid cols={3} stagger={false}>
            <PrincipleCard
              icon={<Scale />}
              principle="Clarity"
              description="Prefer precise language and restrained visuals."
            />
            <PrincipleCard
              icon={<Target />}
              principle="Purpose"
              description="Every element should earn its place on the page."
            />
            <PrincipleCard
              icon={<Box />}
              principle="System"
              description="Compose shared blocks instead of one-off layouts."
            />
          </FeatureGrid>
          <FeatureGrid cols={2} stagger={false}>
            <ValueCard
              icon={<Shield />}
              heading="Trust by default"
              description="Enterprise-ready patterns without theatrical decoration."
            />
            <ValueCard
              icon={<Layers />}
              heading="Layered architecture"
              description="Primitives → marketing blocks → page sections."
            />
          </FeatureGrid>
        </Stack>
      </ShowcaseBlock>

      <ShowcaseBlock
        id="timeline"
        title="Timeline"
        notes="Vertical (default) and horizontal (scroll on small viewports)."
      >
        <Stack gap={10}>
          <Timeline
            events={[
              {
                id: '1',
                date: '2024',
                title: 'Foundation',
                description: 'Sample event — replace with real milestones via props.',
              },
              {
                id: '2',
                date: '2025',
                title: 'Expansion',
                description: 'Second sample event in the vertical timeline.',
              },
              {
                id: '3',
                date: '2026',
                title: 'Platform',
                description: 'Third sample event for spacing QA.',
              },
            ]}
          />
          <Caption>Horizontal</Caption>
          <Timeline
            orientation="horizontal"
            events={[
              { id: 'h1', date: 'Q1', title: 'Discover', description: 'Sample.' },
              { id: 'h2', date: 'Q2', title: 'Design', description: 'Sample.' },
              { id: 'h3', date: 'Q3', title: 'Build', description: 'Sample.' },
              { id: 'h4', date: 'Q4', title: 'Scale', description: 'Sample.' },
            ]}
          />
        </Stack>
      </ShowcaseBlock>

      <ShowcaseBlock
        id="quote"
        title="Quote Block"
        notes="Quote · author · role · organization · avatar placeholder."
      >
        <QuoteBlock
          quote="Sample quotation used only to exercise attribution layout and typography."
          author="Alex Rivera"
          position="Chief Architect"
          organization="Example Corp"
          avatarInitials="AR"
        />
      </ShowcaseBlock>

      <ShowcaseBlock
        id="logo-cloud"
        title="Logo Cloud"
        notes="Placeholder labels · size variants · theme-aware surfaces."
      >
        <Stack gap={6}>
          <LogoCloud
            size="md"
            items={[
              { id: 'a', name: 'Partner A' },
              { id: 'b', name: 'Partner B' },
              { id: 'c', name: 'Partner C' },
              { id: 'd', name: 'Partner D' },
              { id: 'e', name: 'Partner E' },
              { id: 'f', name: 'Partner F' },
            ]}
          />
          <LogoCloud
            size="sm"
            label="Small density"
            items={[
              { id: 's1', name: 'Mark One' },
              { id: 's2', name: 'Mark Two' },
              { id: 's3', name: 'Mark Three' },
            ]}
          />
        </Stack>
      </ShowcaseBlock>

      <Section spacing="lg">
        <Stack gap={2}>
          <Text size="small" weight="medium">
            Accessibility notes
          </Text>
          <Paragraph size="small" tone="muted">
            Heroes use a single h1; section headers use h2/h3. CTAs are real links with
            button styles. Timelines use ordered lists. Logo cloud exposes accessible names.
            Focus rings come from primitives. Prefer keyboard checks on linked FeatureCards.
          </Paragraph>
        </Stack>
      </Section>
    </>
  );
}
