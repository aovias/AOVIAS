import { ApiFirstPlatform } from '@/components/brand';
import {
  FeatureCard,
  FeatureGrid,
  Hero,
  MarketingCtaLink,
  SectionHeader,
} from '@/components/marketing';
import { Container, Stack, Text } from '@/components/primitives';
import { apisContent } from '@/features/technology/ecosystem/content';
import { PlatformSectionBand } from '@/features/technology/platform/section-band';

export function ApisPage() {
  const c = apisContent;

  return (
    <>
      <Hero
        atmosphere="infrastructure-mesh"
        eyebrow={c.hero.eyebrow}
        title={c.hero.title}
        description={c.hero.description}
        primaryCta={c.hero.primaryCta}
        secondaryCta={c.hero.secondaryCta}
        illustration={<ApiFirstPlatform />}
      />

      <section className="bg-muted/40 border-border w-full border-b py-8" aria-label="Future vision note">
        <Container width="wide">
          <Text as="p" size="small" tone="muted" className="max-w-3xl">
            {c.note}
          </Text>
        </Container>
      </section>

      <PlatformSectionBand
        id="api-topics"
        eyebrow="Philosophy"
        title="What API-first means for AOVIAS"
        description="No endpoint structures, protocols, or implementation specifics."
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
        id="api-visual"
        eyebrow="Model"
        title="A platform layer for composition"
        muted
      >
        <div className="mx-auto max-w-2xl">
          <ApiFirstPlatform />
        </div>
      </PlatformSectionBand>

      <section className="w-full py-14 md:py-16" aria-labelledby="apis-next">
        <Container width="wide">
          <Stack gap={6} className="max-w-3xl">
            <div id="apis-next">
              <SectionHeader
                eyebrow="Related"
                heading="Developer vision and integrations"
              />
            </div>
            <Stack direction="horizontal" gap={3} wrap>
              <MarketingCtaLink
                label="Developer platform"
                href="/technology/developer"
                variant="primary"
              />
              <MarketingCtaLink
                label="Integrations"
                href="/technology/integrations"
                variant="outline"
              />
            </Stack>
          </Stack>
        </Container>
      </section>
    </>
  );
}
