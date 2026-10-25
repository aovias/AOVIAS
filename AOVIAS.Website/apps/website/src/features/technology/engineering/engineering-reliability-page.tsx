import { ReliabilityModel } from '@/components/brand';
import {
  FeatureCard,
  FeatureGrid,
  Hero,
  MarketingCtaLink,
  SectionHeader,
} from '@/components/marketing';
import { Container, Stack, Text } from '@/components/primitives';
import { engineeringReliabilityContent } from '@/features/technology/engineering/content';
import { PlatformSectionBand } from '@/features/technology/platform/section-band';

export function EngineeringReliabilityPage() {
  const c = engineeringReliabilityContent;

  return (
    <>
      <Hero
        atmosphere="blueprint-grid"
        eyebrow={c.hero.eyebrow}
        title={c.hero.title}
        description={c.hero.description}
        primaryCta={c.hero.primaryCta}
        secondaryCta={c.hero.secondaryCta}
        illustration={<ReliabilityModel />}
      />

      <section className="bg-muted/40 border-border w-full border-b py-8" aria-label="Editorial note">
        <Container width="wide">
          <Text as="p" size="small" tone="muted" className="max-w-3xl">
            {c.note}
          </Text>
        </Container>
      </section>

      <PlatformSectionBand
        id="reliability-topics"
        eyebrow="Engineering intent"
        title="What dependable software requires"
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
        id="reliability-model"
        eyebrow="Model"
        title="Layers of reliability intent"
        description="Monitoring, resilience, and predictable outcomes reinforce each other."
        muted
      >
        <div className="mx-auto max-w-2xl">
          <ReliabilityModel />
        </div>
      </PlatformSectionBand>

      <section className="w-full py-14 md:py-16" aria-labelledby="reliability-next">
        <Container width="wide">
          <Stack gap={6} className="max-w-3xl">
            <div id="reliability-next">
              <SectionHeader
                eyebrow="Visibility"
                heading="Observability makes reliability operable"
                description="You cannot steward what you cannot see."
              />
            </div>
            <MarketingCtaLink
              label="Observability"
              href="/technology/engineering/observability"
              variant="primary"
              size="lg"
            />
          </Stack>
        </Container>
      </section>
    </>
  );
}
