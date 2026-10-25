'use client';

import { Layers, Network, Shield, Workflow } from 'lucide-react';
import type { ReactNode } from 'react';

import {
  ArchitectureDiagramBlock,
  CapabilityGrid,
  FeatureShowcase,
  IntegrationGrid,
  ProductHero,
  ProductNavigation,
  ProductOverview,
  ScreenshotGallery,
  TechnicalSpecificationBlock,
} from '@/components/product';
import {
  Caption,
  Code,
  Heading,
  Paragraph,
  Section,
  Stack,
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

const placeholder = (label: string) =>
  ({
    kind: 'placeholder' as const,
    label,
    aspect: 'video' as const,
  });

export function ProductShowcase() {
  return (
    <>
      <nav aria-label="Product components" className="border-border mb-8 border-b pb-6">
        <Stack direction="horizontal" gap={3} wrap>
          {[
            'hero',
            'nav',
            'overview',
            'feature',
            'capabilities',
            'gallery',
            'architecture',
            'specs',
            'integrations',
            'media-slots',
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
        title="Product Hero"
        notes="Badge · name · CTAs · Media Slot · feature highlights. No product-specific assumptions."
      >
        <ProductHero
          badge="Product"
          name="Sample product name"
          description="QA copy for layout only — real pages pass product-specific props."
          primaryCta={{ label: 'Primary CTA', href: '/products/preview' }}
          secondaryCta={{ label: 'Secondary CTA', href: '/resources/preview' }}
          highlights={[
            { id: 'h1', label: 'Highlight one for composition QA' },
            { id: 'h2', label: 'Highlight two for composition QA' },
          ]}
          media={placeholder('Product screenshot / media slot')}
          reveal={false}
        />
      </ShowcaseBlock>

      <ShowcaseBlock
        id="nav"
        title="Product Navigation"
        notes="Sticky section links with IntersectionObserver active state. Scroll the page to verify."
      >
        <ProductNavigation
          stickyOffsetClassName="top-14"
          items={[
            { id: 'n1', label: 'Overview', href: '#overview' },
            { id: 'n2', label: 'Feature', href: '#feature' },
            { id: 'n3', label: 'Capabilities', href: '#capabilities' },
            { id: 'n4', label: 'Gallery', href: '#gallery' },
            { id: 'n5', label: 'Architecture', href: '#architecture' },
          ]}
        />
        <Caption>
          Active state updates as showcase sections enter the viewport.
        </Caption>
      </ShowcaseBlock>

      <ShowcaseBlock
        id="overview"
        title="Product Overview"
        notes="Heading · description · metrics · media position."
      >
        <ProductOverview
          heading="Overview heading"
          description="Supporting overview paragraph supplied entirely via props."
          metrics={[
            { id: 'm1', value: 'A', label: 'Metric label' },
            { id: 'm2', value: 'B', label: 'Metric label' },
            { id: 'm3', value: 'C', label: 'Metric label' },
          ]}
          media={placeholder('Overview media')}
          reveal={false}
        />
      </ShowcaseBlock>

      <ShowcaseBlock
        id="feature"
        title="Feature Showcase"
        notes="Left/right media alignment · icon · Media Slot."
      >
        <Stack gap={6}>
          <FeatureShowcase
            title="Feature title (media end)"
            description="Description for a single capability row."
            icon={<Workflow />}
            media={placeholder('Feature media')}
            align="end"
            reveal={false}
          />
          <FeatureShowcase
            title="Feature title (media start)"
            description="Alternate layout with media on the start edge."
            icon={<Shield />}
            media={placeholder('Feature media alt')}
            align="start"
            reveal={false}
          />
        </Stack>
      </ShowcaseBlock>

      <ShowcaseBlock
        id="capabilities"
        title="Capability Grid"
        notes="Category labels · icons · responsive columns."
      >
        <CapabilityGrid
          stagger={false}
          items={[
            {
              id: 'c1',
              category: 'Security',
              title: 'Capability one',
              description: 'Sample capability description.',
              icon: <Shield />,
            },
            {
              id: 'c2',
              category: 'Platform',
              title: 'Capability two',
              description: 'Sample capability description.',
              icon: <Layers />,
            },
            {
              id: 'c3',
              category: 'Ops',
              title: 'Capability three',
              description: 'Sample capability description.',
              icon: <Network />,
            },
          ]}
        />
      </ShowcaseBlock>

      <ShowcaseBlock
        id="gallery"
        title="Screenshot Gallery"
        notes="Grid + carousel modes · modal preview · captions · placeholder media."
      >
        <Stack gap={8}>
          <ScreenshotGallery
            mode="grid"
            items={[
              {
                id: 'g1',
                caption: 'Console view',
                media: placeholder('Screenshot A'),
              },
              {
                id: 'g2',
                caption: 'Workflow view',
                media: placeholder('Screenshot B'),
              },
              {
                id: 'g3',
                caption: 'Insights view',
                media: placeholder('Screenshot C'),
              },
            ]}
          />
          <Caption>Carousel mode</Caption>
          <ScreenshotGallery
            mode="carousel"
            items={[
              { id: 'c1', caption: 'Slide one', media: placeholder('Slide 1') },
              { id: 'c2', caption: 'Slide two', media: placeholder('Slide 2') },
            ]}
          />
        </Stack>
      </ShowcaseBlock>

      <ShowcaseBlock
        id="architecture"
        title="Architecture Diagram Block"
        notes="Title · description · diagram Media Slot · caption · optional download CTA."
      >
        <ArchitectureDiagramBlock
          title="Reference architecture"
          description="Generic diagram block — not tied to any named product."
          diagram={placeholder('Architecture diagram slot')}
          caption="Sample caption for the diagram figure."
          download={{ label: 'Download diagram', href: '/resources/preview' }}
        />
      </ShowcaseBlock>

      <ShowcaseBlock
        id="specs"
        title="Technical Specification Block"
        notes="Auto table (md+) / stacked list (mobile) · cards layout variant."
      >
        <Stack gap={8}>
          <TechnicalSpecificationBlock
            layout="auto"
            categories={[
              {
                id: 's1',
                title: 'Runtime',
                rows: [
                  { id: 'r1', label: 'Deployment', value: 'Sample value' },
                  { id: 'r2', label: 'Regions', value: 'Sample value' },
                  { id: 'r3', label: 'SLA target', value: 'Sample value' },
                ],
              },
            ]}
          />
          <Caption>Cards layout</Caption>
          <TechnicalSpecificationBlock
            layout="cards"
            categories={[
              {
                id: 's2',
                title: 'Security',
                rows: [
                  { id: 'r4', label: 'Auth', value: 'Sample value' },
                  { id: 'r5', label: 'Encryption', value: 'Sample value' },
                ],
              },
              {
                id: 's3',
                title: 'Data',
                rows: [
                  { id: 'r6', label: 'Retention', value: 'Sample value' },
                  { id: 'r7', label: 'Export', value: 'Sample value' },
                ],
              },
            ]}
          />
        </Stack>
      </ShowcaseBlock>

      <ShowcaseBlock
        id="integrations"
        title="Integration Grid"
        notes="Logo placeholder · category · description · optional link."
      >
        <IntegrationGrid
          items={[
            {
              id: 'i1',
              name: 'Integration A',
              category: 'Identity',
              description: 'Sample integration description.',
              href: '/resources/preview',
            },
            {
              id: 'i2',
              name: 'Integration B',
              category: 'Data',
              description: 'Sample integration description.',
            },
            {
              id: 'i3',
              name: 'Integration C',
              category: 'Ops',
              description: 'Sample integration description.',
              href: '/company/preview',
            },
          ]}
        />
      </ShowcaseBlock>

      <ShowcaseBlock
        id="media-slots"
        title="Media Slot kinds"
        notes="Same ProductHero media prop can accept placeholder, code, or custom nodes."
      >
        <Stack gap={6}>
          <ProductHero
            name="Code media slot"
            description="Demonstrates kind: code without page coupling."
            media={{
              kind: 'code',
              code: 'export const product = { id: "example" };',
              language: 'TypeScript',
            }}
            reveal={false}
          />
          <ProductHero
            name="Custom media slot"
            description="kind: custom renders an arbitrary React node."
            media={{
              kind: 'custom',
              alt: 'Custom demo surface',
              node: (
                <div className="flex min-h-48 items-center justify-center p-6">
                  <Code>Interactive demo slot</Code>
                </div>
              ),
            }}
            reveal={false}
          />
        </Stack>
      </ShowcaseBlock>

      <Section spacing="lg">
        <Stack gap={2}>
          <Text size="small" weight="medium">
            Accessibility notes
          </Text>
          <Paragraph size="small" tone="muted">
            Product heroes use a single h1. Gallery dialogs trap focus and support Escape /
            arrow keys. Spec tables expose captions and row headers. Product nav announces
            the current section via aria-current. Prefer keyboard verification on gallery
            and sticky nav.
          </Paragraph>
        </Stack>
      </Section>
    </>
  );
}
