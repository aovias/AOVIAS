import { GovernanceWorkflow } from '@/components/brand';
import {
  FeatureGrid,
  Hero,
  MarketingCtaLink,
  PrincipleCard,
  SectionHeader,
} from '@/components/marketing';
import { Container, Stack, Text } from '@/components/primitives';
import { aiResponsibleContent } from '@/features/technology/ai/content';
import { PlatformSectionBand } from '@/features/technology/platform/section-band';

export function AiResponsiblePage() {
  const c = aiResponsibleContent;

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
          <GovernanceWorkflow title="Responsible AI — oversight and accountability" />
        }
      />

      <section className="bg-muted/40 border-border w-full border-b py-8" aria-label="Guiding statement">
        <Container width="wide">
          <Text as="p" size="body-lg" weight="medium" className="max-w-3xl">
            {c.guidingLine}
          </Text>
        </Container>
      </section>

      <PlatformSectionBand
        id="responsible-framework"
        eyebrow="Framework"
        title="Principles of responsible intelligence"
        description="We discuss governance philosophy — not certifications or compliance badges we have not claimed."
      >
        <FeatureGrid cols={2} stagger={false}>
          {c.topics.map((item) => (
            <PrincipleCard
              key={item.title}
              principle={item.title}
              description={item.description}
            />
          ))}
        </FeatureGrid>
      </PlatformSectionBand>

      <section className="bg-muted/30 w-full py-14 md:py-16" aria-labelledby="responsible-next">
        <Container width="wide">
          <Stack gap={6} className="max-w-3xl">
            <div id="responsible-next">
              <SectionHeader
                eyebrow="Direction"
                heading="Capability evolves; principle remains"
                description="The AI Roadmap outlines themes of maturity without speculative product promises."
              />
            </div>
            <Stack direction="horizontal" gap={3} wrap>
              <MarketingCtaLink
                label="AI roadmap"
                href="/technology/ai/roadmap"
                variant="primary"
              />
              <MarketingCtaLink
                label="AI principles"
                href="/technology/ai/principles"
                variant="outline"
              />
            </Stack>
          </Stack>
        </Container>
      </section>
    </>
  );
}
