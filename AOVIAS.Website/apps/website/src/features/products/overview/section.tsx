import { RegisteredIllustration } from '@/components/brand';
import {
  CommercialProductCard,
  CtaSection,
  FeatureGrid,
  Hero,
  SectionHeader,
  SectionSurface,
  ValueCard,
} from '@/components/marketing';
import { Paragraph, Stack } from '@/components/primitives';
import { ProductComparisonTable } from '@/features/products/comparison/product-comparison-table';
import {
  comparisonColumns,
  comparisonRows,
  overviewContent,
  portfolioProducts,
} from '@/features/products/overview/content';

const portfolioMedia = {
  command: (
    <RegisteredIllustration
      id="ill-command-terminal"
      componentKey="CommandMissionControl"
      title="AOVIAS Command — mission control"
      illustrationProps={{ className: 'border-0 shadow-none' }}
    />
  ),
  studio: (
    <RegisteredIllustration
      id="ill-studio-workstation"
      componentKey="StudioAuthoringWorkspace"
      title="AOVIAS Studio — authoring"
      illustrationProps={{ className: 'border-0 shadow-none' }}
    />
  ),
  platform: (
    <RegisteredIllustration
      id="ill-ai-reasoning"
      componentKey="PlatformServicesConsole"
      title="AOVIAS Platform — services"
      illustrationProps={{ className: 'border-0 shadow-none' }}
    />
  ),
} as const;

/**
 * Products Overview — commercial portfolio landing (Phase V4).
 * Hero illustration SSRs; portfolio art loads client-side for mobile LCP.
 */
export function ProductsOverviewSection() {
  return (
    <>
      <Hero
        eyebrow={overviewContent.eyebrow}
        title={overviewContent.title}
        description={overviewContent.description}
        primaryCta={overviewContent.primaryCta}
        secondaryCta={overviewContent.secondaryCta}
        atmosphere="enterprise"
        atmosphereQuietRight
        illustration={
          <RegisteredIllustration
            id="ill-command-terminal"
            componentKey="CommandMissionControl"
            className="xl:min-h-[20rem] 2xl:min-h-[24rem]"
          />
        }
      />

      <SectionSurface
        id="overview"
        tone="feature"
        spacing="lg"
        aria-label="Portfolio overview"
      >
        <Stack gap={10}>
          <Stack gap={4} className="max-w-3xl">
            <SectionHeader
              eyebrow={overviewContent.portfolio.eyebrow}
              heading={overviewContent.portfolio.heading}
              description={overviewContent.portfolio.description}
            />
          </Stack>
          <RegisteredIllustration
            id="ill-platform-core"
            componentKey="ProductEcosystemFlow"
            title="AOVIAS commercial ecosystem"
            className="mx-auto w-full max-w-3xl"
            illustrationProps={{
              caption: 'Command → Platform → Studio → Customer',
            }}
          />
        </Stack>
      </SectionSurface>

      <SectionSurface
        id="portfolio"
        tone="muted"
        bordered
        spacing="lg"
        aria-label="Product portfolio"
      >
        <Stack gap={10}>
          <SectionHeader
            eyebrow="Products"
            heading="Command. Studio. Platform."
            description="Each product has a distinct commercial job. Together they form a mature institutional stack."
          />
          <div className="grid gap-6 lg:grid-cols-3 lg:gap-7">
            {portfolioProducts.map((product) => (
              <CommercialProductCard
                key={product.id}
                name={product.name}
                purpose={product.purpose}
                audience={product.audience}
                primaryValue={product.primaryValue}
                href={product.href}
                linkLabel={product.linkLabel}
                domainAttr={
                  product.id === 'platform'
                    ? {
                        'data-technology': 'platform',
                        'data-product': 'platform',
                      }
                    : { 'data-product': product.id }
                }
                media={portfolioMedia[product.id]}
              />
            ))}
          </div>
        </Stack>
      </SectionSurface>

      <SectionSurface
        id="together"
        tone="feature"
        spacing="lg"
        aria-label="How products work together"
      >
        <Stack gap={6} className="mx-auto max-w-3xl text-center">
          <SectionHeader
            eyebrow={overviewContent.together.eyebrow}
            heading={overviewContent.together.heading}
            description={overviewContent.together.description}
            align="center"
          />
          <Paragraph size="body" tone="muted" className="text-balance">
            Command runs live operations. Platform supplies identity, security,
            deployment, AI, and governance. Studio authors and publishes
            institutional work. Customers receive coherent outcomes — not a
            collage of disconnected tools.
          </Paragraph>
        </Stack>
      </SectionSurface>

      <SectionSurface
        id="foundation"
        tone="muted"
        bordered
        spacing="lg"
        aria-label="Shared foundation"
      >
        <Stack gap={10}>
          <SectionHeader
            eyebrow={overviewContent.foundation.eyebrow}
            heading={overviewContent.foundation.heading}
            description={overviewContent.foundation.description}
          />
          <FeatureGrid cols={3} stagger={false}>
            {overviewContent.foundation.items.map((item) => (
              <ValueCard
                key={item.id}
                heading={item.heading}
                description={item.description}
              />
            ))}
          </FeatureGrid>
        </Stack>
      </SectionSurface>

      <SectionSurface
        id="compare"
        tone="feature"
        spacing="lg"
        aria-label="Product comparison"
      >
        <Stack gap={10}>
          <SectionHeader
            eyebrow={overviewContent.comparison.eyebrow}
            heading={overviewContent.comparison.heading}
            description={overviewContent.comparison.description}
          />
          <ProductComparisonTable
            columns={comparisonColumns}
            rows={comparisonRows}
          />
        </Stack>
      </SectionSurface>

      <div id="cta">
        <CtaSection
          title={overviewContent.closing.title}
          description={overviewContent.closing.description}
          primaryCta={overviewContent.closing.primaryCta}
          secondaryCta={overviewContent.closing.secondaryCta}
          background="dark"
        />
      </div>
    </>
  );
}
