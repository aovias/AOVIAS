import { TechnologyEvolution } from '@/components/brand';
import {
  FeatureCard,
  FeatureGrid,
  Hero,
  MarketingCtaLink,
  SectionHeader,
} from '@/components/marketing';
import { Container, Stack, Text } from '@/components/primitives';
import { aiRoadmapContent } from '@/features/technology/ai/content';
import { PlatformSectionBand } from '@/features/technology/platform/section-band';

export function AiRoadmapPage() {
  const c = aiRoadmapContent;

  return (
    <>
      <Hero
        eyebrow={c.hero.eyebrow}
        title={c.hero.title}
        description={c.hero.description}
        primaryCta={c.hero.primaryCta}
        secondaryCta={c.hero.secondaryCta}
        atmosphere="ai-neural-network"
        illustration={
          <TechnologyEvolution title="AI capability themes — measured progress" />
        }
      />

      <section className="bg-muted/40 border-border w-full border-b py-8" aria-label="Guiding statement">
        <Container width="wide">
          <Text as="p" size="body-lg" weight="medium" className="max-w-3xl">
            {c.guidingLine}
          </Text>
        </Container>
      </section>

      <PlatformSectionBand
        id="roadmap-themes"
        eyebrow="Themes"
        title="How capability may mature"
        description="These themes describe direction. They are not a release calendar and do not invent product commitments."
      >
        <FeatureGrid cols={2} stagger={false}>
          {c.themes.map((item) => (
            <FeatureCard
              key={item.title}
              title={item.title}
              description={item.description}
            />
          ))}
        </FeatureGrid>
      </PlatformSectionBand>

      <section className="w-full py-14 md:py-16" aria-labelledby="roadmap-next">
        <Container width="wide">
          <Stack gap={6} className="max-w-3xl">
            <div id="roadmap-next">
              <SectionHeader
                eyebrow="Continue"
                heading="Return to AI or the Platform"
                description="Artificial Intelligence remains a capability of the AOVIAS Platform — integrated, governed, and accountable."
              />
            </div>
            <Stack direction="horizontal" gap={3} wrap>
              <MarketingCtaLink label="AI at AOVIAS" href="/technology/ai" variant="primary" />
              <MarketingCtaLink
                label="AOVIAS Platform"
                href="/technology/platform"
                variant="outline"
              />
            </Stack>
          </Stack>
        </Container>
      </section>
    </>
  );
}
