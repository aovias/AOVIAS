import { PlatformEcosystem } from '@/components/brand';
import {
  FeatureCard,
  FeatureGrid,
  Hero,
  MarketingCtaLink,
  SectionHeader,
} from '@/components/marketing';
import { Container, Stack } from '@/components/primitives';
import { platformEcosystemContent } from '@/features/technology/platform/content';
import { PlatformSectionBand } from '@/features/technology/platform/section-band';
import { PlatformStoryVisual } from '@/features/technology/platform/platform-story-visual';

export function PlatformEcosystemPage() {
  const c = platformEcosystemContent;

  return (
    <>
      <Hero
        atmosphere="platform-grid"
        eyebrow={c.hero.eyebrow}
        title={c.hero.title}
        description={c.hero.description}
        primaryCta={c.hero.primaryCta}
        secondaryCta={c.hero.secondaryCta}
        illustration={
          <PlatformEcosystem
            title="Platform to society"
            caption="A calm ladder from foundation to societal outcomes"
          />
        }
      />

      <PlatformSectionBand
        id="ecosystem-story"
        eyebrow="Signature story"
        title="From platform to stronger institutions"
        description="Scroll through the relationship chain. This is the AOVIAS platform narrative in one view."
        muted
      >
        <PlatformStoryVisual />
      </PlatformSectionBand>

      <PlatformSectionBand
        id="ecosystem-layers"
        eyebrow="Relationship layers"
        title="What each layer represents"
      >
        <FeatureGrid cols={2} stagger={false}>
          {c.layers.map((layer) => (
            <FeatureCard
              key={layer.title}
              title={layer.title}
              description={layer.description}
            />
          ))}
        </FeatureGrid>
      </PlatformSectionBand>

      <section className="w-full py-14 md:py-16" aria-labelledby="ecosystem-next">
        <Container width="wide">
          <Stack gap={6} className="max-w-3xl">
            <div id="ecosystem-next">
              <SectionHeader
                eyebrow="Architecture"
                heading="Understand the architectural philosophy"
                description="Next, review modular, secure, and AI-ready architecture principles — still conceptual, never infrastructure-specific."
              />
            </div>
            <MarketingCtaLink
              label="Platform architecture overview"
              href="/technology/architecture"
              variant="primary"
              size="lg"
            />
          </Stack>
        </Container>
      </section>
    </>
  );
}
