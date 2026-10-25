import { EngineeringLifecycle } from '@/components/brand';
import {
  FeatureGrid,
  Hero,
  MarketingCtaLink,
  PrincipleCard,
  SectionHeader,
} from '@/components/marketing';
import { Container, Stack, Text } from '@/components/primitives';
import {
  ENGINEERING_GUIDING_LINE,
  engineeringDesignPrinciplesContent,
} from '@/features/technology/engineering/content';
import { PlatformSectionBand } from '@/features/technology/platform/section-band';

export function EngineeringDesignPrinciplesPage() {
  const c = engineeringDesignPrinciplesContent;

  return (
    <>
      <Hero
        eyebrow={c.hero.eyebrow}
        title={c.hero.title}
        description={c.hero.description}
        primaryCta={c.hero.primaryCta}
        secondaryCta={c.hero.secondaryCta}
        atmosphere="blueprint-grid"
        illustration={
          <EngineeringLifecycle title="Design principles across the lifecycle" />
        }
      />

      <section className="bg-muted/40 border-border w-full border-b py-8" aria-label="Guiding statement">
        <Container width="wide">
          <Text as="p" size="body-lg" weight="medium" className="max-w-3xl">
            {ENGINEERING_GUIDING_LINE}
          </Text>
        </Container>
      </section>

      <PlatformSectionBand
        id="design-principles"
        eyebrow="Principles"
        title="Why these matter"
        description="Enduring guides for product and platform work — not a temporary style guide."
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
                heading="Reliability follows from principle"
                description="Dependable behavior is designed — it is not an afterthought."
              />
            </div>
            <MarketingCtaLink
              label="Reliability"
              href="/technology/engineering/reliability"
              variant="primary"
              size="lg"
            />
          </Stack>
        </Container>
      </section>
    </>
  );
}
