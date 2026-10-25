import { DataProtectionLifecycle } from '@/components/brand';
import {
  FeatureCard,
  FeatureGrid,
  Hero,
  MarketingCtaLink,
  SectionHeader,
} from '@/components/marketing';
import { Container, Stack, Text } from '@/components/primitives';
import {
  TRUST_RECURRING_THEME,
  trustDataProtectionContent,
} from '@/features/technology/trust/content';
import { PlatformSectionBand } from '@/features/technology/platform/section-band';

export function TrustDataProtectionPage() {
  const c = trustDataProtectionContent;

  return (
    <>
      <Hero
        atmosphere="security-geometry"
        eyebrow={c.hero.eyebrow}
        title={c.hero.title}
        description={c.hero.description}
        primaryCta={c.hero.primaryCta}
        secondaryCta={c.hero.secondaryCta}
        illustration={<DataProtectionLifecycle />}
      />

      <section className="bg-muted/40 border-border w-full border-b py-8" aria-label="Guiding statement">
        <Container width="wide">
          <Text as="p" size="body-lg" weight="medium" className="max-w-3xl">
            {TRUST_RECURRING_THEME}
          </Text>
        </Container>
      </section>

      <PlatformSectionBand
        id="data-protection-topics"
        eyebrow="Principles"
        title="Engineering intent for protecting information"
        description="Focus on principles — not cryptographic catalogues or infrastructure claims."
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
        id="lifecycle"
        eyebrow="Lifecycle"
        title="From collection to retirement"
        muted
      >
        <div className="mx-auto max-w-2xl">
          <DataProtectionLifecycle />
        </div>
      </PlatformSectionBand>

      <section className="w-full py-14 md:py-16" aria-labelledby="data-protection-next">
        <Container width="wide">
          <Stack gap={6} className="max-w-3xl">
            <div id="data-protection-next">
              <SectionHeader
                eyebrow="Continue"
                heading="Return to trust or privacy philosophy"
                description="Data protection sits within the broader Security & Trust story."
              />
            </div>
            <Stack direction="horizontal" gap={3} wrap>
              <MarketingCtaLink label="Security" href="/technology/trust" variant="primary" />
              <MarketingCtaLink
                label="Privacy & trust"
                href="/technology/trust/privacy"
                variant="outline"
              />
            </Stack>
          </Stack>
        </Container>
      </section>
    </>
  );
}
