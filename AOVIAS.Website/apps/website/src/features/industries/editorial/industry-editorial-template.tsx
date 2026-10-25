import { DomainScene, WorkflowPanel } from '@/components/brand';
import {
  CtaSection,
  FeatureCard,
  FeatureGrid,
  Hero,
  MarketingCtaLink,
  PrincipleCard,
  ValueCard,
} from '@/components/marketing';
import { Stack, Text } from '@/components/primitives';
import { Faq } from '@/components/trust/faq';
import { BrochureBand } from '@/features/industries/editorial/brochure-band';
import { ComparisonBand } from '@/features/industries/editorial/comparison-band';
import { DemoShowcase } from '@/features/industries/editorial/demo-showcase';
import { DemoVideoGrid } from '@/features/industries/editorial/demo-video-grid';
import { IntegrationOrbit } from '@/features/industries/editorial/integration-orbit';
import { InstitutionalOperatingCycle } from '@/features/industries/editorial/institutional-operating-cycle';
import { InstitutionalTransformationBand } from '@/features/industries/editorial/institutional-transformation-band';
import { MidCtaBand } from '@/features/industries/editorial/mid-cta-band';
import { ProductExperienceShowcase } from '@/features/industries/editorial/product-experience-showcase';
import type { IndustryEditorialContent } from '@/features/industries/editorial/types';
import { TechnologyStoryFlow } from '@/features/technology/editorial/technology-story-flow';
import { PlatformSectionBand } from '@/features/technology/platform/section-band';

export type IndustryEditorialTemplateProps = {
  content: IndustryEditorialContent;
};

function midCta(
  content: IndustryEditorialContent,
  id: string,
) {
  return content.midCtas?.find((item) => item.id === id);
}

/**
 * Industry Editorial page — institutional narrative rhythm (Phase V6).
 * Optional conversion bands render only when content provides them.
 */
export function IndustryEditorialTemplate({
  content,
}: IndustryEditorialTemplateProps) {
  const c = content;
  const environmentVariant = c.environmentVariant ?? c.domainVariant;
  const afterDemo = midCta(c, 'after-demo');
  const afterOutcomes = midCta(c, 'after-outcomes');
  const beforeFaq = midCta(c, 'before-faq');

  return (
    <div data-industry={c.slug}>
      <Hero
        atmosphere={c.atmosphere}
        eyebrow={c.hero.eyebrow}
        title={c.hero.title}
        description={c.hero.description}
        primaryCta={c.hero.primaryCta}
        {...(c.hero.secondaryCta ? { secondaryCta: c.hero.secondaryCta } : {})}
        {...(c.hero.tertiaryCta ? { tertiaryCta: c.hero.tertiaryCta } : {})}
        {...(c.hero.highlights ? { highlights: c.hero.highlights } : {})}
        {...(c.hero.actionLinks ? { actionLinks: c.hero.actionLinks } : {})}
        ctaWithArrow={Boolean(c.demo || c.productExperience)}
        illustration={
          c.heroIllustration ?? (
            <DomainScene
              title={`${c.name} — institutional environment`}
              caption={c.hero.illustrationCaption}
              variant={c.domainVariant}
              className="xl:min-h-[20rem] 2xl:min-h-[24rem]"
            />
          )
        }
      />

      {c.demo ? (
        <PlatformSectionBand
          id="demo"
          eyebrow={c.demo.eyebrow ?? 'Product demonstration'}
          title={c.demo.title}
          description={c.demo.description}
          tone="feature"
          className="pt-[7.5rem] pb-section-y-lg"
        >
          {c.demo.videos && c.demo.videos.length > 0 ? (
            <DemoVideoGrid videos={c.demo.videos} />
          ) : (
            <DemoShowcase
              caption="A concise walkthrough of governance, evidence, and accountability in AOVIAS Command."
              {...(c.demo.runtimeLabel
                ? { runtimeLabel: c.demo.runtimeLabel }
                : {})}
              {...(c.demo.videoSrc ? { videoSrc: c.demo.videoSrc } : {})}
              {...(c.demo.posterSrc ? { posterSrc: c.demo.posterSrc } : {})}
            />
          )}
        </PlatformSectionBand>
      ) : null}

      {afterDemo ? <MidCtaBand cta={afterDemo} /> : null}

      <PlatformSectionBand
        id="reality"
        eyebrow={c.reality.eyebrow ?? 'Industry reality'}
        title={c.reality.title}
        description={c.reality.description}
        tone="feature"
      >
        <ul className="flex flex-wrap gap-2 sm:gap-3">
          {c.reality.pillars.map((pillar) => (
            <li
              key={pillar}
              className="border-border bg-card/70 text-foreground rounded-md border px-3 py-1.5 text-small font-medium"
            >
              {pillar}
            </li>
          ))}
        </ul>
      </PlatformSectionBand>

      <PlatformSectionBand
        id="challenges"
        eyebrow={c.challenges.eyebrow ?? 'Operational challenges'}
        title={c.challenges.title}
        description={c.challenges.description}
        muted
      >
        <FeatureGrid cols={3} stagger>
          {c.challenges.items.map((item) => (
            <PrincipleCard
              key={item.id}
              principle={item.title}
              description={item.description}
            />
          ))}
        </FeatureGrid>
      </PlatformSectionBand>

      {c.productExperience ? (
        <PlatformSectionBand
          id="product-experience"
          eyebrow={c.productExperience.eyebrow ?? 'Product experience'}
          title={c.productExperience.title}
          description={c.productExperience.description}
          tone="feature"
        >
          <ProductExperienceShowcase items={c.productExperience.items} />
        </PlatformSectionBand>
      ) : null}

      {c.outcomes ? (
        <PlatformSectionBand
          id="outcomes"
          eyebrow={c.outcomes.eyebrow ?? 'Institutional outcomes'}
          title={c.outcomes.title}
          description={c.outcomes.description}
          muted
        >
          <FeatureGrid cols={3} stagger={false}>
            {c.outcomes.items.map((item) => (
              <PrincipleCard
                key={item.id}
                principle={item.title}
                description={item.description}
              />
            ))}
          </FeatureGrid>
        </PlatformSectionBand>
      ) : null}

      {afterOutcomes ? <MidCtaBand cta={afterOutcomes} /> : null}

      {c.comparison ? (
        <PlatformSectionBand
          id="comparison"
          eyebrow={c.comparison.eyebrow ?? 'Why change'}
          title={c.comparison.title}
          description={c.comparison.description}
          tone="feature"
        >
          <ComparisonBand comparison={c.comparison} />
        </PlatformSectionBand>
      ) : null}

      <PlatformSectionBand
        id="workflow"
        eyebrow={c.workflow.eyebrow ?? 'How AOVIAS helps'}
        title={c.workflow.title}
        description={c.workflow.description}
        muted
      >
        <div className="mx-auto w-full max-w-3xl">
          <WorkflowPanel title={c.workflow.title} steps={c.workflow.steps} />
        </div>
      </PlatformSectionBand>

      <PlatformSectionBand
        id="portfolio"
        eyebrow={c.portfolio.eyebrow ?? 'Solution portfolio'}
        title={c.portfolio.title}
        description={c.portfolio.description}
        tone="feature"
      >
        <FeatureGrid cols={3} stagger>
          {c.portfolio.items.map((item) => (
            <FeatureCard
              key={item.id}
              title={item.name}
              description={item.role}
              href={item.href}
              linkLabel={`Explore ${item.name}`}
            />
          ))}
        </FeatureGrid>
      </PlatformSectionBand>

      {c.integrations ? (
        <PlatformSectionBand
          id="integrations"
          eyebrow={c.integrations.eyebrow ?? 'Integrations'}
          title={c.integrations.title}
          description={c.integrations.description}
          muted
        >
          <IntegrationOrbit integrations={c.integrations} />
        </PlatformSectionBand>
      ) : null}

      {c.trustPillars ? (
        <PlatformSectionBand
          id="trust"
          eyebrow={c.trustPillars.eyebrow ?? 'Institutional trust'}
          title={c.trustPillars.title}
          description={c.trustPillars.description}
          tone="feature"
        >
          <FeatureGrid cols={3} stagger={false}>
            {c.trustPillars.items.map((item) => (
              <ValueCard
                key={item.id}
                heading={item.title}
                description={item.description}
              />
            ))}
          </FeatureGrid>
        </PlatformSectionBand>
      ) : null}

      <PlatformSectionBand
        id="journey"
        eyebrow={c.journey.eyebrow ?? 'Enterprise story'}
        title={c.journey.title}
        description={c.journey.description}
        muted
      >
        {c.journey.model === 'operating-cycle' && c.journey.cycleSteps ? (
          <div className="flex flex-col gap-8 lg:gap-10">
            {c.journey.lead ? (
              <Text
                as="p"
                size="body"
                tone="muted"
                className="mx-auto max-w-3xl text-center leading-relaxed"
              >
                {c.journey.lead}
              </Text>
            ) : null}
            <InstitutionalOperatingCycle
              title={c.journey.title}
              steps={c.journey.cycleSteps}
            />
          </div>
        ) : (
          <div className="grid items-center gap-10 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.1fr)] xl:gap-14">
            <Stack gap={3}>
              <Text as="p" size="body" tone="muted" className="leading-relaxed">
                {c.journey.lead ??
                  'One institutional journey — from the operating environment through governed work to leadership oversight and continuous improvement. Shared platform. Sector-specific responsibility.'}
              </Text>
            </Stack>
            <TechnologyStoryFlow
              title={c.journey.title}
              steps={c.journey.steps}
              caption="Institutional journey"
            />
          </div>
        )}
      </PlatformSectionBand>

      <PlatformSectionBand
        id="narrative"
        eyebrow={c.narratives.eyebrow ?? 'Scenario narratives'}
        title={c.narratives.title}
        description={c.narratives.description}
        tone="feature"
      >
        {c.narratives.model === 'transformation' &&
        c.narratives.flowSteps &&
        c.narratives.outcomeStrip ? (
          <InstitutionalTransformationBand
            outcomes={c.narratives.items}
            flowSteps={c.narratives.flowSteps}
            strip={c.narratives.outcomeStrip}
          />
        ) : (
          <div className="grid gap-6 lg:grid-cols-[minmax(0,1.05fr)_minmax(0,0.95fr)] xl:gap-10">
            <Stack gap={4}>
              {c.narratives.items.map((item) => (
                <ValueCard
                  key={item.id}
                  heading={item.title}
                  description={item.description}
                />
              ))}
            </Stack>
            <DomainScene
              title={`${c.name} — operating environment`}
              caption="Editorial institutional context"
              variant={environmentVariant}
              className="max-lg:hidden"
            />
          </div>
        )}
      </PlatformSectionBand>

      {c.brochure ? (
        <PlatformSectionBand
          id="brochure"
          eyebrow={c.brochure.eyebrow ?? 'Executive briefing'}
          title={c.brochure.title}
          description={c.brochure.description}
          muted
        >
          <BrochureBand brochure={c.brochure} />
        </PlatformSectionBand>
      ) : null}

      {beforeFaq ? <MidCtaBand cta={beforeFaq} /> : null}

      {c.faq ? (
        <PlatformSectionBand
          id="faq"
          eyebrow={c.faq.eyebrow ?? 'FAQ'}
          title={c.faq.title}
          {...(c.faq.description ? { description: c.faq.description } : {})}
          tone="feature"
        >
          <Faq items={c.faq.items} searchable={false} />
        </PlatformSectionBand>
      ) : null}

      <div id="cta">
        <CtaSection
          title={c.closing.title}
          description={c.closing.description}
          primaryCta={c.closing.primaryCta}
          {...(c.closing.secondaryCta
            ? { secondaryCta: c.closing.secondaryCta }
            : {})}
          background="dark"
        />
        {c.closing.tertiaryCta ? (
          <div className="border-border bg-background flex justify-center border-t py-6">
            <MarketingCtaLink
              label={c.closing.tertiaryCta.label}
              href={c.closing.tertiaryCta.href}
              variant="ghost"
            />
          </div>
        ) : null}
      </div>
    </div>
  );
}
