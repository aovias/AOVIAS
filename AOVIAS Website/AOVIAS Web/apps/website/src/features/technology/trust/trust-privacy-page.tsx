import { GovernanceWorkflow } from '@/components/brand';
import {
  FeatureCard,
  FeatureGrid,
  Hero,
  MarketingCtaLink,
  SectionHeader,
} from '@/components/marketing';
import { Container, Stack, Text } from '@/components/primitives';
import { TRUST_RECURRING_THEME, trustPrivacyContent } from '@/features/technology/trust/content';
import { PlatformSectionBand } from '@/features/technology/platform/section-band';

export function TrustPrivacyPage() {
  const c = trustPrivacyContent;

  return (
    <>
      <Hero
        eyebrow={c.hero.eyebrow}
        title={c.hero.title}
        description={c.hero.description}
        primaryCta={c.hero.primaryCta}
        secondaryCta={c.hero.secondaryCta}
        atmosphere="trust-geometry"
        illustration={
          <GovernanceWorkflow title="Privacy — policy, oversight, accountability" />
        }
      />

      <section className="bg-muted/40 border-border w-full border-b py-8" aria-label="Page distinction">
        <Container width="wide">
          <Stack gap={3} className="max-w-3xl">
            <Text as="p" size="body-lg" weight="medium">
              {TRUST_RECURRING_THEME}
            </Text>
            <Text as="p" size="small" tone="muted">
              {c.distinction}
            </Text>
          </Stack>
        </Container>
      </section>

      <PlatformSectionBand
        id="privacy-topics"
        eyebrow="Stewardship"
        title="How we approach privacy and trust"
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
        id="governance"
        eyebrow="Governance"
        title="Oversight with human accountability"
        muted
      >
        <div className="mx-auto max-w-2xl">
          <GovernanceWorkflow />
        </div>
      </PlatformSectionBand>

      <section className="w-full py-14 md:py-16" aria-labelledby="privacy-next">
        <Container width="wide">
          <Stack gap={6} className="max-w-3xl">
            <div id="privacy-next">
              <SectionHeader
                eyebrow="Next"
                heading="Data protection principles"
                description="Engineering intent for how information is minimized, protected, and retired."
              />
            </div>
            <MarketingCtaLink
              label="Data protection"
              href="/technology/trust/data-protection"
              variant="primary"
              size="lg"
            />
          </Stack>
        </Container>
      </section>
    </>
  );
}
