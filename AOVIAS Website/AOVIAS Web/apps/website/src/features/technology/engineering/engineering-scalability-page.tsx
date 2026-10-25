import { ScalabilityJourney } from '@/components/brand';
import {
  FeatureCard,
  FeatureGrid,
  Hero,
  MarketingCtaLink,
  SectionHeader,
} from '@/components/marketing';
import { Container, Stack } from '@/components/primitives';
import { engineeringScalabilityContent } from '@/features/technology/engineering/content';
import { PlatformSectionBand } from '@/features/technology/platform/section-band';

export function EngineeringScalabilityPage() {
  const c = engineeringScalabilityContent;

  return (
    <>
      <Hero
        atmosphere="infrastructure-mesh"
        eyebrow={c.hero.eyebrow}
        title={c.hero.title}
        description={c.hero.description}
        primaryCta={c.hero.primaryCta}
        secondaryCta={c.hero.secondaryCta}
        illustration={<ScalabilityJourney />}
      />

      <PlatformSectionBand
        id="scalability-topics"
        eyebrow="Growth"
        title="How capability scales with institutions"
        description="We describe organizational and architectural growth — not infrastructure capacity claims."
      >
        <FeatureGrid cols={2} stagger={false}>
          {c.topics.map((item) => (
            <FeatureCard
              key={item.title}
              title={item.title}
              description={item.description}
            />
          ))}
        </FeatureGrid>
      </PlatformSectionBand>

      <PlatformSectionBand
        id="scalability-journey"
        eyebrow="Journey"
        title="From local need to multi-product portfolio"
        muted
      >
        <div className="mx-auto max-w-2xl">
          <ScalabilityJourney />
        </div>
      </PlatformSectionBand>

      <section className="w-full py-14 md:py-16" aria-labelledby="scalability-next">
        <Container width="wide">
          <Stack gap={6} className="max-w-3xl">
            <div id="scalability-next">
              <SectionHeader
                eyebrow="Continue"
                heading="See architecture and observability"
                description="Scale rests on modular architecture and operational visibility."
              />
            </div>
            <Stack direction="horizontal" gap={3} wrap>
              <MarketingCtaLink
                label="Architecture"
                href="/technology/engineering/architecture"
                variant="primary"
              />
              <MarketingCtaLink
                label="Observability"
                href="/technology/engineering/observability"
                variant="outline"
              />
            </Stack>
          </Stack>
        </Container>
      </section>
    </>
  );
}
