import type { ReactNode } from 'react';

import {
  ArchitectureDiagram,
  PlatformIntelligence,
  PlatformOrchestration,
  SecurePlatform,
  SystemEvolution,
  TechnologyEvolution,
  type EnterpriseAtmosphereVariant,
} from '@/components/brand';
import { MarketingReveal } from '@/components/marketing/reveal';
import { FeatureCard, FeatureGrid, Hero, MarketingCtaLink, SectionHeader, SectionSurface, type SectionSurfaceTone } from '@/components/marketing';
import { Heading, Paragraph, Stack } from '@/components/primitives';
import { technologyLandingContent } from '@/features/technology/content/landing';
import {
  getTechnologyLandingCards,
  getTechnologyTopicHref,
  technologyGroups,
} from '@/features/technology/domain';
import { getNavIcon } from '@/lib/navigation/icons';

function ContentBand({
  id,
  eyebrow,
  title,
  description,
  tone = 'canvas',
  atmosphere,
  visual,
}: {
  id: string;
  eyebrow: string;
  title: string;
  description: string;
  tone?: SectionSurfaceTone;
  atmosphere?: EnterpriseAtmosphereVariant;
  visual?: ReactNode;
}) {
  return (
    <SectionSurface
      id={id}
      tone={tone}
      {...(atmosphere ? { atmosphere } : {})}
      bordered
      spacing="lg"
      aria-label={title}
    >
      <div
        className={
          visual
            ? 'grid items-center gap-10 lg:grid-cols-2 xl:gap-16 2xl:gap-20'
            : undefined
        }
      >
        <Stack gap={3} className="max-w-3xl">
          <p className="text-domain-accent text-eyebrow font-medium tracking-wide uppercase">
            {eyebrow}
          </p>
          <Heading level={2} size="section-title" id={`${id}-heading`}>
            {title}
          </Heading>
          <Paragraph tone="muted">{description}</Paragraph>
        </Stack>
        {visual ? (
          <MarketingReveal>
            <div className="relative xl:min-h-[16rem] 2xl:min-h-[18rem]">
              {visual}
            </div>
          </MarketingReveal>
        ) : null}
      </div>
    </SectionSurface>
  );
}

/**
 * Technology landing — enterprise introduction with visual storytelling bands.
 */
export function TechnologyLandingPage() {
  const content = technologyLandingContent;
  const cards = getTechnologyLandingCards().filter(
    (topic) => topic.id !== 'overview',
  );

  return (
    <>
      <Hero
        eyebrow={content.hero.eyebrow}
        title={content.hero.title}
        description={content.hero.description}
        primaryCta={content.hero.primaryCta}
        secondaryCta={content.hero.secondaryCta}
        atmosphere="technology-atmosphere"
        illustration={
          <PlatformOrchestration title="AOVIAS Technology — platform foundation" />
        }
      />

      <ContentBand
        id="engineering-philosophy"
        eyebrow={content.philosophy.eyebrow}
        title={content.philosophy.title}
        description={content.philosophy.description}
        tone="feature"
        atmosphere="blueprint-grid"
        visual={<SystemEvolution title="Stable core · modular growth" />}
      />
      <ContentBand
        id="platform"
        eyebrow={content.platform.eyebrow}
        title={content.platform.title}
        description={content.platform.description}
        tone="feature"
        atmosphere="platform-grid"
        visual={<ArchitectureDiagram title="Platform architecture layers" />}
      />
      <ContentBand
        id="artificial-intelligence"
        eyebrow={content.intelligence.eyebrow}
        title={content.intelligence.title}
        description={content.intelligence.description}
        tone="elevated"
        atmosphere="ai-neural-network"
        visual={<PlatformIntelligence title="Intelligence as an assistive layer" />}
      />
      <ContentBand
        id="security"
        eyebrow={content.security.eyebrow}
        title={content.security.title}
        description={content.security.description}
        tone="muted"
        atmosphere="security-geometry"
        visual={<SecurePlatform title="Secure by design" />}
      />
      <ContentBand
        id="platform-services"
        eyebrow={content.services.eyebrow}
        title={content.services.title}
        description={content.services.description}
        tone="canvas"
        atmosphere="infrastructure-mesh"
      />
      <ContentBand
        id="future-vision"
        eyebrow={content.future.eyebrow}
        title={content.future.title}
        description={content.future.description}
        tone="contrast"
        atmosphere="technology-atmosphere"
        visual={<TechnologyEvolution title="Capability themes — measured progress" />}
      />

      <SectionSurface
        tone="feature"
        spacing="lg"
        aria-label="Topics in this section"
      >
        <Stack gap={8}>
          <SectionHeader
            eyebrow="Explore Technology"
            heading="Topics in this section"
            description="Each topic is addressable in navigation and routing — written for technical and institutional readers."
            align="start"
          />
          <FeatureGrid cols={3} stagger>
            {cards.map((topic) => {
              const Icon = getNavIcon(topic.icon);
              return (
                <FeatureCard
                  key={topic.id}
                  title={topic.label}
                  description={topic.description}
                  href={getTechnologyTopicHref(topic)}
                  linkLabel={topic.status === 'live' ? 'Open' : 'View topic'}
                  {...(Icon
                    ? { icon: <Icon className="size-5" aria-hidden="true" /> }
                    : {})}
                />
              );
            })}
          </FeatureGrid>
          <p className="text-muted-foreground text-small">
            Organized as{' '}
            {technologyGroups.map((group) => group.label).join(', ')}.
          </p>
        </Stack>
      </SectionSurface>

      <SectionSurface tone="canvas" bordered spacing="lg" aria-label="Next step">
        <Stack gap={6} className="max-w-3xl">
          <SectionHeader
            eyebrow="Next step"
            heading={content.cta.title}
            description={content.cta.description}
            align="start"
          />
          <Stack direction="horizontal" gap={3} wrap>
            <MarketingCtaLink
              {...content.cta.primaryCta}
              variant="primary"
              size="lg"
            />
            <MarketingCtaLink
              {...content.cta.secondaryCta}
              variant="outline"
              size="lg"
            />
          </Stack>
        </Stack>
      </SectionSurface>
    </>
  );
}
