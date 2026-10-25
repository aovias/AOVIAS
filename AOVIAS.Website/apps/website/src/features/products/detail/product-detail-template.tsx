import { DomainScene } from '@/components/brand';
import { CtaSection, SectionHeader } from '@/components/marketing';
import {
  ArchitectureDiagramBlock,
  CapabilityGrid,
  FeatureShowcase,
  IntegrationGrid,
  ProductHero,
  ProductNavigation,
  ProductOverview,
  TechnicalSpecificationBlock,
} from '@/components/product';
import { Container, Paragraph, Stack } from '@/components/primitives';
import { ResourceGrid } from '@/components/resources';
import {
  ComplianceGrid,
  SecuritySection,
  TrustBadge,
} from '@/components/trust';
import type { ProductDetailContent } from '@/features/products/detail/types';

export type ProductDetailTemplateProps = {
  content: ProductDetailContent;
};

/**
 * Product Detail Framework — reusable composition for every AOVIAS product page.
 * Available and future products inherit this template; routes only supply configuration.
 */
export function ProductDetailTemplate({ content }: ProductDetailTemplateProps) {
  const isPlanned = content.status === 'planned';

  return (
    <>
      <ProductHero
        name={content.name}
        description={content.tagline}
        atmosphere={content.atmosphere ?? 'workflow'}
        {...(content.badge ? { badge: content.badge } : {})}
        {...(content.primaryCta ? { primaryCta: content.primaryCta } : {})}
        {...(content.secondaryCta ? { secondaryCta: content.secondaryCta } : {})}
        {...(content.highlights ? { highlights: content.highlights } : {})}
        {...(content.heroMedia ? { media: content.heroMedia } : {})}
        {...(content.secondaryHeroMedia
          ? { secondaryMedia: content.secondaryHeroMedia }
          : {})}
        {...(isPlanned
          ? {
              footer: (
                <Paragraph size="small" tone="muted">
                  Vision product — communicates long-term direction, not availability
                  or release timing.
                </Paragraph>
              ),
            }
          : {})}
      />

      <ProductNavigation
        items={content.navigation}
        stickyOffsetClassName="top-16"
      />

      <div id="overview">
        <ProductOverview
          heading={content.overview.heading}
          description={
            content.overview.audience
              ? `${content.overview.description} ${content.overview.audience}`
              : content.overview.description
          }
          {...(content.overview.media ? { media: content.overview.media } : {})}
          {...(content.overview.metrics
            ? { metrics: content.overview.metrics }
            : {})}
        />
      </div>

      <section
        id="capabilities"
        className="border-border w-full border-t py-12 lg:py-16"
        aria-label={content.capabilities.heading}
      >
        <Container width="wide">
          <Stack gap={10}>
            <SectionHeader
              heading={content.capabilities.heading}
              {...(content.capabilities.description
                ? { description: content.capabilities.description }
                : {})}
            />
            <Stack gap={10}>
              {content.capabilities.showcases.map((item, index) => (
                <FeatureShowcase
                  key={item.id}
                  title={item.title}
                  description={item.description}
                  align={item.align ?? (index % 2 === 0 ? 'end' : 'start')}
                  {...(item.media ? { media: item.media } : {})}
                />
              ))}
            </Stack>
            {content.capabilities.summaryItems &&
            content.capabilities.summaryItems.length > 0 ? (
              <CapabilityGrid
                items={content.capabilities.summaryItems}
                cols={3}
                stagger
              />
            ) : null}
          </Stack>
        </Container>
      </section>

      <ArchitectureDiagramBlock
        id="architecture"
        title={content.architecture.title}
        diagram={content.architecture.diagram}
        {...(content.architecture.description
          ? { description: content.architecture.description }
          : {})}
        {...(content.architecture.caption
          ? { caption: content.architecture.caption }
          : {})}
      />

      <section
        id="integrations"
        className="border-border bg-muted/20 w-full border-y py-12 lg:py-16"
        aria-label={content.integrations.heading}
      >
        <Container width="wide">
          <Stack gap={8}>
            <SectionHeader
              heading={content.integrations.heading}
              {...(content.integrations.description
                ? { description: content.integrations.description }
                : {})}
            />
            <IntegrationGrid items={content.integrations.items} cols={3} />
          </Stack>
        </Container>
      </section>

      <section
        id="specifications"
        className="border-border w-full border-t py-12 lg:py-16"
        aria-label={content.specifications.heading}
      >
        <Container width="wide">
          <Stack gap={8}>
            <SectionHeader
              heading={content.specifications.heading}
              {...(content.specifications.description
                ? { description: content.specifications.description }
                : {})}
            />
            <TechnicalSpecificationBlock
              categories={content.specifications.categories}
              className="py-0"
            />
          </Stack>
        </Container>
      </section>

      <section
        id="trust"
        className="border-border bg-muted/20 w-full border-y"
        aria-label={content.trust.heading}
      >
        <Container width="wide" className="pt-12 lg:pt-16">
          <div className="mb-8 grid items-start gap-8 lg:grid-cols-[minmax(0,1fr)_minmax(14rem,0.55fr)] xl:gap-12">
            <Stack gap={6}>
              {content.trust.signals && content.trust.signals.length > 0 ? (
                <Stack direction="horizontal" gap={3} wrap>
                  {content.trust.signals.map((signal) => (
                    <TrustBadge key={signal.id} signal={signal} size="sm" />
                  ))}
                </Stack>
              ) : null}
            </Stack>
            <DomainScene
              title="Security and trust posture"
              caption="Controls as product fundamentals"
              variant="security"
              className="aspect-[4/3] max-lg:hidden"
            />
          </div>
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

      <div id="pricing">
        <CtaSection
          title={content.pricingEntry.title}
          description={content.pricingEntry.description}
          primaryCta={content.pricingEntry.primaryCta}
          {...(content.pricingEntry.secondaryCta
            ? { secondaryCta: content.pricingEntry.secondaryCta }
            : {})}
          background="muted"
        />
      </div>

      <section
        id="resources"
        className="border-border w-full border-t py-12 lg:py-16"
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

/** @deprecated Prefer ProductDetailTemplate */
export const ProductLanding = ProductDetailTemplate;
