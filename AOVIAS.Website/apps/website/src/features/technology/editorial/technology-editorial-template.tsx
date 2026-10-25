import { WorkflowPanel } from '@/components/brand';
import {
  CtaSection,
  FeatureGrid,
  Hero,
  PrincipleCard,
  ValueCard,
} from '@/components/marketing';
import { TechnologyStoryFlow } from '@/features/technology/editorial/technology-story-flow';
import type { TechnologyEditorialContent } from '@/features/technology/editorial/types';
import { PlatformSectionBand } from '@/features/technology/platform/section-band';

export type TechnologyEditorialTemplateProps = {
  content: TechnologyEditorialContent;
};

/**
 * Shared Technology Editorial page — architecture-guide rhythm (Phase V5).
 * Hero → Principle → Architecture → Story → Practices → Workflow → Highlights → CTA.
 */
export function TechnologyEditorialTemplate({
  content,
}: TechnologyEditorialTemplateProps) {
  const c = content;

  return (
    <>
      <Hero
        atmosphere={c.atmosphere}
        eyebrow={c.hero.eyebrow}
        title={c.hero.title}
        description={c.hero.description}
        primaryCta={c.hero.primaryCta}
        {...(c.hero.secondaryCta ? { secondaryCta: c.hero.secondaryCta } : {})}
        illustration={c.hero.illustration}
      />

      <PlatformSectionBand
        id="principle"
        eyebrow={c.principle.eyebrow ?? 'Core principle'}
        title={c.principle.title}
        description={c.principle.description}
        tone="feature"
      />

      <PlatformSectionBand
        id="architecture"
        eyebrow={c.architecture.eyebrow ?? 'Architecture'}
        title={c.architecture.title}
        description={c.architecture.description}
        muted
      >
        <div className="mx-auto w-full max-w-3xl">
          {c.architecture.diagram}
          {c.architecture.caption ? (
            <p className="text-muted-foreground mt-4 text-center text-small">
              {c.architecture.caption}
            </p>
          ) : null}
        </div>
      </PlatformSectionBand>

      <PlatformSectionBand
        id="story"
        eyebrow={c.story.eyebrow ?? 'Technical story'}
        title={c.story.title}
        description={c.story.description}
        tone="feature"
      >
        <div className="mx-auto w-full max-w-xl">
          <TechnologyStoryFlow
            title={c.story.title}
            steps={c.story.steps}
          />
        </div>
      </PlatformSectionBand>

      <PlatformSectionBand
        id="practices"
        eyebrow={c.practices.eyebrow ?? 'Practice'}
        title={c.practices.title}
        description={c.practices.description}
        muted
      >
        <FeatureGrid cols={3} stagger>
          {c.practices.items.map((item) => (
            <PrincipleCard
              key={item.id}
              principle={item.title}
              description={item.description}
            />
          ))}
        </FeatureGrid>
      </PlatformSectionBand>

      <PlatformSectionBand
        id="workflow"
        eyebrow={c.workflow.eyebrow ?? 'Workflow'}
        title={c.workflow.title}
        description={c.workflow.description}
        tone="feature"
      >
        <div className="mx-auto w-full max-w-3xl">
          <WorkflowPanel
            title={c.workflow.title}
            steps={c.workflow.steps}
          />
        </div>
      </PlatformSectionBand>

      <PlatformSectionBand
        id="highlights"
        eyebrow={c.highlights.eyebrow ?? 'Capabilities'}
        title={c.highlights.title}
        description={c.highlights.description}
        muted
      >
        <FeatureGrid cols={3} stagger>
          {c.highlights.items.map((item) => (
            <ValueCard
              key={item.id}
              heading={item.title}
              description={item.description}
            />
          ))}
        </FeatureGrid>
      </PlatformSectionBand>

      <div id="cta">
        <CtaSection
          title={c.closing.title}
          description={c.closing.description}
          primaryCta={c.closing.primaryCta}
          background="dark"
        />
      </div>
    </>
  );
}
