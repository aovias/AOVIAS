import { DeveloperEnablement } from '@/components/brand';
import {
  FeatureCard,
  FeatureGrid,
  Hero,
  MarketingCtaLink,
  SectionHeader,
} from '@/components/marketing';
import { Container, Stack, Text } from '@/components/primitives';
import { developerContent } from '@/features/technology/ecosystem/content';
import { PlatformSectionBand } from '@/features/technology/platform/section-band';

export function DeveloperPlatformPage() {
  const c = developerContent;

  return (
    <>
      <Hero
        atmosphere="infrastructure-mesh"
        eyebrow={c.hero.eyebrow}
        title={c.hero.title}
        description={c.hero.description}
        primaryCta={c.hero.primaryCta}
        secondaryCta={c.hero.secondaryCta}
        illustration={<DeveloperEnablement />}
      />

      <section className="bg-muted/40 border-border w-full border-b py-8" aria-label="Future vision note">
        <Container width="wide">
          <Text as="p" size="small" tone="muted" className="max-w-3xl">
            {c.note}
          </Text>
        </Container>
      </section>

      <PlatformSectionBand
        id="developer-topics"
        eyebrow="Direction"
        title="Themes of developer enablement"
        description="Strategic direction — not a currently available product catalogue."
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
        id="developer-visual"
        eyebrow="Model"
        title="Builders composing platform capabilities"
        muted
      >
        <div className="mx-auto max-w-2xl">
          <DeveloperEnablement />
        </div>
      </PlatformSectionBand>

      <section className="w-full py-14 md:py-16" aria-labelledby="developer-next">
        <Container width="wide">
          <Stack gap={6} className="max-w-3xl">
            <div id="developer-next">
              <SectionHeader
                eyebrow="Continue"
                heading="See where this sits on the roadmap"
              />
            </div>
            <MarketingCtaLink
              label="Technology roadmap"
              href="/technology/roadmap"
              variant="primary"
              size="lg"
            />
          </Stack>
        </Container>
      </section>
    </>
  );
}
