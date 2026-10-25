import { ObservabilityLoop } from '@/components/brand';
import {
  FeatureCard,
  FeatureGrid,
  Hero,
  MarketingCtaLink,
  SectionHeader,
} from '@/components/marketing';
import { Container, Stack, Text } from '@/components/primitives';
import {
  ENGINEERING_GUIDING_LINE,
  engineeringObservabilityContent,
} from '@/features/technology/engineering/content';
import { PlatformSectionBand } from '@/features/technology/platform/section-band';

export function EngineeringObservabilityPage() {
  const c = engineeringObservabilityContent;

  return (
    <>
      <Hero
        atmosphere="infrastructure-mesh"
        eyebrow={c.hero.eyebrow}
        title={c.hero.title}
        description={c.hero.description}
        primaryCta={c.hero.primaryCta}
        secondaryCta={c.hero.secondaryCta}
        illustration={<ObservabilityLoop />}
      />

      <section className="bg-muted/40 border-border w-full border-b py-8" aria-label="Guiding statement">
        <Container width="wide">
          <Text as="p" size="body-lg" weight="medium" className="max-w-3xl">
            {ENGINEERING_GUIDING_LINE}
          </Text>
        </Container>
      </section>

      <PlatformSectionBand
        id="observability-topics"
        eyebrow="Capabilities"
        title="What visibility includes"
        description="Observability is presented as operational capability — not a vendor tool list."
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
        id="observability-loop"
        eyebrow="Feedback loop"
        title="Monitor, diagnose, learn, improve"
        muted
      >
        <div className="mx-auto max-w-2xl">
          <ObservabilityLoop />
        </div>
      </PlatformSectionBand>

      <section className="w-full py-14 md:py-16" aria-labelledby="observability-next">
        <Container width="wide">
          <Stack gap={6} className="max-w-3xl">
            <div id="observability-next">
              <SectionHeader
                eyebrow="Continue"
                heading="Return to engineering or reliability"
                description="Observability closes the loop on dependable institutional software."
              />
            </div>
            <Stack direction="horizontal" gap={3} wrap>
              <MarketingCtaLink
                label="Engineering philosophy"
                href="/technology/engineering"
                variant="primary"
              />
              <MarketingCtaLink
                label="Reliability"
                href="/technology/engineering/reliability"
                variant="outline"
              />
            </Stack>
          </Stack>
        </Container>
      </section>
    </>
  );
}
