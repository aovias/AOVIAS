import { PlatformOrchestration } from '@/components/brand';
import {
  FeatureCard,
  FeatureGrid,
  Hero,
  MarketingCtaLink,
  SectionHeader,
} from '@/components/marketing';
import { Container, Stack } from '@/components/primitives';
import { platformOverviewContent } from '@/features/technology/platform/content';
import { PlatformSectionBand } from '@/features/technology/platform/section-band';

export function PlatformOverviewPage() {
  const c = platformOverviewContent;

  return (
    <>
      <Hero
        atmosphere="platform-grid"
        eyebrow={c.hero.eyebrow}
        title={c.hero.title}
        description={c.hero.description}
        primaryCta={c.hero.primaryCta}
        secondaryCta={c.hero.secondaryCta}
        illustration={<PlatformOrchestration title="Products on one platform" />}
      />

      <PlatformSectionBand
        id="overview-topics"
        eyebrow="Platform concept"
        title="How the platform model works"
        description="Six facets of the platform-first approach — written for technology and institutional leaders."
      >
        <FeatureGrid cols={2} stagger={false}>
          {c.topics.map((topic) => (
            <FeatureCard
              key={topic.title}
              title={topic.title}
              description={topic.description}
            />
          ))}
        </FeatureGrid>
      </PlatformSectionBand>

      <section className="bg-muted/30 w-full py-14 md:py-16" aria-labelledby="overview-next">
        <Container width="wide">
          <Stack gap={6} className="max-w-3xl">
            <div id="overview-next">
              <SectionHeader
                eyebrow="Next"
                heading="From concept to services and ecosystem"
                description="Continue with shared service responsibilities or the full ecosystem relationship map."
              />
            </div>
            <Stack direction="horizontal" gap={3} wrap>
              <MarketingCtaLink
                label="Platform services"
                href="/technology/platform/services"
                variant="primary"
              />
              <MarketingCtaLink
                label="Platform ecosystem"
                href="/technology/platform/ecosystem"
                variant="outline"
              />
            </Stack>
          </Stack>
        </Container>
      </section>
    </>
  );
}
