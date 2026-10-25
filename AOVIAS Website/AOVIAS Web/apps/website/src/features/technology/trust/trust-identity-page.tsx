import { IdentityPlatformVisual } from '@/components/brand';
import {
  FeatureCard,
  FeatureGrid,
  Hero,
  MarketingCtaLink,
  SectionHeader,
} from '@/components/marketing';
import { Container, Stack, Text } from '@/components/primitives';
import { TRUST_RECURRING_THEME, trustIdentityContent } from '@/features/technology/trust/content';
import { PlatformSectionBand } from '@/features/technology/platform/section-band';

export function TrustIdentityPage() {
  const c = trustIdentityContent;

  return (
    <>
      <Hero
        atmosphere="trust-geometry"
        eyebrow={c.hero.eyebrow}
        title={c.hero.title}
        description={c.hero.description}
        primaryCta={c.hero.primaryCta}
        secondaryCta={c.hero.secondaryCta}
        illustration={<IdentityPlatformVisual />}
      />

      <section className="bg-muted/40 border-border w-full border-b py-8" aria-label="Guiding statement">
        <Container width="wide">
          <Text as="p" size="body-lg" weight="medium" className="max-w-3xl">
            {TRUST_RECURRING_THEME}
          </Text>
        </Container>
      </section>

      <PlatformSectionBand
        id="identity-topics"
        eyebrow="Capabilities"
        title="What identity provides — conceptually"
        description="No protocols, vendors, or implementation specifics — only the role identity plays on the platform."
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
        id="identity-visual"
        eyebrow="Model"
        title="One identity service, many products"
        muted
      >
        <div className="mx-auto max-w-2xl">
          <IdentityPlatformVisual />
        </div>
      </PlatformSectionBand>

      <section className="w-full py-14 md:py-16" aria-labelledby="identity-next">
        <Container width="wide">
          <Stack gap={6} className="max-w-3xl">
            <div id="identity-next">
              <SectionHeader
                eyebrow="Continue"
                heading="Security and compliance readiness"
                description="Identity underpins secure access and supports institutional governance."
              />
            </div>
            <Stack direction="horizontal" gap={3} wrap>
              <MarketingCtaLink label="Security" href="/technology/trust" variant="primary" />
              <MarketingCtaLink
                label="Compliance readiness"
                href="/technology/trust/compliance"
                variant="outline"
              />
            </Stack>
          </Stack>
        </Container>
      </section>
    </>
  );
}
