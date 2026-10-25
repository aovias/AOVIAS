import {
  DomainScene,
  type DomainSceneVariant,
} from '@/components/brand';
import {
  CtaSection,
  FeatureCard,
  FeatureGrid,
  Hero,
  PrincipleCard,
  SectionHeader,
  ValueCard,
} from '@/components/marketing';
import { ProductNavigation } from '@/components/product';
import { Container, Paragraph, Stack } from '@/components/primitives';
import { ResourceGrid } from '@/components/resources';
import {
  ComplianceGrid,
  SecuritySection,
  TrustBadge,
} from '@/components/trust';
import { getProductById } from '@/features/products/domain';
import type { IndustryDetailContent } from '@/features/industries/detail/types';

export type IndustryTemplateProps = {
  content: IndustryDetailContent;
};

const domainBySlug: Record<string, DomainSceneVariant> = {
  healthcare: 'healthcare',
  government: 'government',
  education: 'education',
  enterprise: 'enterprise',
};

/**
 * Industry Experience Framework — reusable composition for every industry page.
 * Routes supply configuration only; no industry-specific UI infrastructure.
 */
export function IndustryTemplate({ content }: IndustryTemplateProps) {
  const isPlanned = content.status === 'planned';
  const products = content.products.productIds
    .map((id) => getProductById(id))
    .filter((product) => Boolean(product));
  const domainVariant = domainBySlug[content.slug] ?? 'industry';

  return (
    <>
      <Hero
        {...(content.hero.eyebrow ? { eyebrow: content.hero.eyebrow } : {})}
        title={content.hero.headline}
        description={content.hero.challengesSummary}
        primaryCta={content.hero.primaryCta}
        {...(content.hero.secondaryCta
          ? { secondaryCta: content.hero.secondaryCta }
          : {})}
        atmosphere="corporate-gradient"
        illustration={
          <DomainScene
            title={`${content.name} illustration`}
            caption={content.hero.illustrationLabel}
            variant={domainVariant}
          />
        }
      />

      {isPlanned ? (
        <Container width="wide" className="pt-6">
          <Paragraph size="small" tone="muted">
            Vision industry page — communicates long-term market direction, not a
            committed service catalog or timeline.
          </Paragraph>
        </Container>
      ) : null}

      <ProductNavigation
        items={content.navigation}
        stickyOffsetClassName="top-16"
      />

      <section
        id="overview"
        className="w-full py-12 lg:py-16"
        aria-label={content.overview.heading}
      >
        <Container width="wide">
          <Stack gap={4} className="max-w-3xl">
            <SectionHeader
              eyebrow={content.name}
              heading={content.overview.heading}
              description={content.overview.description}
            />
            <Paragraph size="body-lg" tone="muted">
              {content.overview.opportunities}
            </Paragraph>
          </Stack>
        </Container>
      </section>

      <section
        id="challenges"
        className="border-border bg-muted/20 w-full border-y py-12 lg:py-16"
        aria-label={content.challenges.heading}
      >
        <Container width="wide">
          <Stack gap={10}>
            <SectionHeader
              heading={content.challenges.heading}
              {...(content.challenges.description
                ? { description: content.challenges.description }
                : {})}
            />
            <FeatureGrid cols={2} stagger>
              {content.challenges.items.map((item) => (
                <PrincipleCard
                  key={item.id}
                  principle={item.title}
                  description={item.description}
                />
              ))}
            </FeatureGrid>
          </Stack>
        </Container>
      </section>

      <section
        id="solutions"
        className="w-full py-12 lg:py-16"
        aria-label={content.solutions.heading}
      >
        <Container width="wide">
          <Stack gap={10}>
            <SectionHeader
              heading={content.solutions.heading}
              description={content.solutions.description}
            />
            <FeatureGrid cols={3} stagger>
              {content.solutions.items.map((item) => (
                <ValueCard
                  key={item.id}
                  heading={item.heading}
                  description={item.description}
                />
              ))}
            </FeatureGrid>
          </Stack>
        </Container>
      </section>

      <section
        id="products"
        className="border-border bg-muted/20 w-full border-y py-12 lg:py-16"
        aria-label={content.products.heading}
      >
        <Container width="wide">
          <Stack gap={10}>
            <SectionHeader
              heading={content.products.heading}
              description={content.products.description}
            />
            <FeatureGrid cols={2} stagger>
              {products.map((product) =>
                product ? (
                  <FeatureCard
                    key={product.id}
                    title={product.name}
                    description={product.summary}
                    href={product.href}
                    linkLabel={`Explore ${product.name}`}
                  />
                ) : null,
              )}
            </FeatureGrid>
          </Stack>
        </Container>
      </section>

      <section
        id="benefits"
        className="w-full py-12 lg:py-16"
        aria-label={content.benefits.heading}
      >
        <Container width="wide">
          <Stack gap={10}>
            <SectionHeader
              heading={content.benefits.heading}
              {...(content.benefits.description
                ? { description: content.benefits.description }
                : {})}
            />
            <FeatureGrid cols={3} stagger>
              {content.benefits.items.map((item) => (
                <ValueCard
                  key={item.id}
                  heading={item.heading}
                  description={item.description}
                />
              ))}
            </FeatureGrid>
          </Stack>
        </Container>
      </section>

      <section
        id="trust"
        className="border-border bg-muted/20 w-full border-y"
        aria-label={content.trust.heading}
      >
        <Container width="wide" className="pt-12 lg:pt-16">
          {content.trust.signals && content.trust.signals.length > 0 ? (
            <Stack direction="horizontal" gap={3} wrap className="mb-6">
              {content.trust.signals.map((signal) => (
                <TrustBadge key={signal.id} signal={signal} size="sm" />
              ))}
            </Stack>
          ) : null}
        </Container>
        <SecuritySection
          heading={content.trust.heading}
          {...(content.trust.description
            ? { description: content.trust.description }
            : {})}
          {...(content.trust.highlights
            ? { highlights: content.trust.highlights }
            : {})}
          {...(content.trust.controls ? { controls: content.trust.controls } : {})}
          {...(content.trust.cta ? { cta: content.trust.cta } : {})}
          className="pt-0"
        />
        {content.trust.frameworks && content.trust.frameworks.length > 0 ? (
          <Container width="wide" className="pb-12 lg:pb-16">
            <ComplianceGrid items={content.trust.frameworks} cols={3} />
          </Container>
        ) : null}
      </section>

      <section
        id="resources"
        className="w-full py-12 lg:py-16"
        aria-label={content.resources.heading}
      >
        <Container width="wide">
          <Stack gap={8}>
            <SectionHeader
              heading={content.resources.heading}
              {...(content.resources.description
                ? { description: content.resources.description }
                : {})}
            />
            <ResourceGrid items={content.resources.items} cols={3} />
          </Stack>
        </Container>
      </section>

      <div id="cta">
        <CtaSection
          title={content.primaryCtaBand.title}
          description={content.primaryCtaBand.description}
          primaryCta={content.primaryCtaBand.primaryCta}
          {...(content.primaryCtaBand.secondaryCta
            ? { secondaryCta: content.primaryCtaBand.secondaryCta }
            : {})}
          background="dark"
        />
      </div>
    </>
  );
}
