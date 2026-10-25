import { SystemEvolution } from '@/components/brand';
import {
  FeatureCard,
  FeatureGrid,
  Hero,
  MarketingCtaLink,
  SectionHeader,
} from '@/components/marketing';
import { Container, Stack, Text } from '@/components/primitives';
import { whyTechnologyContent } from '@/features/technology/ecosystem/content';
import { PlatformSectionBand } from '@/features/technology/platform/section-band';

export function WhyTechnologyPage() {
  const c = whyTechnologyContent;

  return (
    <>
      <Hero
        eyebrow={c.hero.eyebrow}
        title={c.hero.title}
        description={c.hero.description}
        primaryCta={c.hero.primaryCta}
        secondaryCta={c.hero.secondaryCta}
        atmosphere="technology-atmosphere"
        illustration={
          <SystemEvolution title="Why AOVIAS Technology — durable platform choice" />
        }
      />

      <section className="bg-muted/40 border-border w-full border-b py-8" aria-label="Executive summary">
        <Container width="wide">
          <Text as="p" size="body-lg" weight="medium" className="max-w-3xl">
            Choose AOVIAS when you need a dependable enterprise platform partner —
            not a collection of disconnected tools.
          </Text>
        </Container>
      </section>

      <PlatformSectionBand
        id="technology-themes"
        eyebrow="The Technology pillar"
        title="Six themes that reinforce one another"
        description="Each theme is explored in depth across the Technology section."
      >
        <FeatureGrid cols={2} stagger={false}>
          {c.themes.map((item) => (
            <FeatureCard
              key={item.href}
              title={item.title}
              description={item.description}
              href={item.href}
              linkLabel="Explore"
            />
          ))}
        </FeatureGrid>
      </PlatformSectionBand>

      <section className="bg-muted/30 w-full py-14 md:py-16" aria-labelledby="why-next">
        <Container width="wide">
          <Stack gap={6} className="max-w-3xl">
            <div id="why-next">
              <SectionHeader
                eyebrow="Next steps"
                heading="Continue into products or conversation"
                description="See how the platform becomes products — or speak with the AOVIAS team."
              />
            </div>
            <Stack direction="horizontal" gap={3} wrap>
              <MarketingCtaLink label="Products" href="/products" variant="primary" />
              <MarketingCtaLink
                label="Technology overview"
                href="/technology"
                variant="outline"
              />
              <MarketingCtaLink
                label="Contact"
                href="/company/contact"
                variant="outline"
              />
            </Stack>
          </Stack>
        </Container>
      </section>
    </>
  );
}
