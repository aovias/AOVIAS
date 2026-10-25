import {
  CtaSection,
  FeatureGrid,
  Hero,
  PrincipleCard,
  SectionHeader,
  ValueCard,
} from '@/components/marketing';
import { ProductComparisonTable } from '@/features/products/comparison/product-comparison-table';
import type { CommerceEditorialContent } from '@/features/commerce/editorial/types';
import { TechnologyStoryFlow } from '@/features/technology/editorial/technology-story-flow';
import { PlatformSectionBand } from '@/features/technology/platform/section-band';
import type { ReactNode } from 'react';

export type CommerceEditorialTemplateProps = {
  content: CommerceEditorialContent;
  /** Interactive plans / billing band injected after philosophy */
  commerceBand?: ReactNode;
  /** Optional FAQ or additional band before closing */
  afterJourney?: ReactNode;
};

/**
 * Enterprise Procurement editorial shell (Phase V9).
 * Hero → Philosophy → Plan summaries → Interactive commerce → Comparison → Journey → CTA
 */
export function CommerceEditorialTemplate({
  content,
  commerceBand,
  afterJourney,
}: CommerceEditorialTemplateProps) {
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
        id="philosophy"
        eyebrow={c.philosophy.eyebrow ?? 'Pricing philosophy'}
        title={c.philosophy.title}
        description={c.philosophy.description}
        tone="feature"
      >
        <FeatureGrid cols={2} stagger>
          {c.philosophy.principles.map((item) => (
            <PrincipleCard
              key={item.id}
              principle={item.title}
              description={item.description}
            />
          ))}
        </FeatureGrid>
      </PlatformSectionBand>

      <PlatformSectionBand
        id="tiers"
        eyebrow={c.plans.eyebrow ?? 'Engagement models'}
        title={c.plans.title}
        description={c.plans.description}
        muted
      >
        <FeatureGrid cols={3} stagger>
          {c.plans.summaries.map((plan) => (
            <article
              key={plan.id}
              className="border-border bg-card/60 flex h-full flex-col gap-4 rounded-xl border p-5"
            >
              <h3 className="text-foreground text-body font-semibold tracking-tight">
                {plan.name}
              </h3>
              <dl className="space-y-3 text-small">
                <div>
                  <dt className="text-muted-foreground text-[0.65rem] font-semibold tracking-wide uppercase">
                    Intended customer
                  </dt>
                  <dd className="text-foreground mt-1 leading-relaxed">
                    {plan.audience}
                  </dd>
                </div>
                <div>
                  <dt className="text-muted-foreground text-[0.65rem] font-semibold tracking-wide uppercase">
                    Deployment
                  </dt>
                  <dd className="text-foreground mt-1 leading-relaxed">
                    {plan.deployment}
                  </dd>
                </div>
                <div>
                  <dt className="text-muted-foreground text-[0.65rem] font-semibold tracking-wide uppercase">
                    Support
                  </dt>
                  <dd className="text-foreground mt-1 leading-relaxed">
                    {plan.support}
                  </dd>
                </div>
                <div>
                  <dt className="text-muted-foreground text-[0.65rem] font-semibold tracking-wide uppercase">
                    Governance
                  </dt>
                  <dd className="text-foreground mt-1 leading-relaxed">
                    {plan.governance}
                  </dd>
                </div>
                <div>
                  <dt className="text-muted-foreground text-[0.65rem] font-semibold tracking-wide uppercase">
                    Commercial engagement
                  </dt>
                  <dd className="text-foreground mt-1 leading-relaxed">
                    {plan.engagement}
                  </dd>
                </div>
              </dl>
            </article>
          ))}
        </FeatureGrid>
      </PlatformSectionBand>

      {commerceBand}

      <PlatformSectionBand
        id="enterprise-comparison"
        eyebrow={c.comparison.eyebrow ?? 'Enterprise comparison'}
        title={c.comparison.title}
        description={c.comparison.description}
        muted
      >
        <ProductComparisonTable
          columns={c.comparison.columns}
          rows={c.comparison.rows}
          caption="Enterprise procurement comparison"
        />
      </PlatformSectionBand>

      {c.illustrations ? (
        <PlatformSectionBand
          id="illustrations"
          eyebrow={c.illustrations.eyebrow ?? 'Commercial illustrations'}
          title={c.illustrations.title}
          description={c.illustrations.description}
          tone="feature"
        >
          <FeatureGrid cols={2} stagger>
            {c.illustrations.items.map((item) => (
              <article
                key={item.id}
                className="border-border flex h-full flex-col gap-4 overflow-hidden rounded-xl border"
              >
                <div className="bg-muted/30 min-h-[12rem]">{item.illustration}</div>
                <div className="space-y-2 px-5 pb-5">
                  <h3 className="text-foreground text-body font-semibold tracking-tight">
                    {item.title}
                  </h3>
                  <p className="text-muted-foreground text-small leading-relaxed">
                    {item.description}
                  </p>
                </div>
              </article>
            ))}
          </FeatureGrid>
        </PlatformSectionBand>
      ) : null}

      <PlatformSectionBand
        id="partnership"
        eyebrow={c.journey.eyebrow ?? 'Partnership journey'}
        title={c.journey.title}
        description={c.journey.description}
        tone="feature"
      >
        <div className="grid items-center gap-10 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.05fr)] xl:gap-14">
          <div className="space-y-4">
            <SectionHeader
              heading="A long-term technology partnership"
              description="Discovery through expansion — consultation-led, not pressure-led."
            />
            <FeatureGrid cols={2} stagger={false}>
              <ValueCard
                heading="Transparency"
                description="Clear next steps after every conversation."
              />
              <ValueCard
                heading="Evaluation"
                description="Technical diligence before commercial commitment."
              />
            </FeatureGrid>
          </div>
          <TechnologyStoryFlow
            title={c.journey.title}
            steps={c.journey.steps}
            caption="Commercial relationship"
          />
        </div>
      </PlatformSectionBand>

      {afterJourney}

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
      </div>
    </>
  );
}
