import { TechnologyEvolution } from '@/components/brand';
import {
  FeatureCard,
  FeatureGrid,
  Hero,
  MarketingCtaLink,
  SectionHeader,
} from '@/components/marketing';
import { Container, Stack, Text } from '@/components/primitives';
import { roadmapContent } from '@/features/technology/ecosystem/content';
import { PlatformSectionBand } from '@/features/technology/platform/section-band';

export function TechnologyRoadmapPage() {
  const c = roadmapContent;

  return (
    <>
      <Hero
        atmosphere="technology-atmosphere"
        eyebrow={c.hero.eyebrow}
        title={c.hero.title}
        description={c.hero.description}
        primaryCta={c.hero.primaryCta}
        secondaryCta={c.hero.secondaryCta}
        illustration={<TechnologyEvolution />}
      />

      <section className="bg-muted/40 border-border w-full border-b py-8" aria-label="Roadmap note">
        <Container width="wide">
          <Text as="p" size="small" tone="muted" className="max-w-3xl">
            {c.note}
          </Text>
        </Container>
      </section>

      <PlatformSectionBand
        id="roadmap-stages"
        eyebrow="Capability themes"
        title="How the platform progresses"
        description="Themes of maturity — not a release calendar."
      >
        <FeatureGrid cols={2} stagger={false}>
          {c.stages.map((item) => (
            <FeatureCard
              key={item.title}
              title={item.title}
              description={item.description}
            />
          ))}
        </FeatureGrid>
      </PlatformSectionBand>

      <PlatformSectionBand
        id="roadmap-diagram"
        eyebrow="Progression"
        title="From foundation to continuous innovation"
        muted
      >
        <div className="mx-auto max-w-2xl">
          <TechnologyEvolution />
        </div>
      </PlatformSectionBand>

      <section className="w-full py-14 md:py-16" aria-labelledby="roadmap-next">
        <Container width="wide">
          <Stack gap={6} className="max-w-3xl">
            <div id="roadmap-next">
              <SectionHeader
                eyebrow="Conclusion"
                heading="Bring the Technology story together"
                description="Why AOVIAS Technology summarizes the pillar for executive decision makers."
              />
            </div>
            <MarketingCtaLink
              label="Why AOVIAS Technology"
              href="/technology/why"
              variant="primary"
              size="lg"
            />
          </Stack>
        </Container>
      </section>
    </>
  );
}
