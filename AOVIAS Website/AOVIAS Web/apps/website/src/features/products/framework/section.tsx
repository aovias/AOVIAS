import { CtaSection, FeatureGrid, PrincipleCard, SectionHeader } from '@/components/marketing';
import { Container, Paragraph, Stack } from '@/components/primitives';

const frameworkSections = [
  {
    id: 'hero',
    principle: 'Hero',
    description: 'Product identity, purpose, CTAs, and primary media.',
  },
  {
    id: 'overview',
    principle: 'Overview',
    description: 'Narrative context, outcomes, and optional metrics.',
  },
  {
    id: 'capabilities',
    principle: 'Capabilities',
    description: 'Practical strengths organized for enterprise evaluation.',
  },
  {
    id: 'integrations',
    principle: 'Integrations',
    description: 'Governed connections to institutional systems.',
  },
  {
    id: 'architecture',
    principle: 'Architecture',
    description: 'How the product fits within a coherent platform.',
  },
  {
    id: 'specifications',
    principle: 'Technical specifications',
    description: 'Clear categories for security, deployment, and operations.',
  },
  {
    id: 'pricing',
    principle: 'Pricing entry',
    description: 'A measured path into packaging conversations.',
  },
  {
    id: 'demo',
    principle: 'Demo CTA',
    description: 'A calm invitation to see the product in context.',
  },
] as const;

/**
 * Product Landing Framework summary — documents the inherited template.
 * Full composition lives in `ProductLanding` (see internal framework preview).
 */
export function ProductFrameworkSection() {
  return (
    <section
      id="framework"
      className="w-full py-12 lg:py-16"
      aria-label="Product landing framework"
    >
      <Container width="wide">
        <Stack gap={10}>
          <Stack gap={4} className="max-w-3xl">
            <SectionHeader
              eyebrow="Landing framework"
              heading="The standard for every AOVIAS product page"
              description="Future product pages inherit one reusable composition. They supply configuration — not a new layout."
            />
            <Paragraph tone="muted">
              The framework combines Product, Marketing, and conversion building blocks into a
              consistent enterprise landing experience: Hero, Overview, Capabilities,
              Integrations, Architecture, Specifications, Pricing entry, and Demo CTA — with
              sticky product navigation throughout.
            </Paragraph>
          </Stack>
          <FeatureGrid cols={2} stagger>
            {frameworkSections.map((item) => (
              <PrincipleCard
                key={item.id}
                principle={item.principle}
                description={item.description}
              />
            ))}
          </FeatureGrid>
        </Stack>
      </Container>

      <div className="mt-10">
        <CtaSection
          title="Ready to explore the portfolio?"
          description="Start with Command or Studio — or follow the roadmap as the ecosystem expands."
          primaryCta={{ label: 'Browse catalog', href: '#catalog' }}
          secondaryCta={{ label: 'Company', href: '/company' }}
          background="muted"
        />
      </div>
    </section>
  );
}
