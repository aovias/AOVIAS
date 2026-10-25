import { PlatformEcosystem } from '@/components/brand';
import {
  CtaSection,
  FeatureCard,
  FeatureGrid,
  Hero,
  SectionHeader,
} from '@/components/marketing';
import { Container, Stack } from '@/components/primitives';
import { TrustBadge } from '@/components/trust';
import type { TrustSignal } from '@/components/trust';
import {
  getAvailableIndustries,
  getPlannedIndustries,
} from '@/features/industries/domain';

const availableSignal: TrustSignal = {
  id: 'available',
  label: 'Available',
  description: 'Current industry experience',
  variant: 'quality',
};

const visionSignal: TrustSignal = {
  id: 'vision',
  label: 'Vision',
  description: 'Future industry direction',
  variant: 'default',
};

/**
 * Solutions overview — entry point to industry pages.
 */
export function SolutionsOverviewPage() {
  const available = getAvailableIndustries();
  const planned = getPlannedIndustries();

  return (
    <>
      <Hero
        eyebrow="Solutions"
        title="One platform. Distinct institutional realities."
        description="AOVIAS adapts Command, Studio, and Platform to healthcare, government, manufacturing, and financial services — shared architecture with sector-specific operational understanding."
        primaryCta={{ label: 'Explore healthcare', href: '/solutions/healthcare' }}
        secondaryCta={{ label: 'View products', href: '/products' }}
        atmosphere="enterprise-abstract"
        illustration={
          <PlatformEcosystem
            title="AOVIAS industry solutions"
            caption="One platform adapted to institutional sectors"
          />
        }
      />

      <section className="bg-background w-full py-12 lg:py-16" aria-label="Current industries">
        <Container width="wide">
          <Stack gap={10}>
            <SectionHeader
              eyebrow="Industries"
              heading="Sectors we serve today"
              description="Each industry page demonstrates institutional understanding — challenges, workflows, and how the shared portfolio supports that environment."
            />
            <FeatureGrid cols={2} stagger>
              {available.map((industry) => (
                <div key={industry.id} className="flex h-full flex-col gap-3">
                  <TrustBadge signal={availableSignal} size="sm" />
                  <FeatureCard
                    title={industry.name}
                    description={industry.summary}
                    href={industry.href}
                    linkLabel={`Explore ${industry.name}`}
                  />
                </div>
              ))}
            </FeatureGrid>
          </Stack>
        </Container>
      </section>

      <section
        className="border-border bg-muted/30 w-full border-y py-12 lg:py-16"
        aria-label="Future industries"
      >
        <Container width="wide">
          <Stack gap={10}>
            <SectionHeader
              eyebrow="Roadmap"
              heading="Future industries"
              description="Additional sectors are described as planned direction — with distinct framing per market, not a committed schedule."
            />
            <FeatureGrid cols={3} stagger>
              {planned.map((industry) => (
                <div key={industry.id} className="flex h-full flex-col gap-3">
                  <TrustBadge signal={visionSignal} size="sm" />
                  <FeatureCard
                    title={industry.name}
                    description={industry.summary}
                    href={industry.href}
                    linkLabel={`View ${industry.name} vision`}
                  />
                </div>
              ))}
            </FeatureGrid>
          </Stack>
        </Container>
      </section>

      <CtaSection
        title="Find the right industry path"
        description="Start with a sector page, or explore the product portfolio that underpins every industry experience."
        primaryCta={{ label: 'Request a demo', href: '/company/contact#form-demo' }}
        secondaryCta={{ label: 'Explore products', href: '/products' }}
        background="dark"
      />
    </>
  );
}
