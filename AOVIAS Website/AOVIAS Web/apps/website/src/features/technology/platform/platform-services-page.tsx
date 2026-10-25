import { ArchitectureDiagram } from '@/components/brand';
import {
  FeatureCard,
  FeatureGrid,
  Hero,
  MarketingCtaLink,
  SectionHeader,
} from '@/components/marketing';
import { Container, Stack, Text } from '@/components/primitives';
import { platformServicesContent } from '@/features/technology/platform/content';
import { PlatformSectionBand } from '@/features/technology/platform/section-band';

export function PlatformServicesPage() {
  const c = platformServicesContent;

  return (
    <>
      <Hero
        atmosphere="platform-grid"
        eyebrow={c.hero.eyebrow}
        title={c.hero.title}
        description={c.hero.description}
        primaryCta={c.hero.primaryCta}
        secondaryCta={c.hero.secondaryCta}
        illustration={<ArchitectureDiagram title="Services under the experience layer" />}
      />

      <PlatformSectionBand
        id="services-matrix"
        eyebrow="Service catalog"
        title="Conceptual platform services"
        description="Each entry describes a responsibility. Implementation topology remains internal."
      >
        <FeatureGrid cols={2} stagger={false}>
          {c.services.map((service) => {
            const isFuture = 'future' in service && service.future;
            return (
              <FeatureCard
                key={service.id}
                title={isFuture ? `${service.title} (Future)` : service.title}
                description={service.description}
              />
            );
          })}
        </FeatureGrid>
        <Text as="p" size="small" tone="muted">
          Commerce is registered as a future platform service so commercial packaging can
          attach to the same foundation when ready.
        </Text>
      </PlatformSectionBand>

      <section className="bg-muted/30 w-full py-14 md:py-16" aria-labelledby="services-next">
        <Container width="wide">
          <Stack gap={6} className="max-w-3xl">
            <div id="services-next">
              <SectionHeader
                eyebrow="Continue"
                heading="See how services fit the ecosystem"
                description="The ecosystem page shows how services and products relate to institutions and outcomes."
              />
            </div>
            <Stack direction="horizontal" gap={3} wrap>
              <MarketingCtaLink
                label="Platform ecosystem"
                href="/technology/platform/ecosystem"
                variant="primary"
              />
              <MarketingCtaLink
                label="Architecture overview"
                href="/technology/architecture"
                variant="outline"
              />
            </Stack>
          </Stack>
        </Container>
      </section>
    </>
  );
}
