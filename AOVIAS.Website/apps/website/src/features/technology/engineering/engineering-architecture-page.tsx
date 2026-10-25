import { ArchitectureDiagram, SystemEvolution } from '@/components/brand';
import {
  FeatureCard,
  FeatureGrid,
  Hero,
  MarketingCtaLink,
  SectionHeader,
} from '@/components/marketing';
import { Container, Stack, Text } from '@/components/primitives';
import { ENGINEERING_GUIDING_LINE, engineeringArchitectureContent } from '@/features/technology/engineering/content';
import { PlatformSectionBand } from '@/features/technology/platform/section-band';

export function EngineeringArchitecturePage() {
  const c = engineeringArchitectureContent;

  return (
    <>
      <Hero
        atmosphere="architecture"
        eyebrow={c.hero.eyebrow}
        title={c.hero.title}
        description={c.hero.description}
        primaryCta={c.hero.primaryCta}
        secondaryCta={c.hero.secondaryCta}
        illustration={
          <ArchitectureDiagram title="Conceptual architecture layers" />
        }
      />

      <section className="bg-muted/40 border-border w-full border-b py-8" aria-label="Guiding statement">
        <Container width="wide">
          <Text as="p" size="body-lg" weight="medium" className="max-w-3xl">
            {ENGINEERING_GUIDING_LINE}
          </Text>
        </Container>
      </section>

      <PlatformSectionBand
        id="architecture-topics"
        eyebrow="Philosophy"
        title="How we structure systems"
        description="Conceptual commitments — not a bill of materials or internal topology."
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
        id="system-evolution"
        eyebrow="Evolution"
        title="Grow capability without rewriting the foundation"
        description="Modules compose on a stable core so institutions gain new capability without operational upheaval."
        muted
      >
        <div className="mx-auto max-w-2xl">
          <SystemEvolution />
        </div>
      </PlatformSectionBand>

      <section className="w-full py-14 md:py-16" aria-labelledby="eng-arch-next">
        <Container width="wide">
          <Stack gap={6} className="max-w-3xl">
            <div id="eng-arch-next">
              <SectionHeader
                eyebrow="Related"
                heading="Principles and platform shape"
                description="Design principles guide daily decisions. Platform Architecture describes the shared foundation story."
              />
            </div>
            <Stack direction="horizontal" gap={3} wrap>
              <MarketingCtaLink
                label="Design principles"
                href="/technology/engineering/design-principles"
                variant="primary"
              />
              <MarketingCtaLink
                label="Platform architecture"
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
