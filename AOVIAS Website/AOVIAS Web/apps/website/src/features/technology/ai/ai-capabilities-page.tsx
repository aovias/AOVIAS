import { PlatformIntelligence } from '@/components/brand';
import {
  FeatureCard,
  FeatureGrid,
  Hero,
  MarketingCtaLink,
  SectionHeader,
} from '@/components/marketing';
import { Container, Stack, Text } from '@/components/primitives';
import { aiCapabilitiesContent } from '@/features/technology/ai/content';
import { PlatformSectionBand } from '@/features/technology/platform/section-band';

export function AiCapabilitiesPage() {
  const c = aiCapabilitiesContent;

  return (
    <>
      <Hero
        atmosphere="ai-neural-network"
        eyebrow={c.hero.eyebrow}
        title={c.hero.title}
        description={c.hero.description}
        primaryCta={c.hero.primaryCta}
        secondaryCta={c.hero.secondaryCta}
        illustration={
          <PlatformIntelligence title="Assistance across products" />
        }
      />

      <PlatformSectionBand
        id="capabilities"
        eyebrow="Capability types"
        title="What assistance can look like"
        description={c.note}
      >
        <FeatureGrid cols={2} stagger={false}>
          {c.capabilities.map((item) => (
            <FeatureCard
              key={item.title}
              title={item.title}
              description={item.description}
            />
          ))}
        </FeatureGrid>
      </PlatformSectionBand>

      <section className="bg-muted/30 w-full py-14 md:py-16" aria-labelledby="capabilities-next">
        <Container width="wide">
          <Stack gap={6} className="max-w-3xl">
            <div id="capabilities-next">
              <SectionHeader
                eyebrow="Accountability"
                heading="Capabilities never remove human duty"
                description="Every assisted outcome still depends on people who review and own institutional decisions."
              />
            </div>
            <Text as="p" size="small" tone="muted">
              We do not promise autonomous decision-making for critical institutional work.
            </Text>
            <MarketingCtaLink
              label="Human-in-the-loop"
              href="/technology/ai/human-in-the-loop"
              variant="primary"
              size="lg"
            />
          </Stack>
        </Container>
      </section>
    </>
  );
}
