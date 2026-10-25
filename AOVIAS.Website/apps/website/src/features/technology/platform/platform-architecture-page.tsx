import { ArchitectureDiagram, PlatformOrchestration } from '@/components/brand';
import {
  FeatureGrid,
  Hero,
  MarketingCtaLink,
  PrincipleCard,
  SectionHeader,
} from '@/components/marketing';
import { Container, Stack } from '@/components/primitives';
import { platformArchitectureContent } from '@/features/technology/platform/content';
import { PlatformSectionBand } from '@/features/technology/platform/section-band';

export function PlatformArchitecturePage() {
  const c = platformArchitectureContent;

  return (
    <>
      <Hero
        atmosphere="architecture"
        eyebrow={c.hero.eyebrow}
        title={c.hero.title}
        description={c.hero.description}
        primaryCta={c.hero.primaryCta}
        secondaryCta={c.hero.secondaryCta}
        illustration={<ArchitectureDiagram title="Conceptual architecture layers" />}
      />

      <PlatformSectionBand
        id="architecture-principles"
        eyebrow="Philosophy"
        title="Principles that shape the platform"
        description="These ideas guide design decisions. They are not a bill of materials."
      >
        <FeatureGrid cols={3} stagger={false}>
          {c.principles.map((item) => (
            <PrincipleCard
              key={item.title}
              principle={item.title}
              description={item.description}
            />
          ))}
        </FeatureGrid>
      </PlatformSectionBand>

      <PlatformSectionBand
        id="architecture-visual"
        eyebrow="Visual model"
        title="Products orchestrated from a shared foundation"
        description="A conceptual view of how products sit on the platform and reach institutional domains."
        muted
      >
        <div className="mx-auto max-w-3xl">
          <PlatformOrchestration title="Platform orchestration (conceptual)" />
        </div>
      </PlatformSectionBand>

      <section className="w-full py-14 md:py-16" aria-labelledby="architecture-next">
        <Container width="wide">
          <Stack gap={6} className="max-w-3xl">
            <div id="architecture-next">
              <SectionHeader
                eyebrow="Continue in Technology"
                heading="Return to the platform or Technology home"
                description="Architecture is one pillar of Technology. Continue into Engineering, Trust, Integrations, or the Technology summary."
              />
            </div>
            <Stack direction="horizontal" gap={3} wrap>
              <MarketingCtaLink
                label="AOVIAS Platform"
                href="/technology/platform"
                variant="primary"
              />
              <MarketingCtaLink
                label="Why AOVIAS Technology"
                href="/technology/why"
                variant="outline"
              />
            </Stack>
          </Stack>
        </Container>
      </section>
    </>
  );
}
