import { PlatformIntelligence } from '@/components/brand';
import {
  FeatureGrid,
  Hero,
  MarketingCtaLink,
  PrincipleCard,
  SectionHeader,
} from '@/components/marketing';
import { Container, Stack, Text } from '@/components/primitives';
import { AI_GUIDING_LINE, aiPrinciplesContent } from '@/features/technology/ai/content';
import { PlatformSectionBand } from '@/features/technology/platform/section-band';

export function AiPrinciplesPage() {
  const c = aiPrinciplesContent;

  return (
    <>
      <Hero
        eyebrow={c.hero.eyebrow}
        title={c.hero.title}
        description={c.hero.description}
        primaryCta={c.hero.primaryCta}
        secondaryCta={c.hero.secondaryCta}
        atmosphere="ai-neural-network"
        illustration={
          <PlatformIntelligence title="AI principles — assistive, accountable" />
        }
      />

      <section className="bg-muted/40 border-border w-full border-b py-8" aria-label="Guiding statement">
        <Container width="wide">
          <Text as="p" size="body-lg" weight="medium" className="max-w-3xl">
            {AI_GUIDING_LINE}
          </Text>
        </Container>
      </section>

      <PlatformSectionBand
        id="principles"
        eyebrow="Commitments"
        title="Principles that endure"
        description="Written for leaders who must trust technology under public and regulatory scrutiny."
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

      <section className="bg-muted/30 w-full py-14 md:py-16" aria-labelledby="principles-next">
        <Container width="wide">
          <Stack gap={6} className="max-w-3xl">
            <div id="principles-next">
              <SectionHeader
                eyebrow="Next"
                heading="See how principles become collaboration"
                description="Human-in-the-loop makes accountability operational."
              />
            </div>
            <Stack direction="horizontal" gap={3} wrap>
              <MarketingCtaLink
                label="Human-in-the-loop"
                href="/technology/ai/human-in-the-loop"
                variant="primary"
              />
              <MarketingCtaLink
                label="Responsible AI"
                href="/technology/ai/responsible"
                variant="outline"
              />
            </Stack>
          </Stack>
        </Container>
      </section>
    </>
  );
}
