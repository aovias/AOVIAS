import { HumanAiCollaboration } from '@/components/brand';
import {
  FeatureCard,
  FeatureGrid,
  Hero,
  MarketingCtaLink,
  SectionHeader,
} from '@/components/marketing';
import { Container, Stack, Text } from '@/components/primitives';
import { aiHitlContent } from '@/features/technology/ai/content';
import { PlatformSectionBand } from '@/features/technology/platform/section-band';

export function AiHitlPage() {
  const c = aiHitlContent;

  return (
    <>
      <Hero
        atmosphere="ai-neural-network"
        eyebrow={c.hero.eyebrow}
        title={c.hero.title}
        description={c.hero.description}
        primaryCta={c.hero.primaryCta}
        secondaryCta={c.hero.secondaryCta}
        illustration={<HumanAiCollaboration />}
      />

      <section className="bg-muted/40 border-border w-full border-b py-8" aria-label="Guiding statement">
        <Container width="wide">
          <Text as="p" size="body-lg" weight="medium" className="max-w-3xl">
            {c.guidingLine}
          </Text>
        </Container>
      </section>

      <PlatformSectionBand
        id="collaboration-model"
        eyebrow="Collaboration model"
        title="Four steps that keep people in charge"
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
        id="workflow-diagram"
        eyebrow="Workflow"
        title="Conceptual collaboration diagram"
        description="Operators and platform intelligence work together. Approval and accountability stay human."
        muted
      >
        <div className="mx-auto max-w-2xl">
          <HumanAiCollaboration />
        </div>
      </PlatformSectionBand>

      <section className="w-full py-14 md:py-16" aria-labelledby="hitl-next">
        <Container width="wide">
          <Stack gap={6} className="max-w-3xl">
            <div id="hitl-next">
              <SectionHeader
                eyebrow="Governance"
                heading="How responsibility is protected by design"
                description="Responsible AI describes the controls and stewardship around intelligence capabilities."
              />
            </div>
            <MarketingCtaLink
              label="Responsible AI"
              href="/technology/ai/responsible"
              variant="primary"
              size="lg"
            />
          </Stack>
        </Container>
      </section>
    </>
  );
}
